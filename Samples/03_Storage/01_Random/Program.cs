namespace StockSharp.Samples.Storage.Random;

using System;
using System.Collections.Generic;

using Ecng.Common;

using StockSharp.Algo;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;

class Program
{
	static void Main()
	{
		// creating AAPL security
		var security = new Security
		{
			Id = "AAPL@NASDAQ",
			PriceStep = 0.1m,
			Decimals = 1,
		};

		var securityId = security.ToSecurityId();

		var trades = new List<ExecutionMessage>();

		// generation 1000 random ticks
		//

		const int count = 1000;

		var begin = DateTime.Today;
		var end = begin + TimeSpan.FromMinutes(count);

		for (var i = 0; i < count; i++)
		{
			var t = new ExecutionMessage
			{
				DataTypeEx = DataType.Ticks,
				ServerTime = begin + TimeSpan.FromMinutes(i),
				TradeId = i + 1,
				SecurityId = securityId,
				TradeVolume = RandomGen.GetInt(1, 10),
				TradePrice = RandomGen.GetInt(1, 100) * security.PriceStep ?? 1m + 99
			};

			trades.Add(t);
		}

		var storageRegistry = new StorageRegistry()
		{
			DefaultDrive = new LocalMarketDataDrive(),
		};

		using (var drive = new LocalMarketDataDrive())
		{
			// get AAPL tick storage
			var tradeStorage = storageRegistry.GetTickMessageStorage(securityId);

			// saving ticks
			tradeStorage.Save(trades);

			for (var d = begin; d < end; d += TimeSpan.FromDays(1))
			{
				// loading ticks
				var loadedTrades = tradeStorage.Load(d);

				foreach (var trade in loadedTrades)
				{
					Console.WriteLine(LocalizedStrings.TradeDetails, trade.TradeId, trade);
				}	
			}

			Console.ReadLine();

			// deleting ticks (and removing file)
			tradeStorage.Delete(DateTime.Today, DateTime.Today + TimeSpan.FromMinutes(1000));	
		}
	}
}