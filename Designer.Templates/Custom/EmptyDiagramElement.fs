#nowarn "3391"

namespace StockSharp.Designer

open System
open System.ComponentModel
open System.Collections.Generic
open Ecng.Common
open Ecng.Serialization

open StockSharp.Messages
open StockSharp.Algo
open StockSharp.Diagram
open StockSharp.Diagram.Elements

/// <summary>
/// Sample diagram element demonstrating input and output sockets usage.
///
/// See more details:
/// https://doc.stocksharp.com/topics/designer/strategies/using_code/fsharp/creating_your_own_cube.html
/// </summary>
type EmptyDiagramElement() as this =
    inherit DiagramExternalElement()

    // Example property showing how to create parameters
    let minValueParam =
        this.AddParam<int>("MinValue", 10)
            .SetBasic(true)  // make the parameter visible in basic mode
            .SetDisplay("Parameters", "Min value", "Min value parameter description", 10)

    // Output sockets are events marked with DiagramExternal attribute
    let output1Event = new Event<Unit>()
    let output2Event = new Event<Unit>()

    [<CLIEvent>]
    [<DiagramExternal>]
    member this.Output1 = output1Event.Publish

    [<CLIEvent>]
    [<DiagramExternal>]
    member this.Output2 = output2Event.Publish

    // Uncomment the following property if you want the Process method 
    // to be called every time when a new argument is received
    // (no need to wait for all input args to be received).
    //
    // override this.WaitAllInput 
    //     with get () = false

    // Input sockets are method parameters marked with DiagramExternal attribute

    [<DiagramExternal>]
    member this.Process(candle: CandleMessage, diff: Unit) =
        let res = candle.ClosePrice + diff

        if diff >= minValueParam.Value then
            // Trigger the first output event
            output1Event.Trigger(res)
        else
            // Trigger the second output event
            output2Event.Trigger(res)

    override this.Start() =
        base.Start()
        // Add logic before start if needed

    override this.Stop() =
        base.Stop()
        // Add logic after stop if needed

    override this.Reset() =
        base.Reset()
        // Add logic for resetting internal state if needed
