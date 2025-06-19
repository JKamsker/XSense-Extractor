using Commands;

using JKToolKit.Spectre.AutoCompletion.Completion;
using JKToolKit.Spectre.AutoCompletion.Integrations;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Spectre.Console.Cli;
using Spectre.Console.Extensions.Hosting;

using System.Diagnostics;

using XSenseExtractor.Database;

namespace XSenseExtractor.Cli;

internal class Program
{
    private static async Task<int> Main(string[] args)
    {
        if (args.Length == 0)
        {
            args = new[] { "monitor" };
        }
        else
        {
            args = GetDebugParams(args);
        }

        await Host.CreateDefaultBuilder(args)
           .UseConsoleLifetime(x =>
           {
               x.SuppressStatusMessages = true;
           })
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
        return args;
#endif

        if (!Debugger.IsAttached)
        {
            return args;
        }

        if (args.Length == 0)
        {
            return new[] { "monitor" };
        }

        return args;
    }
}