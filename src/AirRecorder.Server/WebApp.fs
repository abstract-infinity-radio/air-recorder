module AirRecorder.Server.WebApp

open AirRecorder.Shared.API
open Fable.Remoting.Giraffe
open Fable.Remoting.Server
open Giraffe
open Giraffe.GoodRead
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Logging
open System.IO
open System.Net.Http

let service baseDirectory (httpClient: HttpClient) =
    { GetServerStatus = fun () -> RecordingServer.getServerStatus httpClient
      StartRecording = fun portList -> RecordingServer.startRecording httpClient portList
      StopRecording = fun () -> RecordingServer.stopRecording httpClient
      ListSessions = fun () ->
        async {
            return
                Directory.EnumerateDirectories(baseDirectory)
                |> Seq.map (fun directoryName ->
                    let dirInfo = DirectoryInfo(directoryName)

                    let recordings =
                        DirectoryInfo(directoryName).EnumerateFiles()
                        |> Seq.sortBy (fun x -> x.Name)
                        |> Seq.map (fun file ->
                            { Name = file.Name
                              Size = file.Length })
                        |> Seq.toList

                    { Name = dirInfo.Name
                      Created = dirInfo.CreationTime
                      Recordings = recordings })
                |> Seq.sortByDescending (fun recordingSession -> recordingSession.Created)
                |> Seq.toList
        } }

let createServiceFromContext (httpContext : HttpContext) =
    let baseDirectory =
        httpContext
            .GetService<IConfiguration>()
            .GetValue("Recordings:Directory")

    let absPath = Path.GetFullPath(baseDirectory)

    let httpClient = httpContext.GetService<IHttpClientFactory>().CreateClient("RecordingServer")

    service absPath httpClient



let webApp : HttpHandler =
    let remoting logger =
        Remoting.createApi ()
        |> Remoting.withRouteBuilder Service.RouteBuilder
        // |> Remoting.fromValue service
        |> Remoting.fromContext createServiceFromContext
        |> Remoting.withErrorHandler (Remoting.errorHandler logger)
        |> Remoting.buildHttpHandler

    choose [
        Require.services<ILogger<_>> remoting
    ]
