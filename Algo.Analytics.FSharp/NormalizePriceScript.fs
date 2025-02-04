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
                timeFrame: TimeSpan,
                cancellationToken: CancellationToken
            ) : Task =

            if securities.Length = 0 then
                logs.LogWarning("No instruments.")
                Task.CompletedTask
            else
                // Create a chart for normalized close prices
                let chart = panel.CreateChart<DateTimeOffset, decimal>()

                // Iterate over each security
                for security in securities do
                    // Stop if user cancels execution
                    if cancellationToken.IsCancellationRequested then
                        ()
                    else
                        // Dictionary to store time -> normalized close price
                        let series = Dictionary<DateTimeOffset, decimal>()

                        // Get candle storage for this security
                        let candleStorage =
                            storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format)

                        // We'll store the first close price in a mutable option
                        let mutable firstClose: decimal option = None

                        // Load candles and normalize close prices
                        for candle in candleStorage.Load(fromDate, toDate) do
                            match firstClose with
                            | None ->
                                // First close is not set yet, initialize it
                                firstClose <- Some candle.ClosePrice
                                // Normalized value is 1 at the first candle
                                series.[candle.OpenTime] <- 1m
                            | Some fc ->
                                // Divide by the first close price to normalize
                                series.[candle.OpenTime] <- candle.ClosePrice / fc

                        // Add the series for this security to the chart
                        chart.Append(security.ToStringId(), series.Keys, series.Values)

                Task.CompletedTask
