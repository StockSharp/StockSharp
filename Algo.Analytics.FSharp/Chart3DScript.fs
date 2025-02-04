namespace StockSharp.Algo.Analytics

open System
open System.Linq
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open Ecng.Common
open Ecng.Drawing
open Ecng.Logging

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
                timeFrame: TimeSpan,
                cancellationToken: CancellationToken
            ) : Task =

            if securities.Length = 0 then
                logs.LogWarning("No instruments.")
                Task.CompletedTask
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
                    if cancellationToken.IsCancellationRequested || (not doContinue) then
                        ()
                    else
                        let security = securities.[i]

                        // fill X label
                        x.Add(security.ToStringId())

                        // get candle storage for the specified parameters
                        let candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format)

                        // get available dates for the specified period
                        let dates = candleStorage.GetDates(fromDate, toDate).ToArray()
                        if dates.Length = 0 then
                            logs.LogWarning("no data")
                            doContinue <- false
                        else
                            // load all candles and group them by hour
                            let allCandles = candleStorage.Load(fromDate, toDate) |> Seq.toArray

                            // group by "hour" (truncated)
                            // byHours : dict<int, decimal> => hour -> summed volume
                            let byHours =
                                allCandles
                                |> Seq.groupBy (fun c ->
                                    // truncate time to hour
                                    let time = c.OpenTime.TimeOfDay.Truncate(TimeSpan.FromHours(1.0))
                                    time.Hours
                                )
                                |> Seq.map (fun (hour, group) ->
                                    let sumVolume = group |> Seq.sumBy (fun c -> c.TotalVolume)
                                    (hour, sumVolume)
                                )
                                |> dict

                            // fill Z values
                            for KeyValue(hour, totalVol) in byHours do
                                z.[i, hour] <- float totalVol

                // if we haven't stopped early, draw the 3D chart
                if doContinue then
                    panel.Draw3D(x, y, z, "Instruments", "Hours", "Volume")

                Task.CompletedTask
