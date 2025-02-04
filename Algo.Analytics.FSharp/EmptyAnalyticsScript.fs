namespace StockSharp.Algo.Analytics

open System
open System.Threading
open System.Threading.Tasks

open Ecng.Common
open Ecng.Logging

open StockSharp.Messages
open StockSharp.Algo.Analytics
open StockSharp.Algo.Storages
open StockSharp.Algo.Candles

/// <summary>
/// The empty analytic strategy.
/// </summary>
type EmptyAnalyticsScript() =
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
                // !! add logic here
                Task.CompletedTask
