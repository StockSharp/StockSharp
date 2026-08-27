namespace StockSharp.Samples.Strategies.HistoryQuoting.Avalonia;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Ecng.ComponentModel;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Commissions;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

internal sealed class HistoryStrategySession : IAsyncDisposable
{
	private int _disposeState;

	private HistoryStrategySession(StorageRegistry storageRegistry, HistoryEmulationConnector connector, Strategy strategy)
	{
		StorageRegistry = storageRegistry;
		Connector = connector;
		Strategy = strategy;
	}

	public StorageRegistry StorageRegistry { get; }

	public HistoryEmulationConnector Connector { get; }

	public Strategy Strategy { get; }

	public static HistoryStrategySession Create(
		DateTime startDate,
		DateTime stopDate,
		decimal beginValue,
		Func<Security, Portfolio, HistoryEmulationConnector, Strategy> strategyFactory)
	{
		ArgumentNullException.ThrowIfNull(strategyFactory);
		if (stopDate < startDate)
			throw new ArgumentOutOfRangeException(nameof(stopDate), "The end date must not precede the begin date.");

		StorageRegistry storageRegistry = null;
		HistoryEmulationConnector connector = null;
		Strategy strategy = null;
		try
		{
			var security = new Security
			{
				Id = Paths.HistoryDefaultSecurity,
				PriceStep = 0.01m,
			};
			var portfolio = new Portfolio
			{
				Name = "test account",
				BeginValue = beginValue,
			};
			storageRegistry = new StorageRegistry
			{
				DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath),
			};
			connector = new HistoryEmulationConnector([security], [portfolio])
			{
				HistoryMessageAdapter =
				{
					StorageRegistry = storageRegistry,
					StorageFormat = StorageFormats.Binary,
					StartDate = startDate,
					StopDate = stopDate,
				},
				LogLevel = LogLevels.Info,
				SupportFilteredMarketDepth = true,
			};

			strategy = strategyFactory(security, portfolio, connector)
				?? throw new InvalidOperationException("The strategy factory returned null.");
			return new(storageRegistry, connector, strategy);
		}
		catch
		{
			strategy?.Dispose();
			connector?.Dispose();
			storageRegistry?.Dispose();
			throw;
		}
	}

	public async ValueTask StartAsync(CancellationToken cancellationToken)
	{
		await Strategy.StartAsync(cancellationToken);
		Connector.Connect();
		await Connector.SendInMessageAsync(new CommissionRuleMessage
		{
			Rule = new CommissionTradeRule { Value = 0.01m },
		}, cancellationToken);
		await Connector.StartAsync(cancellationToken);
	}

	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _disposeState, 1) != 0)
			return;

		var errors = new List<Exception>();
		try
		{
			if (Strategy.ProcessState != ProcessStates.Stopped)
			{
				using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
				await Strategy.StopAsync(timeout.Token);
			}
		}
		catch (Exception error)
		{
			errors.Add(error);
		}

		try
		{
			if (Connector.ConnectionState == ConnectionStates.Connected)
				Connector.Disconnect();
		}
		catch (Exception error)
		{
			errors.Add(error);
		}

		TryDispose(Strategy, errors);
		TryDispose(Connector, errors);
		TryDispose(StorageRegistry, errors);
		if (errors.Count > 0)
			throw new AggregateException(errors);
	}

	private static void TryDispose(IDisposable disposable, ICollection<Exception> errors)
	{
		try
		{
			disposable.Dispose();
		}
		catch (Exception error)
		{
			errors.Add(error);
		}
	}
}
