module AirRecorder.Server.Startup

open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.FileProviders
open Giraffe
open System.IO
open System

type Startup (cfg : IConfiguration, env : IWebHostEnvironment) =
    member _.ConfigureServices (services : IServiceCollection) =
        services
            .AddApplicationInsightsTelemetry(cfg.["APPINSIGHTS_INSTRUMENTATIONKEY"])
            .AddGiraffe()
        |> ignore

        services
            .AddHttpClient("RecordingServer")
            .ConfigureHttpClient(fun (client) ->
                client.BaseAddress <- Uri(cfg.["RecordingServerBaseUrl"])
            )
        |> ignore

    member _.Configure (app : IApplicationBuilder) =
        let config = app.ApplicationServices.GetRequiredService<IConfiguration>()
        let dir = Path.GetFullPath(config.GetValue("Recordings:Directory"))

        app
            .UseStaticFiles()
            .UseStaticFiles(
                StaticFileOptions(
                    FileProvider = new PhysicalFileProvider(dir),
                    RequestPath = "/download",
                    ServeUnknownFileTypes = true
                )
            )
            .UseGiraffe WebApp.webApp
