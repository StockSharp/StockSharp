namespace StockSharp.Algo.Analytics

open System
open System.Linq
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic

open Ecng.Common
open Ecng.Drawing
open Ecng.Logging

open FSharp.Control

open StockSharp.Algo.Analytics
open StockSharp.Algo.Storages
open StockSharp.Algo.Candles
open StockSharp.Messages

/// <summary>
/// The analytic script, calculating distribution of the volume by price levels.
/// </summary>
type PriceVolumeScript() =
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
                    // This script processes only the first instrument
                    let security = securities.First()

                    logs.LogInfo("Processing {0}...", security)

                    // Get candle storage
                    let candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format)

                    // Get available dates within the specified period
                    let! datesSeq = candleStorage.GetDatesAsync(fromDate, toDate, cancellationToken)
                    let dates = datesSeq.ToArray()

                    if dates.Length = 0 then
                        logs.LogWarning("no data")
                    else
                        // Group candles by "middle price" = LowPrice + (HighPrice - LowPrice) / 2
                        let rows = Dictionary<decimal, decimal>()
                        let mutable prevDate = DateOnly.MinValue

                        do! candleStorage.LoadAsync(fromDate, toDate)
                            |> TaskSeq.iter (fun candle ->
                                let currDate = DateOnly.FromDateTime(candle.OpenTime.Date)
                                if currDate <> prevDate then
                                    prevDate <- currDate
                                    logs.LogInfo("  {0}...", currDate)

                                let midPrice = candle.LowPrice + (candle.GetLength() / 2m)
                                match rows.TryGetValue(midPrice) with
                                | true, volume -> rows.[midPrice] <- volume + candle.TotalVolume
                                | false, _ -> rows.[midPrice] <- candle.TotalVolume
                            )

                        // Draw histogram on the chart
                        panel.CreateChart<decimal, decimal>()
                             .Append(security.ToStringId(), rows.Keys, rows.Values, DrawStyles.Histogram)
            }
