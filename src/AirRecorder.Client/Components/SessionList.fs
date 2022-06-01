module AirRecorder.Client.Components.SessionList

open Feliz
open Elmish
open Feliz.UseDeferred
open AirRecorder.Client.Server
open AirRecorder.Shared.API

[<ReactComponent>]
let View (timestamp) =
    let data = React.useDeferred (service.ListSessions(), [| timestamp |])

    let renderRecording (sessionName : string) (recording : Recording) =
        let audioUrl = sprintf "%A/%A/%A" "/download" sessionName recording.Name

        Html.div [
            prop.className "recording"
            prop.children [
                Html.div [
                    prop.className "recording__meta"
                    prop.children [
                        Html.div [
                            prop.className "recording__name"
                            prop.text recording.Name
                        ]
                        Html.div [
                            prop.className "recording__size nowrap"
                            prop.text (sprintf "%A kB" (recording.Size / 1024L))
                        ]
                    ]
                ]
                Html.audio [
                    prop.className "recording__audio mt-2"
                    prop.controls true
                    prop.children [
                        Html.source [ prop.src audioUrl ]
                    ]
                ]
            ]
        ]

    let renderSession (session : RecordingSession) =
        Html.div [
            prop.className "session mb-5"
            prop.children [
                Html.h2 [
                    prop.className "is-size-4 mb-1"
                    prop.text $"""{session.Created.ToString("dd. MM. yyyy HH:mm")}"""
                ]
                Html.div [
                    prop.className "recordings"
                    prop.children (
                        session.Recordings
                        |> List.filter (fun i -> not (i.Name.EndsWith ".zip"))
                        |> List.map (renderRecording session.Name)

                    )
                ]
                Html.div [
                    prop.className "mt-4"
                    prop.children [
                        Html.a [
                            prop.href (sprintf "%A/%A/%A" "/download" session.Name "bundle.zip")
                            prop.text "Download bundle"
                        ]
                    ]
                ]
            ]
        ]

    match data with
    | Deferred.HasNotStartedYet -> Html.none
    | Deferred.InProgress -> Html.none
    | Deferred.Failed error -> Html.div error.Message
    | Deferred.Resolved sessions -> Html.div (sessions |> List.map renderSession)
