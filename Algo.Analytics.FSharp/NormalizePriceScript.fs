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
/// The analytic script, normalize securities close prices and shows on the same chart.
/// </summary>
type NormalizePriceScript() =
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
                    // Create a chart for normalized close prices
                    let chart = panel.CreateChart<DateTime, decimal>()
                    let mutable idx = 0

                    // Iterate over each security
                    for security in securities do
                        // Stop if user cancels execution
                        if not cancellationToken.IsCancellationRequested then
                            idx <- idx + 1
                            logs.LogInfo("Processing {0} of {1}: {2}...", idx, securities.Length, security)

                            // Dictionary to store time -> normalized close price
                            let series = Dictionary<DateTime, decimal>()

                            // Get candle storage for this security
                            let candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format)

                            // We'll store the first close price in a mutable option
                            let mutable firstClose: decimal option = None
                            let mutable prevDate = DateOnly.MinValue

                            // Load candles and normalize close prices
                            do! candleStorage.LoadAsync(fromDate, toDate)
                                |> TaskSeq.iter (fun candle ->
                                    let currDate = DateOnly.FromDateTime(candle.OpenTime.Date)
                                    if currDate <> prevDate then
                                        prevDate <- currDate
                                        logs.LogInfo("  {0}...", currDate)

                                    match firstClose with
                                    | None ->
                                        // First close is not set yet, initialize it
                                        firstClose <- Some candle.ClosePrice
                                        // Normalized value is 1 at the first candle
                                        series.[candle.OpenTime] <- 1m
                                    | Some fc ->
                                        // Divide by the first close price to normalize
                                        if fc <> 0m then
                                            series.[candle.OpenTime] <- candle.ClosePrice / fc
                                )

                            // Add the series for this security to the chart
                            chart.Append(security.ToStringId(), series.Keys, series.Values)
            }
