using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace XSenseExtractor.Models.Sensoric.Live;

/*

{
	"state": {
		"reported": {
			"stationSN": "13B96457",
			"wifiRSSI": "-65",
			"devNum": "4",
			"devs": {
		    "00000001": {
			    "type": "STH51",
			    "batInfo": "3", // 0-3
			    "rfLevel": "3",
			    "online": "1",
			    "status": {
				    "a": "0", // alarmstatus
				    "b": "23.0", // Temperature
				    "c": "48.8", // Humidity
				    "d": "1", // Temp unit 1: Celsius
				    "e": [-20, 60], // temperature range min max
				    "f": [0, 100], // humidity range min max
				    "g": "1", // Alarm enabled
				    "h": "0", // continuous alarm
				    "t": "20240123193102" // time
				    // Missing: "on": "somestring"
			    }
		    }
				// ...
			}
		}
	},
	"metadata": {
		"reported": {
			"stationSN": {
				"timestamp": 1706038269
			},
			"wifiRSSI": {
				"timestamp": 1706038269
			},
			"devNum": {
				"timestamp": 1706036891
			},
			"devs": {
				"00000001": {
					"type": {
						"timestamp": 1706038262
					},
					"batInfo": {
						"timestamp": 1706038262
					},
					"rfLevel": {
						"timestamp": 1706038262
					},
					"online": {
						"timestamp": 1706038262
					},
					"status": {
						"a": {
							"timestamp": 1706038262
						},
						"b": {
							"timestamp": 1706038262
						},
						"c": {
							"timestamp": 1706038262
						},
						"d": {
							"timestamp": 1706038262
						},
						"e": [{
							"timestamp": 1706038262
						}, {
							"timestamp": 1706038262
						}],
						"f": [{
							"timestamp": 1706038262
						}, {
							"timestamp": 1706038262
						}],
						"g": {
							"timestamp": 1706038262
						},
						"h": {
							"timestamp": 1706038262
						},
						"t": {
							"timestamp": 1706038262
						}
					}
				},
				"00000002": {
					"type": {
						"timestamp": 1706038264
					},
					"batInfo": {
						"timestamp": 1706038264
					},
					"rfLevel": {
						"timestamp": 1706038264
					},
					"online": {
						"timestamp": 1706038264
					},
					"status": {
						"a": {
							"timestamp": 1706038264
						},
						"b": {
							"timestamp": 1706038264
						},
						"c": {
							"timestamp": 1706038264
						},
						"d": {
							"timestamp": 1706038264
						},
						"e": [{
							"timestamp": 1706038264
						}, {
							"timestamp": 1706038264
						}],
						"f": [{
							"timestamp": 1706038264
						}, {
							"timestamp": 1706038264
						}],
						"g": {
							"timestamp": 1706038264
						},
						"h": {
							"timestamp": 1706038264
						},
						"t": {
							"timestamp": 1706038264
						}
					}
				},
				"00000003": {
					"type": {
						"timestamp": 1706038265
					},
					"batInfo": {
						"timestamp": 1706038265
					},
					"rfLevel": {
						"timestamp": 1706038265
					},
					"online": {
						"timestamp": 1706038265
					},
					"status": {
						"a": {
							"timestamp": 1706038265
						},
						"b": {
							"timestamp": 1706038265
						},
						"c": {
							"timestamp": 1706038265
						},
						"d": {
							"timestamp": 1706038265
						},
						"e": [{
							"timestamp": 1706038265
						}, {
							"timestamp": 1706038265
						}],
						"f": [{
							"timestamp": 1706038265
						}, {
							"timestamp": 1706038265
						}],
						"g": {
							"timestamp": 1706038265
						},
						"h": {
							"timestamp": 1706038265
						},
						"t": {
							"timestamp": 1706038265
						}
					}
				},
				"00000004": {
					"type": {
						"timestamp": 1706038269
					},
					"batInfo": {
						"timestamp": 1706038269
					},
					"rfLevel": {
						"timestamp": 1706038269
					},
					"online": {
						"timestamp": 1706038269
					},
					"status": {
						"a": {
							"timestamp": 1706038269
						},
						"b": {
							"timestamp": 1706038269
						},
						"c": {
							"timestamp": 1706038269
						},
						"d": {
							"timestamp": 1706038269
						},
						"e": [{
							"timestamp": 1706038269
						}, {
							"timestamp": 1706038269
						}],
						"f": [{
							"timestamp": 1706038269
						}, {
							"timestamp": 1706038269
						}],
						"g": {
							"timestamp": 1706038269
						},
						"h": {
							"timestamp": 1706038269
						},
						"t": {
							"timestamp": 1706038269
						}
					}
				}
			}
		}
	},
	"version": 1056,
	"timestamp": 1706042852
}
 */

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
    public Dictionary<string, MetadataDev> Devs { get; set; }
}

public partial class DevNum
{
    [JsonPropertyName("timestamp")]
    public long Timestamp { get; set; }
}

public partial class MetadataDev
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
    public Dictionary<string, StateReportedDev> Devs { get; set; }
}

public partial class StateReportedDev
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
    public ReportedStatus Status { get; set; }
}

//public partial class FluffyStatus
//{
//    [JsonPropertyName("a")]
//    public string A { get; set; }

//    // Temparature
//    [JsonPropertyName("b")]
//    public string Temparature { get; set; }

//    // Hiumidity
//    [JsonPropertyName("c")]
//    public string Humidity { get; set; }

//    [JsonPropertyName("d")]
//    public string D { get; set; }

//    [JsonPropertyName("e")]
//    public long[] E { get; set; }

//    [JsonPropertyName("f")]
//    public long[] F { get; set; }

//    [JsonPropertyName("g")]
//    public string G { get; set; }

//    [JsonPropertyName("h")]
//    public string H { get; set; }

//    [JsonPropertyName("t")]
//    public string T { get; set; }
//}

/*
 "status": {
				    "a": "0", // alarmstatus
				    "b": "23.0", // Temperature
				    "c": "48.8", // Humidity
				    "d": "1", // Temp unit 1: Celsius
				    "e": [-20, 60], // temperature range min max
				    "f": [0, 100], // humidity range min max
				    "g": "1", // Alarm enabled
				    "h": "0", // continuous alarm
				    "t": "20240123193102" // time
				    // Missing: "on": "somestring"
			    }

[JsonPropertyName("a")]
    public string Alarmstatus { get; set; }

	[JsonPropertyName("b")]
	public string Temperature { get; set; }
...
 */

public class ReportedStatus
{
    [JsonPropertyName("a")]
    public string Alarmstatus { get; set; }

    [JsonPropertyName("b")]
    public string Temperature { get; set; }

    [JsonPropertyName("c")]
    public string Humidity { get; set; }

    [JsonPropertyName("d")]
    public string TempUnit { get; set; }

    [JsonPropertyName("e")]
    public int[] TemperatureRange { get; set; }

    [JsonPropertyName("f")]
    public int[] HumidityRange { get; set; }

    [JsonPropertyName("g")]
    public string AlarmEnabled { get; set; }

    [JsonPropertyName("h")]
    public string ContinuousAlarm { get; set; }

    [JsonPropertyName("t")]
    public string Time { get; set; }
}