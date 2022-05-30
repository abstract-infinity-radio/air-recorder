module AirRecorder.Client.View

open Feliz
open AirRecorder.Client.Components
open System

[<ReactComponent>]
let AppView () =
    let (timestamp, setTimestamp) = React.useState (DateTimeOffset.UtcNow)

    let updateSessionList () = setTimestamp (DateTimeOffset.UtcNow)

    Html.div [
        prop.className "columns"
        prop.children [
            Html.div [
                prop.className "column is-6-tablet"
                prop.children [
                    Recorder.View(updateSessionList)
                ]
            ]
            Html.div [
                prop.className "column is-6-tablet"
                prop.children [
                    Html.h1 "Listen"
                    SessionList.View(timestamp)
                ]
            ]
        ]
    ]
