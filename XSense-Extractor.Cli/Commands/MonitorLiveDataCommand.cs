using Spectre.Console;
using Spectre.Console.Cli;
using XSenseExtractor;
using XSenseExtractor.Models.Init;

namespace Commands;

internal class MonitorLiveDataCommand : AsyncCommand<MonitorLiveDataCommand.Settings>
{
    private readonly XSenseApiClient _apiClient;

    public class Settings : CommandSettings
    {
        [CommandOption("--stationId <STATIONID>")]
        public string StationId { get; set; }

        [CommandOption("--houseId <HOUSEID>")]
        public string HouseId { get; set; }
    }

    public MonitorLiveDataCommand(XSenseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        // Implement monitor live data logic here
        AnsiConsole.MarkupLine($"Monitoring station [green]{settings.StationId}[/]...");

        var loggedIn = await _apiClient.LoginWithLastUserAsync();
        if (!loggedIn)
        {
            AnsiConsole.MarkupLine("[red]Login failed[/]");
            return 1;
        }

        var stations = await GetStations(settings).ToListAsync();

        Console.WriteLine("Monitoring live data...");
        Console.WriteLine("Press Ctrl+C to exit");
        Console.WriteLine("Stations: " + string.Join(", ", stations.Select(s => s.StationId)));

        while (true)
        {
            var data = await PollOnce(stations).ToListAsync();
            var collection = new LiveSensoricDataCollection(data);
            collection.Print();
            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        return 0;
    }

    private async IAsyncEnumerable<LiveSensoricDataDto> PollOnce()
    {
        var houses = await _apiClient.GetHousesAsync();
        foreach (var house in houses)
        {
            var details = await _apiClient.GetHouseDetailsAsync(house.HouseId);

            foreach (var station in details.Stations)
            {
                var shadowData = await _apiClient.PollSensoricDataAsync(station);

                var mapped = LiveSensoricDataMapper.Map(shadowData, station);
                foreach (var item in mapped)
                {
                    yield return item;
                }
            }
        }
    }

    private async IAsyncEnumerable<LiveSensoricDataDto> PollOnce(IEnumerable<Station> station)
    {
        foreach (var s in station)
        {
            var shadowData = await _apiClient.PollSensoricDataAsync(s);

            var mapped = LiveSensoricDataMapper.Map(shadowData, s);
            foreach (var item in mapped)
            {
                yield return item;
            }
        }
    }

    private async IAsyncEnumerable<Station> GetStations(Settings settings)
    {
        var houses = await _apiClient.GetHousesAsync();

        foreach (var house in houses)
        {
            if (!string.IsNullOrWhiteSpace(settings.HouseId) && !string.Equals(house.HouseId, settings.HouseId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var details = await _apiClient.GetHouseDetailsAsync(house.HouseId);
            foreach (var station in details.Stations)
            {
                if (!string.IsNullOrWhiteSpace(settings.StationId) && !string.Equals(station.StationId, settings.StationId, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                yield return station;
            }
        }
    }
}