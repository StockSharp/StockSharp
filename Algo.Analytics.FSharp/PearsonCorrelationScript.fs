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

open MathNet.Numerics.Statistics

/// <summary>
/// The analytic script, calculating Pearson correlation by specified securities.
/// </summary>
type PearsonCorrelationScript() =
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
                // If no securities are selected, log a warning and finish
                if securities.Length = 0 then
                    logs.LogWarning("No instruments.")
                else
                    // A list of arrays, each array containing double-precision close prices for a single security
                    let closes = ResizeArray<float[]>()
                    let mutable idx = 0

                    for security in securities do
                        if not cancellationToken.IsCancellationRequested then
                            idx <- idx + 1
                            logs.LogInfo("Processing {0} of {1}: {2}...", idx, securities.Length, security)

                            let candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format)
                            let pricesList = ResizeArray<float>()
                            let mutable prevDate = DateOnly.MinValue

                            do! candleStorage.LoadAsync(fromDate, toDate)
                                |> TaskSeq.iter (fun candle ->
                                    let currDate = DateOnly.FromDateTime(candle.OpenTime.Date)
                                    if currDate <> prevDate then
                                        prevDate <- currDate
                                        logs.LogInfo("  {0}...", currDate)

                                    pricesList.Add(float candle.ClosePrice)
                                )

                            let prices = pricesList.ToArray()
                            if prices.Length = 0 then
                                logs.LogWarning("No data for {0}", security)
                            else
                                closes.Add(prices)

                    if closes.Count > 0 then
                        // All arrays must be same length, truncate longer ones
                        let minLen =
                            closes
                            |> Seq.map (fun arr -> arr.Length)
                            |> Seq.min

                        let truncatedCloses =
                            closes
                            |> Seq.map (fun arr ->
                              if arr.Length > minLen then arr.[0..(minLen - 1)] else arr)
                            |> Seq.toList

                        // Calculate correlation matrix
                        let matrix = Correlation.PearsonMatrix(truncatedCloses :> seq<_>)

                        // Get security names for heatmap axes
                        let ids =
                            securities
                            |> Seq.map (fun s -> s.ToStringId())
                            |> Seq.toArray

                        // Convert matrix to 2D array for drawing
                        let arrMatrix = matrix.ToArray()

                        // Draw result as heatmap
                        panel.DrawHeatmap(ids, ids, arrMatrix)
            }
