using Amazon.IotData;
using Amazon.IotData.Model;

using System.Collections;
using System.Text.Json;

using XSense.Models.Init;
using XSense.Models.Sensoric;
using XSense.Models.Sensoric.Live;

namespace XSense;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var storage = InMemoryStorage.LoadFromDisk("strorage.json");
        storage.OnCacheUpdated = async s => await s.SaveToDiskAsync("strorage.json");

        var xsenseClient = new XSenseHttpClient(new HttpClient());
        var xsenseApiClient = new XSenseApiClient(xsenseClient, storage);

        await xsenseApiClient.LoginAsync("USERNAME", "PASSWORD");

        var houses = await xsenseApiClient.GetHousesAsync();
        var details = await xsenseApiClient.GetHouseDetailsAsync(houses[0].HouseId);

        foreach (var station in details.Stations)
        {
            LiveSensoricDataCollection? lastData = null;

            while (true)
            {
                var shadowData = await xsenseApiClient.GetThingsShadowAsync<LiveSensoricData>(
                    $"{station.ThingName}",
                    "2nd_mainpage"
                );

                var collection = new LiveSensoricDataCollection(shadowData, station);

                if (lastData is null || !lastData.Equals(collection))
                {
                    lastData = collection;
                    collection.Print();
                }
                else
                {
                    Console.WriteLine("No new data");
                }

                await Task.Delay(TimeSpan.FromSeconds(25));
            }

            foreach (var device in station.Devices)
            {
                var nextToken = "";
                var lastTime = "0";
                // Pagination: Loop till NextToken is null or empty
                do
                {
                    var sensoricData = await xsenseApiClient.GetSensoricDataAsync(new GetSensoricDataRequest
                    {
                        HouseId = details.HouseId,
                        StationId = station.StationId,
                        DeviceId = device.DeviceId,
                        LastTime = lastTime,
                        NextToken = nextToken // "2024012314060020240201000000"
                    });

                    nextToken = sensoricData.NextToken;
                    lastTime = sensoricData.LastTime;
                } while (!string.IsNullOrWhiteSpace(nextToken));
            }
        }
    }
}

public record LiveSensoricDataDto(Device? Device, string Temperature, string Humidity, string BatInfo);

public class LiveSensoricDataCollection : IEnumerable<LiveSensoricDataDto>
{
    private readonly List<LiveSensoricDataDto> _data = new();

    public LiveSensoricDataCollection()
    {
    }

    public LiveSensoricDataCollection(LiveSensoricData data, Station station)
    {
        Add(data, station);
    }

    public void Add(LiveSensoricData data, Station station)
    {
        foreach (var dev in data.State.Reported.Devs)
        {
            var device = station.Devices.FirstOrDefault(d => string.Equals(d.DeviceSn, dev.Key, StringComparison.OrdinalIgnoreCase));

            var status = dev.Value.Status;
            _data.Add(new LiveSensoricDataDto(device, status.Temperature, status.Humidity, dev.Value.BatInfo));
        }
    }

    public void Print()
    {
        foreach (var item in _data)
        {
            Console.WriteLine($"Device: {item.Device?.DeviceName}, Temperature: {item.Temperature}, Humidity: {item.Humidity}, BatInfo: {item.BatInfo}");
        }
    }

    // Equals
    public override bool Equals(object? obj)
    {
        if (obj is not LiveSensoricDataCollection other)
        {
            return false;
        }

        return Equals(other);
    }

    public bool Equals(LiveSensoricDataCollection? other)
    {
        if (other is null)
        {
            return false;
        }

        if (_data.Count != other._data.Count)
        {
            return false;
        }

        return _data.SequenceEqual(other._data);
    }

    public IEnumerator<LiveSensoricDataDto> GetEnumerator()
    {
        return ((IEnumerable<LiveSensoricDataDto>)_data).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return ((IEnumerable)_data).GetEnumerator();
    }
}