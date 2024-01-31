using System.Text.Json.Serialization;

using XSense.Models.Init;

namespace XSense.Models;

public class XSenseResponse<T>
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
    public T ReData { get; set; }
}