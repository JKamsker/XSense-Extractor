using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XSense.Models.Sensoric;

public class GetSensoricDataRequest
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
}

//GetSensoricDataResponse
public partial class GetSensoricDataResponse : XSenseResponse<GetSensoricDataResponseData>
{
    [JsonPropertyName("delData")]
    public object[] DelData { get; set; }

    [JsonPropertyName("deSubData")]
    public object[] DeSubData { get; set; }

    [JsonPropertyName("utctimestamp")]
    public long Utctimestamp { get; set; }
}

public class GetSensoricDataResponseData
{
}