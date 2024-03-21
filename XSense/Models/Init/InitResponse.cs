using System.Text.Json.Serialization;

namespace XSenseExtractor.Models.Init;

/*JSON

	 {
		"reCode": 200,
		"reMsg": "success !",
		"cntVersion": "5",
		"cfgVersion": {
			"deviceControl": "7"
		},
		"reData": {
			"clientName": "Android",
			"cgtRegion": "us-east-1",
			"userPoolId": "us-east-1_nX8mlT3dQ",
			"clientId": "1jok6cf2o57ndalv2a6ol0ogqh",
			"clientSecret": "MTE3MjFmcjBya3BkNGFtdnB1czI1cmQ2N284b2E1NHNnZXBlNmljbmVmOTA1Z2RtaWQ3ZmFhb3My"
		}
	}
 */

public partial class InitResponse : XSenseResponse<InitResponseData>
{
    [JsonPropertyName("reData")]
    public InitResponseData ReData { get; set; }
}

public partial class CfgVersion
{
    [JsonPropertyName("deviceControl")]
    public string DeviceControl { get; set; }
}

public partial class InitResponseData
{
    [JsonPropertyName("clientName")]
    public string ClientName { get; set; }

    [JsonPropertyName("cgtRegion")]
    public string CgtRegion { get; set; }

    [JsonPropertyName("userPoolId")]
    public string UserPoolId { get; set; }

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; }

    [JsonPropertyName("clientSecret")]
    public string ClientSecret { get; set; }
}