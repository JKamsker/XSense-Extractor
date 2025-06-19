FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copy csproj and restore as distinct layers
COPY *.sln .
COPY XSense-Extractor.Cli/*.csproj ./XSense-Extractor.Cli/
COPY XSense-Extractor.Lib/*.csproj ./XSense-Extractor.Lib/
RUN dotnet restore

# Copy everything else and build
COPY . .
WORKDIR /app/XSense-Extractor.Cli
RUN dotnet publish -c Release -o out

# Build runtime image
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app
COPY --from=build /app/XSense-Extractor.Cli/out .
ENTRYPOINT ["dotnet", "XSense-Extractor.Cli.dll"]