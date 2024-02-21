using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace XSense.Models.Init;

public class GetHousesResponse : XSenseResponse<House[]>
{
    [JsonPropertyName("delData")]
    public object[] DelData { get; set; }

    [JsonPropertyName("deSubData")]
    public string[] DeSubData { get; set; }

    [JsonPropertyName("utctimestamp")]
    public long Utctimestamp { get; set; }
}

public class House
{
    [JsonPropertyName("houseId")]
    public string HouseId { get; set; }

    [JsonPropertyName("houseName")]
    public string HouseName { get; set; }

    [JsonPropertyName("houseRegion")]
    public string HouseRegion { get; set; }

    [JsonPropertyName("mqttRegion")]
    public string MqttRegion { get; set; }

    [JsonPropertyName("mqttServer")]
    public string MqttServer { get; set; }

    [JsonPropertyName("loraBand")]
    public string LoraBand { get; set; }

    [JsonPropertyName("houseOrigin")]
    public long HouseOrigin { get; set; }

    [JsonPropertyName("createTime")]
    public string CreateTime { get; set; }
}