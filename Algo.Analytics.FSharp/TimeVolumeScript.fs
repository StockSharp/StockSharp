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
                    let datesSeq = candleStorage.GetDatesAsync(fromDate, toDate)
                    let! dates = datesSeq.ToArrayAsync(cancellationToken)

                    if dates.Length = 0 then
                        logs.LogWarning("no data")
                    else
                        // Group candles by hour (truncate open time to 1 hour)
                        let rows = Dictionary<TimeSpan, decimal>()
                        let mutable prevDate = DateOnly.MinValue

                        do! candleStorage.LoadAsync(fromDate, toDate)
                            |> TaskSeq.iter (fun candle ->
                                let currDate = DateOnly.FromDateTime(candle.OpenTime.Date)
                                if currDate <> prevDate then
                                    prevDate <- currDate
                                    logs.LogInfo("  {0}...", currDate)

                                let hour = candle.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1.0))
                                match rows.TryGetValue(hour) with
                                | true, volume -> rows.[hour] <- volume + candle.TotalVolume
                                | false, _ -> rows.[hour] <- candle.TotalVolume
                            )

                        // Create a grid with two columns: Time, Volume
                        let grid = panel.CreateGrid("Time", "Volume")

                        // Fill the grid with data (hour -> volume)
                        for KeyValue(hour, volume) in rows do
                            grid.SetRow(hour, volume)

                        // Sort the grid by the "Volume" column in descending order
                        grid.SetSort("Volume", false)
            }
