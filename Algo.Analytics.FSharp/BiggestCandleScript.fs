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
/// The analytic script, shows biggest candle (by volume and by length) for specified securities.
/// </summary>
type BiggestCandleScript() =
    interface IAnalyticsScript with
        member this.Run
            (
                logs: ILogReceiver,
                panel: IAnalyticsPanel,
                securities: SecurityId[],
                from: DateTime,
                ``to``: DateTime,
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
                    // Create 2D/3D charts for biggest candle by price length and by volume
                    let priceChart = panel.CreateChart<DateTime, decimal, decimal>()
                    let volChart = panel.CreateChart<DateTime, decimal, decimal>()

                    // Lists to store the biggest candles
                    let bigPriceCandles = List<CandleMessage>()
                    let bigVolCandles = List<CandleMessage>()
                    let mutable idx = 0

                    // Iterate over each security
                    for security in securities do
                        // Stop calculation if user canceled script execution
                        if not cancellationToken.IsCancellationRequested then
                            idx <- idx + 1
                            logs.LogInfo("Processing {0} of {1}: {2}...", idx, securities.Length, security)

                            // Get candle storage for the specified timeframe
                            let candleStorage = storage.GetCandleMessageStorage(security, dataType, drive, format)

                            // Find biggest candles by iterating through stream
                            let mutable bigPriceCandle: CandleMessage = null
                            let mutable bigVolCandle: CandleMessage = null
                            let mutable maxLength = 0m
                            let mutable maxVolume = 0m
                            let mutable prevDate = DateOnly.MinValue

                            do! candleStorage.LoadAsync(from, ``to``)
                                |> TaskSeq.iter (fun candle ->
                                    let currDate = DateOnly.FromDateTime(candle.OpenTime.Date)
                                    if currDate <> prevDate then
                                        prevDate <- currDate
                                        logs.LogInfo("  {0}...", currDate)

                                    let length = candle.GetLength()
                                    if isNull bigPriceCandle || length > maxLength then
                                        maxLength <- length
                                        bigPriceCandle <- candle

                                    if isNull bigVolCandle || candle.TotalVolume > maxVolume then
                                        maxVolume <- candle.TotalVolume
                                        bigVolCandle <- candle
                                )

                            // If found, add to respective lists
                            if not (isNull bigPriceCandle) then
                                bigPriceCandles.Add(bigPriceCandle)

                            if not (isNull bigVolCandle) then
                                bigVolCandles.Add(bigVolCandle)

                    // Draw the biggest price candles on the price chart
                    priceChart.Append(
                        "prices",
                        bigPriceCandles.Select(fun c -> c.OpenTime),
                        bigPriceCandles.Select(fun c -> c.GetMiddlePrice(Nullable())),
                        bigPriceCandles.Select(fun c -> c.GetLength())
                    )

                    // Draw the biggest volume candles on the volume chart
                    volChart.Append(
                        "prices",
                        bigVolCandles.Select(fun c -> c.OpenTime),
                        bigPriceCandles.Select(fun c -> c.GetMiddlePrice(Nullable())),
                        bigVolCandles.Select(fun c -> c.TotalVolume)
                    )
            }
