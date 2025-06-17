namespace StockSharp.Tests;

[TestClass]
public class OrderLogTests
{
	private static readonly SecurityId _secId = Helper.CreateSecurityId();

	private static ExecutionMessage CreateOrderLogMessage(
		long? orderId = null,
		string orderStringId = null,
		Sides side = Sides.Buy,
		decimal price = 100m,
		decimal? volume = 10m,
		OrderStates? orderState = OrderStates.Active,
		decimal? tradeVolume = null)
	{
		return new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			SecurityId = _secId,
			HasOrderInfo = orderId is not null,
			OrderId = orderId,
			OrderStringId = orderStringId,
			Side = side,
			OrderPrice = price,
			OrderVolume = volume,
			OrderState = orderState,
			TradeVolume = tradeVolume,
			ServerTime = DateTimeOffset.UtcNow,
			LocalTime = DateTimeOffset.UtcNow
		};
	}

	[TestMethod]
	public void BuilderSecurityId()
	{
		var securityId = _secId;
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(securityId);

		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);
		snapshot.SecurityId.AssertEqual(securityId);
		snapshot.BuildFrom.AssertEqual(DataType.OrderLog);
		snapshot.State.AssertEqual(QuoteChangeStates.SnapshotComplete);
		snapshot.Bids.Length.AssertEqual(0);
		snapshot.Asks.Length.AssertEqual(0);
	}

	[TestMethod]
	public void BuilderDepth()
	{
		var securityId = _secId;
		var initialDepth = new QuoteChangeMessage
		{
			SecurityId = securityId,
			Bids = [new(95m, 5m), new(94m, 3m)],
			Asks = [new(105m, 4m), new(106m, 6m)]
		};

		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(initialDepth);
		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);

		snapshot.SecurityId.AssertEqual(securityId);
		snapshot.BuildFrom.AssertEqual(DataType.OrderLog);
		snapshot.State.AssertEqual(QuoteChangeStates.SnapshotComplete);
		snapshot.Bids.Length.AssertEqual(2);
		snapshot.Asks.Length.AssertEqual(2);

		snapshot.Bids[0].Price.AssertEqual(95m);
		snapshot.Bids[0].Volume.AssertEqual(5m);
		snapshot.Bids[1].Price.AssertEqual(94m);
		snapshot.Bids[1].Volume.AssertEqual(3m);

		snapshot.Asks[0].Price.AssertEqual(105m);
		snapshot.Asks[0].Volume.AssertEqual(4m);
		snapshot.Asks[1].Price.AssertEqual(106m);
		snapshot.Asks[1].Volume.AssertEqual(6m);
	}

	[TestMethod]
	public void BuilderNullDepth()
	{
		Assert.ThrowsExactly<ArgumentNullException>(() => _ = new OrderLogMarketDepthBuilder(null));
	}

	[TestMethod]
	public void NullItem()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);
		Assert.ThrowsExactly<ArgumentNullException>(() => builder.Update(null));
	}

	[TestMethod]
	public void WrongDataType()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);
		var message = new ExecutionMessage { DataTypeEx = DataType.Ticks };

		Assert.ThrowsExactly<ArgumentException>(() => builder.Update(message));
	}

	[TestMethod]
	public void ZeroPrice()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);
		var message = CreateOrderLogMessage(price: 0m);

		var result = builder.Update(message);
		result.AssertNull();
	}

	[TestMethod]
	public void OrderId()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);
		var message = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			volume: 10m,
			orderState: OrderStates.Active);

		var result = builder.Update(message);

		result.AssertNotNull();
		result.State.AssertEqual(QuoteChangeStates.Increment);
		result.Bids.Length.AssertEqual(1);
		result.Asks.Length.AssertEqual(0);
		result.Bids[0].Price.AssertEqual(100m);
		result.Bids[0].Volume.AssertEqual(10m);

		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);
		snapshot.Bids.Length.AssertEqual(1);
		snapshot.Bids[0].Price.AssertEqual(100m);
		snapshot.Bids[0].Volume.AssertEqual(10m);
	}

	[TestMethod]
	public void OrderStringId()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);
		var message = CreateOrderLogMessage(
			orderStringId: "ORDER_123",
			side: Sides.Sell,
			price: 105m,
			volume: 15m,
			orderState: OrderStates.Active);

		var result = builder.Update(message);

		result.AssertNotNull();
		result.State.AssertEqual(QuoteChangeStates.Increment);
		result.Asks.Length.AssertEqual(1);
		result.Bids.Length.AssertEqual(0);
		result.Asks[0].Price.AssertEqual(105m);
		result.Asks[0].Volume.AssertEqual(15m);
	}

	[TestMethod]
	public void RegisterMultipleOrdersSamePrice()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		var message1 = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			volume: 10m,
			orderState: OrderStates.Active);

		var message2 = CreateOrderLogMessage(
			orderId: 124,
			side: Sides.Buy,
			price: 100m,
			volume: 5m,
			orderState: OrderStates.Active);

		builder.Update(message1).AssertNotNull();

		var result = builder.Update(message2);

		result.AssertNotNull();
		result.Bids[0].Price.AssertEqual(100m);
		result.Bids[0].Volume.AssertEqual(15m);

		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);
		snapshot.Bids.Length.AssertEqual(1);
		snapshot.Bids[0].Volume.AssertEqual(15m);
	}

	[TestMethod]
	public void OrderReplace()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		// Register order
		var registerMessage = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			volume: 10m,
			orderState: OrderStates.Active);

		builder.Update(registerMessage).AssertNotNull();

		// Replace order with different volume
		var replaceMessage = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			volume: 8m,
			orderState: OrderStates.Active);

		var result = builder.Update(replaceMessage);

		result.AssertNotNull();
		result.Bids[0].Volume.AssertEqual(8m);

		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);
		snapshot.Bids[0].Volume.AssertEqual(8m);
	}

	[TestMethod]
	public void MatchOrderWithOrderId()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		// Register order
		var registerMessage = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			volume: 10m,
			orderState: OrderStates.Active);

		builder.Update(registerMessage).AssertNotNull();

		// Match part of order
		var matchMessage = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			orderState: OrderStates.Done,
			tradeVolume: 3m);

		var result = builder.Update(matchMessage);

		result.AssertNotNull();
		result.Bids[0].Volume.AssertEqual(7m);

		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);
		snapshot.Bids[0].Volume.AssertEqual(7m);
	}

	[TestMethod]
	public void MatchOrderCompletely()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		// Register order
		var registerMessage = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			volume: 10m,
			orderState: OrderStates.Active);

		builder.Update(registerMessage).AssertNotNull();

		// Match complete order
		var matchMessage = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			orderState: OrderStates.Done,
			tradeVolume: 10m);

		var result = builder.Update(matchMessage);

		result.AssertNotNull();
		result.Bids[0].Volume.AssertEqual(0m);

		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);
		snapshot.Bids.Length.AssertEqual(0);
	}

	[TestMethod]
	public void MatchOrderVolume()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		// Register order
		var registerMessage = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			volume: 10m,
			orderState: OrderStates.Active);

		builder.Update(registerMessage).AssertNotNull();

		// Match using OrderVolume when TradeVolume is null
		var matchMessage = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			tradeVolume: 4m,
			orderState: OrderStates.Done);

		var result = builder.Update(matchMessage);

		result.AssertNotNull();
		result.Bids[0].Volume.AssertEqual(6m);

		matchMessage = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			tradeVolume: 6m,
			orderState: OrderStates.Done);

		result = builder.Update(matchMessage);
		result.AssertNotNull();
		result.Bids[0].Volume.AssertEqual(0);
	}

	[TestMethod]
	public void CancelOrderId()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		// Register order
		var registerMessage = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			volume: 10m,
			orderState: OrderStates.Active);

		builder.Update(registerMessage).AssertNotNull();

		// Cancel order
		var cancelMessage = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			orderState: OrderStates.Done);

		var result = builder.Update(cancelMessage);

		result.AssertNotNull();
		result.Bids[0].Volume.AssertEqual(0m);

		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);
		snapshot.Bids.Length.AssertEqual(0);
	}

	[TestMethod]
	public void CancelOrderStringId()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		// Register order
		var registerMessage = CreateOrderLogMessage(
			orderStringId: "ORDER_123",
			side: Sides.Sell,
			price: 105m,
			volume: 15m,
			orderState: OrderStates.Active);

		builder.Update(registerMessage).AssertNotNull();

		// Cancel order
		var cancelMessage = CreateOrderLogMessage(
			orderStringId: "ORDER_123",
			side: Sides.Sell,
			price: 105m,
			orderState: OrderStates.Done);

		var result = builder.Update(cancelMessage);

		result.AssertNotNull();
		result.Asks[0].Volume.AssertEqual(0m);

		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);
		snapshot.Asks.Length.AssertEqual(0);
	}

	[TestMethod]
	public void UnknownOrderId()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		// Try to match non-existent order
		var matchMessage = CreateOrderLogMessage(
			orderId: 999,
			side: Sides.Buy,
			price: 100m,
			orderState: OrderStates.Done,
			tradeVolume: 5m);

		var result = builder.Update(matchMessage);
		result.AssertNull();
	}

	[TestMethod]
	public void UnknownStringId()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		// Try to cancel non-existent order
		var cancelMessage = CreateOrderLogMessage(
			orderStringId: "UNKNOWN_ORDER",
			side: Sides.Buy,
			price: 100m,
			orderState: OrderStates.Done);

		var result = builder.Update(cancelMessage);
		result.AssertNull();
	}

	[TestMethod]
	public void MultipleOrdersDifferentPrices()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		// Register multiple buy orders
		builder.Update(CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			volume: 10m,
			orderState: OrderStates.Active));

		builder.Update(CreateOrderLogMessage(
			orderId: 124,
			side: Sides.Buy,
			price: 99m,
			volume: 5m,
			orderState: OrderStates.Active));

		// Register multiple sell orders
		builder.Update(CreateOrderLogMessage(
			orderId: 125,
			side: Sides.Sell,
			price: 101m,
			volume: 8m,
			orderState: OrderStates.Active));

		builder.Update(CreateOrderLogMessage(
			orderId: 126,
			side: Sides.Sell,
			price: 102m,
			volume: 12m,
			orderState: OrderStates.Active));

		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);

		snapshot.Bids.Length.AssertEqual(2);
		snapshot.Asks.Length.AssertEqual(2);

		// Check bids are sorted descending
		snapshot.Bids[0].Price.AssertEqual(100m);
		snapshot.Bids[0].Volume.AssertEqual(10m);
		snapshot.Bids[1].Price.AssertEqual(99m);
		snapshot.Bids[1].Volume.AssertEqual(5m);

		// Check asks are sorted ascending
		snapshot.Asks[0].Price.AssertEqual(101m);
		snapshot.Asks[0].Volume.AssertEqual(8m);
		snapshot.Asks[1].Price.AssertEqual(102m);
		snapshot.Asks[1].Volume.AssertEqual(12m);
	}

	[TestMethod]
	public void OrderNullVolume()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		var message = CreateOrderLogMessage(
			orderId: 123,
			side: Sides.Buy,
			price: 100m,
			volume: null,
			orderState: OrderStates.Active);

		var result = builder.Update(message);
		result.AssertNull();
	}

	[TestMethod]
	public void OrderNoId()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		var message = CreateOrderLogMessage(
			side: Sides.Buy,
			price: 100m,
			volume: 10m,
			orderState: OrderStates.Active);

		// Clear both IDs
		message.OrderId = null;
		message.OrderStringId = null;

		var result = builder.Update(message);
		result.AssertNull();
	}

	[TestMethod]
	public void StringId()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		// Register with lowercase
		var registerMessage = CreateOrderLogMessage(
			orderStringId: "order_123",
			side: Sides.Buy,
			price: 100m,
			volume: 10m,
			orderState: OrderStates.Active);

		builder.Update(registerMessage).AssertNotNull();

		// Cancel with uppercase
		var cancelMessage = CreateOrderLogMessage(
			orderStringId: "ORDER_123",
			side: Sides.Buy,
			price: 100m,
			orderState: OrderStates.Done);

		var result = builder.Update(cancelMessage);
		result.AssertNotNull();

		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);
		snapshot.Bids.Length.AssertEqual(0);
	}

	[TestMethod]
	public void ComplexScenario()
	{
		IOrderLogMarketDepthBuilder builder = new OrderLogMarketDepthBuilder(_secId);

		// Add initial orders
		builder.Update(CreateOrderLogMessage(orderId: 1, side: Sides.Buy, price: 100m, volume: 10m, orderState: OrderStates.Active));
		builder.Update(CreateOrderLogMessage(orderId: 2, side: Sides.Buy, price: 99m, volume: 5m, orderState: OrderStates.Active));
		builder.Update(CreateOrderLogMessage(orderId: 3, side: Sides.Sell, price: 101m, volume: 8m, orderState: OrderStates.Active));

		// Partial match of order 1
		builder.Update(CreateOrderLogMessage(orderId: 1, side: Sides.Buy, price: 100m, orderState: OrderStates.Done, tradeVolume: 3m));

		// Cancel order 2
		builder.Update(CreateOrderLogMessage(orderId: 2, side: Sides.Buy, price: 99m, orderState: OrderStates.Done));

		// Add new order at existing price level
		builder.Update(CreateOrderLogMessage(orderId: 4, side: Sides.Buy, price: 100m, volume: 5m, orderState: OrderStates.Active));

		var snapshot = builder.GetSnapshot(DateTimeOffset.UtcNow);

		// Should have one bid level with combined volume (7 + 5 = 12)
		snapshot.Bids.Length.AssertEqual(1);
		snapshot.Bids[0].Price.AssertEqual(100m);
		snapshot.Bids[0].Volume.AssertEqual(12m);

		// Should have one ask level
		snapshot.Asks.Length.AssertEqual(1);
		snapshot.Asks[0].Price.AssertEqual(101m);
		snapshot.Asks[0].Volume.AssertEqual(8m);
	}

	private static (Security sec, IStorageRegistry registry, DateTimeOffset date) Init()
	{
		var gazp = new Security { Id = "GAZP@TQBR", Board = ExchangeBoard.MicexTqbr };
		var registry = Helper.GetResourceStorage();

		var date = new DateTime(2022, 10, 26);

		return (gazp, registry, date);
	}

	[TestMethod]
	public void CheckSnapshotNotEmpty()
	{
		var (sec, registry, date) = Init();

		var secId = sec.Id.ToSecurityId();
		IOrderLogMarketDepthBuilder builder = typeof(OrderLogMarketDepthBuilder).CreateOrderLogMarketDepthBuilder(secId);
		var depths = registry
			.GetOrderLogMessageStorage(secId, registry.DefaultDrive, StorageFormats.Binary)
			.Load(date, date + TimeSpan.FromDays(1))
			.ToOrderBooks(builder);

		foreach (var d in depths.Skip(100).Take(100))
			if (builder.GetSnapshot(d.ServerTime).IsFullEmpty())
				throw new InvalidOperationException("snapshot is empty");
	}

	[TestMethod]
	public void CheckBuildWithInterval()
	{
		var (sec, registry, date) = Init();

		using var culture = ThreadingHelper.WithInvariantCulture();

		var secId = sec.Id.ToSecurityId();
		IOrderLogMarketDepthBuilder builder = typeof(OrderLogMarketDepthBuilder).CreateOrderLogMarketDepthBuilder(secId);
		var depths = registry
			.GetOrderLogMessageStorage(secId, registry.DefaultDrive, StorageFormats.Binary)
			.Load(date, date + TimeSpan.FromDays(1))
			.ToOrderBooks(builder, TimeSpan.FromSeconds(1), 50);

		var book = depths.Skip(300).First();
		var bookStr = book.Bids.Reverse().Concat(book.Asks).Select(q => q.ToString()).JoinPipe();

		var expected = "171.76 9|171.80 1023|171.81 2|171.82 450|171.85 3|171.89 10|171.90 1|171.94 1|171.95 1|171.99 3|172.00 1149|172.05 1000|172.06 8|172.08 1|172.09 13|172.10 12|172.19 50|172.20 714|172.25 502|172.28 10|172.30 1867|172.31 2|172.35 101|172.38 3173|172.39 1019|172.40 453|172.42 30|172.50 690|172.57 100|172.60 7|172.61 21|172.67 1|172.70 11|172.72 660|172.82 321|172.90 64|173.00 289|173.08 40|173.12 320|173.21 7|173.32 100|173.39 50|173.70 260|173.80 1150|173.81 1|175.00 15|175.61 10|180.00 10|186.97 16|189.61 4|170.00 10|172.20 23|172.30 1000|172.38 5|172.40 52|172.47 30|172.48 20|172.50 2|172.60 5|172.69 19|172.70 100|172.72 10|172.85 820|172.89 21|172.90 160|172.94 11|172.98 19|172.99 210|173.00 861|173.08 11|173.10 181|173.13 1|173.31 11|173.33 10|173.36 500|173.37 1|173.40 120|173.42 500|173.44 4|173.45 215|173.48 3|173.49 1|173.50 711|173.53 60|173.54 5|173.58 3|173.59 86|173.62 58|173.64 3|173.68 20|173.70 13|173.73 50|173.76 500|173.78 3|173.79 521|173.80 700|173.87 10|173.90 796|173.92 500|173.94 201";

		bookStr.AssertEqual(expected);
	}
}