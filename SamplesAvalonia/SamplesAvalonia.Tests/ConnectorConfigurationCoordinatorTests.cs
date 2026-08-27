namespace StockSharp.Samples.Avalonia.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;

using Ecng.Common;
using Ecng.Serialization;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using StockSharp.Algo;
using StockSharp.Algo.Candles.Compression;
using StockSharp.Algo.Storages;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

[TestClass]
public class ConnectorConfigurationCoordinatorTests
{
	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Cancel_PersistsWindowStateButLeavesLiveBasketUntouched()
	{
		var connector = new Connector();

		try
		{
			connector.Adapter.SupportOffline = false;
			var store = new RecordingSettingsStore();
			var dialog = new RecordingDialog(false, static adapter =>
				adapter.InnerAdapters.Add(new CoordinatorTestMessageAdapter(adapter.TransactionIdGenerator)));
			var coordinator = new ConnectorConfigurationCoordinator(connector, store);

			var accepted = await coordinator.ConfigureAsync(dialog);

			Assert.IsFalse(accepted);
			Assert.AreEqual(0, connector.Adapter.InnerAdapters.Count, "A cancelled edit mutated the live basket.");
			Assert.AreEqual(1, dialog.CallCount);
			Assert.AreNotSame(connector.Adapter, dialog.LastWorkingAdapter);
			Assert.IsTrue(dialog.LastWorkingAdapter.IsDisposed, "The cancelled working basket leaked.");
			Assert.AreEqual(0, store.ConnectorSaveCount);
			Assert.AreEqual(1, store.WindowSaveCount);
			Assert.IsFalse(coordinator.AutoConnect, "A cancelled edit changed AutoConnect.");
		}
		finally
		{
			connector.Dispose();
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Accept_AppliesCloneAndPersistsConnectorAndWindowOnce()
	{
		var connector = new Connector();

		try
		{
			connector.Adapter.SupportOffline = false;
			var store = new RecordingSettingsStore();
			var dialog = new RecordingDialog(true, static adapter =>
				adapter.InnerAdapters.Add(new CoordinatorTestMessageAdapter(adapter.TransactionIdGenerator)));
			var coordinator = new ConnectorConfigurationCoordinator(connector, store);

			var accepted = await coordinator.ConfigureAsync(dialog);

			Assert.IsTrue(accepted);
			Assert.AreEqual(1, connector.Adapter.InnerAdapters.Count, "The accepted working basket was not applied.");
			Assert.AreEqual(1, dialog.CallCount);
			Assert.AreNotSame(connector.Adapter, dialog.LastWorkingAdapter);
			Assert.IsTrue(dialog.LastWorkingAdapter.IsDisposed, "The accepted working basket leaked.");
			Assert.AreEqual(1, store.ConnectorSaveCount);
			Assert.AreEqual(1, store.WindowSaveCount);
			Assert.IsNotNull(store.SavedConnector);
			Assert.IsNotNull(store.SavedWindow);
			Assert.IsTrue(coordinator.AutoConnect);
			Assert.IsTrue(store.SavedConnector.GetValue(nameof(coordinator.AutoConnect), false));
		}
		finally
		{
			connector.Dispose();
		}
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Configure_IsSingleFlightAndRejectsACompetingDialog()
	{
		var connector = new Connector();

		try
		{
			var store = new RecordingSettingsStore();
			var dialog = new BlockingDialog();
			var coordinator = new ConnectorConfigurationCoordinator(connector, store);
			var first = coordinator.ConfigureAsync(dialog);
			await dialog.Entered;
			var firstResult = true;

			try
			{
				var second = await coordinator.ConfigureAsync(dialog);

				Assert.IsFalse(second);
				Assert.AreEqual(1, dialog.CallCount);
				Assert.AreEqual(1, dialog.MaximumActiveCount);
			}
			finally
			{
				dialog.Release();
				firstResult = await first;
			}

			Assert.IsFalse(firstResult);
		}
		finally
		{
			connector.Dispose();
		}
	}

	[TestMethod]
	[Timeout(10_000)]
	public void Load_RoundTripsWpfConnectorSettingsSchemaAndAutoConnect()
	{
		using var source = new Connector();
		source.Adapter.InnerAdapters.Add(new CoordinatorTestMessageAdapter(source.TransactionIdGenerator));
		var persisted = source.Save();
		persisted.SetValue("AutoConnect", true);

		using var target = new Connector();
		var store = new RecordingSettingsStore(persisted);
		var coordinator = new ConnectorConfigurationCoordinator(target, store);

		coordinator.Load();

		Assert.AreEqual(1, target.Adapter.InnerAdapters.Count,
			"The top-level Connector.Adapter settings produced by the WPF sample were not loaded.");
		Assert.IsInstanceOfType<CoordinatorTestMessageAdapter>(target.Adapter.InnerAdapters[0]);
		Assert.IsTrue(coordinator.AutoConnect);
	}

	[TestMethod]
	[Timeout(10_000, CooperativeCancellation = true)]
	public async Task Accept_ApplyFailureRestoresLiveAdapterSnapshot()
	{
		using var connector = new RollbackTestConnector();
		connector.TestAdapter.InnerAdapters.Add(
			new CoordinatorTestMessageAdapter(connector.TransactionIdGenerator));
		var store = new RecordingSettingsStore();
		var dialog = new RecordingDialog(true, static adapter =>
			adapter.InnerAdapters.Add(new CoordinatorTestMessageAdapter(adapter.TransactionIdGenerator)));
		var coordinator = new ConnectorConfigurationCoordinator(connector, store);
		connector.TestAdapter.FailNextLoad = true;

		try
		{
			await coordinator.ConfigureAsync(dialog);
			Assert.Fail("The injected adapter-load failure was not surfaced.");
		}
		catch (InvalidOperationException error)
		{
			Assert.AreEqual(RollbackTestBasketMessageAdapter.FailureMessage, error.Message);
		}

		Assert.AreEqual(1, connector.Adapter.InnerAdapters.Count,
			"The live adapter was not restored after a partial apply failure.");
		Assert.IsInstanceOfType<CoordinatorTestMessageAdapter>(connector.Adapter.InnerAdapters[0]);
		Assert.AreEqual(0, store.ConnectorSaveCount);
		Assert.AreEqual(1, store.WindowSaveCount);
	}

	private sealed class RecordingDialog(
		bool accepted,
		Action<BasketMessageAdapter> edit) : ISampleConnectorConfigurationDialog
	{
		public int CallCount { get; private set; }

		public BasketMessageAdapter LastWorkingAdapter { get; private set; }

		public Task<SampleConnectorConfigurationResult> EditAsync(
			BasketMessageAdapter workingAdapter,
			SettingsStorage windowSettings,
			bool autoConnect,
			CancellationToken cancellationToken)
		{
			cancellationToken.ThrowIfCancellationRequested();
			CallCount++;
			LastWorkingAdapter = workingAdapter;
			edit(workingAdapter);
			return Task.FromResult(new SampleConnectorConfigurationResult(accepted, true, new SettingsStorage()));
		}
	}

	private sealed class RecordingSettingsStore(SettingsStorage connectorSettings = null) : ISampleConnectorSettingsStore
	{
		public int ConnectorSaveCount { get; private set; }

		public int WindowSaveCount { get; private set; }

		public SettingsStorage SavedConnector { get; private set; }

		public SettingsStorage SavedWindow { get; private set; }

		public SettingsStorage LoadConnector() => connectorSettings;

		public SettingsStorage LoadWindow() => null;

		public void SaveConnector(SettingsStorage settings)
		{
			ConnectorSaveCount++;
			SavedConnector = settings;
		}

		public void SaveWindow(SettingsStorage settings)
		{
			WindowSaveCount++;
			SavedWindow = settings;
		}
	}

	private sealed class BlockingDialog : ISampleConnectorConfigurationDialog
	{
		private readonly TaskCompletionSource _entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private int _activeCount;

		public Task Entered => _entered.Task;

		public int CallCount { get; private set; }

		public int MaximumActiveCount { get; private set; }

		public async Task<SampleConnectorConfigurationResult> EditAsync(
			BasketMessageAdapter workingAdapter,
			SettingsStorage windowSettings,
			bool autoConnect,
			CancellationToken cancellationToken)
		{
			CallCount++;
			var active = Interlocked.Increment(ref _activeCount);
			MaximumActiveCount = Math.Max(MaximumActiveCount, active);
			_entered.TrySetResult();

			try
			{
				await _release.Task.WaitAsync(cancellationToken);
				return new(false, autoConnect, new SettingsStorage());
			}
			finally
			{
				Interlocked.Decrement(ref _activeCount);
			}
		}

		public void Release() => _release.TrySetResult();
	}

	private sealed class RollbackTestConnector : Connector
	{
		public RollbackTestConnector()
			: base(
				new InMemorySecurityStorage(),
				new InMemoryPositionStorage(),
				new InMemoryExchangeInfoProvider(),
				initAdapter: false)
		{
			TestAdapter = new(
				new MillisecondIncrementalIdGenerator(),
				new CandleBuilderProvider(ExchangeInfoProvider),
				new InMemorySecurityMessageAdapterProvider(),
				new InMemoryPortfolioMessageAdapterProvider());
			Adapter = TestAdapter;
		}

		public RollbackTestBasketMessageAdapter TestAdapter { get; }
	}

	private sealed class RollbackTestBasketMessageAdapter(
		IdGenerator transactionIdGenerator,
		CandleBuilderProvider candleBuilderProvider,
		ISecurityMessageAdapterProvider securityAdapterProvider,
		IPortfolioMessageAdapterProvider portfolioAdapterProvider)
		: BasketMessageAdapter(
			transactionIdGenerator,
			candleBuilderProvider,
			securityAdapterProvider,
			portfolioAdapterProvider,
			null)
	{
		public const string FailureMessage = "Injected adapter load failure.";

		public bool FailNextLoad { get; set; }

		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			if (!FailNextLoad)
				return;

			FailNextLoad = false;
			throw new InvalidOperationException(FailureMessage);
		}
	}
}

public sealed class CoordinatorTestMessageAdapter(IdGenerator transactionIdGenerator)
	: MessageAdapter(transactionIdGenerator)
{
	protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
		=> default;

	public override IMessageAdapter Clone()
		=> new CoordinatorTestMessageAdapter(TransactionIdGenerator);
}
