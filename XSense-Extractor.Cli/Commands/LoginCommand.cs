using Spectre.Console;
using Spectre.Console.Cli;

using XSenseExtractor;
using XSenseExtractor.Database;

namespace Commands;

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

    private readonly XSenseApiClient _apiClient;

    public LoginCommand(XSenseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.Markup($"Logging in as [Yellow]{settings.Username}[/]...");

        var success = await _apiClient.LoginAsync(settings.Username, settings.Password);

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