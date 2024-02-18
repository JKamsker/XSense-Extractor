using Spectre.Console;
using Spectre.Console.Cli;

namespace XSense.Cli;

// Lists stations and houses
internal class GetStationsCommand : AsyncCommand
{
    private readonly XSenseApiClient _apiClient;

    public GetStationsCommand(XSenseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public override async Task<int> ExecuteAsync(CommandContext context)
    {
        var loggedIn = await _apiClient.LoginWithLastUserAsync();
        if (!loggedIn)
        {
            AnsiConsole.MarkupLine("[red]Login failed[/]");
            return 1;
        }

        var houses = await _apiClient.GetHousesAsync();

        foreach (var house in houses)
        {
            var details = await _apiClient.GetHouseDetailsAsync(house.HouseId);

            //AnsiConsole.MarkupLine($"House [yellow]{house.HouseId}[/]");
            Console.WriteLine($"House [{house.HouseId}] {house.HouseName}:");

            foreach (var station in details.Stations)
            {
                //yield return station;
                //AnsiConsole.MarkupLine($"House: [yellow]{house.HouseId}[/] - Station: [green]{station.StationId}[/]");
                // [HouseId] HouseName: [StationId] StationName
                //AnsiConsole.MarkupLine($"[[{house.HouseId}]] {house.HouseName}: [[{station.StationId}]] {station.StationName}");

                //AnsiConsole.MarkupLine($"\t[green]{station.StationId}[/] {station.StationName}");
                Console.WriteLine($"\tStation [{station.StationId}] {station.StationName}");
            }
        }

        return 0;
    }
}