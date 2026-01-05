namespace StockSharp.Tests;

using StockSharp.Algo.Testing;
using StockSharp.Algo.Testing.Generation;

[TestClass]
public class HistoryMessageAdapterTests : BaseTestClass
{
	private static SecurityId CreateSecurityId() => Helper.CreateSecurityId();

	private static Mock<ISecurityProvider> CreateSecurityProviderMock()
	{
		var mock = new Mock<ISecurityProvider>();
		mock.Setup(p => p.LookupAll()).Returns([]);
		return mock;
	}

	private static Mock<IHistoryMarketDataManager> CreateManagerMock()
	{
		var mock = new Mock<IHistoryMarketDataManager>();
		mock.SetupProperty(m => m.StartDate, DateTime.MinValue);
		mock.SetupProperty(m => m.StopDate, DateTime.MaxValue);
		mock.SetupProperty(m => m.MarketTimeChangedInterval, TimeSpan.FromSeconds(1));
		mock.SetupProperty(m => m.PostTradeMarketTimeChangedCount, 2);
		mock.SetupProperty(m => m.CheckTradableDates, false);
		mock.SetupProperty(m => m.StorageRegistry, null);
		mock.SetupProperty(m => m.Drive, null);
		mock.SetupProperty(m => m.StorageFormat, StorageFormats.Binary);
		mock.SetupProperty(m => m.StorageCache, null);
		mock.SetupProperty(m => m.AdapterCache, null);
		mock.Setup(m => m.LoadedMessageCount).Returns(0);
		mock.Setup(m => m.CurrentTime).Returns(DateTime.MinValue);
		mock.Setup(m => m.IsStarted).Returns(false);
		return mock;
	}

	#region Constructor Tests

	[TestMethod]
	public void Constructor_WithSecurityProvider_CreatesDefaultManager()
	{
		var secProvider = CreateSecurityProviderMock();

		using var adapter = new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider.Object);

		adapter.SecurityProvider.AssertEqual(secProvider.Object);
	}

	[TestMethod]
	public void Constructor_WithManager_UsesProvidedManager()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		adapter.SecurityProvider.AssertEqual(secProvider.Object);
	}

	[TestMethod]
	public void Constructor_ThrowsOnNullSecurityProvider()
	{
		ThrowsExactly<ArgumentNullException>(() =>
			new HistoryMessageAdapter(new IncrementalIdGenerator(), null));
	}

	[TestMethod]
	public void Constructor_ThrowsOnNullManager()
	{
		var secProvider = CreateSecurityProviderMock();

		ThrowsExactly<ArgumentNullException>(() =>
			new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider.Object, null));
	}

	#endregion

	#region Property Delegation Tests

	[TestMethod]
	public void Properties_DelegateToManager()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		var startDate = DateTime.UtcNow;
		var stopDate = DateTime.UtcNow.AddDays(1);
		var interval = TimeSpan.FromMinutes(5);

		adapter.StartDate = startDate;
		adapter.StopDate = stopDate;
		adapter.MarketTimeChangedInterval = interval;
		adapter.PostTradeMarketTimeChangedCount = 5;
		adapter.CheckTradableDates = true;

		manager.Object.StartDate.AssertEqual(startDate);
		manager.Object.StopDate.AssertEqual(stopDate);
		manager.Object.MarketTimeChangedInterval.AssertEqual(interval);
		manager.Object.PostTradeMarketTimeChangedCount.AssertEqual(5);
		manager.Object.CheckTradableDates.AssertEqual(true);
	}

	[TestMethod]
	public void LoadedMessageCount_DelegatesToManager()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();
		manager.Setup(m => m.LoadedMessageCount).Returns(42);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		adapter.LoadedMessageCount.AssertEqual(42);
	}

	[TestMethod]
	public void CurrentTimeUtc_DelegatesToManager()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();
		var expectedTime = DateTime.UtcNow;
		manager.Setup(m => m.CurrentTime).Returns(expectedTime);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		adapter.CurrentTimeUtc.AssertEqual(expectedTime);
	}

	#endregion

	#region Adapter Properties Tests

	[TestMethod]
	public void UseOutChannel_ReturnsFalse()
	{
		var secProvider = CreateSecurityProviderMock();

		using var adapter = new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider.Object);

		adapter.UseOutChannel.AssertFalse();
	}

	[TestMethod]
	public void IsFullCandlesOnly_ReturnsFalse()
	{
		var secProvider = CreateSecurityProviderMock();

		using var adapter = new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider.Object);

		adapter.IsFullCandlesOnly.AssertFalse();
	}

	[TestMethod]
	public void IsSupportCandlesUpdates_ReturnsTrue()
	{
		var secProvider = CreateSecurityProviderMock();

		using var adapter = new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider.Object);

		var subscription = new MarketDataMessage
		{
			DataType2 = DataType.CandleTimeFrame,
			IsSubscribe = true,
		};

		adapter.IsSupportCandlesUpdates(subscription).AssertTrue();
	}

	[TestMethod]
	public void IsAllDownloadingSupported_ReturnsTrueForSecurities()
	{
		var secProvider = CreateSecurityProviderMock();

		using var adapter = new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider.Object);

		adapter.IsAllDownloadingSupported(DataType.Securities).AssertTrue();
	}

	#endregion

	#region Generator Tests

	[TestMethod]
	public async Task GeneratorMessage_Subscribe_RegistersGenerator()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();
		var secId = CreateSecurityId();
		var generator = new RandomWalkTradeGenerator(secId);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		var generatorMsg = new GeneratorMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			Generator = generator,
			TransactionId = 1,
			IsSubscribe = true,
		};

		await adapter.SendInMessageAsync(generatorMsg, CancellationToken);

		manager.Verify(m => m.RegisterGenerator(secId, DataType.Ticks, generator, 1), Times.Once);
	}

	[TestMethod]
	public async Task GeneratorMessage_Unsubscribe_UnregistersGenerator()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();
		var secId = CreateSecurityId();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		var generatorMsg = new GeneratorMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			OriginalTransactionId = 1,
			IsSubscribe = false,
		};

		await adapter.SendInMessageAsync(generatorMsg, CancellationToken);

		manager.Verify(m => m.UnregisterGenerator(1), Times.Once);
	}

	#endregion

	#region Reset Tests

	[TestMethod]
	public async Task ResetMessage_CallsManagerReset()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();
		manager.Setup(m => m.IsStarted).Returns(false);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		manager.Verify(m => m.Reset(), Times.Once);
	}

	#endregion

	#region Connect Tests

	[TestMethod]
	public async Task ConnectMessage_ThrowsWhenAlreadyStarted()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();
		manager.Setup(m => m.IsStarted).Returns(true);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		await ThrowsExactlyAsync<InvalidOperationException>(() =>
			adapter.SendInMessageAsync(new ConnectMessage(), CancellationToken).AsTask());
	}

	#endregion

	#region Disconnect Tests

	[TestMethod]
	public async Task DisconnectMessage_StopsManager()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		await adapter.SendInMessageAsync(new DisconnectMessage(), CancellationToken);

		manager.Verify(m => m.Stop(), Times.Once);
	}

	#endregion

	#region MarketData Tests

	[TestMethod]
	public async Task MarketDataMessage_Subscribe_CallsManagerSubscribe()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();
		var secId = CreateSecurityId();

		manager.Setup(m => m.SubscribeAsync(It.IsAny<MarketDataMessage>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((Exception)null);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 1,
			IsSubscribe = true,
		};

		await adapter.SendInMessageAsync(mdMsg, CancellationToken);

		manager.Verify(m => m.SubscribeAsync(mdMsg, It.IsAny<CancellationToken>()), Times.Once);
	}

	[TestMethod]
	public async Task MarketDataMessage_Unsubscribe_CallsManagerUnsubscribe()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();
		var secId = CreateSecurityId();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		var mdMsg = new MarketDataMessage
		{
			SecurityId = secId,
			DataType2 = DataType.Ticks,
			TransactionId = 2,
			OriginalTransactionId = 1,
			IsSubscribe = false,
		};

		await adapter.SendInMessageAsync(mdMsg, CancellationToken);

		manager.Verify(m => m.Unsubscribe(1), Times.Once);
	}

	#endregion

	#region GetSupportedMarketDataTypes Tests

	[TestMethod]
	public void GetSupportedMarketDataTypes_DelegatesToManager()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();
		var secId = CreateSecurityId();
		var expectedTypes = new[] { DataType.Ticks, DataType.Level1 };

		manager.Setup(m => m.GetSupportedDataTypes(secId)).Returns(expectedTypes);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		var result = adapter.GetSupportedMarketDataTypes(secId, null, null).ToList();

		result.Count.AssertEqual(2);
		result.Contains(DataType.Ticks).AssertTrue();
		result.Contains(DataType.Level1).AssertTrue();
	}

	#endregion

	#region ToString Tests

	[TestMethod]
	public void ToString_ReturnsFormattedString()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();
		var startDate = new DateTime(2024, 1, 1);
		var stopDate = new DateTime(2024, 12, 31);

		manager.Setup(m => m.StartDate).Returns(startDate);
		manager.Setup(m => m.StopDate).Returns(stopDate);

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		var result = adapter.ToString();

		result.Contains("Hist:").AssertTrue();
	}

	#endregion

	#region EmulationState Tests

	[TestMethod]
	public async Task EmulationStateMessage_Stopping_StopsManager()
	{
		var secProvider = CreateSecurityProviderMock();
		var manager = CreateManagerMock();

		using var adapter = new HistoryMessageAdapter(
			new IncrementalIdGenerator(),
			secProvider.Object,
			manager.Object);

		var stateMsg = new EmulationStateMessage
		{
			State = ChannelStates.Stopping,
		};

		await adapter.SendInMessageAsync(stateMsg, CancellationToken);

		manager.Verify(m => m.Stop(), Times.Once);
	}

	#endregion
}
