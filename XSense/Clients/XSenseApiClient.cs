using Amazon.CognitoIdentityProvider.Model;
using Amazon.IotData;
using Amazon.IotData.Model;
using Amazon.Runtime;

using System.Diagnostics;
using System.Globalization;
using System.Text.Json;

using XSense.Clients;
using XSense.Database;
using XSense.Models.Aggregates;
using XSense.Models.Aws;
using XSense.Models.Init;
using XSense.Models.Sensoric;
using XSense.Models.Sensoric.Live;
using XSense.Utils;

namespace XSense;

public class XSenseApiClient
{
    private readonly XSenseHttpClient _httpClient;
    private readonly XDao _dao;

    private readonly XSenseAwsIotClient _iotClient;

    private Credentials _credentials;

    // Should be userid (GUID)
    public string Username => _credentials.Username;

    public XSenseApiClient(XSenseHttpClient httpClient, XDao dao)
    {
        _httpClient = httpClient;
        _dao = dao;
        //_storage = storage ?? new InMemoryStorage();
        _iotClient = new XSenseAwsIotClient(this, httpClient, dao);
    }

    public async Task<bool> LoginWithLastUserAsync()
    {
        var settings = await _dao.GetSettingsAsync();
        if (string.IsNullOrWhiteSpace(settings.LastUser))
        {
            return false;
        }

        return await LoginAsync(settings.LastUser, null);
    }

    public async Task<bool> LoginAsync(string userName, string? password)
    {
        // 1: Check if we have a valid refresh token
        // 2: If not, authenticate with SRP
        // 3: If so, refresh the token
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
        try
        {
            await LoginInternal(userName, password, clientInfo);
        }
        catch (Exception e)
        {
            if (e is UserNotFoundException || e is NotAuthorizedException)
            {
                return false;
            }
            throw;
        }

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

            _dao.DeleteCredentials(userName);
            await LoginInternal(userName, password, clientInfo);

            var isValid2 = await _httpClient.TestLogin(clientInfo, _credentials);
            if (!isValid2)
            {
                return false;
            }
        }

        var success = _credentials is not null;

        if (success)
        {
            var settings = await _dao.GetSettingsAsync();
            settings.LastUser = userName;

            await _dao.SaveChangesAsync();
        }
        return success;
    }

    private async Task LoginInternal(string userName, string password, ClientInfo clientInfo)
    {
        _credentials = await _dao.GetOrAddCredentialsAsync(userName, async old =>
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

    internal async ValueTask<Credentials> GetCredentialsAsync()
    {
        var creds = _credentials ?? throw new InvalidOperationException("Not logged in");

        return await _dao.GetOrAddCredentialsAsync(creds.UserId, async old =>
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

    internal async ValueTask<ClientInfo> GetClientInfoAsync()
    {
        return await _dao.GetOrAddClientInfoAsync(async _ =>
        {
            var clientInfo = await _httpClient.QueryClientInfo().ConfigureAwait(false);
            return clientInfo;
        }).ConfigureAwait(false);
    }

    public async Task<House[]> GetHousesAsync()
    {
        var creds = await GetCredentialsAsync().ConfigureAwait(false);
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
        return await _httpClient.GetHouses(clientInfo, creds).ConfigureAwait(false);
    }

    // GetHouseDetails
    public async Task<HouseDetail> GetHouseDetailsAsync(string houseId)
    {
        var creds = await GetCredentialsAsync().ConfigureAwait(false);
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
        return await _httpClient.GetHouseDetails(clientInfo, creds, houseId).ConfigureAwait(false);
    }

    public async Task<HouseDetailAggregate> GetHouseDetailsAsync(House house)
    {
        var details = await GetHouseDetailsAsync(house.HouseId);
        return new HouseDetailAggregate(house, details);
    }

    public async Task<LiveSensoricData> PollSensoricDataAsync(Station station)
    {
        await _iotClient.UpdateThingsShadow(station);

        var shadowData = await _iotClient.GetThingsShadowAsync<LiveSensoricData>(
            $"{station.ThingName}",
            "2nd_mainpage"
        );

        return shadowData;
    }

    public async Task<GetSensoricDataResponseData> GetSensoricHistoryPageAsync(GetSensoricDataRequest request)
    {
        var creds = await GetCredentialsAsync().ConfigureAwait(false);
        var clientInfo = await GetClientInfoAsync().ConfigureAwait(false);
        return await _httpClient.GetSensoricData(clientInfo, creds, request).ConfigureAwait(false);
    }

    public IAsyncEnumerable<LiveMetricsDataPoint> EnumerateSensoricHistoryAsync(HouseDetailAggregate details, Station station, Device device, bool smartStopEnabled)
    {
        var request = new GetSensoricDataRequest(details, station, device);
        return EnumerateSensoricHistoryAsync(request, smartStopEnabled);
    }

    public async IAsyncEnumerable<LiveMetricsDataPoint> EnumerateSensoricHistoryAsync(GetSensoricDataRequest request, bool smartStopEnabled)
    {
        var nextToken = "";
        var lastTime = "0";

        int smartStop = 0;

        do
        {
            bool hadData = false;
            var currentRequest = request with
            {
                LastTime = lastTime,
                NextToken = nextToken
            };

            var sensoricData = await GetSensoricHistoryPageAsync(currentRequest);

            foreach (var item in sensoricData.DataList)
            {
                // item.Key: 20240131 (yyyyMMdd)
                var date = DateTime.ParseExact(item.Key, "yyyyMMdd", CultureInfo.InvariantCulture);
                foreach (var value in item.Value)
                {
                    hadData = true;
                    // value: 235900,22.3,48.8 (HHmmss,temperature,humidity)
                    var parts = value.Split(',');
                    var time = TimeSpan.ParseExact(parts[0], "hhmmss", CultureInfo.InvariantCulture);
                    var temperature = double.Parse(parts[1], CultureInfo.InvariantCulture);
                    var humidity = double.Parse(parts[2], CultureInfo.InvariantCulture);

                    var dateTime = date.Add(time);
                    yield return new LiveMetricsDataPoint(dateTime, temperature, humidity);
                }
            }

            smartStop = hadData ? 0 : smartStop + 1;

            nextToken = sensoricData.NextToken;
            lastTime = sensoricData.LastTime;

            //var lastTimeParsed = DateTime.ParseExact(lastTime, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
            //var nextTokenParsed = DateTime.ParseExact(nextToken[..14], "yyyyMMddHHmmss", CultureInfo.InvariantCulture);
        } while (!string.IsNullOrWhiteSpace(nextToken) && ((smartStopEnabled && smartStop <= 3) || !smartStopEnabled));
    }
}

public record LiveMetricsDataPoint(DateTime Time, double Temperature, double Humidity);