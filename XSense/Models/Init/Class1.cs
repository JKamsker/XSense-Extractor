using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XSense.Models.Init;

public partial class AwsIotCredentialsResponse
{
    [JsonPropertyName("reCode")]
    public long ReCode { get; set; }

    [JsonPropertyName("reMsg")]
    public string ReMsg { get; set; }

    [JsonPropertyName("cntVersion")]
    public string CntVersion { get; set; }

    [JsonPropertyName("cfgVersion")]
    public CfgVersion CfgVersion { get; set; }

    [JsonPropertyName("reData")]
    public AwsIotCredentials ReData { get; set; }
}

//public class CfgVersion
//{
//    [JsonPropertyName("deviceControl")]
//    public string DeviceControl { get; set; }
//}

public partial class AwsIotCredentials
{
    [JsonPropertyName("accessKeyId")]
    public string AccessKeyId { get; set; }

    [JsonPropertyName("secretAccessKey")]
    public string SecretAccessKey { get; set; }

    [JsonPropertyName("sessionToken")]
    public string SessionToken { get; set; }

    [JsonPropertyName("expiration")]
    public string Expiration { get; set; }
}