using Amazon.IotData;
using Amazon.IotData.Model;
using Amazon.Runtime;

using System.Text;

using XSense.Models.Sensoric;

namespace XSense;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var storage = InMemoryStorage.LoadFromDisk("strorage.json");
        storage.OnCacheUpdated = async s => await s.SaveToDiskAsync("strorage.json");

        var xsenseClient = new XSenseHttpClient(new HttpClient());
        var xsenseApiClient = new XSenseApiClient(xsenseClient, storage);

        await xsenseApiClient.LoginAsync("USERNAME", "PASSWORD");

        var houses = await xsenseApiClient.GetHousesAsync();
        var details = await xsenseApiClient.GetHouseDetailsAsync(houses[0].HouseId);

        foreach (var station in details.Stations)
        {
            foreach (var device in station.Devices)
            {
                var sensoricData = await xsenseApiClient.GetSensoricDataAsync(
                    new GetSensoricDataRequest
                    {
                        HouseId = details.HouseId,
                        StationId = station.StationId,
                        DeviceId = device.DeviceId,
                        LastTime = "20240123140600"
                    }
                );
            }
        }

        var clientInfo = await xsenseClient.QueryClientInfo();
        var authResult = await xsenseClient.AuthenticateWithSrpAsync(clientInfo, "USERNAME", "PASSWORD");

        var refreshed = await xsenseClient.RefreshTokenAsync(clientInfo, authResult.Username, authResult.RefreshToken);

        await xsenseClient.GetHouses(clientInfo, authResult);

        var iotCreds = await xsenseClient.GetAwsIotCredentials(clientInfo, authResult);

        //await xsenseClient.GetSensoricData(authResult);

        //curl -H "Authorization: AWS4-HMAC-SHA256 Credential=ASIASBP3JFJYUGSENDH4/20240123/eu-central-1/iotdata/aws4_request, SignedHeaders=host;x-amz-date;x-amz-security-token, Signature=f7d029b9ad1b135e75d8a575be7eec9bd0c1c8b87f1326d86ea0c08a315e0ae9" -H "X-Amz-Date: 20240123T204731Z" -H "aws-sdk-invocation-id: 302c32ff-d21c-48c8-b777-d16602ec5179" -H "User-Agent: aws-sdk-android/2.22.6 Linux/4.14.180-perf-g4b73fd3 Dalvik/2.1.0/0 de_DE" -H "aws-sdk-retry: 0/0" -H "x-amz-security-token: FwoGZXIvYXdzEL7//////////wEaDKbWjvEiYzu+BKdjLCLXAiISlhcZC3ZJt1KW/Xxs1XJoKSaiNz5ictYNZSRITO8T5LrVNIkqrgJ/C+WSfpjyiJxiK478Gz8eOyVQCTfJCZRx40x+ANQyZQJKC/HEuOb2TPBA7zpuvn/9XbOc5YFiJEBAfTNQxkh13BorfVfvVrdz8/bQuUvUVygTcbhh9w2M173wEfy+Ct7ziTbTRTKixw8283v+DvGWQ+uZxo3X0Gv0VO27RaxERq1e2RoBiRf8gYT+nylHsmb/Fxdt+6XEIOEAO6h444Td2L6uV+pCEl+RwSdIeXDoE0dXcA62CHw7BAHmoGh7ulGOnTIzeBbHXdGoKOjL/WBodu1vHsnfUxsgug956e+S4exsjcaPjjgCWZssLlFFLj+09cStgG/wIQavm5D3cEa5YrgR9VUPjgb5llNaaX1sc0ERADINLNgkAfSA+97A2vsVJADCeMO9tcT0tkAS+k8o48vArQYyLWAn/Ar0CLF2LDt/HboFPEaj1/VGpILrMsmDl7VEWf02bTQyGewuyYoSIJEk9A==" -H "Content-Type: application/x-amz-json-1.0" -H "Host: data.iot.eu-central-1.amazonaws.com" "https://data.iot.eu-central-1.amazonaws.com/things/SBS5013B96457/shadow?name=2nd_mainpage"

        var credentials = new SessionAWSCredentials(
            iotCreds.AccessKeyId,
            iotCreds.SecretAccessKey,
            iotCreds.SessionToken
        )
        {
        };

        // Set up the AWS IoT Data Plane client configuration
        var config = new AmazonIotDataConfig
        {
            //RegionEndpoint = RegionEndpoint.EUCentral1 // The region to connect to
            ServiceURL = "https://data.iot.eu-central-1.amazonaws.com",
            //UseHttp = true,
            HttpClientFactory = new HttpClientFactoryWithSslDisabled()
        };

        // Create the AWS IoT Data Plane client
        using (var client = new AmazonIotDataClient(credentials, config))
        {
            // Create the request
            var request = new GetThingShadowRequest
            {
                ThingName = "SBS5013B96457",
                ShadowName = "2nd_mainpage",
            };

            // Execute the request
            var response = client.GetThingShadowAsync(request).Result;

            // Read the response (the shadow document is in the payload as a memory stream)
            using (var reader = new StreamReader(response.Payload))
            {
                string shadowDocument = reader.ReadToEnd();
                Console.WriteLine(shadowDocument);
            }
        }
    }
}

public class HttpClientFactoryWithSslDisabled : HttpClientFactory
{
    public HttpClient CreateClient(string name)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        return new HttpClient(handler);
    }

    public override HttpClient CreateHttpClient(IClientConfig clientConfig)
    {
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
        };

        return new HttpClient(handler);
    }
}

public record ClientInfo
(
    string? Region,
    string PoolId,
    string ClientId,
    ClientSecret ClientSecret
)
{
    // $"{PoolID}#{ClientID}"
    public string UserPoolConfig => $"{PoolId}#{ClientId}";

    /*
	 "reData": {
			"clientName": "Android",
			"cgtRegion": "us-east-1",
			"userPoolId": "us-east-1_nX8mlT3dQ",
			"clientId": "1jok6cf2o57ndalv2a6ol0ogqh",
			"clientSecret": "MTE3MjFmcjBya3BkNGFtdnB1czI1cmQ2N284b2E1NHNnZXBlNmljbmVmOTA1Z2RtaWQ3ZmFhb3My"
		}
	 */

    public static readonly ClientInfo Default = new(
        "us-east-1",
        "us-east-1_nX8mlT3dQ",
        "1jok6cf2o57ndalv2a6ol0ogqh",
        "MTE3MjFmcjBya3BkNGFtdnB1czI1cmQ2N284b2E1NHNnZXBlNmljbmVmOTA1Z2RtaWQ3ZmFhb3My"
    );
}

public class ClientSecret
{
    public string Base64Value { get; }

    public string FullValue { get; }

    public string ShortValue { get; }

    public ClientSecret(string base64Value)
    {
        Base64Value = base64Value;
        FullValue = Encoding.UTF8.GetString(Convert.FromBase64String(base64Value));
        if (FullValue.Length >= 5)
        {
            ShortValue = FullValue.Substring(0, FullValue.Length - 1).Substring((1172).ToString().Length);
        }
    }

    public static implicit operator ClientSecret(string base64Value) => new(base64Value);
}