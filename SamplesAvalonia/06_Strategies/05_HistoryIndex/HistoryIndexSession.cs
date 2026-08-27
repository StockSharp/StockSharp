namespace StockSharp.Samples.Strategies.HistoryIndex.Avalonia;

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Ecng.ComponentModel;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Expressions;
using StockSharp.Algo.Storages;
using StockSharp.Algo.Testing;
using StockSharp.BusinessEntities;
using StockSharp.Configuration;
using StockSharp.Messages;

internal sealed class HistoryIndexSession : IAsyncDisposable
{
	private int _disposeState;

	private HistoryIndexSession(
		StorageRegistry storageRegistry,
		HistoryEmulationConnector connector,
		Security security,
		ExpressionIndexSecurity indexSecurity,
		Portfolio portfolio,
		Subscription indexSubscription)
	{
		StorageRegistry = storageRegistry;
		Connector = connector;
		Security = security;
		IndexSecurity = indexSecurity;
		Portfolio = portfolio;
		IndexSubscription = indexSubscription;
	}

	public StorageRegistry StorageRegistry { get; }

	public HistoryEmulationConnector Connector { get; }

	public Security Security { get; }

	public ExpressionIndexSecurity IndexSecurity { get; }

	public Portfolio Portfolio { get; }

	public Subscription IndexSubscription { get; }

	public static HistoryIndexSession Create(
		DateTime startDate,
		DateTime stopDate,
		DataType candleType,
		string expression)
	{
		if (stopDate < startDate)
			throw new ArgumentOutOfRangeException(nameof(stopDate), "The end date must not precede the begin date.");
		ArgumentNullException.ThrowIfNull(candleType);
		if (string.IsNullOrWhiteSpace(expression))
			throw new ArgumentException("Specify an index expression.", nameof(expression));

		StorageRegistry storageRegistry = null;
		HistoryEmulationConnector connector = null;
		ExpressionIndexSecurity indexSecurity = null;

		try
		{
			var security = new Security
			{
				Id = Paths.HistoryDefaultSecurity,
				PriceStep = 0.01m,
				Board = ExchangeBoard.Binance,
			};
			indexSecurity = new ExpressionIndexSecurity
			{
				Id = $"INDEX@{ExchangeBoard.Binance.Code}",
				Expression = expression,
				Board = ExchangeBoard.Binance,
			};
			if (!string.IsNullOrEmpty(indexSecurity.Formula.Error))
				throw new InvalidOperationException(indexSecurity.Formula.Error);

			var portfolio = new Portfolio
			{
				Name = "test portfolio",
				BeginValue = 10_000_000m,
			};
			storageRegistry = new StorageRegistry
			{
				DefaultDrive = new LocalMarketDataDrive(Paths.FileSystem, Paths.HistoryDataPath),
			};
			connector = new HistoryEmulationConnector([security, indexSecurity], [portfolio])
			{
				HistoryMessageAdapter =
				{
					StorageRegistry = storageRegistry,
					StorageFormat = StorageFormats.Binary,
					StartDate = startDate,
					StopDate = stopDate,
				},
				LogLevel = LogLevels.Info,
			};

			var subscription = new Subscription(candleType, indexSecurity)
			{
				MarketData =
				{
					BuildMode = MarketDataBuildModes.Build,
					BuildFrom = DataType.Ticks,
				},
			};

			return new(storageRegistry, connector, security, indexSecurity, portfolio, subscription);
		}
		catch
		{
			connector?.Dispose();
			storageRegistry?.Dispose();
			if (indexSecurity is IDisposable disposable)
				disposable.Dispose();
			throw;
		}
	}

	public async ValueTask StartAsync(CancellationToken cancellationToken)
	{
		var connected = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		void OnConnected() => connected.TrySetResult();

		Connector.Connected += OnConnected;
		try
		{
			Connector.Connect();
			await connected.Task.WaitAsync(cancellationToken);
			Connector.Subscribe(new Subscription(DataType.Ticks, Security));
			Connector.Subscribe(IndexSubscription);
			await Connector.StartAsync(cancellationToken);
		}
		finally
		{
			Connector.Connected -= OnConnected;
		}
	}

	public async ValueTask DisposeAsync()
	{
		if (Interlocked.Exchange(ref _disposeState, 1) != 0)
			return;

		var errors = new List<Exception>();
		try
		{
			if (Connector.ConnectionState == ConnectionStates.Connected)
				Connector.Disconnect();
		}
		catch (Exception error)
		{
			errors.Add(error);
		}

		TryDispose(Connector, errors);
		TryDispose(StorageRegistry, errors);
		TryDispose(IndexSecurity, errors);
		await Task.CompletedTask;

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
