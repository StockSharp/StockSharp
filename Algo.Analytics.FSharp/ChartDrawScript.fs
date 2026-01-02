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
                dataType: DataType,
                cancellationToken: CancellationToken
            ) : Task =

            task {
                if securities.Length = 0 then
                    logs.LogWarning("No instruments.")
                else
                    // Create two charts: lineChart and histogramChart
                    let lineChart = panel.CreateChart<DateTime, decimal>()
                    let histogramChart = panel.CreateChart<DateTime, decimal>()
                    let mutable idx = 0

                    // Iterate over each security
                    for security in securities do
                        // Stop if user cancels script execution
                        if not cancellationToken.IsCancellationRequested then
                            idx <- idx + 1
                            logs.LogInfo("Processing {0} of {1}: {2}...", idx, securities.Length, security)

                            let candlesSeries = Dictionary<DateTime, decimal>()
                            let volsSeries = Dictionary<DateTime, decimal>()

                            // Get candle storage for this security
                            let candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format)
                            let mutable prevDate = DateOnly.MinValue

                            // Load candles and fill dictionaries
                            do! candleStorage.LoadAsync(fromDate, toDate)
                                |> TaskSeq.iter (fun candle ->
                                    let currDate = DateOnly.FromDateTime(candle.OpenTime.Date)
                                    if currDate <> prevDate then
                                        prevDate <- currDate
                                        logs.LogInfo("  {0}...", currDate)

                                    candlesSeries.[candle.OpenTime] <- candle.ClosePrice
                                    volsSeries.[candle.OpenTime] <- candle.TotalVolume
                                )

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
            }
