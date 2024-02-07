using System.Text.Json;
using System.Text.Json.Serialization;

namespace XSense.Models.Aws;

/*
      var request = new UpdateThingShadowRequest
        {
            ThingName = "SBS5013B96457",
            ShadowName = "2nd_apptempdata",
            Payload = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new
            {
                state = new
                {
                    desired = new
                    {
                        deviceSN = new string[] { "00000001", "00000002", "00000003", "00000004" },
                        report = "1",
                        reportDst = "1",
                        shadow = "appTempData",
                        source = "1",
                        stationSN = "13B96457",
                        //time = "20240123214731", // 2024-01-23-21:47:31
                        time = $"{DateTime.UtcNow:yyyyMMddHHmmss}",
                        timeoutM = "5",
                        userId = "e2251ab2-46e8-497a-af56-44d4c5be95f1"
                    }
                }
            })))
        };
 */

public class UpdateThermoSensorShadowRequestPayload
{
    //deviceSN = new string[] { "00000001", "00000002", "00000003", "00000004" },
    //report = "1",
    //reportDst = "1",
    //shadow = "appTempData",
    //source = "1",
    //stationSN = "13B96457",
    ////time = "20240123214731", // 2024-01-23-21:47:31
    //time = $"{DateTime.UtcNow:yyyyMMddHHmmss}",
    //timeoutM = "5",
    //userId = "e2251ab2-46e8-497a-af56-44d4c5be95f1"

    [JsonIgnore]
    public string CategoryName { get; set; }

    [JsonPropertyName("deviceSN")]
    public string[] DeviceSN { get; set; }

    [JsonPropertyName("report")]
    public string Report { get; set; }

    [JsonPropertyName("reportDst")]
    public string ReportDst { get; set; }

    [JsonPropertyName("shadow")]
    public string Shadow { get; set; }

    [JsonPropertyName("source")]
    public string Source { get; set; }

    [JsonPropertyName("stationSN")]
    public string StationSN { get; set; }

    [JsonPropertyName("time")]
    public string Time { get; set; }

    [JsonPropertyName("timeoutM")]
    public string TimeoutM { get; set; }

    [JsonPropertyName("userId")]
    public string UserId { get; set; }

    public UpdateThermoSensorShadowRequestPayload(string userId, string categoryName, string stationSN, string[] deviceSN)
    {
        //DeviceSN = new string[] { "00000001", "00000002", "00000003", "00000004" };
        //UserId = "e2251ab2-46e8-497a-af56-44d4c5be95f1";
        //StationSN = "13B96457";

        UserId = userId;
        StationSN = stationSN;
        DeviceSN = deviceSN;
        CategoryName = categoryName;

        Report = "1";
        ReportDst = "1";
        Shadow = "appTempData";
        Source = "1";
        Time = $"{DateTime.UtcNow:yyyyMMddHHmmss}";
        TimeoutM = "5";
    }

    public MemoryStream ToMemoryStream()
    {
        var obj = new
        {
            state = new
            {
                desired = this
            }
        };

        var serialized = JsonSerializer.Serialize(obj);

        return new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(serialized));
    }
}