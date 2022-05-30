module AirRecorder.Shared.API
open System

type Port = string

type RecordingServerError =
    | NotAvailable

type RecordingServerStatus =
    | Ready of Port list
    | Recording of DateTimeOffset
    | ServerError of RecordingServerError

type Recording = { Name: string; Size: int64 }

type RecordingSession =
    { Name: string
      Created: DateTime
      Recordings: Recording list }

type Service =
    { GetServerStatus: unit -> Async<RecordingServerStatus>
      StartRecording: Port list -> Async<RecordingServerStatus>
      StopRecording: unit -> Async<RecordingServerStatus>
      ListSessions: unit -> Async<RecordingSession list> }
    static member RouteBuilder _ m = sprintf "/api/service/%s" m
