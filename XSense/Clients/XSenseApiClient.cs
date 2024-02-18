using Amazon.CognitoIdentityProvider.Model;
using Amazon.IotData;
using Amazon.IotData.Model;
using Amazon.Runtime;

using System.Diagnostics;
using System.Text.Json;

using XSense.Clients;
using XSense.Database;
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