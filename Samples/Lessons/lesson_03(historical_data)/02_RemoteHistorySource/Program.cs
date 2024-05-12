using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Candles;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Finam;
using StockSharp.Messages;

namespace RemoteHistorySource;

static class Program
{
	private static void Main()
	{
		var connector = new Connector();
		connector.LookupMessagesOnConnect.Clear();

		var messageAdapter = new FinamMessageAdapter(connector.TransactionIdGenerator);
		connector.Adapter.InnerAdapters.Add(messageAdapter);
		connector.Connect();

		//--------------------------Security--------------------------------------------------------------------------------
		Console.WriteLine("Security:");
		connector.LookupSecuritiesResult += (message, securities, arg3) =>
		{
			foreach (var security1 in securities)
			{
				Console.WriteLine(security1);
			}
		};
		connector.LookupSecurities(new Security() { Code = "SBER", Type = SecurityTypes.Stock });
		var secId = "SBER@TQBR".ToSecurityId();
		var security = connector.GetSecurity(secId);

		Console.ReadLine();

		////--------------------------Candles--------------------------------------------------------------------------------
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

		//---------------------------------------------------------------------------------------------
		const string pathHistory = "Storage";
		pathHistory.SafeDeleteDir();

		var storageRegistry = new StorageRegistry
		{
			DefaultDrive = new LocalMarketDataDrive(pathHistory)
		};

		//------------------------------Save---------------------------------------------------
		Console.WriteLine("Saving...");
		var candlesStorageCsv = storageRegistry.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5), format: StorageFormats.Csv);
		candlesStorageCsv.Save(candles);
		var candlesStorageBin = storageRegistry.GetTimeFrameCandleMessageStorage(secId, TimeSpan.FromMinutes(5), format: StorageFormats.Binary);
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