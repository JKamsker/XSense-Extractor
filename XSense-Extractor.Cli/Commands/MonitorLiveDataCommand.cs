using MQTTnet;
using MQTTnet.Client;
using Spectre.Console;
using Spectre.Console.Cli;
using System.Text.Json;
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

        [CommandOption("--mqtt-server <SERVER>")]
        public string MqttServer { get; set; }

        [CommandOption("--mqtt-port <PORT>")]
        public int MqttPort { get; set; } = 1883;

        [CommandOption("--mqtt-user <USER>")]
        public string MqttUser { get; set; }

        [CommandOption("--mqtt-password <PASSWORD>")]
        public string MqttPassword { get; set; }
    }

    public MonitorLiveDataCommand(XSenseApiClient apiClient)
    {
        _apiClient = apiClient;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var useMqtt = !string.IsNullOrWhiteSpace(settings.MqttServer);
        IMqttClient mqttClient = null;

        if (useMqtt)
        {
            var mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();
            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer(settings.MqttServer, settings.MqttPort)
                .WithCredentials(settings.MqttUser, settings.MqttPassword)
                .Build();
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            AnsiConsole.MarkupLine("[green]Connected to MQTT broker.[/]");
        }

        AnsiConsole.MarkupLine($"Monitoring station...");

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

        var discoveredDevices = new HashSet<string>();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true; // prevent the process from terminating.
            cts.Cancel();
        };

        while (!cts.Token.IsCancellationRequested)
        {
            var data = await PollOnce(stations).ToListAsync(cts.Token);
            var collection = new LiveSensoricDataCollection(data);
            if (!useMqtt)
            {
                collection.Print();
            }
            else
            {
                foreach (var item in data)
                {
                    if (discoveredDevices.Add(item.DeviceId))
                    {
                        await PublishDiscoveryDocuments(mqttClient, item);
                    }
                    await PublishSensorData(mqttClient, item);
                }
            }

            try
            {
                await Task.Delay(TimeSpan.FromSeconds(30), cts.Token);
            }
            catch (TaskCanceledException)
            {
                // Ignore
            }
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
    private async Task PublishDiscoveryDocuments(IMqttClient mqttClient, LiveSensoricDataDto item)
    {
        var deviceIdentifier = $"xsense_{item.DeviceId}";

        // Temperature
        var tempConfigPayload = new
        {
            name = $"{item.DeviceName} Temperature",
            state_topic = $"homeassistant/sensor/{deviceIdentifier}/temperature/state",
            unit_of_measurement = "°C",
            device_class = "temperature",
            unique_id = $"{deviceIdentifier}_temperature",
            device = new { identifiers = new[] { deviceIdentifier }, name = item.DeviceName, manufacturer = "XSense" }
        };
        var tempApplicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"homeassistant/sensor/{deviceIdentifier}/temperature/config")
            .WithPayload(JsonSerializer.Serialize(tempConfigPayload))
            .WithRetainFlag()
            .Build();
        await mqttClient.PublishAsync(tempApplicationMessage, CancellationToken.None);

        // Humidity
        var humidityConfigPayload = new
        {
            name = $"{item.DeviceName} Humidity",
            state_topic = $"homeassistant/sensor/{deviceIdentifier}/humidity/state",
            unit_of_measurement = "%",
            device_class = "humidity",
            unique_id = $"{deviceIdentifier}_humidity",
            device = new { identifiers = new[] { deviceIdentifier }, name = item.DeviceName, manufacturer = "XSense" }
        };
        var humidityApplicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"homeassistant/sensor/{deviceIdentifier}/humidity/config")
            .WithPayload(JsonSerializer.Serialize(humidityConfigPayload))
            .WithRetainFlag()
            .Build();
        await mqttClient.PublishAsync(humidityApplicationMessage, CancellationToken.None);

        // Battery
        var batteryConfigPayload = new
        {
            name = $"{item.DeviceName} Battery",
            state_topic = $"homeassistant/sensor/{deviceIdentifier}/battery/state",
            device_class = "battery",
            unique_id = $"{deviceIdentifier}_battery",
            device = new { identifiers = new[] { deviceIdentifier }, name = item.DeviceName, manufacturer = "XSense" }
        };
        var batteryApplicationMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"homeassistant/sensor/{deviceIdentifier}/battery/config")
            .WithPayload(JsonSerializer.Serialize(batteryConfigPayload))
            .WithRetainFlag()
            .Build();
        await mqttClient.PublishAsync(batteryApplicationMessage, CancellationToken.None);
    }

    private async Task PublishSensorData(IMqttClient mqttClient, LiveSensoricDataDto item)
    {
        var deviceIdentifier = $"xsense_{item.DeviceId}";

        // Temperature
        var tempMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"homeassistant/sensor/{deviceIdentifier}/temperature/state")
            .WithPayload(item.Temperature.ToString())
            .Build();
        await mqttClient.PublishAsync(tempMessage, CancellationToken.None);

        // Humidity
        var humidityMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"homeassistant/sensor/{deviceIdentifier}/humidity/state")
            .WithPayload(item.Humidity.ToString())
            .Build();
        await mqttClient.PublishAsync(humidityMessage, CancellationToken.None);

        // Battery
        var batteryPercentage = item.BatInfo switch
        {
            "0" => 0,
            "1" => 25,
            "2" => 50,
            "3" => 100,
            _ => 0
        };
        var batteryMessage = new MqttApplicationMessageBuilder()
            .WithTopic($"homeassistant/sensor/{deviceIdentifier}/battery/state")
            .WithPayload(batteryPercentage.ToString())
            .Build();
        await mqttClient.PublishAsync(batteryMessage, CancellationToken.None);
    }
}