namespace StockSharp.Samples.Strategies.HistorySMA.Avalonia;

using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
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

/// <summary>
/// Owns one historical emulation run. The storage registry remains alive until
/// the strategy and connector have stopped because the history adapter borrows it.
/// </summary>
internal sealed class HistoryStrategySession : IAsyncDisposable
{
	private int _disposeState;

	private HistoryStrategySession(
		StorageRegistry storageRegistry,
		HistoryEmulationConnector connector,
		Strategy strategy)
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

			storageRegistry = new StorageRegistry();
			storageRegistry.DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath);

			connector = new HistoryEmulationConnector([security], [portfolio]);
			connector.HistoryMessageAdapter.StorageRegistry = storageRegistry;
			connector.HistoryMessageAdapter.StorageFormat = StorageFormats.Binary;
			connector.HistoryMessageAdapter.StartDate = startDate;
			connector.HistoryMessageAdapter.StopDate = stopDate;
			connector.LogLevel = LogLevels.Info;

			strategy = strategyFactory(security, portfolio, connector)
				?? throw new InvalidOperationException("The strategy factory returned null.");
			return new(storageRegistry, connector, strategy);
		}
		catch (Exception initializationError)
		{
			var errors = new List<Exception> { initializationError };
			if (strategy is not null)
				TryDispose(strategy, errors);
			if (connector is not null)
				TryDispose(connector, errors);
			if (storageRegistry is not null)
				TryDispose(storageRegistry, errors);

			if (errors.Count > 1)
				throw new AggregateException("The historical strategy session failed to initialize and clean up.", errors);

			ExceptionDispatchInfo.Capture(initializationError).Throw();
			throw;
		}
	}

	public async ValueTask StartAsync(bool applyTradeCommission, CancellationToken cancellationToken)
	{
		await Strategy.StartAsync(cancellationToken);
		Connector.Connect();

		if (applyTradeCommission)
		{
			await Connector.SendInMessageAsync(new CommissionRuleMessage
			{
				Rule = new CommissionTradeRule { Value = 0.01m },
			}, cancellationToken);
		}

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
				using var stopCancellation = new CancellationTokenSource(TimeSpan.FromSeconds(10));
				await Strategy.StopAsync(stopCancellation.Token);
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
