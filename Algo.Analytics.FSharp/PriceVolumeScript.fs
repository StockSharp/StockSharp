namespace StockSharp.Algo.Analytics

open System
open System.Linq
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic

open Ecng.Common
open Ecng.Drawing
open Ecng.Logging

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
                timeFrame: TimeSpan,
                cancellationToken: CancellationToken
            ) : Task =

            if securities.Length = 0 then
                logs.LogWarning("No instruments.")
                Task.CompletedTask
            else
                // This script processes only the first instrument
                let security = securities.First()

                // Get candle storage
                let candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format)

                // Get available dates within the specified period
                let dates = candleStorage.GetDates(fromDate, toDate).ToArray()

                if dates.Length = 0 then
                    logs.LogWarning("no data")
                    Task.CompletedTask
                else
                    // Group candles by "middle price" = LowPrice + (HighPrice - LowPrice) / 2
                    // In StockSharp code, c.GetLength() == c.HighPrice - c.LowPrice
                    // thus c.LowPrice + c.GetLength() / 2 is the middle
                    let rows =
                        candleStorage
                            .Load(fromDate, toDate)
                            .GroupBy(fun c -> c.LowPrice + (c.GetLength() / 2m))
                            .ToDictionary(
                                (fun g -> g.Key),
                                (fun g -> g.Sum(fun c -> c.TotalVolume))
                            )

                    // Draw histogram on the chart
                    panel.CreateChart<decimal, decimal>()
                         .Append(security.ToStringId(), rows.Keys, rows.Values, DrawStyles.Histogram)

                    Task.CompletedTask
