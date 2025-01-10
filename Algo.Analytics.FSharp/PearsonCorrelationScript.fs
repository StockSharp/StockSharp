namespace StockSharp.Algo.Analytics

open System
open System.Linq
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks

open Ecng.Common
open Ecng.Drawing

open StockSharp.Logging
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
                logs.AddWarningLog("No instruments.")
                Task.CompletedTask
            else
                // A list of arrays, each array containing double-precision close prices for a single security
                let closes = List<double[]>()

                // Load closing prices for each security
                for security in securities do
                    // Stop if the user cancels the script
                    if cancellationToken.IsCancellationRequested then
                        ()
                    else
                        // Get candle storage
                        let candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format)

                        // Convert close prices to double
                        let prices =
                            candleStorage
                                .Load(fromDate, toDate)
                                .Select(fun c -> float c.ClosePrice)
                                .ToArray()

                        if prices.Length = 0 then
                            logs.AddWarningLog("No data for {0}", security)
                            // Возвращаем пустую задачу, как в исходном коде (при желании можно было бы продолжить без прерывания)
                            Task.CompletedTask |> ignore
                        else
                            closes.Add(prices)

                // Все массивы должны быть одинаковой длины. Если какие-то длиннее,
                // обрезаем их до минимальной длины (minLen).
                let minLen =
                    closes
                    |> Seq.map (fun arr -> arr.Length)
                    |> Seq.min

                for i in 0..(closes.Count - 1) do
                    let arr = closes.[i]
                    if arr.Length > minLen then
                        // Возьмём элементы с 0 по (minLen - 1)
                        closes.[i] <- arr.[0..(minLen - 1)]

                // Вычисляем матрицу корреляции
                // Correlation.PearsonMatrix принимает последовательность массивов и возвращает матрицу
                let matrix = Correlation.PearsonMatrix(closes :> seq<_>)

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
