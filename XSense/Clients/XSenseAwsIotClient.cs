using Amazon.IotData.Model;
using Amazon.IotData;
using Amazon.Runtime;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using XSenseExtractor.Models.Aws;

using XSenseExtractor.Models.Init;

using XSenseExtractor.Utils;
using XSenseExtractor.Database;

namespace XSenseExtractor.Clients;

internal class XSenseAwsIotClient
{
    private readonly XSenseApiClient _apiClient;
    private readonly XSenseHttpClient _httpClient;
    private readonly XDao _dao;
    //private readonly InMemoryStorage? _storage;

    public XSenseAwsIotClient(XSenseApiClient apiClient, XSenseHttpClient httpClient, XDao dao)
    {
        _apiClient = apiClient;
        _httpClient = httpClient;
        _dao = dao;
        //_storage = storage;
    }

    private Expirable<AmazonIotDataClient> _iotDataClient;

    public async Task<AmazonIotDataClient> CreateIotDataClientAsync()
    {
        if (_iotDataClient is not null && !_iotDataClient.IsExpired)
        {
            return _iotDataClient.Value;
        }

        var creds = await _apiClient.GetCredentialsAsync().ConfigureAwait(false);
        var clientInfo = await _apiClient.GetClientInfoAsync().ConfigureAwait(false);
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
        return await _dao.GetOrAddIotCredsAsync(creds.UserId, async old =>
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

        //await UpdateThingsShadow();

        var response = await iotClient.GetThingShadowAsync(request).ConfigureAwait(false);

        // deserialize as T
        var stream = response.Payload;
        var json = await JsonSerializer.DeserializeAsync<T>(stream).ConfigureAwait(false);
        return json;
    }

    public async Task UpdateThingsShadow(Station station)
    {
        var payload = new UpdateThermoSensorShadowRequestPayload(
            _apiClient.Username,
            station.Category,
            station.StationSn,
            station.Devices.Select(d => d.DeviceSn).ToArray()
        );

        await UpdateThingsShadow(payload);
    }

    public async Task UpdateThingsShadow(UpdateThermoSensorShadowRequestPayload payload)
    {
        var client = await CreateIotDataClientAsync().ConfigureAwait(false);

        var request = new UpdateThingShadowRequest
        {
            ThingName = $"{payload.CategoryName}{payload.StationSN}",
            ShadowName = "2nd_apptempdata",
            Payload = payload.ToMemoryStream()
        };

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