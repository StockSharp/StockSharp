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
                timeFrame: TimeSpan,
                cancellationToken: CancellationToken
            ) : Task =

            // If no securities are selected, log a warning and finish
            if securities.Length = 0 then
                logs.LogWarning("No instruments.")
                Task.CompletedTask
            else
                // A list of arrays, each array containing double-precision close prices for a single security
                let closes =
                    securities
                    |> Seq.choose (fun security ->
                        if cancellationToken.IsCancellationRequested then None
                        else
                            let candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format)
                            let prices = 
                                candleStorage.Load(fromDate, toDate) 
                                |> Seq.map (fun c -> float c.ClosePrice)
                                |> Seq.toArray
                            if prices.Length = 0 then
                                logs.LogWarning("No data for {0}", security)
                                None
                            else
                                Some prices
                    )
                    |> Seq.toList

                // Все массивы должны быть одинаковой длины. Если какие-то длиннее,
                // обрезаем их до минимальной длины (minLen).
                let minLen =
                    closes
                    |> Seq.map (fun arr -> arr.Length)
                    |> Seq.min

                let truncatedCloses =
                    closes
                    |> List.map (fun arr ->
                      if arr.Length > minLen then arr.[0..(minLen - 1)] else arr)

                // Вычисляем матрицу корреляции
                // Correlation.PearsonMatrix принимает последовательность массивов и возвращает матрицу
                let matrix = Correlation.PearsonMatrix(truncatedCloses :> seq<_>)

                // Получаем названия инструментов для осей heatmap
                let ids =
                    securities
                    |> Seq.map (fun s -> s.ToStringId())
                    |> Seq.toArray

                // Преобразуем матрицу в двумерный массив для отрисовки
                let arrMatrix = matrix.ToArray()

                // Отрисовываем результат в виде heatmap
                panel.DrawHeatmap(ids, ids, arrMatrix)

                Task.CompletedTask
