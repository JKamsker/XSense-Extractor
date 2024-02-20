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

            AnsiConsole.MarkupLine($"[bold]House[/] [underline]{house.HouseId}[/]");
            var table = new Table();

            // Configure table headers
            table.AddColumn("[bold]Station Name[/]");
            table.AddColumn("[bold]Station ID[/]");
            table.AddColumn("[bold]Device Name[/]");
            table.AddColumn("[bold]Device ID[/]");

            foreach (var station in details.Stations)
            {
                // This will track if we've added the first device for a station
                // to avoid repeating the station info for each device
                bool firstDevice = true;

                foreach (var device in station.Devices)
                {
                    if (firstDevice)
                    {
                        table.AddRow(station.StationName, station.StationId, device.DeviceName, device.DeviceId);
                        firstDevice = false;
                    }
                    else
                    {
                        // For additional devices, we don't repeat the station info
                        table.AddRow("", "", device.DeviceName, device.DeviceId);
                    }
                }

                // If a station has no devices, we still want to list the station
                if (station.Devices.Length == 0)
                {
                    table.AddRow(station.StationName, station.StationId, "-", "-");
                }
            }

            // Style the table
            table.Border(TableBorder.Rounded);
            table.Title($"[underline bold]Devices in House {house.HouseId}[/]");
            AnsiConsole.Write(table);

            AnsiConsole.WriteLine(); // Add a blank line for better readability between houses
        }

        return 0;
    }
}