#nowarn "3391"

namespace StockSharp.Designer

open System

open Ecng.Common
open Ecng.Logging
open Ecng.Drawing

open StockSharp.Messages
open StockSharp.Algo
open StockSharp.Algo.Strategies
open StockSharp.Algo.Indicators
open StockSharp.BusinessEntities
open StockSharp.Localization
open StockSharp.Charting

/// <summary>
/// Empty strategy.
///
/// See more examples at:
/// https://github.com/StockSharp/AlgoTrading
/// </summary>
type EmptyStrategy() as this =
    inherit Strategy()

    // Create a parameter, similar to _intParam in C#.
    let intParam =
        this.Param<int>(nameof this.IntParam, 80)

    /// <summary>
    /// Integer parameter, analogous to the C# property IntParam.
    /// </summary>
    member this.IntParam
        with get () = intParam.Value
        and set value = intParam.Value <- value

    /// <summary>
    /// Overridden method to log information when the strategy is started.
    /// </summary>
    override this.OnStarted(time: DateTimeOffset) =
        this.LogInfo(nameof this.OnStarted)
        base.OnStarted(time)
