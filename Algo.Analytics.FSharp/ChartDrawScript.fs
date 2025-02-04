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
open StockSharp.Algo.Candles
open StockSharp.Messages

/// <summary>
/// The analytic script that shows chart drawing possibilities.
/// </summary>
type ChartDrawScript() =
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
                // Create two charts: lineChart and histogramChart
                let lineChart = panel.CreateChart<DateTimeOffset, decimal>()
                let histogramChart = panel.CreateChart<DateTimeOffset, decimal>()

                // Iterate over each security
                for security in securities do
                    // Stop if user cancels script execution
                    if cancellationToken.IsCancellationRequested then
                        ()
                    else
                        let candlesSeries = Dictionary<DateTimeOffset, decimal>()
                        let volsSeries = Dictionary<DateTimeOffset, decimal>()

                        // Get candle storage for this security
                        let candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format)

                        // Load candles within the specified date range
                        let candles = candleStorage.Load(fromDate, toDate)

                        // Fill dictionaries with close price and volume
                        for candle in candles do
                            candlesSeries.[candle.OpenTime] <- candle.ClosePrice
                            volsSeries.[candle.OpenTime] <- candle.TotalVolume

                        // Draw close prices as a dashed line
                        lineChart.Append(
                            sprintf "%O (close)" security,
                            candlesSeries.Keys,
                            candlesSeries.Values,
                            DrawStyles.DashedLine
                        )

                        // Draw volumes as a histogram
                        histogramChart.Append(
                            sprintf "%O (vol)" security,
                            volsSeries.Keys,
                            volsSeries.Values,
                            DrawStyles.Histogram
                        )

                Task.CompletedTask
