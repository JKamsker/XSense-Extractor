using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using XSense.Models.Init;

namespace XSense.Models.Sensoric;

public record class GetSensoricDataRequest
{
    /*
        houseId = "64C4BD7BB9DF11EE8FFBF7BED2BE3C43",
        stationId = "914AABBCB9DF11EEB68155479EFE878E",
        deviceId = "49AB1D9DB9E011EEBB759F5A3BFA1896",
        lastTime = "20240123140600",
    */

    [JsonPropertyName("houseId")]
    public string HouseId { get; set; }

    [JsonPropertyName("stationId")]
    public string StationId { get; set; }

    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; }

    [JsonPropertyName("lastTime")]
    public string LastTime { get; set; }

    // 2023020423242620240201000000
    [JsonPropertyName("nextToken")]
    // ignore when null
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NextToken { get; set; }

    public GetSensoricDataRequest()
    {
    }

    public GetSensoricDataRequest(GetHousesDetailResponseData details, Station station, Device device)
    {
        HouseId = details.HouseId;
        StationId = station.StationId;
        DeviceId = device.DeviceId;
        LastTime = "0";
        NextToken = "";
    }
}

//GetSensoricDataResponse
//public partial class GetSensoricDataResponse : XSenseResponse<GetSensoricDataResponseData>
//{
//    [JsonPropertyName("delData")]
//    public object[] DelData { get; set; }

//    [JsonPropertyName("deSubData")]
//    public object[] DeSubData { get; set; }

//    [JsonPropertyName("utctimestamp")]
//    public long Utctimestamp { get; set; }
//}

public class GetSensoricDataResponseData
{
    [JsonPropertyName("dataList")]
    public Dictionary<string, string[]> DataList { get; set; }

    [JsonPropertyName("lastTime")]
    public string LastTime { get; set; }

    [JsonPropertyName("nextToken")]
    public string NextToken { get; set; }
}

# nullable disable

public class GetSensoricDataResponse : XSenseResponse<GetSensoricDataResponseData>
{
    //[JsonPropertyName("cfgVersion")]
    //public CfgVersion CfgVersion { get; set; }

    //[JsonPropertyName("cntVersion")]
    //public string CntVersion { get; set; }

    //[JsonPropertyName("reCode")]
    //public long ReCode { get; set; }

    //[JsonPropertyName("reData")]
    //public ReData ReData { get; set; }

    //[JsonPropertyName("reMsg")]
    //public string ReMsg { get; set; }
}

//public partial class CfgVersion
//{
//    [JsonPropertyName("deviceControl")]
//    public string DeviceControl { get; set; }
//}