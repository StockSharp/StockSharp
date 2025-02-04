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
/// The analytic script, calculating distribution of the biggest volume by hours.
/// </summary>
type TimeVolumeScript() =
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
                    // Group candles by hour (truncate open time to 1 hour)
                    let rows =
                        candleStorage
                            .Load(fromDate, toDate)
                            .GroupBy(fun c ->
                                // c.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1))
                                let truncated = c.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1.0))
                                truncated
                            )
                            .ToDictionary(
                                (fun g -> g.Key),
                                (fun g -> g.Sum(fun c -> c.TotalVolume))
                            )

                    // Create a grid with two columns: Time, Volume
                    let grid = panel.CreateGrid("Time", "Volume")

                    // Fill the grid with data (hour -> volume)
                    for KeyValue(hour, volume) in rows do
                        grid.SetRow(hour, volume)

                    // Sort the grid by the "Volume" column in descending order
                    grid.SetSort("Volume", false)

                    Task.CompletedTask
