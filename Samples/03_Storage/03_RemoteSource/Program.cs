namespace StockSharp.Samples.Storage.RemoteSource
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Common;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	static class Program
	{
		private static void Main()
		{
			// Initialisiere den Connector
			var connector = new Connector();
			connector.LookupMessagesOnConnect.Clear();

			// Adapter für Coinbase (CNBS)
			connector.Connect();

			//--------------------------Security--------------------------------------------------------------------------------
			Console.WriteLine("Security:");
			var securityId = "BTC-USD@CNBS".ToSecurityId();

			var security = new Security
			{
				Id = "BTC-EUR@CNBS",
				Code = "BTC-EUR",
				Board = ExchangeBoard.Coinbase
			};

			Console.WriteLine($"Abonniert: {security.Id}");

			////--------------------------Candles--------------------------------------------------------------------------------
			Console.WriteLine("Candles:");
			var candles = new List<CandleMessage>();

			// Ereignis abonnieren, um Kerzen zu empfangen
			connector.CandleReceived += (series, candle) =>
			{
				Console.WriteLine(candle);
				candles.Add((CandleMessage)candle);
			};

			// Kerzen-Daten abonnieren
			//CandleSeries candleSeries = DataType.CandleRange.ToCandleSeries(security);
			connector.Subscribe( new Subscription(DataType.CandleRange,security));


			//connector.Subscribe(candleSeries);

			Console.ReadLine();

			//---------------------------------------------------------------------------------------------
			const string pathHistory = "Storage";
			pathHistory.SafeDeleteDir();

			var storageRegistry = new StorageRegistry
			{
				DefaultDrive = new LocalMarketDataDrive(pathHistory)
			};

			//------------------------------Save---------------------------------------------------
			Console.WriteLine("Saving...");
			var candlesStorageCsv = storageRegistry.GetTimeFrameCandleMessageStorage(securityId, TimeSpan.FromMinutes(5), format: StorageFormats.Csv);
			candlesStorageCsv.Save(candles);

			var candlesStorageBin = storageRegistry.GetTimeFrameCandleMessageStorage(securityId, TimeSpan.FromMinutes(5), format: StorageFormats.Binary);
			candlesStorageBin.Save(candles);

			Console.WriteLine("Save done!");

			Console.ReadLine();

			//------------------------------Delete---------------------------------------------------
			Console.WriteLine("Deleting...");
			candlesStorageCsv.Delete(candles.First().OpenTime, candles.Last().CloseTime);
			candlesStorageBin.Delete(candles.First().OpenTime, candles.Last().CloseTime);
			Console.WriteLine("Delete done!");

			Console.ReadLine();
		}
	}
}
