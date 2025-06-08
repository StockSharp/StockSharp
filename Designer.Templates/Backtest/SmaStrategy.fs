#nowarn "3391"

namespace StockSharp.Designer

open System
open System.Collections.Generic

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
/// Sample strategy demonstrating the work with SMA indicators.
/// 
/// See more examples https://github.com/StockSharp/AlgoTrading
/// </summary>
type SmaStrategy() as this =
    inherit Strategy()

    // --- Strategy parameters: CandleType, Long, Short, TakeValue, StopValue ---

    // Parameter for the candle type
    let candleTypeParam =
        this.Param<DataType>(nameof(this.CandleType), DataType.TimeFrame(TimeSpan.FromMinutes 1.0))
            .SetDisplay("Candle type", "Candle type for strategy calculation.", "General")

    // Parameter for the long SMA
    let longParam =
        this.Param<int>(nameof(this.Long), 80)

    // Parameter for the short SMA
    let shortParam =
        this.Param<int>(nameof(this.Short), 30)

    // Parameter for take profit
    let takeValueParam =
        this.Param<Unit>(nameof(this.TakeValue), Unit(0m, UnitTypes.Absolute))

    // Parameter for stop loss
    let stopValueParam =
        this.Param<Unit>(nameof(this.StopValue), Unit(2m, UnitTypes.Percent))

    // Flag to track SMA crossing
    let mutable isShortLessThenLong : bool option = None

    // --------------------- Public properties ---------------------

    /// <summary>The candle type used by the strategy.</summary>
    member this.CandleType
        with get () = candleTypeParam.Value
        and set value = candleTypeParam.Value <- value

    /// <summary>The period for the long SMA indicator.</summary>
    member this.Long
        with get () = longParam.Value
        and set value = longParam.Value <- value

    /// <summary>The period for the short SMA indicator.</summary>
    member this.Short
        with get () = shortParam.Value
        and set value = shortParam.Value <- value

    /// <summary>Take profit value.</summary>
    member this.TakeValue
        with get () = takeValueParam.Value
        and set value = takeValueParam.Value <- value

    /// <summary>Stop loss value.</summary>
    member this.StopValue
        with get () = stopValueParam.Value
        and set value = stopValueParam.Value <- value

    /// <summary>
    /// Determines which securities and data types the strategy uses.
    /// Required for display in the Designer.
    /// </summary>
    override this.GetWorkingSecurities() : IEnumerable<_> =
        seq {
            yield (this.Security, this.CandleType)
        }

    /// <summary>
    /// Resets the strategy to its initial state.
    /// </summary>
    override this.OnReseted() =
        base.OnReseted()
        isShortLessThenLong <- None

    /// <summary>
    /// Initializes and starts the strategy.
    /// </summary>
    override this.OnStarted(time: DateTimeOffset) =
        base.OnStarted(time)

        // ---------- Create indicators ----------
        let longSma = SMA()
        longSma.Length <- this.Long

        let shortSma = SMA()
        shortSma.Length <- this.Short
        // ---------------------------------------

        // ------ Subscribe to the candle flow and bind indicators ------
        let subscription = this.SubscribeCandles(this.CandleType)

        // Bind our indicators to the subscription and assign the processing function
        subscription
            .Bind(longSma, shortSma, fun candle longV shortV -> this.OnProcess(candle, longV, shortV))
            .Start() |> ignore
        // --------------------------------------------------------------

        // ------------- Configure chart -------------
        let area = this.CreateChartArea()

        // area can be null in case there is no GUI (e.g., Runner or console app)
        if not (isNull area) then
            // Draw candles
            this.DrawCandles(area, subscription) |> ignore

            // Draw indicators
            this.DrawIndicator(area, shortSma, System.Drawing.Color.Coral) |> ignore
            this.DrawIndicator(area, longSma) |> ignore

            // Draw own trades
            this.DrawOwnTrades(area) |> ignore
        // -------------------------------------------

        // ------------- Start position protection mechanism (take/stop) -------------
        this.StartProtection(this.TakeValue, this.StopValue)
        // ----------------------------------------------------------------------------

    /// <summary>
    /// Processes each new (finished) candle and new indicator values.
    /// </summary>
    member private this.OnProcess
        (
            candle: ICandleMessage,
            longValue: decimal,
            shortValue: decimal
        ) =
        // Log candle information
        this.LogInfo(
            LocalizedStrings.SmaNewCandleLog,
            candle.OpenTime,
            candle.OpenPrice,
            candle.HighPrice,
            candle.LowPrice,
            candle.ClosePrice,
            candle.TotalVolume,
            candle.SecurityId
        )

        // Skip if the candle is not finished
        if candle.State <> CandleStates.Finished then
            ()
        else
            // Determine if short SMA is less than long SMA
            let shortLess = shortValue < longValue

            match isShortLessThenLong with
            | None ->
                // First time: just remember the current relation
                isShortLessThenLong <- Some shortLess
            | Some prevValue when prevValue <> shortLess ->
                // A crossing has occurred
                // If short < long, that means Sell, otherwise Buy
                let direction =
                    if shortLess then
                        Sides.Sell
                    else
                        Sides.Buy

                // Calculate volume for opening a new position or reversing
                // If there is no position, use Volume; otherwise, double
                // the minimum of the absolute position size and Volume
                let vol =
                    if this.Position = 0m then
                        this.Volume
                    else
                        (abs this.Position |> min this.Volume) * 2m

                // Get price step for the limit price
                let priceStep =
                    let step = this.GetSecurity().PriceStep
                    if step.HasValue then step.Value else 1m

                // Set the limit order price slightly higher/lower than the current close price
                let limitPrice =
                    match direction with
                    | Sides.Buy  -> candle.ClosePrice + priceStep
                    | Sides.Sell -> candle.ClosePrice - priceStep
                    | _          -> candle.ClosePrice // should not occur

                // Send limit order
                match direction with
                | Sides.Buy  -> this.BuyLimit(limitPrice, vol) |> ignore
                | Sides.Sell -> this.SellLimit(limitPrice, vol) |> ignore
                | _          -> ()

                // Update the tracking flag
                isShortLessThenLong <- Some shortLess
            | _ ->
                // Do nothing if no crossing
                ()
