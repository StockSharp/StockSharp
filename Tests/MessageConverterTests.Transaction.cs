namespace StockSharp.Tests;

using StockSharp.Fix;
using StockSharp.Fix.Native;

using FixExtensions = StockSharp.Fix.Native.Extensions;
using MessagesExtensions = StockSharp.Messages.Extensions;

/// <summary>
/// Transaction message converter tests.
/// </summary>
partial class MessageConverterTests
{
	#region OrderRegisterMessage

	[TestMethod]
	public void OrderRegisterMessage_RoundTrip()
	{
		var original = new OrderRegisterMessage
		{
			TransactionId = 12345,
			PortfolioName = "TestPortfolio",
			SecurityId = CreateTestSecurityId(),
			Price = 150.25m,
			Volume = 100,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			Comment = "Test order",
			Slippage = 0.01m,
			IsManual = true,
			MinOrderVolume = 10,
			PostOnly = true,
			Leverage = 2,
		};

		var fix = Converter.ToFixNewOrderSingle(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	private static OrderRegisterMessage CreateTifOrder(long txId, DateTime? tillDate)
		=> new()
		{
			TransactionId = txId,
			PortfolioName = "P",
			SecurityId = CreateTestSecurityId(),
			Price = 100m,
			Volume = 1,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			TimeInForce = TimeInForce.PutInQueue,
			TillDate = tillDate,
		};

	[TestMethod]
	public void OrderRegisterMessage_TimeInForce_GoodTillCancel_RoundTrip()
	{
		// PutInQueue + no TillDate => GoodTillCancel.
		var result = Converter.ToMessage(Converter.ToFixNewOrderSingle(CreateTifOrder(12390, null)));

		result.TimeInForce.AssertEqual(TimeInForce.PutInQueue);
		result.TillDate.AssertNull();
	}

	[TestMethod]
	public void OrderRegisterMessage_TimeInForce_Day_RoundTrip()
	{
		// PutInQueue + the Today sentinel => Day; encoding the enum alone collapsed it to GTC.
		var result = Converter.ToMessage(Converter.ToFixNewOrderSingle(CreateTifOrder(12391, MessagesExtensions.Today)));

		result.TimeInForce.AssertEqual(TimeInForce.PutInQueue);
		result.TillDate.AssertEqual(MessagesExtensions.Today);
	}

	[TestMethod]
	public void OrderRegisterMessage_TimeInForce_GoodTillDate_RoundTrip()
	{
		// PutInQueue + a future date => GoodTillDate; the expiry date must survive.
		var till = new DateTime(2030, 6, 15, 0, 0, 0, DateTimeKind.Utc);
		var result = Converter.ToMessage(Converter.ToFixNewOrderSingle(CreateTifOrder(12392, till)));

		result.TimeInForce.AssertEqual(TimeInForce.PutInQueue);
		result.TillDate.AssertEqual(till);
	}

	[TestMethod]
	public void OrderRegisterMessage_MarketOrder_RoundTrip()
	{
		var original = new OrderRegisterMessage
		{
			TransactionId = 12346,
			PortfolioName = "Portfolio1",
			SecurityId = CreateTestSecurityId("TSLA", "NASDAQ"),
			Volume = 50,
			Side = Sides.Sell,
			OrderType = OrderTypes.Market,
		};

		var fix = Converter.ToFixNewOrderSingle(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void OrderRegisterMessage_WithPositionEffect_RoundTrip()
	{
		var original = new OrderRegisterMessage
		{
			TransactionId = 12347,
			PortfolioName = "MarginAccount",
			SecurityId = CreateTestSecurityId("GOOG"),
			Price = 2800.00m,
			Volume = 10,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			PositionEffect = OrderPositionEffects.OpenOnly,
			MarginMode = MarginModes.Cross,
		};

		var fix = Converter.ToFixNewOrderSingle(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void OrderRegisterMessage_WithClientAndBrokerCode_RoundTrip()
	{
		var original = new OrderRegisterMessage
		{
			TransactionId = 12348,
			PortfolioName = "TestPortfolio",
			ClientCode = "client1",
			BrokerCode = "100",
			SecurityId = CreateTestSecurityId(),
			Price = 50_000m,
			Volume = 1,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
		};

		var fix = Converter.ToFixNewOrderSingle(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void FixNewOrderSingle_MissingSide_Throws()
	{
		// A NewOrderSingle without a valid Side (tag 54) must be rejected,
		// not silently coerced into Sides.Buy.
		var original = new OrderRegisterMessage
		{
			TransactionId = 12349,
			PortfolioName = "TestPortfolio",
			SecurityId = CreateTestSecurityId(),
			Price = 150.25m,
			Volume = 100,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
		};

		var fix = Converter.ToFixNewOrderSingle(original) with { Side = null };

		Throws<InvalidOperationException>(() => Converter.ToMessage(fix));
	}

	[TestMethod]
	public void FixNewOrderSingle_MissingOrdType_Throws()
	{
		// A NewOrderSingle without a valid OrdType (tag 40) must be rejected,
		// not silently coerced into OrderTypes.Limit.
		var original = new OrderRegisterMessage
		{
			TransactionId = 12349,
			PortfolioName = "TestPortfolio",
			SecurityId = CreateTestSecurityId(),
			Price = 150.25m,
			Volume = 100,
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
		};

		var fix = Converter.ToFixNewOrderSingle(original) with { OrdType = null };

		Throws<InvalidOperationException>(() => Converter.ToMessage(fix));
	}

	#endregion

	#region OrderCancelMessage

	[TestMethod]
	public void OrderCancelMessage_RoundTrip()
	{
		var original = new OrderCancelMessage
		{
			TransactionId = 12350,
			OriginalTransactionId = 12345,
			OrderId = 99999,
			PortfolioName = "TestPortfolio",
			SecurityId = CreateTestSecurityId(),
			Volume = 100,
			Balance = 50,
		};

		var fix = Converter.ToFixOrderCancelRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void OrderCancelMessage_ByStringId_RoundTrip()
	{
		var original = new OrderCancelMessage
		{
			TransactionId = 12351,
			OriginalTransactionId = 12346,
			PortfolioName = "Portfolio1",
			SecurityId = CreateTestSecurityId("MSFT"),
		};

		var fix = Converter.ToFixOrderCancelRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void OrderCancelMessage_ByStringId_CarriesOrderStringId()
	{
		var original = new OrderCancelMessage
		{
			TransactionId = 12353,
			OriginalTransactionId = 12347,
			OrderStringId = "EXCH-123",
			PortfolioName = "Portfolio1",
			SecurityId = CreateTestSecurityId("MSFT"),
		};

		var fix = Converter.ToFixOrderCancelRequest(original);

		fix.OrderId.AssertEqual("EXCH-123");
	}

	[TestMethod]
	public void OrderCancelMessage_ByNumericId_UsesNumericId()
	{
		var original = new OrderCancelMessage
		{
			TransactionId = 12354,
			OriginalTransactionId = 12348,
			OrderId = 987654,
			OrderStringId = "EXCH-123",
			PortfolioName = "Portfolio1",
			SecurityId = CreateTestSecurityId("MSFT"),
		};

		var fix = Converter.ToFixOrderCancelRequest(original);

		fix.OrderId.AssertEqual("987654");
	}

	[TestMethod]
	public void OrderCancelMessage_WithClientAndBrokerCode_RoundTrip()
	{
		var original = new OrderCancelMessage
		{
			TransactionId = 12352,
			OriginalTransactionId = 12345,
			OrderId = 99999,
			PortfolioName = "TestPortfolio",
			ClientCode = "client1",
			BrokerCode = "100",
			SecurityId = CreateTestSecurityId(),
			Volume = 100,
			Balance = 50,
		};

		var fix = Converter.ToFixOrderCancelRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	// cancel carries CFI (instrument identification on some venues) and the operator
	// text/reason. Encoding null dropped both even though decode reads them back.
	[TestMethod]
	public void OrderCancelMessage_CarriesCfiCodeAndText()
	{
		var original = new OrderCancelMessage
		{
			TransactionId = 12356,
			OriginalTransactionId = 12345,
			OrderId = 99999,
			PortfolioName = "TestPortfolio",
			SecurityId = CreateTestSecurityId(),
			CfiCode = "OCXXXX",
			Comment = "cancel: risk breach",
		};

		var fix = Converter.ToFixOrderCancelRequest(original);
		fix.CfiCode.AssertEqual("OCXXXX");
		fix.Text.AssertEqual("cancel: risk breach");

		var result = Converter.ToMessage(fix);
		result.CfiCode.AssertEqual("OCXXXX");
		result.Comment.AssertEqual("cancel: risk breach");
	}

	#endregion

	#region OrderReplaceMessage

	[TestMethod]
	public void OrderReplaceMessage_RoundTrip()
	{
		var original = new OrderReplaceMessage
		{
			TransactionId = 12360,
			OriginalTransactionId = 12345,
			OldOrderId = 99999,
			PortfolioName = "TestPortfolio",
			SecurityId = CreateTestSecurityId(),
			Price = 155.50m,
			Volume = 150,
			OldOrderPrice = 150.25m,
			OldOrderVolume = 100,
		};

		var fix = Converter.ToFixOrderCancelReplaceRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	// a replace on an alphanumeric-id venue references the original order by its
	// string exchange id. Encoding only the numeric OldOrderId dropped that reference.
	[TestMethod]
	public void OrderReplaceMessage_ByStringId_CarriesOldOrderStringId()
	{
		var original = new OrderReplaceMessage
		{
			TransactionId = 12365,
			OriginalTransactionId = 12349,
			OldOrderStringId = "EXCH-REPL-77",
			PortfolioName = "Portfolio1",
			SecurityId = CreateTestSecurityId("MSFT"),
			Price = 10m,
			Volume = 5,
		};

		var fix = Converter.ToFixOrderCancelReplaceRequest(original);
		fix.OrderId.AssertEqual("EXCH-REPL-77");

		var result = Converter.ToMessage(fix);
		result.OldOrderStringId.AssertEqual("EXCH-REPL-77");
	}

	[TestMethod]
	public void OrderReplaceMessage_WithAllFields_RoundTrip()
	{
		var original = new OrderReplaceMessage
		{
			TransactionId = 12361,
			OriginalTransactionId = 12346,
			OldOrderId = 88888,
			PortfolioName = "Portfolio2",
			SecurityId = CreateTestSecurityId("AMZN"),
			Price = 3500.00m,
			Volume = 25,
			OldOrderPrice = 3450.00m,
			OldOrderVolume = 20,
			Slippage = 1.00m,
			IsManual = false,
			MinOrderVolume = 5,
			PositionEffect = OrderPositionEffects.CloseOnly,
			PostOnly = false,
			StrategyId = "Strategy1",
			Leverage = 3,
		};

		var fix = Converter.ToFixOrderCancelReplaceRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void OrderReplaceMessage_WithClientAndBrokerCode_RoundTrip()
	{
		var original = new OrderReplaceMessage
		{
			TransactionId = 12362,
			OriginalTransactionId = 12345,
			OldOrderId = 99999,
			PortfolioName = "TestPortfolio",
			ClientCode = "client1",
			BrokerCode = "100",
			SecurityId = CreateTestSecurityId(),
			Price = 55_000m,
			Volume = 2,
			OldOrderPrice = 50_000m,
			OldOrderVolume = 1,
		};

		var fix = Converter.ToFixOrderCancelReplaceRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion

	#region OrderGroupCancelMessage

	[TestMethod]
	public void OrderGroupCancelMessage_RoundTrip()
	{
		var original = new OrderGroupCancelMessage
		{
			TransactionId = 12370,
			PortfolioName = "TestPortfolio",
			SecurityId = CreateTestSecurityId(),
			Side = Sides.Buy,
		};

		var fix = Converter.ToFixOrderMassCancelRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void OrderGroupCancelMessage_SecurityTypes_RoundTrip()
	{
		// The per-type filter must survive Message -> FIX -> Message; dropping it on
		// encode silently widened the cancel scope from these types to all orders.
		var original = new OrderGroupCancelMessage
		{
			TransactionId = 12380,
			PortfolioName = "TestPortfolio",
			SecurityTypes = [SecurityTypes.Stock, SecurityTypes.Future],
		};

		var fix = Converter.ToFixOrderMassCancelRequest(original);
		var result = Converter.ToMessage(fix);

		result.SecurityTypes.AssertNotNull();
		result.SecurityTypes.Length.AssertEqual(2);
		result.SecurityTypes.Contains(SecurityTypes.Stock).AssertTrue();
		result.SecurityTypes.Contains(SecurityTypes.Future).AssertTrue();
	}

	[TestMethod]
	public void MassCancelRequestType_DerivedFromScope()
	{
		// Tag 530 as it reaches the wire, rather than the helper that derives it: the scope a
		// caller asked for is only meaningful once it is on the record.
		char? scopeOf(OrderGroupCancelMessage message)
			=> Converter.ToFixOrderMassCancelRequest(message).MassCancelRequestType;

		scopeOf(new() { SecurityTypes = [SecurityTypes.Stock] }).AssertEqual('9');
		scopeOf(new() { SecurityType = SecurityTypes.Future }).AssertEqual('9');
		scopeOf(new() { SecurityId = CreateTestSecurityId() }).AssertEqual('1');
		scopeOf(new()).AssertEqual('7');
	}

	[TestMethod]
	public void OrderGroupCancelMessage_AllSides_RoundTrip()
	{
		var original = new OrderGroupCancelMessage
		{
			TransactionId = 12371,
			PortfolioName = "Portfolio3",
		};

		var fix = Converter.ToFixOrderMassCancelRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void OrderGroupCancelMessage_WithClientAndBrokerCode_RoundTrip()
	{
		var original = new OrderGroupCancelMessage
		{
			TransactionId = 12372,
			PortfolioName = "TestPortfolio",
			ClientCode = "client1",
			BrokerCode = "100",
			SecurityId = CreateTestSecurityId(),
			Side = Sides.Buy,
		};

		var fix = Converter.ToFixOrderMassCancelRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion

	#region OrderStatusMessage

	[TestMethod]
	public void OrderStatusMessage_RoundTrip()
	{
		var original = new OrderStatusMessage
		{
			TransactionId = 12380,
			OrderId = 99999,
			OrderStringId = "ORD001",
			IsSubscribe = true,
		};

		var fix = Converter.ToFixOrderStatusRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void OrderStatusMessage_MassStatus_RoundTrip()
	{
		var original = new OrderStatusMessage
		{
			TransactionId = 12381,
			IsSubscribe = true,
			OrderStringId = "MASS001",
		};

		var fix = Converter.ToFixOrderMassStatusRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void OrderStatusMessage_MassStatus_WithIncrementalAndUserId_RoundTrip()
	{
		var original = new OrderStatusMessage
		{
			TransactionId = 12382,
			IsSubscribe = true,
			PortfolioName = "TestPortfolio",
			SecurityId = CreateTestSecurityId(),
			IsIncremental = true,
			UserId = "user123",
		};

		var fix = Converter.ToFixOrderMassStatusRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	[TestMethod]
	public void OrderStatusMessage_MassStatus_Unsubscribe_RoundTrip()
	{
		// Unsubscribe form: TransactionId is a fresh unsub-id, OriginalTransactionId
		// references the original subscribe-id. ToFixOrderMassStatusRequest must
		// emit the subscribe-id via OrigClOrdId (not MassStatusReqId, which is the
		// unsub-id) and ToMessage must round-trip it back into OriginalTransactionId.
		// Without the symmetric round-trip the server cannot resolve which
		// subscription this unsub references and the subscription leaks.
		var original = new OrderStatusMessage
		{
			TransactionId = 99001,
			OriginalTransactionId = 42,
			IsSubscribe = false,
		};

		var fix = Converter.ToFixOrderMassStatusRequest(original);

		AreEqual("99001", (string)fix.MassStatusReqId, "MassStatusReqId is the fresh unsub-id");
		AreEqual("42", (string)fix.OrigClOrdId, "OrigClOrdId references the original subscribe-id");

		var result = Converter.ToMessage(fix);

		AreEqual(99001L, result.TransactionId);
		AreEqual(42L, result.OriginalTransactionId, "Original subscribe-id must survive the wire round-trip");
		IsFalse(result.IsSubscribe);
	}

	[TestMethod]
	public void OrderStatusMessage_MassStatus_WithSecurityIds_RoundTrip()
	{
		var original = new OrderStatusMessage
		{
			TransactionId = 12383,
			IsSubscribe = true,
			PortfolioName = "TestPortfolio",
			SecurityIds =
			[
				CreateTestSecurityId("AAPL", "NASDAQ"),
				CreateTestSecurityId("GOOG", "NASDAQ"),
				CreateTestSecurityId("MSFT", "NYSE"),
			],
		};

		var fix = Converter.ToFixOrderMassStatusRequest(original);
		var result = Converter.ToMessage(fix);

		Helper.CheckEqual(original, result);
	}

	#endregion

	#region OrderReplace wire fields

	// A replace must preserve the order-shaping fields Side / TimeInForce / VisibleVolume / Comment.
	// The wire record originally omitted them, so an amend could flip the side or lose its concealed
	// (iceberg) volume.
	[TestMethod]
	public void OrderReplace_RoundTrip_PreservesSideTifVisibleVolumeComment()
	{
		var msg = new OrderReplaceMessage
		{
			TransactionId = 100,
			OriginalTransactionId = 50,
			SecurityId = new SecurityId { SecurityCode = "BTC", BoardCode = "TESTEX" },
			PortfolioName = "PF",
			Price = 100m,
			Volume = 5m,
			Side = Sides.Sell,
			TimeInForce = TimeInForce.CancelBalance,
			VisibleVolume = 2m,
			Comment = "replace-note",
		};

		var round = Converter.ToMessage(Converter.ToFixOrderCancelReplaceRequest(msg));

		AreEqual(Sides.Sell, round.Side, "OrderReplace lost Side");
		AreEqual(TimeInForce.CancelBalance, round.TimeInForce, "OrderReplace lost TimeInForce");
		AreEqual(2m, round.VisibleVolume, "OrderReplace lost VisibleVolume");
		AreEqual("replace-note", round.Comment, "OrderReplace lost Comment");
	}

	[TestMethod]
	public void OrderReplace_RoundTrip_PreservesTillDate()
	{
		// PutInQueue + a future TillDate encode FIX GoodTillDate on the wire.
		var till = new DateTime(2026, 12, 31, 0, 0, 0, DateTimeKind.Utc);
		var msg = new OrderReplaceMessage
		{
			TransactionId = 101,
			OriginalTransactionId = 51,
			SecurityId = new SecurityId { SecurityCode = "BTC", BoardCode = "TESTEX" },
			PortfolioName = "PF",
			Price = 100m,
			Volume = 5m,
			Side = Sides.Buy,
			TimeInForce = TimeInForce.PutInQueue,
			TillDate = till,
		};

		var round = Converter.ToMessage(Converter.ToFixOrderCancelReplaceRequest(msg));

		AreEqual<DateTime?>(till, round.TillDate, "OrderReplace lost TillDate");
	}

	// A rejected replace must be reported as a replace reject: the OrderCancelReject
	// must carry CxlRejResponseTo = OrderCancelReplaceRequest, not always OrderCancelRequest.
	[TestMethod]
	public void OrderCancelReject_CarriesReplaceFlavor()
	{
		var rec = Converter.ToFixOrderCancelReject("100", "EX-1", "boom",
			new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), CxlRejResponseTo.OrderCancelReplaceRequest);

		rec.CxlRejResponseTo.AssertEqual(CxlRejResponseTo.OrderCancelReplaceRequest,
			"a replace reject must carry the OrderCancelReplaceRequest response-to flavor");
	}

	#endregion

	#region Conditional order stop flavor (take-profit vs stop-loss)

	private static OrderRegisterMessage ConditionalOrder(FixOrderCondition condition) => new()
	{
		TransactionId = 1,
		SecurityId = new SecurityId { SecurityCode = "BTC", BoardCode = "TESTEX" },
		OrderType = OrderTypes.Conditional,
		Condition = condition,
	};

	// The stop flavor must survive the OrdType mapping so a take-profit is not collapsed to a stop.
	[TestMethod]
	public void TakeProfitCondition_EmitsTakeProfitOrdType()
	{
		var record = Converter.ToFixNewOrderSingle(ConditionalOrder(new FixOrderCondition { Type = FixStopOrderTypes.TakeProfit, TakeProfit = 100m }));
		record.OrdType.AssertEqual(FixExtensions.TakeProfit, "a take-profit condition must be emitted as a take-profit OrdType, not a stop");
	}

	[TestMethod]
	public void TakeProfitTrailingCondition_EmitsTakeProfitTrailingOrdType()
	{
		var record = Converter.ToFixNewOrderSingle(ConditionalOrder(new FixOrderCondition { Type = FixStopOrderTypes.TakeProfitTrailing, TakeProfit = 100m }));
		record.OrdType.AssertEqual(FixExtensions.TakeProfitTrailing, "a trailing take-profit must be emitted as a trailing take-profit OrdType");
	}

	[TestMethod]
	public void StopLossTrailingCondition_EmitsStopTrailingOrdType()
	{
		var record = Converter.ToFixNewOrderSingle(ConditionalOrder(new FixOrderCondition { Type = FixStopOrderTypes.StopLossTrailing, StopLoss = 100m }));
		record.OrdType.AssertEqual(FixExtensions.StopTrailing, "a trailing stop-loss must be emitted as a trailing stop OrdType");
	}

	#endregion
}
