# Detailed Documentation for `Program.cs`

## Overview

This program is designed to load and display various types of market data using the StockSharp framework, interacting with a [local market data drive](https://doc.stocksharp.com/topics/api/market_data_storage.html). It handles securities, candles, trades, market depths, level1 messages, and an expression-based index.

## Detailed Code Explanation

### Setup Local Market Data Drive

```csharp
var pathHistory = Paths.HistoryDataPath;
var localDrive = new LocalMarketDataDrive(pathHistory);
```
This initializes the path for historical data and sets up a local drive to manage this data.

### Listing Available Securities

```csharp
var securities = localDrive.AvailableSecurities;
foreach (var sec in securities)
{
    Console.WriteLine(sec);
}
Console.ReadLine();
```
Here, the program lists all [securities](https://doc.stocksharp.com/topics/api/instruments.html) stored in the local drive and outputs each to the console.

### Loading and Displaying Candle Data

```csharp
var secId = Paths.HistoryDefaultSecurity.ToSecurityId();
var candleStorage = storageRegistry.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(1), format: StorageFormats.Binary);
var candles = candleStorage.Load(new DateTime(2020, 4, 1), new DateTime(2020, 4, 2));
foreach (var candle in candles)
{
    Console.WriteLine(candle);
}
Console.ReadLine();
```
This code loads [candle data](https://doc.stocksharp.com/topics/api/candles.html) for the security `secId` using a one-minute timeframe, from April 1, 2020, to April 2, 2020. Each candle is then printed to the console.

### Loading and Displaying Trade Data

```csharp
var tradeStorage = storageRegistry.GetTickMessageStorage(secId, format: StorageFormats.Binary);
var trades = tradeStorage.Load(new DateTime(2020, 4, 1), new DateTime(2020, 4, 2));
foreach (var trade in trades)
{
    Console.WriteLine(trade);
}
Console.ReadLine();
```
This snippet retrieves and displays tick trade data for the specified security within the given date range.

### Loading and Displaying Market Depths

```csharp
var marketDepthStorage = storageRegistry.GetQuoteMessageStorage(secId, format: StorageFormats.Binary);
var marketDepths = marketDepthStorage.Load(new DateTime(2020, 4, 1), new DateTime(2020, 4, 2));
foreach (var marketDepth in marketDepths)
{
    Console.WriteLine(marketDepth);
}
Console.ReadLine();
```
This part of the program loads and displays [market depth](https://doc.stocksharp.com/topics/api/order_books.html) data, showing bid and ask prices and quantities.

### Loading and Displaying Level1 Messages

```csharp
var level1Storage = storageRegistry.GetLevel1MessageStorage(secId, format: StorageFormats.Binary);
var levels1 = level1Storage.Load(new DateTime(2020, 4, 1), new DateTime(2020, 4, 2));
foreach (var level1 in levels1)
{
    Console.WriteLine(level1);
}
Console.ReadLine();
```
This section loads and prints Level1 messages, which contain various fundamental and technical data points for a security.

### Expression-Based Index Creation

```csharp
ConfigManager.RegisterService<ICompiler>(new RoslynCompiler());
var basketSecurity = new ExpressionIndexSecurity
{
    Id = "IndexInstr@TQBR",
    Board = ExchangeBoard.MicexTqbr,
    BasketExpression = $"{secId} + 987654321",
};
```
This configures an [expression-based index](https://doc.stocksharp.com/topics/api/instruments/index.html) which is calculated using the specified formula. This demonstrates how to extend the application to handle composite securities based on expressions.

### Calculating and Displaying Index Candles

```csharp
var innerCandleList = new List<IEnumerable<CandleMessage>>();
foreach (var innerSec in basketSecurity.InnerSecurityIds)
{
    var innerStorage = storageRegistry.GetTimeFrameCandleMessageStorage(innerSec, TimeSpan.FromMinutes(1), format: StorageFormats.Binary);
    innerCandleList.Add(innerStorage.Load(new DateTime(2020, 4, 1), new DateTime(2020, 4, 2)));
}
var indexCandles = innerCandleList
    .SelectMany(i => i)
    .OrderBy(t => t.OpenTime)
    .Select(c => c)
    .ToBasket(basketSecurity, processorProvider);
foreach (var candle in indexCandles)
{
    Console.WriteLine(candle);
}
Console.ReadLine();
```
This section creates candles for the index based on candles from its component securities. This is useful for analyzing composite instruments like indexes or baskets of stocks.

## Conclusion

This detailed documentation is designed to guide a developer through understanding and possibly modifying the application to handle different securities or data types, demonstrating robust data handling and visualization techniques using the StockSharp framework. Adjust the documentation as needed to fit the specifics of your implementation or project requirements.