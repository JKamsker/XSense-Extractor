using System.Text.Json.Serialization;

namespace XSense.Models.Init;

public partial class GetHousesDetailResponse : XSenseResponse<GetHousesDetailResponseData>
{
    [JsonPropertyName("delData")]
    public object[] DelData { get; set; }

    [JsonPropertyName("deSubData")]
    public object[] DeSubData { get; set; }

    [JsonPropertyName("utctimestamp")]
    public long Utctimestamp { get; set; }
}

public partial class GetHousesDetailResponseData
{
    [JsonPropertyName("houseId")]
    public string HouseId { get; set; }

    [JsonPropertyName("stations")]
    public Station[] Stations { get; set; }

    [JsonPropertyName("stationSort")]
    public string[] StationSort { get; set; }

    [JsonPropertyName("ownStations")]
    public string[] OwnStations { get; set; }

    [JsonPropertyName("cameras")]
    public object[] Cameras { get; set; }
}

public partial class Station
{
    [JsonIgnore]
    public string ThingName => $"{Category}{StationSn}";

    [JsonPropertyName("stationId")]
    public string StationId { get; set; }

    [JsonPropertyName("stationSn")]
    public string StationSn { get; set; }

    [JsonPropertyName("stationName")]
    public string StationName { get; set; }

    [JsonPropertyName("category")]
    public string Category { get; set; }

    [JsonPropertyName("roomId")]
    public string RoomId { get; set; }

    [JsonPropertyName("safeMode")]
    public string SafeMode { get; set; }

    [JsonPropertyName("onLine")]
    public long OnLine { get; set; }

    [JsonPropertyName("onLineTime")]
    public long OnLineTime { get; set; }

    [JsonPropertyName("userId")]
    public Guid UserId { get; set; }

    [JsonPropertyName("userName")]
    public string UserName { get; set; }

    [JsonPropertyName("groupList")]
    public object[] GroupList { get; set; }

    [JsonPropertyName("sbs50Sw")]
    public string Sbs50Sw { get; set; }

    [JsonPropertyName("devices")]
    public Device[] Devices { get; set; }

    [JsonPropertyName("deviceSort")]
    public string[] DeviceSort { get; set; }
}

public partial class Device
{
    [JsonPropertyName("deviceId")]
    public string DeviceId { get; set; }

    [JsonPropertyName("deviceSn")]
    public string DeviceSn { get; set; }

    [JsonPropertyName("deviceName")]
    public string DeviceName { get; set; }

    [JsonPropertyName("deviceType")]
    public string DeviceType { get; set; }

    [JsonPropertyName("roomId")]
    public string RoomId { get; set; }
}