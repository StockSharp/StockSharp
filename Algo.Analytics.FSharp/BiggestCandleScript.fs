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
                timeFrame: TimeSpan,
                cancellationToken: CancellationToken
            ) : Task =

            if securities.Length = 0 then
                logs.LogWarning("No instruments.")
                Task.CompletedTask
            else
                // Create 2D/3D charts for biggest candle by price length and by volume
                let priceChart = panel.CreateChart<DateTimeOffset, decimal, decimal>()
                let volChart = panel.CreateChart<DateTimeOffset, decimal, decimal>()

                // Lists to store the biggest candles
                let bigPriceCandles = List<CandleMessage>()
                let bigVolCandles = List<CandleMessage>()

                // Iterate over each security
                for security in securities do
                    // Stop calculation if user canceled script execution
                    if cancellationToken.IsCancellationRequested then
                        ()
                    else
                        // Get candle storage for the specified timeframe
                        let candleStorage = storage.GetTimeFrameCandleMessageStorage(security, timeFrame, drive, format)
                        // Load all candles in the specified date range
                        let allCandles = candleStorage.Load(from, ``to``).ToArray()

                        // The candle with the biggest range (by high-low difference)
                        let bigPriceCandle =
                            allCandles
                                .OrderByDescending(fun c -> c.GetLength())
                                .FirstOrDefault()

                        // The candle with the biggest volume
                        let bigVolCandle =
                            allCandles
                                .OrderByDescending(fun c -> c.TotalVolume)
                                .FirstOrDefault()

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
                    // Notice that for the Y2 axis we use the middle price from bigPriceCandles 
                    // to match the C# code; but you may want to adjust this logic if needed
                    bigPriceCandles.Select(fun c -> c.GetMiddlePrice(Nullable())),
                    bigVolCandles.Select(fun c -> c.TotalVolume)
                )

                Task.CompletedTask
