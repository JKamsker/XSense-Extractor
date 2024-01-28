using Amazon;
using Amazon.CognitoIdentityProvider;
using Amazon.Extensions.CognitoAuthentication;
using Amazon.Runtime;

using System.Net.Http.Json;

using XSense.Models.Init;

namespace XSense;

public class XSenseHttpClient
{
    private readonly HttpClient _client;

    private ClientInfo? _clientInfo;

    public XSenseHttpClient(HttpClient client, ClientInfo? clientInfo = null)
    {
        _client = client;
        _clientInfo = clientInfo;
    }

    /// <summary>
    ///  Result should be cached
    /// </summary>
    /// <returns></returns>
    public async ValueTask<ClientInfo?> QueryClientInfo()
    {
        if (_clientInfo is not null)
        {
            return _clientInfo;
        }

        using var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.x-sense-iot.com/app");
        request.Headers.TryAddWithoutValidation("Host", "api.x-sense-iot.com");
        request.Headers.TryAddWithoutValidation("language", "de");
        request.Headers.TryAddWithoutValidation("user-agent", "okhttp/3.14.7");

        var content = JsonContent.Create(new
        {
            mac = "37a6259cc0c1dae299a7866489dff0bd",
            bizCode = "101001",
            appCode = "1172",
            appVersion = "v1.17.2_20240115",
            clientType = "2"
        });

        request.Content = content;

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var parsed = await response.Content.ReadFromJsonAsync<InitResponse>();
        if (parsed?.ReData is null)
        {
            return null;
        }

        var reData = parsed.ReData;

        var result = new ClientInfo(
            reData.CgtRegion,
            reData.UserPoolId,
            reData.ClientId,
            reData.ClientSecret
        );

        _clientInfo = result;
        return result;
    }

    public async Task<Credentials> AuthenticateWithSrpAsync(string userName, string password)
    {
        var clientInfo = await QueryClientInfo()
            ?? throw new InvalidOperationException("ClientInfo is null");

        Credentials result = null;

        var region = string.IsNullOrEmpty(clientInfo.Region)
            ? RegionEndpoint.USEast1
            : RegionEndpoint.GetBySystemName(clientInfo.Region);

        using var provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), region);
        var userPool = new CognitoUserPool(clientInfo.PoolId, clientInfo.ClientId, provider);
        var user = new CognitoUser(userName, clientInfo.ClientId, userPool, provider, clientInfo.ClientSecret.ShortValue);

        AuthFlowResponse authResponse = await user.StartWithSrpAuthAsync(new InitiateSrpAuthRequest()
        {
            Password = password,
        }).ConfigureAwait(false);

        return new Credentials(user, authResponse.AuthenticationResult);
    }

    public async Task<Credentials> GetCredsFromRefreshAsync(string userName, string refreshToken)
    {
        var clientInfo = await QueryClientInfo()
            ?? throw new InvalidOperationException("ClientInfo is null");

        var region = string.IsNullOrEmpty(clientInfo.Region)
            ? RegionEndpoint.USEast1
            : RegionEndpoint.GetBySystemName(clientInfo.Region);

        using var provider = new AmazonCognitoIdentityProviderClient(new AnonymousAWSCredentials(), region);
        var userPool = new CognitoUserPool(clientInfo.PoolId, clientInfo.ClientId, provider);
        var user = new CognitoUser(userName, clientInfo.ClientId, userPool, provider, clientInfo.ClientSecret.ShortValue)
        {
            SessionTokens = new CognitoUserSession(null, null, refreshToken, DateTime.UtcNow, DateTime.UtcNow.AddHours(1))
        };
        // If the user pool is configured to track and remember user devices, it must be attached to the user before initiating the flow:
        // user.Device = new CognitoDevice(new DeviceType { DeviceKey = deviceKey }, user);

        var authResponse = await user.StartWithRefreshTokenAuthAsync(new InitiateRefreshTokenAuthRequest
        {
            AuthFlowType = AuthFlowType.REFRESH_TOKEN_AUTH
        });

        return new Credentials(user, authResponse.AuthenticationResult);
    }

    public async Task FirstRequest(Credentials credentials)
    {
        var clientInfo = await QueryClientInfo()
            ?? throw new InvalidOperationException("ClientInfo is null");

        using var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.x-sense-iot.com/app");
        request.Headers.TryAddWithoutValidation("Host", "api.x-sense-iot.com");
        request.Headers.TryAddWithoutValidation("authorization", credentials.AuthenticationResult.AccessToken);
        request.Headers.TryAddWithoutValidation("userpoolconfig", clientInfo.UserPoolConfig);
        request.Headers.TryAddWithoutValidation("language", "de");
        request.Headers.TryAddWithoutValidation("user-agent", "okhttp/3.14.7");

        request.Content = JsonContent.Create(new
        {
            userId = credentials.User.Username,
            //mac = "101b6ec532230a2aafe906be4b63ad59",
            mac = MacUtils.GetRequestMac(new Dictionary<string, object>
            {
                { "userId", credentials.User.Username },
            }, clientInfo.ClientSecret),
            bizCode = "501001",
            appCode = "1172",
            appVersion = "v1.17.2_20240115",
            clientType = "2"
        });

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var text = await response.Content.ReadAsStringAsync();

        /*
         {
	        "reCode": 200,
	        "reMsg": "success !",
	        "cntVersion": "5",
	        "cfgVersion": {
		        "deviceControl": "7"
	        },
	        "reData": [],
	        "delData": {
		        "isTrial": "0"
	        },
	        "utctimestamp": 1706042850
        }
        */
    }

    public async Task GetHouses(Credentials creds)
    {
        /*
          using (var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.x-sense-iot.com/app"))
        {
            request.Headers.TryAddWithoutValidation("Host", "api.x-sense-iot.com");
            request.Headers.TryAddWithoutValidation("authorization", "eyJraWQiOiJZMGxoZEF3V3dENHZ3eU1RMHp6UExFUnJtN2F0M2Y2MjNKYzdzeENCSjdvPSIsImFsZyI6IlJTMjU2In0.eyJzdWIiOiJlMjI1MWFiMi00NmU4LTQ5N2EtYWY1Ni00NGQ0YzViZTk1ZjEiLCJpc3MiOiJodHRwczpcL1wvY29nbml0by1pZHAudXMtZWFzdC0xLmFtYXpvbmF3cy5jb21cL3VzLWVhc3QtMV9uWDhtbFQzZFEiLCJjbGllbnRfaWQiOiIxam9rNmNmMm81N25kYWx2MmE2b2wwb2dxaCIsIm9yaWdpbl9qdGkiOiI2MWRkMWFmYy1mOTFlLTQ3MDMtOGU1My03OGM5MGUyYWI2YWQiLCJldmVudF9pZCI6IjJjZmRmMDhlLWM5ZTktNGFhZS04ZGQwLTQ5NzIxMTc4Yjk0ZSIsInRva2VuX3VzZSI6ImFjY2VzcyIsInNjb3BlIjoiYXdzLmNvZ25pdG8uc2lnbmluLnVzZXIuYWRtaW4iLCJhdXRoX3RpbWUiOjE3MDYwNDI4NDksImV4cCI6MTcwNjEyOTI0OSwiaWF0IjoxNzA2MDQyODQ5LCJqdGkiOiIxMTk3ZDg4Zi1iMmNkLTRiYjUtYWM2NS04ZjViNzYxNTg1YjYiLCJ1c2VybmFtZSI6ImUyMjUxYWIyLTQ2ZTgtNDk3YS1hZjU2LTQ0ZDRjNWJlOTVmMSJ9.mwYXLDeI1FIojb3dT_7k_Q6AQLzAnk-uAU5CGCpEcvXSCSBnpWvhAxglInWkOmuP8ChTJ21WcCdv-iVwuRynK_4ilKvOjBib2V0P-1a6RJ82pq0e--_9Bo7kyeVW4ywdBHB38g7Hx1zngGGNpNHGvhpr8TrrWWrjdnvKg0CCjrjCnPG0c-939C35kR9R0Cz3Tn2V3iydJ8u9iPdk_8J5b3uGK1up9xHOG8vsuWzqOkY_H0_0R_-Oz8uMbklL4Slh4Gd-S07LIJT6F-My9mpZ3O_xqs9LhIe89ltZJNaBmkf-gTVxlxvrvQLlQAW0wMRqtJrkqutm3FS-cC6KB68Nlg");
            request.Headers.TryAddWithoutValidation("userpoolconfig", "us-east-1_nX8mlT3dQ#1jok6cf2o57ndalv2a6ol0ogqh");
            request.Headers.TryAddWithoutValidation("language", "de");
            request.Headers.TryAddWithoutValidation("user-agent", "okhttp/3.14.7");

            request.Content = new StringContent("{\"utctimestamp\":0,\"mac\":\"2b409ca418f7112c6c8f8403de4433e4\",\"bizCode\":\"102007\",\"appCode\":\"1172\",\"appVersion\":\"v1.17.2_20240115\",\"clientType\":\"2\"}");
            request.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");

            var response = await httpClient.SendAsync(request);
        }
        */

        var clientInfo = await QueryClientInfo()
            ?? throw new InvalidOperationException("ClientInfo is null");

        using var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.x-sense-iot.com/app");
        request.Headers.TryAddWithoutValidation("authorization", creds.AuthenticationResult.AccessToken);
        request.Headers.TryAddWithoutValidation("userpoolconfig", clientInfo.UserPoolConfig);
        request.Headers.TryAddWithoutValidation("language", "de");
        request.Headers.TryAddWithoutValidation("user-agent", "okhttp/3.14.7");

        request.Content = JsonContent.Create(new
        {
            utctimestamp = 0,
            mac = MacUtils.GetRequestMac(new Dictionary<string, object>
            {
                { "utctimestamp", 0 },
            }, clientInfo.ClientSecret),
            bizCode = "102007",
            appCode = "1172",
            appVersion = "v1.17.2_20240115",
            clientType = "2"
        });

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var text = await response.Content.ReadAsStringAsync();
        //{"reCode": 200, "reMsg": "success !", "cntVersion": "5", "cfgVersion": {"deviceControl": "7"}, "reData": [{"houseId": "64C4BD7BB9DF11EE8FFBF7BED2BE3C43", "houseName": "Zu hause", "houseRegion": "Germany (Deutschland)", "mqttRegion": "eu-central-1", "mqttServer": "eu-central-1.x-sense-iot.com", "loraBand": "868", "houseOrigin": 0, "createTime": "20240123110601"}], "delData": [], "deSubData": ["64C4BD7BB9DF11EE8FFBF7BED2BE3C43"], "utctimestamp": 1706478440}
    }

    public async Task GetSensoricData(Credentials creds)
    {
        var clientInfo = await QueryClientInfo()
            ?? throw new InvalidOperationException("ClientInfo is null");

        using var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.x-sense-iot.com/app");
        //request.Headers.TryAddWithoutValidation("Host", "api.x-sense-iot.com");
        request.Headers.TryAddWithoutValidation("authorization", creds.AuthenticationResult.AccessToken);
        request.Headers.TryAddWithoutValidation("userpoolconfig", clientInfo.UserPoolConfig);
        request.Headers.TryAddWithoutValidation("language", "de");
        request.Headers.TryAddWithoutValidation("user-agent", "okhttp/3.14.7");

        request.Content = JsonContent.Create(new
        {
            houseId = "64C4BD7BB9DF11EE8FFBF7BED2BE3C43",
            stationId = "914AABBCB9DF11EEB68155479EFE878E",
            deviceId = "49AB1D9DB9E011EEBB759F5A3BFA1896",
            lastTime = "20240123140600",
            mac = MacUtils.GetRequestMac(new Dictionary<string, object>
            {
                { "houseId", "64C4BD7BB9DF11EE8FFBF7BED2BE3C43" },
                { "stationId", "914AABBCB9DF11EEB68155479EFE878E" },
                { "deviceId", "49AB1D9DB9E011EEBB759F5A3BFA1896" },
                { "lastTime", "20240123140600" },
            }, clientInfo.ClientSecret),
            bizCode = "104011",
            appCode = "1172",
            appVersion = "v1.17.2_20240115",
            clientType = "2"
        });

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        var text = await response.Content.ReadAsStringAsync();
    }

    //curl -H "Host: api.x-sense-iot.com" -H "authorization: eyJraWQiOiJZMGxoZEF3V3dENHZ3eU1RMHp6UExFUnJtN2F0M2Y2MjNKYzdzeENCSjdvPSIsImFsZyI6IlJTMjU2In0.eyJzdWIiOiJlMjI1MWFiMi00NmU4LTQ5N2EtYWY1Ni00NGQ0YzViZTk1ZjEiLCJpc3MiOiJodHRwczpcL1wvY29nbml0by1pZHAudXMtZWFzdC0xLmFtYXpvbmF3cy5jb21cL3VzLWVhc3QtMV9uWDhtbFQzZFEiLCJjbGllbnRfaWQiOiIxam9rNmNmMm81N25kYWx2MmE2b2wwb2dxaCIsIm9yaWdpbl9qdGkiOiI2MWRkMWFmYy1mOTFlLTQ3MDMtOGU1My03OGM5MGUyYWI2YWQiLCJldmVudF9pZCI6IjJjZmRmMDhlLWM5ZTktNGFhZS04ZGQwLTQ5NzIxMTc4Yjk0ZSIsInRva2VuX3VzZSI6ImFjY2VzcyIsInNjb3BlIjoiYXdzLmNvZ25pdG8uc2lnbmluLnVzZXIuYWRtaW4iLCJhdXRoX3RpbWUiOjE3MDYwNDI4NDksImV4cCI6MTcwNjEyOTI0OSwiaWF0IjoxNzA2MDQyODQ5LCJqdGkiOiIxMTk3ZDg4Zi1iMmNkLTRiYjUtYWM2NS04ZjViNzYxNTg1YjYiLCJ1c2VybmFtZSI6ImUyMjUxYWIyLTQ2ZTgtNDk3YS1hZjU2LTQ0ZDRjNWJlOTVmMSJ9.mwYXLDeI1FIojb3dT_7k_Q6AQLzAnk-uAU5CGCpEcvXSCSBnpWvhAxglInWkOmuP8ChTJ21WcCdv-iVwuRynK_4ilKvOjBib2V0P-1a6RJ82pq0e--_9Bo7kyeVW4ywdBHB38g7Hx1zngGGNpNHGvhpr8TrrWWrjdnvKg0CCjrjCnPG0c-939C35kR9R0Cz3Tn2V3iydJ8u9iPdk_8J5b3uGK1up9xHOG8vsuWzqOkY_H0_0R_-Oz8uMbklL4Slh4Gd-S07LIJT6F-My9mpZ3O_xqs9LhIe89ltZJNaBmkf-gTVxlxvrvQLlQAW0wMRqtJrkqutm3FS-cC6KB68Nlg" -H "userpoolconfig: us-east-1_nX8mlT3dQ#1jok6cf2o57ndalv2a6ol0ogqh" -H "language: de" -H "content-type: application/json; charset=utf-8" -H "user-agent: okhttp/3.14.7" --data-binary "{\"userName\":\"USERNAME\",\"mac\":\"57a252adbc1af382d1d784db2e40e267\",\"bizCode\":\"101003\",\"appCode\":\"1172\",\"appVersion\":\"v1.17.2_20240115\",\"clientType\":\"2\"}" --compressed "https://api.x-sense-iot.com/app"
    // GetAwsIotCredentials
    public async Task<AwsIotCredentials> GetAwsIotCredentials(Credentials creds)
    {
        var clientInfo = await QueryClientInfo()
            ?? throw new InvalidOperationException("ClientInfo is null");

        using var request = new HttpRequestMessage(new HttpMethod("POST"), "https://api.x-sense-iot.com/app");
        request.Headers.TryAddWithoutValidation("Host", "api.x-sense-iot.com");
        request.Headers.TryAddWithoutValidation("authorization", creds.AuthenticationResult.AccessToken);
        request.Headers.TryAddWithoutValidation("userpoolconfig", clientInfo.UserPoolConfig);
        request.Headers.TryAddWithoutValidation("language", "de");
        request.Headers.TryAddWithoutValidation("user-agent", "okhttp/3.14.7");

        request.Content = JsonContent.Create(new
        {
            userName = creds.User.UserID,
            mac = MacUtils.GetRequestMac(new Dictionary<string, object>
            {
                { "userName", creds.User.UserID },
            }, clientInfo.ClientSecret),
            bizCode = "101003",
            appCode = "1172",
            appVersion = "v1.17.2_20240115",
            clientType = "2"
        });

        var response = await _client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        //var text = await response.Content.ReadAsStringAsync();
        var parsed = await response.Content.ReadFromJsonAsync<AwsIotCredentialsResponse>();

        if (parsed?.ReData is null)
        {
            throw new InvalidOperationException("ReData is null");
        }

        return parsed.ReData;
    }
}