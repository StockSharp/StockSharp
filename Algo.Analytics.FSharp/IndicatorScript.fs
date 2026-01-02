namespace StockSharp.Algo.Analytics

open System
open System.Linq
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open Ecng.Common
open Ecng.Drawing
open Ecng.Logging

open FSharp.Control

open StockSharp.Algo.Analytics
open StockSharp.Algo.Storages
open StockSharp.Algo.Indicators
open StockSharp.Algo.Candles
open StockSharp.Messages

/// <summary>
/// The analytic script, using indicator ROC (Rate of Change).
/// </summary>
type IndicatorScript() =
    interface IAnalyticsScript with
        member this.Run
            (
                logs: ILogReceiver,
                panel: IAnalyticsPanel,
                securities: SecurityId[],
                fromDate: DateTime,
                toDate: DateTime,
                storage: IStorageRegistry,
                drive: IMarketDataDrive,
                format: StorageFormats,
                dataType: DataType,
                cancellationToken: CancellationToken
            ) : Task =

            task {
                if securities.Length = 0 then
                    logs.LogWarning("No instruments.")
                else
                    // create 2 panes for candles (close prices) and indicator (ROC) series
                    let candleChart = panel.CreateChart<DateTime, decimal>()
                    let indicatorChart = panel.CreateChart<DateTime, decimal>()
                    let mutable idx = 0

                    // process each security
                    for security in securities do
                        // stop calculation if user cancels script execution
                        if not cancellationToken.IsCancellationRequested then
                            idx <- idx + 1
                            logs.LogInfo("Processing {0} of {1}: {2}...", idx, securities.Length, security)

                            // dictionaries to store candle close prices and ROC values
                            let candlesSeries = Dictionary<DateTime, decimal>()
                            let indicatorSeries = Dictionary<DateTime, decimal>()

                            // create ROC indicator
                            let roc = RateOfChange()

                            // get candle storage
                            let candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format)
                            let mutable prevDate = DateOnly.MinValue

                            // load candles and fill series
                            do! candleStorage.LoadAsync(fromDate, toDate)
                                |> TaskSeq.iter (fun candle ->
                                    let currDate = DateOnly.FromDateTime(candle.OpenTime.Date)
                                    if currDate <> prevDate then
                                        prevDate <- currDate
                                        logs.LogInfo("  {0}...", currDate)

                                    candlesSeries.[candle.OpenTime] <- candle.ClosePrice
                                    // Process each candle in the ROC indicator, get result as decimal
                                    indicatorSeries.[candle.OpenTime] <- roc.Process(candle).ToDecimal()
                                )

                            // draw close prices on candleChart
                            candleChart.Append(
                                sprintf "%O (close)" security,
                                candlesSeries.Keys,
                                candlesSeries.Values
                            )

                            // draw ROC values on indicatorChart
                            indicatorChart.Append(
                                sprintf "%O (ROC)" security,
                                indicatorSeries.Keys,
                                indicatorSeries.Values
                            )
            }
