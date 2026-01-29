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

open StockSharp.Messages
open StockSharp.Algo.Analytics
open StockSharp.Algo.Storages
open StockSharp.Algo.Candles

/// <summary>
/// The analytic script, calculating distribution of the biggest volume by hours
/// and shows it in a 3D chart.
/// </summary>
type Chart3DScript() =
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
                    // X and Y labels
                    let x = ResizeArray<string>()  // instruments
                    let y = ResizeArray<string>()  // hours

                    // fill Y labels with hours [0..23]
                    for h in 0..23 do
                        y.Add(h.ToString())

                    // create Z 2D array with [#instruments, #hours]
                    let z = Array2D.zeroCreate<float> securities.Length y.Count

                    // this flag will let us skip processing if we run into "no data"
                    let mutable doContinue = true

                    // iterate instruments
                    for i in 0..(securities.Length - 1) do
                        if (not cancellationToken.IsCancellationRequested) && doContinue then
                            let security = securities.[i]

                            logs.LogInfo("Processing {0} of {1}: {2}...", i + 1, securities.Length, security)

                            // fill X label
                            x.Add(security.ToStringId())

                            // get candle storage for the specified parameters
                            let candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format)

                            // get available dates for the specified period
                            let datesSeq = candleStorage.GetDatesAsync(fromDate, toDate)
                            let! dates = datesSeq.ToArrayAsync(cancellationToken)
                            if dates.Length = 0 then
                                logs.LogWarning("no data")
                                doContinue <- false
                            else
                                // load candles and group them by hour
                                let byHours = Dictionary<int, decimal>()
                                let mutable prevDate = DateOnly.MinValue

                                do! candleStorage.LoadAsync(fromDate, toDate)
                                    |> TaskSeq.iter (fun candle ->
                                        let currDate = DateOnly.FromDateTime(candle.OpenTime.Date)
                                        if currDate <> prevDate then
                                            prevDate <- currDate
                                            logs.LogInfo("  {0}...", currDate)

                                        let hour = candle.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1.0)).Hours
                                        match byHours.TryGetValue(hour) with
                                        | true, volume -> byHours.[hour] <- volume + candle.TotalVolume
                                        | false, _ -> byHours.[hour] <- candle.TotalVolume
                                    )

                                // fill Z values
                                for KeyValue(hour, totalVol) in byHours do
                                    z.[i, hour] <- float totalVol

                    // if we haven't stopped early, draw the 3D chart
                    if doContinue then
                        panel.Draw3D(x, y, z, "Instruments", "Hours", "Volume")
            }
