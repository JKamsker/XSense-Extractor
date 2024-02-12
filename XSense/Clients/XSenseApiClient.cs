using Amazon.IotData;
using Amazon.IotData.Model;
using Amazon.Runtime;

using System.Diagnostics;
using System.Text.Json;

using XSense.Clients;
using XSense.Models.Aws;
using XSense.Models.Init;
using XSense.Models.Sensoric;
using XSense.Models.Sensoric.Live;
using XSense.Utils;

namespace XSense;

internal class XSenseApiClient
{
    private readonly XSenseHttpClient _httpClient;
    private readonly InMemoryStorage _storage;
    private readonly XSenseAwsIotClient _iotClient;
    private Credentials _credentials;

    // Should be userid (GUID)
    public string Username => _credentials.Username;

    public XSenseApiClient(XSenseHttpClient httpClient, InMemoryStorage? storage = null)
    {
        _httpClient = httpClient;
        _storage = storage ?? new InMemoryStorage();
        _iotClient = new XSenseAwsIotClient(this, httpClient, _storage);
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

    internal async ValueTask<Credentials> GetCredentialsAsync()
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

    internal async ValueTask<ClientInfo> GetClientInfoAsync()
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

    public async Task<LiveSensoricData> PollSensoricDataAsync(Station station)
    {
        await _iotClient.UpdateThingsShadow(station);

        var shadowData = await _iotClient.GetThingsShadowAsync<LiveSensoricData>(
            $"{station.ThingName}",
            "2nd_mainpage"
        );

        return shadowData;
    }
}