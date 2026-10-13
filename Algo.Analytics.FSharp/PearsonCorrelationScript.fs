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
                cancellationToken.ThrowIfCancellationRequested()

                // If no securities are selected, log a warning and finish
                if securities.Length = 0 then
                    logs.LogWarning("No instruments.")
                else
                    // Keep the timestamp with every close so gaps in different instruments do not
                    // make unrelated candles look like a pair of observations.
                    let closes = ResizeArray<Dictionary<DateTime, float>>()
                    let mutable idx = 0
                    let mutable hasMissingData = false

                    for security in securities do
                        cancellationToken.ThrowIfCancellationRequested()

                        idx <- idx + 1
                        logs.LogInfo("Processing {0} of {1}: {2}...", idx, securities.Length, security)
                        cancellationToken.ThrowIfCancellationRequested()

                        let candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format)
                        let pricesByTime = Dictionary<DateTime, float>()
                        let mutable prevDate = DateOnly.MinValue

                        do! candleStorage.LoadAsync(fromDate, toDate).WithEnforcedCancellation(cancellationToken)
                            |> TaskSeq.iter (fun candle ->
                                cancellationToken.ThrowIfCancellationRequested()

                                let currDate = DateOnly.FromDateTime(candle.OpenTime.Date)
                                if currDate <> prevDate then
                                    prevDate <- currDate
                                    logs.LogInfo("  {0}...", currDate)
                                    cancellationToken.ThrowIfCancellationRequested()

                                pricesByTime.[candle.OpenTime] <- float candle.ClosePrice
                            )

                        if pricesByTime.Count = 0 then
                            logs.LogWarning("No data for {0}", security)
                            hasMissingData <- true
                        else
                            closes.Add(pricesByTime)

                    cancellationToken.ThrowIfCancellationRequested()

                    if not hasMissingData && closes.Count > 0 then
                        let commonTimes =
                            closes.[0].Keys
                            |> Seq.filter (fun time ->
                                closes
                                |> Seq.skip 1
                                |> Seq.forall (fun series -> series.ContainsKey(time)))
                            |> Seq.sort
                            |> Seq.toArray

                        if commonTimes.Length = 0 then
                            logs.LogWarning("The instruments have no candles at the same time.")
                        else
                            let pairedCloses =
                                closes
                                |> Seq.map (fun series ->
                                    commonTimes
                                    |> Array.map (fun time -> series.[time]))
                                |> Seq.toList

                            // Calculate correlation matrix
                            let matrix = Correlation.PearsonMatrix(pairedCloses :> seq<_>)

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
