namespace StockSharp.Algo.Analytics

open System
open System.Linq
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open Ecng.Common
open Ecng.Drawing
open Ecng.Logging

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
                timeFrame: TimeSpan,
                cancellationToken: CancellationToken
            ) : Task =

            if securities.Length = 0 then
                logs.LogWarning("No instruments.")
                Task.CompletedTask
            else
                // create 2 panes for candles (close prices) and indicator (ROC) series
                let candleChart = panel.CreateChart<DateTimeOffset, decimal>()
                let indicatorChart = panel.CreateChart<DateTimeOffset, decimal>()

                // process each security
                for security in securities do
                    // stop calculation if user cancels script execution
                    if cancellationToken.IsCancellationRequested then
                        ()
                    else
                        // dictionaries to store candle close prices and ROC values
                        let candlesSeries = Dictionary<DateTimeOffset, decimal>()
                        let indicatorSeries = Dictionary<DateTimeOffset, decimal>()

                        // create ROC indicator
                        let roc = RateOfChange()

                        // get candle storage
                        let candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format)

                        // load candles in the specified date range
                        let candles = candleStorage.Load(fromDate, toDate)

                        // fill close-price and indicator series
                        for candle in candles do
                            candlesSeries.[candle.OpenTime] <- candle.ClosePrice
                            // Process each candle in the ROC indicator, get result as decimal
                            indicatorSeries.[candle.OpenTime] <- roc.Process(candle).ToDecimal()

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

                Task.CompletedTask
