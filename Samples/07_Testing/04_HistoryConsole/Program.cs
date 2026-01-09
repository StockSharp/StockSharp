namespace StockSharp.Samples.Testing.HistoryConsole;

using System;
using System.Linq;
using System.Threading;

using Ecng.Common;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Messages;
using StockSharp.Configuration;
using StockSharp.Samples.Testing.History;

class Program
{
	static void Main()
	{
		// check history data path
		if (Paths.HistoryDataPath == null)
		{
			Console.WriteLine("History data path not found. Install StockSharp.Samples.HistoryData package.");
			return;
		}

		Console.WriteLine($"History data path: {Paths.HistoryDataPath}");
		Console.WriteLine($"Security: {Paths.HistoryDefaultSecurity}");
		Console.WriteLine($"Period: {Paths.HistoryBeginDate:d} - {Paths.HistoryEndDate:d}");
		Console.WriteLine();

		// create log manager with console and file output
		var logManager = new LogManager();
		logManager.Listeners.Add(new ConsoleLogListener());
		logManager.Listeners.Add(new FileLogListener("backtest.log"));

		var exchangeInfoProvider = new InMemoryExchangeInfoProvider();

		var id = Paths.HistoryDefaultSecurity.ToSecurityId();
		var board = exchangeInfoProvider.GetOrCreateBoard(id.BoardCode);

		// create test security
		var security = new Security
		{
			Id = Paths.HistoryDefaultSecurity,
			Code = id.SecurityCode,
			Board = board,
		};

		// storage for historical data
		var storageRegistry = new StorageRegistry
		{
			DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath)
		};

		var startTime = Paths.HistoryBeginDate.UtcKind();
		var stopTime = Paths.HistoryEndDate.UtcKind();

		var secId = security.ToSecurityId();

		var level1Info = new Level1ChangeMessage
		{
			SecurityId = secId,
			ServerTime = startTime,
		}
		.TryAdd(Level1Fields.MinPrice, 0.01m)
		.TryAdd(Level1Fields.MaxPrice, 1000000m)
		.TryAdd(Level1Fields.MarginBuy, 10000m)
		.TryAdd(Level1Fields.MarginSell, 10000m);

		var secProvider = (ISecurityProvider)new CollectionSecurityProvider(new[] { security });
		var pf = Portfolio.CreateSimulator();
		pf.CurrentValue = 1000;

		// create backtesting connector
		var connector = new HistoryEmulationConnector(secProvider, new[] { pf })
		{
			EmulationAdapter =
			{
				Settings =
				{
					MatchOnTouch = false,
					CommissionRules = new ICommissionRule[]
					{
						new CommissionTradeRule { Value = 0.01m },
					},
				}
			},

			HistoryMessageAdapter =
			{
				StorageRegistry = storageRegistry,
			},
		};

		((ILogSource)connector).LogLevel = LogLevels.Info;
		logManager.Sources.Add(connector);

		// create strategy based on 80 and 10 SMA
		var strategy = new SmaStrategy
		{
			LongSma = 80,
			ShortSma = 10,
			Volume = 1,
			Portfolio = connector.Portfolios.First(),
			Security = security,
			Connector = connector,
			LogLevel = LogLevels.Info,

			// candle type for subscription
			CandleType = TimeSpan.FromMinutes(1).TimeFrame(),

			UnrealizedPnLInterval = ((stopTime - startTime).Ticks / 1000).To<TimeSpan>(),
		};

		logManager.Sources.Add(strategy);

		// set history range
		connector.HistoryMessageAdapter.StartDate = startTime;
		connector.HistoryMessageAdapter.StopDate = stopTime;

		var finishedEvent = new ManualResetEvent(false);

		connector.SecurityReceived += (subscr, s) =>
		{
			if (s != security)
				return;

			// fill level1 values
			_ = connector.EmulationAdapter.SendInMessageAsync(level1Info, default);
		};

		var lastProgress = 0;
		connector.ProgressChanged += steps =>
		{
			var progress = (int)steps;
			if (progress > lastProgress && progress % 10 == 0)
			{
				Console.WriteLine($"Progress: {progress}%");
				lastProgress = progress;
			}
		};

		connector.StateChanged2 += state =>
		{
			if (state == ChannelStates.Stopped)
			{
				strategy.Stop();

				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine("=== Backtest completed ===");
				Console.WriteLine();
				Console.WriteLine($"Total trades: {strategy.StatisticManager.Parameters.FirstOrDefault(p => p.Name == "TradeCount")?.Value}");
				Console.WriteLine($"Net profit: {strategy.StatisticManager.Parameters.FirstOrDefault(p => p.Name == "NetProfit")?.Value}");
				Console.WriteLine($"Max drawdown: {strategy.StatisticManager.Parameters.FirstOrDefault(p => p.Name == "MaxDrawdown")?.Value}");
				Console.WriteLine();

				Console.WriteLine("Statistics:");
				foreach (var param in strategy.StatisticManager.Parameters)
				{
					Console.WriteLine($"  {param.Name}: {param.Value}");
				}

				finishedEvent.Set();
			}
		};

		Console.WriteLine("Starting backtest on candles...");
		Console.WriteLine();

		// start strategy before emulation started
		strategy.Start();

		// start connector
		connector.Connect();
		connector.Start();

		// wait for completion
		finishedEvent.WaitOne();

		logManager.Dispose();
	}
}
