using Spectre.Console;
using Spectre.Console.Cli;

using System.Globalization;
using System.Text;

using XSense.Models.Aggregates;
using XSense.Models.Init;

using static System.Collections.Specialized.BitVector32;

namespace XSense.Cli;

internal class GetHistoryCommand : AsyncCommand<GetHistoryCommand.Settings>
{
    private readonly XSenseApiClient _apiClient;
    private Settings _settings;

    public class Settings : CommandSettings
    {
        [CommandOption("--houseId <STATIONID>")]
        public string? HouseId { get; set; }

        [CommandOption("--stationId <STATIONID>")]
        public string? StationId { get; set; }

        [CommandOption("--deviceId <DEVICEID>")]
        public string? DeviceId { get; set; }

        [CommandOption("--from <FROM>")]
        public string? From { get; set; }

        [CommandOption("--to <TO>")]
        public string? To { get; set; }

        [CommandOption("--disable-smart-stop")]
        public bool DisableSmartStop { get; set; }

        //output file
        [CommandOption("--output <OUTPUT>")]
        public string? Output { get; set; }
    }

    public GetHistoryCommand(XSenseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        _settings = settings;

        var loggedIn = await _apiClient.LoginWithLastUserAsync();
        if (!loggedIn)
        {
            AnsiConsole.MarkupLine("[red]Login failed[/]");
            return 1;
        }

        if (!string.IsNullOrEmpty(settings.Output))
        {
            await WriteToCsv(settings);
        }
        else
        {
            await DisplayLineByLine(settings);
        }

        return 0;
    }

    private async Task DisplayLineByLine(Settings settings)
    {
        await foreach (var (house, station, device, time, temperature, humidity) in EnumerateSensoricHistoryAsync(settings))
        {
            AnsiConsole.MarkupLine($"[yellow]{time:yyyy-MM-dd HH:mm:ss}[/] [green]{temperature}°C[/] [blue]{humidity}%[/]");
        }
    }

    private async Task WriteToCsv(Settings settings)
    {
        //var csv = new StringBuilder();
        using var fs = new FileStream(settings.Output, FileMode.Create);
        using var csv = new StreamWriter(fs, Encoding.UTF8);

        //await csv.WriteLineAsync("House,Station,Device,Time,Temperature,Humidity");
        await csv.WriteLineAsync("House;Station;Device;Time;Temperature;Humidity");

        await foreach (var (house, station, device, time, temperature, humidity) in EnumerateSensoricHistoryAsync(settings))
        {
            //await csv.WriteLineAsync($"{house.Name},{station.StationName},{device.DeviceName},{time:yyyy-MM-dd HH:mm:ss},{temperature:0.0},{humidity:0.0}");
            //;
            await csv.WriteLineAsync($"{house.Name};{station.StationName};{device.DeviceName};{time:yyyy-MM-dd HH:mm:ss};{temperature:0.0};{humidity:0.0}");
        }

        //File.WriteAllText(settings.Output, csv.ToString());
    }

    private async Task DisplayInTable(Settings settings)
    {
        // LiveTable: HouseName, StationName, DeviceName, Time, Temperature, Humidity
        var table = new Table();
        table.AddColumn("House");
        table.AddColumn("Station");
        table.AddColumn("Device");
        table.AddColumn("Time");
        table.AddColumn("Temperature");
        table.AddColumn("Humidity");

        var display = new LiveDisplay(AnsiConsole.Console, table);
        await display.StartAsync(async ctx =>
        {
            var houses = await _apiClient.GetHousesAsync();
            foreach (var house in houses)
            {
                if (!ShouldDisplayHouse(house))
                {
                    continue;
                }

                var details = await _apiClient.GetHouseDetailsAsync(house);
                foreach (var station in details.Stations)
                {
                    if (!ShouldDisplayStation(station))
                    {
                        continue;
                    }

                    foreach (var device in station.Devices)
                    {
                        if (!ShouldDisplayDevice(device))
                        {
                            continue;
                        }

                        var i = 0;
                        await foreach (var dataPoint in _apiClient.EnumerateSensoricHistoryAsync(details, station, device, !settings.DisableSmartStop))
                        {
                            table.AddRow(
                                house.HouseName,
                                station.StationName,
                                device.DeviceName,
                                dataPoint.Time.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                                dataPoint.Temperature.ToString("0.0", CultureInfo.InvariantCulture),
                                dataPoint.Humidity.ToString("0.0", CultureInfo.InvariantCulture)
                            );

                            if (++i % 10 == 0)
                            {
                                ctx.UpdateTarget(table);
                                //await
                            }
                        }
                    }
                }
            }
        });
    }

    private async IAsyncEnumerable<LiveMetricsDataPointEx> EnumerateSensoricHistoryAsync(Settings settings)
    {
        var houses = await _apiClient.GetHousesAsync();
        foreach (var house in houses)
        {
            if (!ShouldDisplayHouse(house))
            {
                continue;
            }

            var details = await _apiClient.GetHouseDetailsAsync(house);

            foreach (var station in details.Stations)
            {
                if (!ShouldDisplayStation(station))
                {
                    continue;
                }

                foreach (var device in station.Devices)
                {
                    if (!ShouldDisplayDevice(device))
                    {
                        continue;
                    }

                    await foreach (var dataPoint in _apiClient.EnumerateSensoricHistoryAsync(details, station, device, !settings.DisableSmartStop))
                    {
                        yield return new LiveMetricsDataPointEx(details, station, device, dataPoint.Time, dataPoint.Temperature, dataPoint.Humidity);
                    }
                }
            }
        }
    }

    private bool ShouldDisplayHouse(House house)
    {
        if (string.IsNullOrEmpty(_settings.HouseId))
        {
            return true;
        }

        return string.Equals(house.HouseId, _settings.HouseId, StringComparison.InvariantCulture);
    }

    private bool ShouldDisplayStation(Station station)
    {
        if (string.IsNullOrEmpty(_settings.StationId))
        {
            return true;
        }

        return string.Equals(station.StationId, _settings.StationId, StringComparison.InvariantCulture);
    }

    private bool ShouldDisplayDevice(Device device)
    {
        if (string.IsNullOrEmpty(_settings.DeviceId))
        {
            return true;
        }

        return string.Equals(device.DeviceId, _settings.DeviceId, StringComparison.InvariantCulture);
    }
}

record LiveMetricsDataPointEx
(
    HouseDetailAggregate House,
    Station Station,
    Device Device,
    DateTime Time,
    double Temperature,
    double Humidity
) : LiveMetricsDataPoint(Time, Temperature, Humidity)
{
    public LiveMetricsDataPointEx(
        HouseDetailAggregate house,
        Station station,
        Device device,
        LiveMetricsDataPoint dataPoint
    ) : this(house, station, device, dataPoint.Time, dataPoint.Temperature, dataPoint.Humidity
    )
    {
        this.House = house;
        this.Station = station;
        this.Device = device;
    }
}