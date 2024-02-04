using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XSense.Models.Init;

public partial class AwsIotCredentialsResponse : XSenseResponse<AwsIotCredentials>
{
    [JsonPropertyName("reData")]
    public AwsIotCredentials ReData { get; set; }
}

public partial class AwsIotCredentials : IExpirable
{
    [JsonPropertyName("accessKeyId")]
    public string AccessKeyId { get; set; }

    [JsonPropertyName("secretAccessKey")]
    public string SecretAccessKey { get; set; }

    [JsonPropertyName("sessionToken")]
    public string SessionToken { get; set; }

    [JsonPropertyName("expiration")]
    public string Expiration { get; set; }

    [JsonIgnore]
    public bool IsExpired => _expiresAt.Value < DateTime.UtcNow - TimeSpan.FromMinutes(5);

    private Lazy<DateTime> _expiresAt;

    public AwsIotCredentials()
    {
        _expiresAt = new Lazy<DateTime>(() => DateTime.Parse(Expiration));
    }
}