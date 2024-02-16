using System;

using JKToolKit.Spectre.AutoCompletion.Completion;
using JKToolKit.Spectre.AutoCompletion.Integrations;

using Spectre.Console;
using Spectre.Console.Cli;

namespace XSense.Cli;

internal class Program
{
    private static int Main(string[] args)
    {
        args = GetDebugParams(args);

        var app = new CommandApp();

        app.Configure(config =>
        {
            config.AddAutoCompletion(x => x.AddPowershell());

            config.PropagateExceptions();

            config.AddCommand<LoginCommand>("login")
                .WithDescription("Logs into the system")
                .WithExample(new[] { "login", "--username", "abc", "--password", "123" })
                //.WithOption("username", 'u', "The username")
                //.WithOption("password", 'p', "The password")
                ;

            config.AddCommand<GetStationsCommand>("stations")
                .WithDescription("Lists all stations of all houses")
                .WithExample(new[] { "stations", "list" })
                //.AddAlias("list")
                //.WithOption("houseId", 'h', "Optional house ID to filter stations")
                ;

            config.AddCommand<MonitorLiveDataCommand>("monitor")
                .WithDescription("Monitor live data for a station")
                .WithExample(new[] { "monitor", "--stationId", "123" })
                //.WithOption("stationId", 's', "The station ID to monitor")
                ;

            config.AddCommand<GetHistoryCommand>("history")
                .WithDescription("Get history for a station or device within a date range")
                .WithExample(new[] { "history", "--stationId", "123", "--from", "2022-01-01", "--to", "2022-01-02" })
                //.WithOption("stationId", 's', "Optional station ID")
                //.WithOption("deviceId", 'd', "Optional device ID")
                //.WithOption("from", 'f', "Start date for the history")
                //.WithOption("to", 't', "End date for the history")
                //.WithOption("disable-smart-stop", 'n', "Disable smart stop feature", false);
                ;
        });

        return app.Run(args);
    }

    private static string[] GetDebugParams(string[] args)
    {
#if !DEBUG
        return Array.Empty<string>();
#endif

        if (args.Length == 0)
        {
            //return new[] { "login", "--username", "abc", "--password", "123" };
            //return new[] { "login", "--username", "USERNAME", "--password", "123" };
            return new[] { "login", "--username", "USERNAME", "--password", "PASSWORD" };
        }

        return args;
    }
}

internal class LoginCommand : AsyncCommand<LoginCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--username <USERNAME>")]
        public string Username { get; set; }

        [CommandOption("--password <PASSWORD>")]
        public string Password { get; set; }

        public override ValidationResult Validate()
        {
            if (string.IsNullOrWhiteSpace(Username))
            {
                return ValidationResult.Error("Username is required");
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                return ValidationResult.Error("Password is required");
            }

            return base.Validate();
        }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var storage = InMemoryStorage.LoadFromDisk("strorage.json");
        storage.OnCacheUpdated = async s => await s.SaveToDiskAsync("strorage.json");

        var xsenseClient = new XSenseHttpClient(new HttpClient());
        var xsenseApiClient = new XSenseApiClient(xsenseClient, storage);

        AnsiConsole.Markup($"Logging in as [Yellow]{settings.Username}[/]...");

        var success = await xsenseApiClient.LoginAsync(settings.Username, settings.Password);

        if (success)
        {
            AnsiConsole.MarkupLine($"[green]Success![/]");
            return 0;
        }
        else
        {
            AnsiConsole.MarkupLine("[red]Login failed[/]");
            return 1;
        }
    }
}

internal class GetStationsCommand : Command
{
    public override int Execute(CommandContext context)
    {
        // Implement get stations logic here
        AnsiConsole.MarkupLine("Listing all stations...");
        return 0;
    }
}

internal class MonitorLiveDataCommand : Command<MonitorLiveDataCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--stationId <STATIONID>")]
        public int StationId { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        // Implement monitor live data logic here
        AnsiConsole.MarkupLine($"Monitoring station [green]{settings.StationId}[/]...");
        return 0;
    }
}

internal class GetHistoryCommand : Command<GetHistoryCommand.Settings>
{
    public class Settings : CommandSettings
    {
        [CommandOption("--stationId <STATIONID>")]
        public int? StationId { get; set; }

        [CommandOption("--deviceId <DEVICEID>")]
        public int? DeviceId { get; set; }

        [CommandOption("--from <FROM>")]
        public string From { get; set; }

        [CommandOption("--to <TO>")]
        public string To { get; set; }

        [CommandOption("--disable-smart-stop")]
        public bool DisableSmartStop { get; set; }
    }

    public override int Execute(CommandContext context, Settings settings)
    {
        // Implement get history logic here
        AnsiConsole.MarkupLine($"Getting history from [green]{settings.From}[/] to [green]{settings.To}[/]...");
        return 0;
    }
}