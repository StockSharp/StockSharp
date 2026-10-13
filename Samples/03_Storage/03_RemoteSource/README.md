# StockSharp with BinanceHistory Integration Example

## Overview

This application demonstrates how to set up a connector with a BinanceHistory message adapter in the StockSharp framework, retrieve historical trade data (specifically candle data), and save it locally in both CSV and binary formats. It also includes mechanisms to delete saved data. BinanceHistory is a free public data source, so no credentials are required to run this example.

## Detailed Code Walkthrough

### Initializing the Connector and BinanceHistory Message Adapter

```csharp
var connector = new Connector();
connector.LookupMessagesOnConnect.Clear();
var messageAdapter = new BinanceHistoryMessageAdapter(connector.TransactionIdGenerator);
connector.Adapter.InnerAdapters.Add(messageAdapter);
connector.Connect();
```
This initializes the `Connector` and adds a `BinanceHistoryMessageAdapter` to handle messages specifically for the Binance historical data archive. The connection is established immediately after setup.

### Security Lookup and Retrieval

```csharp
Console.WriteLine("Security:");
connector.LookupSecuritiesResult += (message, securities, arg3) =>
{
    foreach (var security1 in securities)
    {
        Console.WriteLine(security1);
    }
};
connector.Subscribe(new(new SecurityLookupMessage() { SecurityId = new() { SecurityCode = "BTCUSDT" } }));
var secId = "BTCUSDT@BNBFT".ToSecurityId();
var security = connector.GetSecurity(secId);
Console.ReadLine();
```
The program looks up securities using the specified code (in this case, "BTCUSDT", matched across the Binance spot and futures boards). It then prints out details for each security found.

### Subscribing to and Receiving Candle Data

```csharp
Console.WriteLine("Candles:");
var candles = new List<CandleMessage>();
connector.CandleReceived += (series, candle) =>
{
    Console.WriteLine(candle);
    candles.Add((CandleMessage)candle);
};

connector.Subscribe(new(security.TimeFrame(TimeSpan.FromMinutes(15)))
{
    MarketData =
    {
        From = DateTime.Now.AddDays(-3),
        To = DateTime.Now.AddDays(-1),
    },
});
Console.ReadLine();
```
The application subscribes to receive candle data for the selected security over a specified time range, capturing the data in a list and outputting each candle to the console.

### Data Storage Setup

```csharp
const string pathHistory = "Storage";
pathHistory.SafeDeleteDir();
var storageRegistry = new StorageRegistry
{
    DefaultDrive = new LocalMarketDataDrive(pathHistory)
};
```
Sets up local storage for the data and clears any existing data in the specified directory.

### Saving Candle Data Locally

```csharp
Console.WriteLine("Saving...");
var candlesStorageCsv = storageRegistry.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5), format: StorageFormats.Csv);
candlesStorageCsv.Save(candles);
var candlesStorageBin = storageRegistry.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5), format: StorageFormats.Binary);
candlesStorageBin.Save(candles);
Console.WriteLine("Save done!");
Console.ReadLine();
```
The retrieved candle data is saved locally in both CSV and binary formats for further analysis or backup purposes.

### Deleting Saved Data

```csharp
Console.WriteLine("Deleting...");
candlesStorageCsv.Delete(candles.First().OpenTime, candles.Last().CloseTime);
candlesStorageBin.Delete(candles.First().OpenTime, candles.Last().CloseTime);
Console.WriteLine("Delete done!");
Console.ReadLine();
```
Deletes the previously saved candle data from local storage.

## Conclusion

This program provides a complete workflow for connecting to the Binance historical data service using StockSharp, retrieving and handling historical data, and managing local data storage. It's designed to be an educational tool to understand the integration of external trading services with local data management using the StockSharp library. Adjust the code and configurations as necessary to fit specific requirements or trading strategies.