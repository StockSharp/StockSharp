namespace StockSharp.Samples.Storage.Local;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Compilation;
using Ecng.Configuration;
using Ecng.Compilation.Roslyn;

using StockSharp.Algo;
using StockSharp.Messages;
using StockSharp.Algo.Expressions;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;

static class Program
{
	private static async Task Main()
	{
		var token = CancellationToken.None;

		//--------------------------------Security--------------------------------------
		var pathHistory = Paths.HistoryDataPath;
		var localDrive = new LocalMarketDataDrive(Paths.FileSystem, pathHistory);

		await foreach (var sec in localDrive.GetAvailableSecuritiesAsync(token).WithEnforcedCancellation(token))
		{
			Console.WriteLine(sec);
		}

		Console.ReadLine();

		var secId = Paths.HistoryDefaultSecurity.ToSecurityId();

		var storageRegistry = new StorageRegistry()
		{
			DefaultDrive = localDrive,
		};
		//--------------------------------Candles--------------------------------------
		var candleStorage = storageRegistry.GetTimeFrameCandleMessageStorage(secId,
			TimeSpan.FromMinutes(1), format: StorageFormats.Binary);
		var candles = candleStorage.LoadAsync(new DateTime(2020, 4, 1), new DateTime(2020, 4, 2));

		await foreach (var candle in candles.WithEnforcedCancellation(token))
		{
			Console.WriteLine(candle);
		}

		Console.ReadLine();

		//--------------------------------Trades--------------------------------------
		var tradeStorage = storageRegistry.GetTickMessageStorage(secId, format: StorageFormats.Binary);
		var trades = tradeStorage.LoadAsync(new DateTime(2020, 4, 1), new DateTime(2020, 4, 2));

		await foreach (var trade in trades.WithEnforcedCancellation(token))
		{
			Console.WriteLine(trade);
		}

		Console.ReadLine();

		//--------------------------------MarketDepths--------------------------------------
		var marketDepthStorage = storageRegistry.GetQuoteMessageStorage(secId, format: StorageFormats.Binary);
		var marketDepths = marketDepthStorage.LoadAsync(new DateTime(2020, 4, 1), new DateTime(2020, 4, 2));

		await foreach (var marketDepth in marketDepths.WithEnforcedCancellation(token))
		{
			Console.WriteLine(marketDepth);
		}

		Console.ReadLine();

		//--------------------------------Level1--------------------------------------------
		var level1Storage = storageRegistry.GetLevel1MessageStorage(secId, format: StorageFormats.Binary);
		var levels1 = level1Storage.LoadAsync(new DateTime(2020, 4, 1), new DateTime(2020, 4, 2));

		await foreach (var level1 in levels1.WithEnforcedCancellation(token))
		{
			Console.WriteLine(level1);
		}

		Console.ReadLine();

		//--------------------------------Index--------------------------------------------
		ConfigManager.RegisterService<ICompiler>(new CSharpCompiler());
		var basketSecurity = new ExpressionIndexSecurity
		{
			Id = $"Index{secId}",
			Board = ExchangeBoard.Binance,
			BasketExpression = $"{secId} + 987654321",
		};

		var innerCandleList = new List<IEnumerable<CandleMessage>>();

		foreach (var innerSec in basketSecurity.InnerSecurityIds)
		{
			var innerStorage = storageRegistry.GetTimeFrameCandleMessageStorage(innerSec,
				TimeSpan.FromMinutes(1), format: StorageFormats.Binary);

			innerCandleList.Add(await innerStorage.LoadAsync(new DateTime(2020, 4, 1), new DateTime(2020, 4, 2)).ToArrayAsync(token));
		}

		var processorProvider = new BasketSecurityProcessorProvider();

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
	}
}