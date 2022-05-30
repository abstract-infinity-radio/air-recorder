module AirRecorder.Server.RecordingServer

open System
open System.Net.Http
open AirRecorder.Shared.API
open System.Text.Json
open System.Text.Json.Serialization

type ServerStatus =
    { [<JsonPropertyName("is_recording")>]
      IsRecording : bool

      [<JsonPropertyName("start_time")>]
      StartTime : Nullable<DateTimeOffset> }

// type PortList = string list

let getServerStatus (httpClient : HttpClient) =
    async {
        let! response = httpClient.GetAsync("status") |> Async.AwaitTask

        let! content =
            response.Content.ReadAsStringAsync()
            |> Async.AwaitTask

        let status = JsonSerializer.Deserialize<ServerStatus>(content)

        let getPortList =
            async {
                let! response = httpClient.GetAsync("list") |> Async.AwaitTask

                let! content =
                    response.Content.ReadAsStringAsync()
                    |> Async.AwaitTask

                return JsonSerializer.Deserialize<string list>(content)
            }

        match status.IsRecording with
        | true -> return Recording status.StartTime.Value
        | false ->
            let! portList = getPortList
            return Ready portList
    }

let startRecording (httpClient : HttpClient) (portList : Port list) =
    async {
        let payload =
            new StringContent(
                JsonSerializer.Serialize({| ports = portList |}),
                System.Text.Encoding.UTF8,
                "application/json"
            )

        let! response =
            httpClient.PostAsync("start_recording", payload)
            |> Async.AwaitTask

        if not response.IsSuccessStatusCode then
            return ServerError NotAvailable
        else
            return! getServerStatus httpClient
    }

let stopRecording (httpClient : HttpClient) =
    async {
        let! response = httpClient.PostAsync("stop_recording", null) |> Async.AwaitTask

        if not response.IsSuccessStatusCode then
            return ServerError NotAvailable
        else
            return! getServerStatus httpClient
    }
