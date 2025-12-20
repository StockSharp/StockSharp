namespace StockSharp.Samples.Storage.Random;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;

using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Localization;
using StockSharp.Messages;

class Program
{
	static async Task Main()
	{
		var token = CancellationToken.None;

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

		var begin = DateTime.UtcNow.Date;
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

		using var drive = new LocalMarketDataDrive();

		// get AAPL tick storage
		var tradeStorage = storageRegistry.GetTickMessageStorage(securityId);

		// saving ticks
		await tradeStorage.SaveAsync(trades, token);

		for (var d = begin; d < end; d += TimeSpan.FromDays(1))
		{
			// loading ticks
			var loadedTrades = tradeStorage.LoadAsync(d);

			await foreach (var trade in loadedTrades.WithCancellation(token))
			{
				Console.WriteLine(LocalizedStrings.TradeDetails, trade.TradeId, trade);
			}
		}

		Console.ReadLine();

		// deleting ticks (and removing file)
		await tradeStorage.DeleteAsync(begin, begin + TimeSpan.FromMinutes(1000), token);
	}
}