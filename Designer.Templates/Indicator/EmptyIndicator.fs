#nowarn "3391"

namespace StockSharp.Designer

open System
open Ecng.Common
open Ecng.Serialization

open StockSharp.Messages
open StockSharp.Algo
open StockSharp.Algo.Indicators

/// <summary>
/// Sample indicator demonstrating how to save and load parameters.
/// Changes the input price by +20% or -20%.
///
/// See more examples:
/// https://github.com/StockSharp/StockSharp/tree/master/Algo/Indicators
///
/// Documentation:
/// https://doc.stocksharp.com/topics/designer/strategies/using_code/fsharp/create_own_indicator.html
/// </summary>
type EmptyIndicator() as this =
    inherit BaseIndicator()

    // Internal fields
    let mutable changeValue = 20
    let mutable counter = 0
    let mutable isFormedValue = false
    // where the random decisions below come from - hand in a seeded source to repeat a run,
    // or a stub to state in a test what the values will be
    let mutable randomProvider: IRandomProvider = DefaultRandomProvider.Instance

    /// <summary>
    /// The percentage value (+/-) used to modify the input price.
    /// </summary>
    member this.Change
        with get () = changeValue
        and set value =
            changeValue <- value
            this.Reset()

    /// <summary>
    /// The source the random decisions are drawn from.
    /// </summary>
    member this.RandomProvider
        with get () = randomProvider
        and set value = randomProvider <- value

    /// <summary>
    /// Defines if the indicator has formed (became ready for trading).
    /// </summary>
    override this.CalcIsFormed() = isFormedValue

    /// <summary>
    /// Resets the indicator to its initial state.
    /// </summary>
    override this.Reset() =
        base.Reset()
        isFormedValue <- false
        counter <- 0

    /// <summary>
    /// The main logic to process input values.
    /// </summary>
    override this.OnProcess(input: IIndicatorValue) : IIndicatorValue =
        // every 10th call try to return an "empty" value
        if randomProvider.GetInt(0, 10) = 0 then
            // empty value still contains just time, no actual data
            DecimalIndicatorValue(this, input.Time)
        else
            // increment counter on each call
            counter <- counter + 1

            // after 5 inputs, indicator is considered formed
            if counter = 5 then
                isFormedValue <- true

            let mutable value = input.ToDecimal()

            // random change by a factor of +/- Change%
            let randomFactor = decimal (randomProvider.GetInt(-changeValue, changeValue)) / 100m
            value <- value + (value * randomFactor)

            // return final indicator value
            let result = DecimalIndicatorValue(this, value, input.Time)
            // randomly mark it as final or not
            result.IsFinal <- randomProvider.GetBool()
            result

    /// <summary>
    /// Load indicator settings from a given <see cref="SettingsStorage"/>.
    /// </summary>
    override this.Load(storage: SettingsStorage) =
        base.Load(storage)
        this.Change <- storage.GetValue<int>(nameof(this.Change))

    /// <summary>
    /// Save indicator settings to a given <see cref="SettingsStorage"/>.
    /// </summary>
    override this.Save(storage: SettingsStorage) =
        base.Save(storage)
        storage.SetValue(nameof(this.Change), this.Change)

    /// <summary>
    /// A string representation that includes the current <see cref="Change"/> value.
    /// </summary>
    override this.ToString() =
        sprintf "Change: %d" this.Change
