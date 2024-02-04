using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XSense.Models.Sensoric.Live;

public partial class LiveSensoricData
{
    [JsonPropertyName("state")]
    public State State { get; set; }

    [JsonPropertyName("metadata")]
    public Metadata Metadata { get; set; }

    [JsonPropertyName("version")]
    public long Version { get; set; }

    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

public partial class Metadata
{
    [JsonPropertyName("reported")]
    public MetadataReported Reported { get; set; }
}

public partial class MetadataReported
{
    [JsonPropertyName("stationSN")]
    public DevNum StationSn { get; set; }

    [JsonPropertyName("wifiRSSI")]
    public DevNum WifiRssi { get; set; }

    [JsonPropertyName("devNum")]
    public DevNum DevNum { get; set; }

    [JsonPropertyName("devs")]
    public Dictionary<string, PurpleDev> Devs { get; set; }
}

public partial class DevNum
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

public partial class PurpleDev
{
    [JsonPropertyName("type")]
    public DevNum Type { get; set; }

    [JsonPropertyName("batInfo")]
    public DevNum BatInfo { get; set; }

    [JsonPropertyName("rfLevel")]
    public DevNum RfLevel { get; set; }

    [JsonPropertyName("online")]
    public DevNum Online { get; set; }

    [JsonPropertyName("status")]
    public PurpleStatus Status { get; set; }
}

public partial class PurpleStatus
{
    [JsonPropertyName("a")]
    public DevNum A { get; set; }

    [JsonPropertyName("b")]
    public DevNum B { get; set; }

    [JsonPropertyName("c")]
    public DevNum C { get; set; }

    [JsonPropertyName("d")]
    public DevNum D { get; set; }

    [JsonPropertyName("e")]
    public DevNum[] E { get; set; }

    [JsonPropertyName("f")]
    public DevNum[] F { get; set; }

    [JsonPropertyName("g")]
    public DevNum G { get; set; }

    [JsonPropertyName("h")]
    public DevNum H { get; set; }

    [JsonPropertyName("t")]
    public DevNum T { get; set; }
}

public partial class State
{
    [JsonPropertyName("reported")]
    public StateReported Reported { get; set; }
}

public partial class StateReported
{
    [JsonPropertyName("stationSN")]
    public string StationSn { get; set; }

    [JsonPropertyName("wifiRSSI")]
    public string WifiRssi { get; set; }

    [JsonPropertyName("devNum")]
    public string DevNum { get; set; }

    [JsonPropertyName("devs")]
    public Dictionary<string, FluffyDev> Devs { get; set; }
}

public partial class FluffyDev
{
    [JsonPropertyName("type")]
    public string Type { get; set; }

    [JsonPropertyName("batInfo")]
    public string BatInfo { get; set; }

    [JsonPropertyName("rfLevel")]
    public string RfLevel { get; set; }

    [JsonPropertyName("online")]
    public string Online { get; set; }

    [JsonPropertyName("status")]
    public FluffyStatus Status { get; set; }
}

public partial class FluffyStatus
{
    [JsonPropertyName("a")]
    public string A { get; set; }

    [JsonPropertyName("b")]
    public string B { get; set; }

    [JsonPropertyName("c")]
    public string C { get; set; }

    [JsonPropertyName("d")]
    public string D { get; set; }

    [JsonPropertyName("e")]
    public long[] E { get; set; }

    [JsonPropertyName("f")]
    public long[] F { get; set; }

    [JsonPropertyName("g")]
    public string G { get; set; }

    [JsonPropertyName("h")]
    public string H { get; set; }

    [JsonPropertyName("t")]
    public string T { get; set; }
}