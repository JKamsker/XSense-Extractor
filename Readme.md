<!-- Enhance readme: 
For github.
Tool is for extracting data from XSense sensors (currently only for Temperature and Humidity sensors). -->

# XSenseExtractor

`XSenseExtractor` is a .NET Tool to extract data from XSense sensors. <br/>
It provides a command-line interface to interact with XSense, allowing you to retrieve data from the sensors and display it in human and machine-readable formats.


## Installation and Usage

`XSenseExtractor` can be easily installed and updated as a .NET Global Tool, allowing you to run it from anywhere on your system. Below are the instructions for installation, updating, and basic usage.

### Prerequisites

- .NET 8: Make sure you have the .NET 8 installed on your machine. You can download it from [the official .NET website](https://dot.net/download).

### Installing XSenseExtractor

To install `XSenseExtractor` as a global tool, open your terminal and run the following command:

```shell
dotnet tool install --global XSense-Extractor
```

This command will download and install the latest version of `XSenseExtractor`, making it globally available from the command line.

### Updating XSenseExtractor

If you already have `XSenseExtractor` installed and want to update it to the latest version, use the following command:

```shell
dotnet tool update XSense-Extractor -g
```

This will check for the latest version of the tool and update it accordingly.

## Usage

### Login

Authenticate a user with a username and password.

```shell
xsense login --username <USERNAME> --password <PASSWORD>
```
### List Stations

Display all stations and their devices.

```shell
xsense stations
```

### Monitor Live Data

Monitor real-time data from a specific station.

```shell
xsense monitor --stationId <STATION_ID> [--houseId <HOUSE_ID>]
```

### Retrieve Historical Data

Get historical data for a station or device within a date range.

```shell
xsense history --stationId <STATION_ID> [--houseId <HOUSE_ID>] [--deviceId <DEVICE_ID>] --from <START_DATE> --to <END_DATE>
```

##

</br>
<p align="center">
Made with <span style="color: #e25555;">&hearts;</span> in Austria <img src="https://images.emojiterra.com/google/noto-emoji/v2.034/128px/1f1e6-1f1f9.png" width="20" height="20"/> 
</p>