namespace StockSharp.Tests;

using System.Collections.Concurrent;

using StockSharp.Algo.Basket;
using StockSharp.Algo.Candles.Compression;

/// <summary>
/// Shared infrastructure for BasketMessageAdapter tests.
/// </summary>
public abstract class BasketTestBase : BaseTestClass
{
	protected sealed class TestBasketInnerAdapter : MessageAdapter
	{
		private readonly ConcurrentQueue<Message> _inMessages = [];

		public TestBasketInnerAdapter(IdGenerator idGen)
			: base(idGen)
		{
			this.AddMarketDataSupport();
			this.AddTransactionalSupport();
			this.AddSupportedMessage(MessageTypes.SecurityLookup, null);
			this.AddSupportedMessage(MessageTypes.PortfolioLookup, null);
			this.AddSupportedMessage(MessageTypes.OrderStatus, null);
			this.AddSupportedMessage(MessageTypes.MarketData, null);
			this.AddSupportedMessage(MessageTypes.OrderRegister, null);
			this.AddSupportedMessage(MessageTypes.OrderCancel, null);
			this.AddSupportedMarketDataType(DataType.Ticks);
			this.AddSupportedMarketDataType(DataType.MarketDepth);
			this.AddSupportedMarketDataType(DataType.Level1);
		}

		public IReadOnlyList<Message> ReceivedMessages => [.. _inMessages];
		public IEnumerable<T> GetMessages<T>() where T : Message => _inMessages.OfType<T>();
		public bool AutoRespond { get; set; } = true;
		public Exception ConnectError { get; set; }

		protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken ct)
		{
			_inMessages.Enqueue(message.TypedClone());

			if (!AutoRespond)
				return;

			switch (message.Type)
			{
				case MessageTypes.Reset:
					await SendOutMessageAsync(new ResetMessage(), ct);
					break;
				case MessageTypes.Connect:
					await SendOutMessageAsync(ConnectError != null
						? new ConnectMessage { Error = ConnectError }
						: new ConnectMessage(), ct);
					break;
				case MessageTypes.Disconnect:
					await SendOutMessageAsync(new DisconnectMessage(), ct);
					break;
				case MessageTypes.MarketData:
				{
					var md = (MarketDataMessage)message;
					await SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = md.TransactionId }, ct);
					if (md.IsSubscribe)
						await SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = md.TransactionId }, ct);
					break;
				}
				case MessageTypes.SecurityLookup:
				{
					var sl = (SecurityLookupMessage)message;
					await SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = sl.TransactionId }, ct);
					await SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = sl.TransactionId }, ct);
					break;
				}
				case MessageTypes.PortfolioLookup:
				{
					var pl = (PortfolioLookupMessage)message;
					await SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = pl.TransactionId }, ct);

					var pfMsg = new PortfolioMessage
					{
						PortfolioName = "TestPortfolio",
						OriginalTransactionId = pl.TransactionId,
					};
					pfMsg.SetSubscriptionIds([pl.TransactionId]);
					await SendOutMessageAsync(pfMsg, ct);

					await SendOutMessageAsync(new SubscriptionFinishedMessage { OriginalTransactionId = pl.TransactionId }, ct);
					break;
				}
				case MessageTypes.OrderStatus:
				{
					var os = (OrderStatusMessage)message;
					await SendOutMessageAsync(new SubscriptionResponseMessage { OriginalTransactionId = os.TransactionId }, ct);

					var orderMsg = new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						SecurityId = Helper.CreateSecurityId(),
						OriginalTransactionId = os.TransactionId,
						HasOrderInfo = true,
						OrderState = OrderStates.Active,
						OrderPrice = 100,
						OrderVolume = 10,
						ServerTime = DateTime.UtcNow,
						LocalTime = DateTime.UtcNow,
					};
					orderMsg.SetSubscriptionIds([os.TransactionId]);
					await SendOutMessageAsync(orderMsg, ct);

					await SendOutMessageAsync(new SubscriptionOnlineMessage { OriginalTransactionId = os.TransactionId }, ct);
					break;
				}
				case MessageTypes.OrderRegister:
				{
					var reg = (OrderRegisterMessage)message;
					await SendOutMessageAsync(new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						SecurityId = reg.SecurityId,
						OriginalTransactionId = reg.TransactionId,
						OrderState = OrderStates.Active,
						HasOrderInfo = true,
						ServerTime = DateTime.UtcNow,
						LocalTime = DateTime.UtcNow,
					}, ct);
					break;
				}
				case MessageTypes.OrderCancel:
				{
					var cancel = (OrderCancelMessage)message;
					await SendOutMessageAsync(new ExecutionMessage
					{
						DataTypeEx = DataType.Transactions,
						SecurityId = cancel.SecurityId,
						OriginalTransactionId = cancel.TransactionId,
						OrderState = OrderStates.Done,
						HasOrderInfo = true,
						ServerTime = DateTime.UtcNow,
						LocalTime = DateTime.UtcNow,
					}, ct);
					break;
				}
			}
		}

		public override IMessageAdapter Clone() => new TestBasketInnerAdapter(TransactionIdGenerator);
	}

	protected static readonly SecurityId SecId1 = "AAPL@NASDAQ".ToSecurityId();
	protected static readonly SecurityId SecId2 = "SBER@MOEX".ToSecurityId();
	protected const string Portfolio1 = "Portfolio1";
	protected const string Portfolio2 = "Portfolio2";

	protected ConcurrentQueue<Message> OutMessages;

	protected (BasketMessageAdapter basket, TestBasketInnerAdapter adapter1, TestBasketInnerAdapter adapter2)
		CreateBasket(
			IAdapterConnectionState connectionState = null,
			IAdapterConnectionManager connectionManager = null,
			IPendingMessageState pendingState = null,
			ISubscriptionRoutingState subscriptionRouting = null,
			IParentChildMap parentChildMap = null,
			IOrderRoutingState orderRouting = null,
			bool twoAdapters = true)
	{
		var idGen = new IncrementalIdGenerator();
		var candleBuilderProvider = new CandleBuilderProvider(new InMemoryExchangeInfoProvider());

		var cs = connectionState ?? new AdapterConnectionState();
		var cm = connectionManager ?? new AdapterConnectionManager(cs);
		var ps = pendingState ?? new PendingMessageState();
		var sr = subscriptionRouting ?? new SubscriptionRoutingState();
		var pcm = parentChildMap ?? new ParentChildMap();
		var or = orderRouting ?? new OrderRoutingState();

		var routingManager = new BasketRoutingManager(
			cs, cm, ps, sr, pcm, or,
			a => a, candleBuilderProvider, () => false, idGen);

		var basket = new BasketMessageAdapter(
			idGen,
			candleBuilderProvider,
			new InMemorySecurityMessageAdapterProvider(),
			new InMemoryPortfolioMessageAdapterProvider(),
			null,
			null,
			routingManager);

		basket.IgnoreExtraAdapters = true;
		basket.LatencyManager = null;
		basket.SlippageManager = null;
		basket.CommissionManager = null;

		var adapter1 = new TestBasketInnerAdapter(idGen);
		basket.InnerAdapters.Add(adapter1);
		basket.ApplyHeartbeat(adapter1, false);

		TestBasketInnerAdapter adapter2 = null;

		if (twoAdapters)
		{
			adapter2 = new TestBasketInnerAdapter(idGen);
			basket.InnerAdapters.Add(adapter2);
			basket.ApplyHeartbeat(adapter2, false);
		}

		OutMessages = [];
		basket.NewOutMessageAsync += (msg, ct) =>
		{
			OutMessages.Enqueue(msg);
			return default;
		};

		return (basket, adapter1, adapter2);
	}

	protected static async Task SendToBasket(BasketMessageAdapter basket, Message message, CancellationToken ct = default)
	{
		await ((IMessageTransport)basket).SendInMessageAsync(message, ct);
	}

	protected T[] GetOut<T>() where T : Message
		=> [.. OutMessages.OfType<T>()];

	protected void ClearOut() => OutMessages = [];

	// Strict broadcast output verification
	protected void AssertBroadcastOutput(
		long parentId, long[] childIds,
		int expectedResponse, bool? expectError,
		int expectedFinished, int expectedOnline)
	{
		var responses = GetOut<SubscriptionResponseMessage>()
			.Where(r => r.OriginalTransactionId == parentId).ToArray();
		responses.Length.AssertEqual(expectedResponse, $"Parent SubscriptionResponse count");

		if (expectedResponse > 0 && expectError.HasValue)
		{
			if (expectError.Value)
				responses[0].Error.AssertNotNull("Parent response should have error");
			else
				responses[0].Error.AssertNull("Parent response should not have error");
		}

		GetOut<SubscriptionFinishedMessage>()
			.Count(m => m.OriginalTransactionId == parentId)
			.AssertEqual(expectedFinished, $"Parent SubscriptionFinished count");

		GetOut<SubscriptionOnlineMessage>()
			.Count(m => m.OriginalTransactionId == parentId)
			.AssertEqual(expectedOnline, $"Parent SubscriptionOnline count");

		// No child messages should leak to output
		foreach (var childId in childIds)
		{
			GetOut<SubscriptionResponseMessage>()
				.Any(r => r.OriginalTransactionId == childId)
				.AssertFalse($"Child {childId} SubscriptionResponse leaked");

			GetOut<SubscriptionFinishedMessage>()
				.Any(m => m.OriginalTransactionId == childId)
				.AssertFalse($"Child {childId} SubscriptionFinished leaked");

			GetOut<SubscriptionOnlineMessage>()
				.Any(m => m.OriginalTransactionId == childId)
				.AssertFalse($"Child {childId} SubscriptionOnline leaked");
		}
	}
}
