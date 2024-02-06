using Amazon.IotData;
using Amazon.IotData.Model;
using Amazon.Runtime;

using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;

using XSense.Models.Init;
using XSense.Models.Sensoric;
using XSense.Utils;

namespace XSense;

internal class XSenseApiClient
{
    private readonly XSenseHttpClient _httpClient;
    private readonly InMemoryStorage _storage;
    private Credentials _credentials;

    public XSenseApiClient(XSenseHttpClient httpClient, InMemoryStorage? storage = null)
    {
        _httpClient = httpClient;
        _storage = storage ?? new InMemoryStorage();
    }

    public async Task<bool> LoginAsync(string userName, string password)
    {
        // 1: Check if we have a valid refresh token
        // 2: If not, authenticate with SRP
        // 3: If so, refresh the token
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);

        await LoginInternal(userName, password, clientInfo);

        var isValid = await _httpClient.TestLogin(clientInfo, _credentials);
        if (!isValid)
        {
            _credentials = null;
            if (string.IsNullOrWhiteSpace(password))
            {
                // No password = User expected to use refresh token
                // But that didn't work, so we need to authenticate with SRP
                // But we can't do that without a password
                return false;
            }

            _storage.Remove($"login_{userName}");
            await LoginInternal(userName, password, clientInfo);

            var isValid2 = await _httpClient.TestLogin(clientInfo, _credentials);
            if (!isValid2)
            {
                return false;
            }
        }

        return _credentials is not null;
    }

    private async Task LoginInternal(string userName, string password, ClientInfo clientInfo)
    {
        _credentials = await _storage.GetOrAddAsync<Credentials>($"login_{userName}", async old =>
        {
            if (old is not null && !old.ShouldRefresh)
            {
                return old;
            }

            if (old is null)
            {
                return await _httpClient.AuthenticateWithSrpAsync(clientInfo, userName, password).ConfigureAwait(false);
            }

            var refreshValue = await _httpClient.RefreshTokenAsync(clientInfo, old).ConfigureAwait(false);

            refreshValue.UserId = old.UserId;

            return refreshValue;
        });
    }

    private async ValueTask<Credentials> GetCredentialsAsync()
    {
        var creds = _credentials ?? throw new InvalidOperationException("Not logged in");

        return await _storage.GetOrAddAsync<Credentials>($"login_{creds.UserId}", async old =>
        {
            _ = old ?? throw new InvalidOperationException("Not logged in");
            if (!old.ShouldRefresh)
            {
                return old;
            }

            var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
            return await _httpClient.RefreshTokenAsync(clientInfo, old).ConfigureAwait(false);
        });
    }

    private async ValueTask<ClientInfo> GetClientInfoAsync()
    {
        return await _storage.GetOrAddAsync<ClientInfo>("clientInfo", async _ =>
        {
            var clientInfo = await _httpClient.QueryClientInfo().ConfigureAwait(false);
            return clientInfo;
        }).ConfigureAwait(false);
    }

    public async Task<GetHousesResponseData[]> GetHousesAsync()
    {
        var creds = await GetCredentialsAsync().ConfigureAwait(false);
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
        return await _httpClient.GetHouses(clientInfo, creds).ConfigureAwait(false);
    }

    // GetHouseDetails
    public async Task<GetHousesDetailResponseData> GetHouseDetailsAsync(string houseId)
    {
        var creds = await GetCredentialsAsync().ConfigureAwait(false);
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
        return await _httpClient.GetHouseDetails(clientInfo, creds, houseId).ConfigureAwait(false);
    }

    public async Task<GetSensoricDataResponseData> GetSensoricDataAsync(GetSensoricDataRequest request)
    {
        var creds = await GetCredentialsAsync().ConfigureAwait(false);
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
        return await _httpClient.GetSensoricData(clientInfo, creds, request).ConfigureAwait(false);
    }

    private Expirable<AmazonIotDataClient> _iotDataClient;

    public async Task<AmazonIotDataClient> CreateIotDataClientAsync()
    {
        if (_iotDataClient is not null && !_iotDataClient.IsExpired)
        {
            return _iotDataClient.Value;
        }

        var creds = await GetCredentialsAsync().ConfigureAwait(false);
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
        AwsIotCredentials iotCreds = await GetIotCredsCached(creds, clientInfo).ConfigureAwait(false);

        var credentials = new SessionAWSCredentials(
            iotCreds.AccessKeyId,
            iotCreds.SecretAccessKey,
            iotCreds.SessionToken
        );

        var config = new AmazonIotDataConfig
        {
            ServiceURL = "https://data.iot.eu-central-1.amazonaws.com",
            HttpClientFactory = new HttpClientFactoryWithSslDisabled()
        };

        var cli = new AmazonIotDataClient(credentials, config);
        _iotDataClient = new Expirable<AmazonIotDataClient>(cli, DateTime.Parse(iotCreds.Expiration) - TimeSpan.FromMinutes(5));
        return cli;
    }

    private async Task<AwsIotCredentials> GetIotCredsCached(Credentials creds, ClientInfo clientInfo)
    {
        var key = $"iotCreds_{creds.UserId}";
        return await _storage.GetOrAddAsync<AwsIotCredentials>(key, async old =>
        {
            if (old is not null && !old.IsExpired)
            {
                return old;
            }

            return await _httpClient.GetAwsIotCredentials(clientInfo, creds).ConfigureAwait(false);
        }).ConfigureAwait(false);
    }

    public async Task<T> GetThingsShadowAsync<T>(string thingShadow, string shadowName)
    {
        var iotClient = await CreateIotDataClientAsync().ConfigureAwait(false);
        var request = new GetThingShadowRequest
        {
            ThingName = thingShadow,
            ShadowName = shadowName,
        };

        await UpdateThingsShadow();

        var response = await iotClient.GetThingShadowAsync(request).ConfigureAwait(false);

        //iotClient.GetRetainedMessageAsync(new GetRetainedMessageRequest
        //{
        //    Topic = "topic"
        //});

        //using var reader = new StreamReader(response.Payload);
        //string shadowDocument = reader.ReadToEnd();

        // deserialize as T
        var stream = response.Payload;
        var json = await JsonSerializer.DeserializeAsync<T>(stream).ConfigureAwait(false);
        return json;
    }

    private async Task UpdateThingsShadow()
    {
        var request = new UpdateThingShadowRequest
        {
            ThingName = "SBS5013B96457",
            ShadowName = "2nd_apptempdata",
            Payload = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                state = new
                {
                    desired = new
                    {
                        deviceSN = new string[] { "00000001", "00000002", "00000003", "00000004" },
                        report = "1",
                        reportDst = "1",
                        shadow = "appTempData",
                        source = "1",
                        stationSN = "13B96457",
                        //time = "20240123214731", // 2024-01-23-21:47:31
                        time = $"{DateTime.UtcNow:yyyyMMddHHmmss}",
                        timeoutM = "5",
                        userId = "e2251ab2-46e8-497a-af56-44d4c5be95f1"
                    }
                }
            })))
        };

        var client = await CreateIotDataClientAsync().ConfigureAwait(false);

        try
        {
            var sw = Stopwatch.StartNew();
            var response = await client.UpdateThingShadowAsync(request);
            Console.WriteLine($"Shadow updated successfully. ({sw.Elapsed.TotalMilliseconds}ms)");
            //var reader = new StreamReader(response.Payload);
            //var shadowDocument = reader.ReadToEnd();
            //Console.WriteLine(shadowDocument);
            //Console.WriteLine();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating shadow: {ex.Message}");
        }
    }
}