using Amazon.IotData;
using Amazon.IotData.Model;

using System.Collections;
using System.Text.Json;

using XSense.Database;
using XSense.Models.Aws;
using XSense.Models.Init;
using XSense.Models.Sensoric;
using XSense.Models.Sensoric.Live;

namespace XSense;

internal class Program
{
    private static async Task MainX()
    {
        var storage = InMemoryStorage.LoadFromDisk("strorage.json");
        storage.OnCacheUpdated = async s => await s.SaveToDiskAsync("strorage.json");

        var dao = new XDao(storage);

        var xsenseClient = new XSenseHttpClient(new HttpClient());
        var xsenseApiClient = new XSenseApiClient(xsenseClient, dao);

        await xsenseApiClient.LoginAsync("USERNAME", "PASSWORD");

        var houses = await xsenseApiClient.GetHousesAsync();
        var details = await xsenseApiClient.GetHouseDetailsAsync(houses[0].HouseId);

        foreach (var station in details.Stations)
        {
            LiveSensoricDataCollection? lastData = null;

            while (true)
            {
                var shadowData = await xsenseApiClient.PollSensoricDataAsync(station);

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
                    var sensoricData = await xsenseApiClient.GetSensoricHistoryPageAsync(new GetSensoricDataRequest
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

public class LiveSensoricDataMapper
{
    public static IEnumerable<LiveSensoricDataDto> Map(LiveSensoricData data, Station station)
    {
        foreach (var dev in data.State.Reported.Devs)
        {
            var device = station.Devices.FirstOrDefault(d => string.Equals(d.DeviceSn, dev.Key, StringComparison.OrdinalIgnoreCase));

            var status = dev.Value.Status;
            yield return new LiveSensoricDataDto(device, status.Temperature, status.Humidity, dev.Value.BatInfo);
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

    public LiveSensoricDataCollection(List<LiveSensoricDataDto> data)
    {
        AddRange(data);
    }

    public void Add(LiveSensoricData data, Station station)
    {
        //foreach (var dev in data.State.Reported.Devs)
        //{
        //    var device = station.Devices.FirstOrDefault(d => string.Equals(d.DeviceSn, dev.Key, StringComparison.OrdinalIgnoreCase));

        //    var status = dev.Value.Status;
        //    _data.Add(new LiveSensoricDataDto(device, status.Temperature, status.Humidity, dev.Value.BatInfo));
        //}

        var mapped = LiveSensoricDataMapper.Map(data, station);
        _data.AddRange(mapped);
    }

    public void Add(LiveSensoricDataDto data)
    {
        _data.Add(data);
    }

    public void AddRange(IEnumerable<LiveSensoricDataDto> data)
    {
        _data.AddRange(data);
    }

    public void Print()
    {
        foreach (var item in _data)
        {
            //var paddedName = item.Device?.DeviceName.PadRight(20);
            var minLen = 20;
            var paddedName = item.Device?.DeviceName.Length > minLen
                ? item.Device?.DeviceName.Substring(0, minLen)
                : item.Device?.DeviceName.PadRight(minLen);

            Console.WriteLine(
                $"Device: {paddedName} \t" +
                $"Temperature: {item.Temperature} \t" +
                $"Humidity: {item.Humidity} \t" +
                $"BatInfo: {item.BatInfo}");
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

/*
 * Console Application:
 *  - Login: xsense.exe login --username abc --password 123
 *  - GetStations: xsense.exe stations list // Lists all stations of all houses (Optional --houseId)
 *  - Monitor live data: xsense.exe monitor --stationId 123
 *  - Get History: xsense.exe history [--stationId 123] [--deviceId 456] --from 2022-01-01 --to 2022-01-02 [--disable-smart-stop] // Smart stop: Stop when no data is available for 3 consecutive months
 */