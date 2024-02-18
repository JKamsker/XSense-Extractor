using JKToolKit.Spectre.AutoCompletion.Completion;
using JKToolKit.Spectre.AutoCompletion.Integrations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Spectre.Console;
using Spectre.Console.Cli;
using Spectre.Console.Extensions.Hosting;

using XSense.Database;
using XSense.Models.Init;

namespace XSense.Cli;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        args = GetDebugParams(args);

        await Host.CreateDefaultBuilder(args)
           .UseConsoleLifetime()
           .ConfigureServices((_, services) =>
            {
                services.AddSingleton<IStorage>(InMemoryStorage.LoadFromDisk("storage.json", false));
                services.AddSingleton<XDao>();

                services.AddSingleton<HttpClient>();
                services.AddSingleton<XSenseHttpClient>();
                services.AddSingleton<XSenseApiClient>();
            })
           .UseSpectreConsole(config =>
           {
               config.UseArgs(args);
#if DEBUG
               config.PropagateExceptions();
               config.ValidateExamples();
#endif

               //config.AddCommand<AddCommand>("add");
               //config.AddCommand<CommitCommand>("commit");
               //config.AddCommand<RebaseCommand>("rebase");

               config.AddAutoCompletion(x => x.AddPowershell());

               config.AddCommand<LoginCommand>("login")
                   .WithDescription("Logs into the system")
                   .WithExample(new string[] { "login", "--username", "abc", "--password", "123" })
                   //.WithOption("username", 'u', "The username")
                   //.WithOption("password", 'p', "The password")
                   ;

               config.AddCommand<GetStationsCommand>("stations")
                   .WithDescription("Lists all stations of all houses")
                   //.WithExample(new[] { "stations", "list" })
                   //.AddAlias("list")
                   //.WithOption("houseId", 'h', "Optional house ID to filter stations")
                   ;

               config.AddCommand<MonitorLiveDataCommand>("monitor")
                   .WithDescription("Monitor live data for a station")
                   //.WithExample(new[] { "monitor", "--stationId", "123" })
                   //.WithOption("stationId", 's', "The station ID to monitor")
                   ;

               config.AddCommand<GetHistoryCommand>("history")
                   .WithDescription("Get history for a station or device within a date range")
                   //.WithExample(new[] { "history", "--stationId", "123", "--from", "2022-01-01", "--to", "2022-01-02" })
                   //.WithOption("stationId", 's', "Optional station ID")
                   //.WithOption("deviceId", 'd', "Optional device ID")
                   //.WithOption("from", 'f', "Start date for the history")
                   //.WithOption("to", 't', "End date for the history")
                   //.WithOption("disable-smart-stop", 'n', "Disable smart stop feature", false);
                   ;
           })
           .RunConsoleAsync();
        return Environment.ExitCode;
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
            // return new[] { "login", "--username", "USERNAME", "--password", "PASSWORD" };

            //MonitorLiveDataCommand
            //return new[] { "monitor" };

            // gets all stations
            return new[] { "stations" };
        }

        return args;
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