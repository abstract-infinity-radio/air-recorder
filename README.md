B-AIR recorder is part of AIR Infinity Radio project and is used to control the process of recording arbitrary JACK inputs and provide downloadable files of recording sessions.

# Development setup

1. Make sure that session recordings directory exists and is properly configured in `appsettings.json`
1. dotnet run

# Deploying

1. Use `dotnet run publish` to publish the binaries.
2. The published output will be contained in the `publish/app` directory which you can deploy to the server
3. Ensure that dotnet and aspnetcore runtimes version 6 are installed on the server
4. Run with `dotnet AirRecorder.Server.dll`