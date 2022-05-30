module AirRecorder.Client.Components.Recorder

open Feliz
open Elmish
open Feliz.UseElmish
open AirRecorder.Client.Server
open AirRecorder.Shared.API
open AirRecorder.Shared.Errors
open Fable.Core.JS
open System
open Fable.Core

[<Global>]
let console : JS.Console = jsNative

type ServerState =
    | WaitingForServerStatus
    | ServerStatus of Result<RecordingServerStatus, ServerError>

type PortState =
    | Selected
    | NotSelected

    member self.toBool() =
        match self with
        | Selected -> true
        | NotSelected -> false

    static member ofBool(state) =
        match state with
        | true -> Selected
        | false -> NotSelected

type State =
    { ServerState : ServerState
      Ports : (Port * PortState) list }

type Msg =
    | StartRecording of Port list
    | StopRecording
    | ServerStatusRequested
    | ServerStatusReceived of Result<RecordingServerStatus, ServerError>
    | SetPortChecked of Port * PortState

let init () =
    { ServerState = WaitingForServerStatus
      Ports = [] },
    Cmd.ofMsg ServerStatusRequested

let stopRecordingAndUpdateSessionList updateSessionList =
    fun () ->
        async {
            let! result = service.StopRecording()
            updateSessionList ()
            return result
        }

let update updateSessionList (msg : Msg) (state : State) : State * Cmd<Msg> =
    match msg with
    | ServerStatusRequested -> state, Cmd.OfAsync.eitherAsResult service.GetServerStatus ServerStatusReceived
    | StartRecording portList ->
        state, Cmd.OfAsync.eitherAsResult (fun () -> service.StartRecording portList) ServerStatusReceived
    | StopRecording ->
        state, Cmd.OfAsync.eitherAsResult (stopRecordingAndUpdateSessionList updateSessionList) ServerStatusReceived
    | ServerStatusReceived serverStatusResult ->
        let newServerState =
            match serverStatusResult with
            | Ok serverState -> ServerStatus(Ok serverState)
            | Error serverError -> ServerStatus(Error serverError)

        let newPortState =
            match newServerState with
            | ServerStatus (Ok (Ready ports)) ->
                // update portState with checked state from before based on name
                ports
                |> Seq.map (fun (port) ->
                    match List.tryFind (fun (p, selected) -> p = port) state.Ports with
                    | Some (port, selected) -> (port, selected)
                    | None -> (port, NotSelected))
                |> Seq.toList
            | _ -> state.Ports // use old state otherwise

        { state with
            ServerState = newServerState
            Ports = newPortState },
        Cmd.none
    | SetPortChecked (port, isChecked) ->
        let newPortState =
            state.Ports
            |> Seq.map (fun p ->
                let (name, _) = p

                if name = port then
                    (name, isChecked)
                else
                    p)
            |> Seq.toList

        { state with Ports = newPortState }, Cmd.none

[<ReactComponent>]
let Timer (startTime : DateTimeOffset) =
    let (value, setValue) = React.useState (DateTimeOffset.UtcNow - startTime)

    let subscribeToTimer () =
        let subscriptionId =
            setInterval (fun _ -> setValue (DateTimeOffset.UtcNow - startTime)) 1000

        { new IDisposable with
            member this.Dispose () = clearTimeout (subscriptionId) }

    React.useEffect (subscribeToTimer, [||])

    Html.div [
        prop.className "is-size-3"
        prop.text (sprintf "%02d:%02d:%02d" value.Hours value.Minutes value.Seconds)
    ]

[<ReactComponent>]
let View (updateSessionList) =
    let state, dispatch = React.useElmish (init, update updateSessionList, [||])

    let ports =
        state.Ports
        |> Seq.map (fun (portName, isChecked) ->
            Html.div [
                prop.className "field"
                prop.children [
                    Html.label [
                        prop.className "checkbox"
                        prop.children [
                            Html.input [
                                prop.type' "checkbox"
                                prop.isChecked (isChecked.toBool())
                                prop.onChange (fun (isChecked : bool) -> SetPortChecked(portName, PortState.ofBool isChecked) |> dispatch)
                            ]
                            Html.text $" {portName}"
                        ]
                    ]
                ]
            ])
        |> Seq.toList

    let renderServerError error =
        React.fragment [
            Html.h1 "Recording server error"
            Html.div [
                Html.div (sprintf "%A" error)
                Html.button [
                    prop.className "button is-primary is-small mt-4"
                    prop.text "Retry"
                    prop.onClick (fun _ -> ServerStatusRequested |> dispatch)
                ]
            ]
        ]

    match state.ServerState with
    | WaitingForServerStatus -> Html.div "Loading ..."
    | ServerStatus (Error error) -> renderServerError error
    | ServerStatus (Ok status) ->
        match status with
        | Ready _ ->
            React.fragment [
                Html.h1 "Record"
                Html.div [
                    if state.Ports.IsEmpty then
                        Html.p "There are no ports to record from!"
                    else
                        Html.p [
                            prop.className "lead"
                            prop.text "Select the channels you want to record:"
                        ]

                        Html.div [ prop.children ports ]
                    Html.div [
                        prop.className "field is-grouped mt-4"
                        prop.children [
                            Html.div [
                                prop.className "control"
                                prop.children [
                                    Html.button [
                                        prop.className "button is-primary is-small"
                                        prop.text "Start recording"
                                        prop.disabled (not (state.Ports |> List.exists (fun (_, selected) -> selected = Selected)))
                                        prop.onClick (fun _ ->
                                            StartRecording(
                                                state.Ports
                                                |> List.filter (fun (portName, isChecked) -> isChecked = Selected)
                                                |> List.map (fun (a, b) -> a)
                                            )
                                            |> dispatch)
                                    ]

                                    ]
                            ]
                            Html.div [
                                prop.className "control"
                                prop.children [
                                    Html.button [
                                        prop.className "button is-primary is-small is-outlined"
                                        prop.text "Refresh status"
                                        prop.onClick (fun _ -> ServerStatusRequested |> dispatch)
                                    ]
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        | Recording startTime ->
            React.fragment [
                Html.h1 "Recording"
                Html.div [
                    Timer(startTime)
                    Html.button [
                        prop.className "button is-primary is-small mt-4"
                        prop.text "Stop recording"
                        prop.onClick (fun _ -> StopRecording |> dispatch)
                    ]
                ]
            ]
        | ServerError error -> renderServerError error
