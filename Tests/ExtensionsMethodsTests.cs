namespace StockSharp.Tests;

using StockSharp.Messages;

[TestClass]
public class ExtensionsMethodsTests : BaseTestClass
{
	#region GetSpreadMiddle

	[TestMethod]
	public void GetSpreadMiddle_BothPrices_ReturnsAverage()
	{
		var result = 100m.GetSpreadMiddle(102m, null);
		result.AssertEqual(101m);
	}

	[TestMethod]
	public void GetSpreadMiddle_BothPrices_WithPriceStep_Shrinks()
	{
		var result = 100m.GetSpreadMiddle(101m, 0.5m);
		result.AssertEqual(100.5m);
	}

	[TestMethod]
	public void GetSpreadMiddle_BothPrices_RoundsToStep()
	{
		// (100 + 103) / 2 = 101.5, with step 1.0 should round
		var result = 100m.GetSpreadMiddle(103m, 1m);
		result.AssertEqual(102m);
	}

	[TestMethod]
	public void GetSpreadMiddle_Nullable_BothNull_ReturnsNull()
	{
		decimal? bid = null;
		decimal? ask = null;
		var result = bid.GetSpreadMiddle(ask, null);
		result.AssertNull();
	}

	[TestMethod]
	public void GetSpreadMiddle_Nullable_OnlyBid_ReturnsBid()
	{
		decimal? bid = 100m;
		decimal? ask = null;
		var result = bid.GetSpreadMiddle(ask, null);
		result.AssertEqual(100m);
	}

	[TestMethod]
	public void GetSpreadMiddle_Nullable_OnlyAsk_ReturnsAsk()
	{
		decimal? bid = null;
		decimal? ask = 102m;
		var result = bid.GetSpreadMiddle(ask, null);
		result.AssertEqual(102m);
	}

	[TestMethod]
	public void GetSpreadMiddle_Level1_UsesSpreadMiddleField()
	{
		var msg = new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId(), ServerTime = DateTime.UtcNow };
		msg.Add(Level1Fields.SpreadMiddle, 50m);
		msg.Add(Level1Fields.BestBidPrice, 40m);
		msg.Add(Level1Fields.BestAskPrice, 60m);

		var result = msg.GetSpreadMiddle(null);
		// Should use SpreadMiddle field directly, not compute from bid/ask
		result.AssertEqual(50m);
	}

	[TestMethod]
	public void GetSpreadMiddle_Level1_FallsBackToBidAsk()
	{
		var msg = new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId(), ServerTime = DateTime.UtcNow };
		msg.Add(Level1Fields.BestBidPrice, 100m);
		msg.Add(Level1Fields.BestAskPrice, 104m);

		var result = msg.GetSpreadMiddle(null);
		result.AssertEqual(102m);
	}

	[TestMethod]
	public void GetSpreadMiddle_OrderBook_UsesbestBidAsk()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(104m, 10)],
		};

		var result = msg.GetSpreadMiddle(null);
		result.AssertEqual(102m);
	}

	#endregion

	#region ShrinkPrice

	[TestMethod]
	public void ShrinkPrice_WithStep_Rounds()
	{
		var result = 100.123m.ShrinkPrice(0.01m, null);
		result.AssertEqual(100.12m);
	}

	[TestMethod]
	public void ShrinkPrice_NullStep_DefaultsTo001()
	{
		var result = 100.123m.ShrinkPrice(null, null);
		result.AssertEqual(100.12m);
	}

	[TestMethod]
	public void ShrinkPrice_RemovesTrailingZeros()
	{
		var result = 100.10m.ShrinkPrice(0.1m, null);
		// Should remove trailing zeros
		result.AssertEqual(100.1m);
	}

	[TestMethod]
	public void ShrinkPrice_WithDecimals()
	{
		var result = 100.12345m.ShrinkPrice(0.001m, 3);
		result.AssertEqual(100.123m);
	}

	[TestMethod]
	public void ShrinkPrice_SecurityMessage()
	{
		var sec = new SecurityMessage { PriceStep = 0.05m, Decimals = 2 };
		var result = 100.123m.ShrinkPrice(sec);
		result.AssertEqual(100.1m);
	}

	#endregion

	#region Iso10962

	[TestMethod]
	public void Iso10962_Stock_ReturnsESXXXX()
	{
		var sec = new SecurityMessage { SecurityType = SecurityTypes.Stock };
		sec.Iso10962().AssertEqual("ESXXXX");
	}

	[TestMethod]
	public void Iso10962_Future_ReturnsFFXXXX()
	{
		var sec = new SecurityMessage { SecurityType = SecurityTypes.Future };
		sec.Iso10962().AssertEqual("FFXXXX");
	}

	[TestMethod]
	public void Iso10962_CallOption_ReturnsOCXXXX()
	{
		var sec = new SecurityMessage { SecurityType = SecurityTypes.Option, OptionType = OptionTypes.Call };
		sec.Iso10962().AssertEqual("OCXXXX");
	}

	[TestMethod]
	public void Iso10962_PutOption_ReturnsOPXXXX()
	{
		var sec = new SecurityMessage { SecurityType = SecurityTypes.Option, OptionType = OptionTypes.Put };
		sec.Iso10962().AssertEqual("OPXXXX");
	}

	[TestMethod]
	public void Iso10962_OptionNoType_ReturnsOXXXXX()
	{
		var sec = new SecurityMessage { SecurityType = SecurityTypes.Option };
		sec.Iso10962().AssertEqual("OXXXXX");
	}

	[TestMethod]
	public void Iso10962_Bond_ReturnsDBXXXX()
	{
		var sec = new SecurityMessage { SecurityType = SecurityTypes.Bond };
		sec.Iso10962().AssertEqual("DBXXXX");
	}

	[TestMethod]
	public void Iso10962_Index_ReturnsMRIXXX()
	{
		var sec = new SecurityMessage { SecurityType = SecurityTypes.Index };
		sec.Iso10962().AssertEqual("MRIXXX");
	}

	[TestMethod]
	public void Iso10962_Currency_ReturnsMRCXXX()
	{
		var sec = new SecurityMessage { SecurityType = SecurityTypes.Currency };
		sec.Iso10962().AssertEqual("MRCXXX");
	}

	[TestMethod]
	public void Iso10962_CryptoCurrency_ReturnsMMBXXX()
	{
		var sec = new SecurityMessage { SecurityType = SecurityTypes.CryptoCurrency };
		sec.Iso10962().AssertEqual("MMBXXX");
	}

	[TestMethod]
	public void Iso10962_Null_ReturnsXXXXXX()
	{
		var sec = new SecurityMessage();
		sec.Iso10962().AssertEqual("XXXXXX");
	}

	[TestMethod]
	public void Iso10962ToSecurityType_Stock()
	{
		"ESXXXX".Iso10962ToSecurityType().AssertEqual(SecurityTypes.Stock);
	}

	[TestMethod]
	public void Iso10962ToSecurityType_Future()
	{
		"FFXXXX".Iso10962ToSecurityType().AssertEqual(SecurityTypes.Future);
	}

	[TestMethod]
	public void Iso10962ToSecurityType_Swap()
	{
		"FFWXXX".Iso10962ToSecurityType().AssertEqual(SecurityTypes.Swap);
	}

	[TestMethod]
	public void Iso10962ToSecurityType_Forward()
	{
		"FFMXXX".Iso10962ToSecurityType().AssertEqual(SecurityTypes.Forward);
	}

	[TestMethod]
	public void Iso10962ToSecurityType_Bond()
	{
		"DBXXXX".Iso10962ToSecurityType().AssertEqual(SecurityTypes.Bond);
	}

	[TestMethod]
	public void Iso10962ToSecurityType_Option()
	{
		"OCXXXX".Iso10962ToSecurityType().AssertEqual(SecurityTypes.Option);
	}

	[TestMethod]
	public void Iso10962ToSecurityType_EmptyString_ReturnsNull()
	{
		"".Iso10962ToSecurityType().AssertNull();
	}

	[TestMethod]
	public void Iso10962ToSecurityType_WrongLength_ReturnsNull()
	{
		"ES".Iso10962ToSecurityType().AssertNull();
	}

	[TestMethod]
	public void Iso10962ToSecurityType_Unknown_ReturnsNull()
	{
		"ZZZZZZ".Iso10962ToSecurityType().AssertNull();
	}

	[TestMethod]
	public void Iso10962ToOptionType_Call()
	{
		"OCXXXX".Iso10962ToOptionType().AssertEqual(OptionTypes.Call);
	}

	[TestMethod]
	public void Iso10962ToOptionType_Put()
	{
		"OPXXXX".Iso10962ToOptionType().AssertEqual(OptionTypes.Put);
	}

	[TestMethod]
	public void Iso10962ToOptionType_NotOption_ReturnsNull()
	{
		"ESXXXX".Iso10962ToOptionType().AssertNull();
	}

	[TestMethod]
	public void Iso10962ToOptionType_X_ReturnsNull()
	{
		"OXXXXX".Iso10962ToOptionType().AssertNull();
	}

	[TestMethod]
	public void Iso10962_Roundtrip_UniqueTypes()
	{
		// Types with unique first-char mapping (Fund/Stock share 'E', News/Weather/Adr/Cfd share 'M')
		var types = new[]
		{
			SecurityTypes.Stock, SecurityTypes.Future, SecurityTypes.Bond,
			SecurityTypes.Index, SecurityTypes.Currency, SecurityTypes.Warrant,
			SecurityTypes.Forward, SecurityTypes.Swap, SecurityTypes.Commodity,
			SecurityTypes.Cfd, SecurityTypes.Adr, SecurityTypes.CryptoCurrency,
		};

		foreach (var t in types)
		{
			var sec = new SecurityMessage { SecurityType = t };
			var cfi = sec.Iso10962();
			var back = cfi.Iso10962ToSecurityType();
			back.AssertEqual(t);
		}
	}

	[TestMethod]
	public void Iso10962_Fund_EncodesToEU()
	{
		var sec = new SecurityMessage { SecurityType = SecurityTypes.Fund };
		var cfi = sec.Iso10962();
		cfi.AssertEqual("EUXXXX");
		// Fund decodes back as Stock (shares 'E' prefix)
		cfi.Iso10962ToSecurityType().AssertEqual(SecurityTypes.Stock);
	}

	#endregion

	#region GetOrderLogCancelReason

	[TestMethod]
	public void GetOrderLogCancelReason_ReRegistered()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			OrderState = OrderStates.Done,
			TradeVolume = null,
			OrderStatus = 0x100000,
			HasOrderInfo = true,
		};

		msg.GetOrderLogCancelReason().AssertEqual(OrderLogCancelReasons.ReRegistered);
	}

	[TestMethod]
	public void GetOrderLogCancelReason_Canceled()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			OrderState = OrderStates.Done,
			TradeVolume = null,
			OrderStatus = 0x200000,
			HasOrderInfo = true,
		};

		msg.GetOrderLogCancelReason().AssertEqual(OrderLogCancelReasons.Canceled);
	}

	[TestMethod]
	public void GetOrderLogCancelReason_GroupCanceled()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			OrderState = OrderStates.Done,
			TradeVolume = null,
			OrderStatus = 0x400000,
			HasOrderInfo = true,
		};

		msg.GetOrderLogCancelReason().AssertEqual(OrderLogCancelReasons.GroupCanceled);
	}

	[TestMethod]
	public void GetOrderLogCancelReason_CrossTrade()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.OrderLog,
			OrderState = OrderStates.Done,
			TradeVolume = null,
			OrderStatus = 0x800000,
			HasOrderInfo = true,
		};

		msg.GetOrderLogCancelReason().AssertEqual(OrderLogCancelReasons.CrossTrade);
	}

	#endregion

	#region CreateErrorResponse

	private sealed class TestReceiver : TestLogReceiver { }

	[TestMethod]
	public void CreateErrorResponse_ConnectMessage_ReturnsConnectError()
	{
		var msg = new ConnectMessage();
		var ex = new InvalidOperationException("test");
		var result = msg.CreateErrorResponse(ex, new TestReceiver());

		result.Type.AssertEqual(MessageTypes.Connect);
		((ConnectMessage)result).Error.AssertEqual(ex);
	}

	[TestMethod]
	public void CreateErrorResponse_DisconnectMessage_ReturnsDisconnectError()
	{
		var msg = new DisconnectMessage();
		var ex = new InvalidOperationException("test");
		var result = msg.CreateErrorResponse(ex, new TestReceiver());

		result.Type.AssertEqual(MessageTypes.Disconnect);
		((DisconnectMessage)result).Error.AssertEqual(ex);
	}

	[TestMethod]
	public void CreateErrorResponse_SubscriptionMessage_ReturnsSubscriptionResponse()
	{
		var msg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
			TransactionId = 42,
		};
		var ex = new InvalidOperationException("test");
		var result = msg.CreateErrorResponse(ex, new TestReceiver());

		result.Type.AssertEqual(MessageTypes.SubscriptionResponse);
		((SubscriptionResponseMessage)result).Error.AssertEqual(ex);
		((SubscriptionResponseMessage)result).OriginalTransactionId.AssertEqual(42L);
	}

	#endregion

	#region ToOrderSnapshot

	[TestMethod]
	public void ToOrderSnapshot_SingleDiff_ReturnsSame()
	{
		var exec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderId = 1,
			OrderPrice = 100m,
			OrderVolume = 10,
			OrderState = OrderStates.Active,
			TransactionId = 1,
		};

		var snapshot = new[] { exec }.ToOrderSnapshot(1, new TestReceiver());

		snapshot.OrderId.AssertEqual(1L);
		snapshot.OrderPrice.AssertEqual(100m);
		snapshot.OrderState.AssertEqual(OrderStates.Active);
	}

	[TestMethod]
	public void ToOrderSnapshot_MultipleDiffs_MergesFields()
	{
		var exec1 = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderId = 1,
			OrderPrice = 100m,
			OrderVolume = 10,
			OrderState = OrderStates.Pending,
			Balance = 10m,
			TransactionId = 1,
		};

		var exec2 = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderState = OrderStates.Active,
			Balance = 7m,
			TransactionId = 1,
		};

		var exec3 = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
			Balance = 0m,
			TransactionId = 1,
		};

		var snapshot = new[] { exec1, exec2, exec3 }.ToOrderSnapshot(1, new TestReceiver());

		snapshot.OrderId.AssertEqual(1L);
		snapshot.OrderPrice.AssertEqual(100m);
		snapshot.OrderState.AssertEqual(OrderStates.Done);
		snapshot.Balance.AssertEqual(0m);
	}

	[TestMethod]
	public void ToOrderSnapshot_OrdersByState()
	{
		// Pass in reverse order — should still sort by state
		var execDone = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
			Balance = 0m,
			OrderId = 1,
			OrderPrice = 100m,
			TransactionId = 1,
		};

		var execPending = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderState = OrderStates.Pending,
			Balance = 10m,
			TransactionId = 1,
		};

		// Done first, Pending second — but should be sorted to Pending first
		var snapshot = new[] { execDone, execPending }.ToOrderSnapshot(1, new TestReceiver());

		// Pending is base, Done overwrites
		snapshot.OrderState.AssertEqual(OrderStates.Done);
		snapshot.Balance.AssertEqual(0m);
	}

	[TestMethod]
	public void ToOrderSnapshot_EmptyCollection_Throws()
	{
		Throws<InvalidOperationException>(() =>
			Array.Empty<ExecutionMessage>().ToOrderSnapshot(1, new TestReceiver()));
	}

	#endregion

	#region AddDelta

	[TestMethod]
	public void AddDelta_Bids_AddNewLevel()
	{
		var from = new QuoteChange[] { new(100m, 10) };
		var delta = new QuoteChange[] { new(99m, 5) };

		var result = from.AddDelta(delta, true);

		result.Length.AssertEqual(2);
		result[0].Price.AssertEqual(100m);
		result[1].Price.AssertEqual(99m);
	}

	[TestMethod]
	public void AddDelta_Bids_UpdateExisting()
	{
		var from = new QuoteChange[] { new(100m, 10) };
		var delta = new QuoteChange[] { new(100m, 20) };

		var result = from.AddDelta(delta, true);

		result.Length.AssertEqual(1);
		result[0].Price.AssertEqual(100m);
		result[0].Volume.AssertEqual(20m);
	}

	[TestMethod]
	public void AddDelta_Bids_RemoveLevel_ZeroVolume()
	{
		var from = new QuoteChange[] { new(100m, 10), new(99m, 5) };
		var delta = new QuoteChange[] { new(100m, 0) };

		var result = from.AddDelta(delta, true);

		result.Length.AssertEqual(1);
		result[0].Price.AssertEqual(99m);
	}

	[TestMethod]
	public void AddDelta_Asks_AddNewLevel()
	{
		var from = new QuoteChange[] { new(100m, 10) };
		var delta = new QuoteChange[] { new(101m, 5) };

		var result = from.AddDelta(delta, false);

		result.Length.AssertEqual(2);
		result[0].Price.AssertEqual(100m);
		result[1].Price.AssertEqual(101m);
	}

	[TestMethod]
	public void AddDelta_Asks_RemoveLevel()
	{
		var from = new QuoteChange[] { new(100m, 10), new(101m, 5) };
		var delta = new QuoteChange[] { new(100m, 0) };

		var result = from.AddDelta(delta, false);

		result.Length.AssertEqual(1);
		result[0].Price.AssertEqual(101m);
	}

	[TestMethod]
	public void AddDelta_OrderBookMessage()
	{
		var from = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
		};

		var delta = new QuoteChangeMessage
		{
			SecurityId = from.SecurityId,
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(99m, 5)],
			Asks = [new QuoteChange(102m, 5)],
		};

		var result = from.AddDelta(delta);

		result.Bids.Length.AssertEqual(2);
		result.Asks.Length.AssertEqual(2);
		result.SecurityId.AssertEqual(from.SecurityId);
	}

	#endregion

	#region Group

	[TestMethod]
	public void Group_EmptyQuotes_ReturnsEmpty()
	{
		var result = Array.Empty<QuoteChange>().Group(Sides.Buy, 1m);
		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public void Group_SingleQuote_ReturnsOneGrouped()
	{
		var quotes = new QuoteChange[] { new(100m, 10) };
		var result = quotes.Group(Sides.Buy, 1m);

		result.Length.AssertEqual(1);
		result[0].Price.AssertEqual(100m);
		result[0].InnerQuotes.Length.AssertEqual(1);
	}

	[TestMethod]
	public void Group_Asks_GroupsByRange()
	{
		var quotes = new QuoteChange[]
		{
			new(100m, 10),
			new(100.5m, 5),
			new(101m, 3),
			new(102m, 7),
		};

		var result = quotes.Group(Sides.Sell, 1m);

		// 100 and 100.5 in first group, 101 starts new group (crosses nextPrice=101), 102 in third
		IsTrue(result.Length > 0);
		result[0].Price.AssertEqual(100m);
	}

	[TestMethod]
	public void Group_InvalidPriceRange_Throws()
	{
		var quotes = new QuoteChange[] { new(100m, 10) };
		Throws<ArgumentOutOfRangeException>(() => quotes.Group(Sides.Buy, 0m));
		Throws<ArgumentOutOfRangeException>(() => quotes.Group(Sides.Buy, -1m));
	}

	#endregion

	#region IsTradeTime

	[TestMethod]
	public void IsTradeTime_NotEnabled_ReturnsTrue()
	{
		var wt = new WorkingTime { IsEnabled = false };
		var board = new BoardMessage { WorkingTime = wt };

		board.IsTradeTime(DateTime.UtcNow).AssertTrue();
	}

	[TestMethod]
	public void IsTradeTime_Enabled_WithinPeriod_ReturnsTrue()
	{
		var now = new DateTime(2025, 1, 6, 12, 0, 0); // Monday
		var wt = new WorkingTime
		{
			IsEnabled = true,
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2026, 1, 1),
					Times = [new Range<TimeSpan>(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0))],
				}
			],
		};
		var board = new BoardMessage { WorkingTime = wt };

		board.IsTradeTime(now).AssertTrue();
	}

	[TestMethod]
	public void IsTradeTime_Enabled_OutsidePeriod_ReturnsFalse()
	{
		var now = new DateTime(2025, 1, 6, 20, 0, 0); // Monday 20:00
		var wt = new WorkingTime
		{
			IsEnabled = true,
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2026, 1, 1),
					Times = [new Range<TimeSpan>(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0))],
				}
			],
		};
		var board = new BoardMessage { WorkingTime = wt };

		board.IsTradeTime(now).AssertFalse();
	}

	[TestMethod]
	public void IsTradeTime_Holiday_ReturnsFalse()
	{
		var holiday = new DateTime(2025, 1, 6); // Monday
		var wt = new WorkingTime
		{
			IsEnabled = true,
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2026, 1, 1),
					Times = [new Range<TimeSpan>(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0))],
				}
			],
			SpecialHolidays = [holiday.Date],
		};
		var board = new BoardMessage { WorkingTime = wt };

		board.IsTradeTime(holiday.AddHours(12)).AssertFalse();
	}

	#endregion

	#region AddOrSubtractTradingDays

	[TestMethod]
	public void AddOrSubtractTradingDays_AddDays_SkipsWeekends()
	{
		// Friday Jan 3, 2025
		var friday = new DateTime(2025, 1, 3);
		var board = new BoardMessage
		{
			WorkingTime = new WorkingTime
			{
				IsEnabled = true,
				Periods =
				[
					new WorkingTimePeriod
					{
						Till = new DateTime(2026, 1, 1),
						Times = [new Range<TimeSpan>(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0))],
					}
				],
			},
		};

		var result = board.AddOrSubtractTradingDays(friday, 1, true);
		// Next trading day after Friday is Monday Jan 6
		result.AssertEqual(new DateTime(2025, 1, 6));
	}

	[TestMethod]
	public void AddOrSubtractTradingDays_SubtractDays()
	{
		// Monday Jan 6, 2025
		var monday = new DateTime(2025, 1, 6);
		var board = new BoardMessage
		{
			WorkingTime = new WorkingTime
			{
				IsEnabled = true,
				Periods =
				[
					new WorkingTimePeriod
					{
						Till = new DateTime(2026, 1, 1),
						Times = [new Range<TimeSpan>(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0))],
					}
				],
			},
		};

		var result = board.AddOrSubtractTradingDays(monday, -1, true);
		// Previous trading day before Monday is Friday Jan 3
		result.AssertEqual(new DateTime(2025, 1, 3));
	}

	[TestMethod]
	public void AddOrSubtractTradingDays_ZeroDays_ReturnsSame()
	{
		var date = new DateTime(2025, 1, 6);
		var board = new BoardMessage { WorkingTime = new WorkingTime() };

		var result = board.AddOrSubtractTradingDays(date, 0);
		result.AssertEqual(date);
	}

	#endregion

	#region DecodeToPeriods / EncodeToString

	[TestMethod]
	public void DecodeToPeriods_Empty_ReturnsEmpty()
	{
		var result = "".DecodeToPeriods().ToArray();
		result.Length.AssertEqual(0);
	}

	[TestMethod]
	public void EncodeToString_DecodeToPeriods_Roundtrip()
	{
		var periods = new[]
		{
			new WorkingTimePeriod
			{
				Till = new DateTime(2025, 12, 31, 0, 0, 0, DateTimeKind.Utc),
				Times = [new Range<TimeSpan>(new TimeSpan(9, 30, 0), new TimeSpan(16, 0, 0))],
			},
		};

		var encoded = periods.EncodeToString();
		IsTrue(encoded.Length > 0);

		var decoded = encoded.DecodeToPeriods().ToArray();
		decoded.Length.AssertEqual(1);
		decoded[0].Till.AssertEqual(periods[0].Till);
		decoded[0].Times.Count.AssertEqual(1);
		decoded[0].Times[0].Min.AssertEqual(new TimeSpan(9, 30, 0));
		decoded[0].Times[0].Max.AssertEqual(new TimeSpan(16, 0, 0));
	}

	#endregion

	// ===== Tier 2 =====

	#region ToReg / ToExec roundtrip

	[TestMethod]
	public void ToReg_CopiesAllFields()
	{
		var exec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			SecurityId = Helper.CreateSecurityId(),
			TransactionId = 42,
			OrderPrice = 100m,
			OrderVolume = 10,
			Balance = 7m,
			Currency = CurrencyTypes.USD,
			PortfolioName = "pf1",
			ClientCode = "cc",
			BrokerCode = "bc",
			Comment = "test",
			Side = Sides.Buy,
			TimeInForce = TimeInForce.PutInQueue,
			OrderType = OrderTypes.Limit,
			UserOrderId = "u1",
			StrategyId = "s1",
		};

		var reg = exec.ToReg();

		reg.SecurityId.AssertEqual(exec.SecurityId);
		reg.TransactionId.AssertEqual(42L);
		reg.Price.AssertEqual(100m);
		reg.Volume.AssertEqual(7m); // Balance ?? OrderVolume
		reg.Currency.AssertEqual(CurrencyTypes.USD);
		reg.PortfolioName.AssertEqual("pf1");
		reg.Side.AssertEqual(Sides.Buy);
		reg.OrderType.AssertEqual(OrderTypes.Limit);
	}

	[TestMethod]
	public void ToExec_CopiesAllFields()
	{
		var reg = new OrderRegisterMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			TransactionId = 42,
			Price = 100m,
			Volume = 10,
			Currency = CurrencyTypes.USD,
			PortfolioName = "pf1",
			Side = Sides.Buy,
			OrderType = OrderTypes.Limit,
			UserOrderId = "u1",
		};

		var exec = reg.ToExec();

		exec.SecurityId.AssertEqual(reg.SecurityId);
		exec.TransactionId.AssertEqual(42L);
		exec.OrderPrice.AssertEqual(100m);
		exec.OrderVolume.AssertEqual(10m);
		exec.Balance.AssertEqual(10m);
		exec.PortfolioName.AssertEqual("pf1");
		exec.Side.AssertEqual(Sides.Buy);
		exec.OrderType.AssertEqual(OrderTypes.Limit);
		exec.OrderState.AssertEqual(OrderStates.Pending);
		exec.HasOrderInfo.AssertTrue();
	}

	[TestMethod]
	public void ToReg_ToExec_Roundtrip()
	{
		var exec = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			SecurityId = Helper.CreateSecurityId(),
			TransactionId = 42,
			OrderPrice = 100m,
			OrderVolume = 10,
			Side = Sides.Sell,
			OrderType = OrderTypes.Limit,
			PortfolioName = "pf",
		};

		var reg = exec.ToReg();
		var back = reg.ToExec();

		back.SecurityId.AssertEqual(exec.SecurityId);
		back.TransactionId.AssertEqual(exec.TransactionId);
		back.OrderPrice.AssertEqual(exec.OrderPrice);
		back.Side.AssertEqual(exec.Side);
		back.OrderType.AssertEqual(exec.OrderType);
		back.PortfolioName.AssertEqual(exec.PortfolioName);
	}

	#endregion

	#region IsCanceled / IsMatched / IsMatchedPartially / IsMatchedEmpty

	[TestMethod]
	public void IsCanceled_DoneWithBalance_ReturnsTrue()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
			Balance = 5m,
			OrderVolume = 10m,
		};

		((IOrderMessage)msg).IsCanceled().AssertTrue();
	}

	[TestMethod]
	public void IsCanceled_DoneZeroBalance_ReturnsFalse()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
			Balance = 0m,
			OrderVolume = 10m,
		};

		((IOrderMessage)msg).IsCanceled().AssertFalse();
	}

	[TestMethod]
	public void IsMatched_DoneZeroBalance_ReturnsTrue()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
			Balance = 0m,
			OrderVolume = 10m,
		};

		((IOrderMessage)msg).IsMatched().AssertTrue();
	}

	[TestMethod]
	public void IsMatched_DoneWithBalance_ReturnsFalse()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderState = OrderStates.Done,
			Balance = 5m,
			OrderVolume = 10m,
		};

		((IOrderMessage)msg).IsMatched().AssertFalse();
	}

	[TestMethod]
	public void IsMatchedPartially_BalanceLessThanVolume_ReturnsTrue()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			Balance = 5m,
			OrderVolume = 10m,
		};

		((IOrderMessage)msg).IsMatchedPartially().AssertTrue();
	}

	[TestMethod]
	public void IsMatchedPartially_BalanceEqualsVolume_ReturnsFalse()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			Balance = 10m,
			OrderVolume = 10m,
		};

		((IOrderMessage)msg).IsMatchedPartially().AssertFalse();
	}

	[TestMethod]
	public void IsMatchedEmpty_BalanceEqualsVolume_ReturnsTrue()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			Balance = 10m,
			OrderVolume = 10m,
		};

		((IOrderMessage)msg).IsMatchedEmpty().AssertTrue();
	}

	[TestMethod]
	public void IsMatchedEmpty_BalanceLessThanVolume_ReturnsFalse()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			Balance = 5m,
			OrderVolume = 10m,
		};

		((IOrderMessage)msg).IsMatchedEmpty().AssertFalse();
	}

	#endregion

	#region ApplyNewBalance

	[TestMethod]
	public void ApplyNewBalance_ReturnsNewValue()
	{
		var result = ((decimal?)10m).ApplyNewBalance(7m, 1, new TestReceiver());
		result.AssertEqual(7m);
	}

	[TestMethod]
	public void ApplyNewBalance_NegativeBalance_LogsError()
	{
		var logger = new TestReceiver();
		((decimal?)10m).ApplyNewBalance(-1m, 1, logger);
		IsTrue(logger.Logs.Count > 0);
	}

	[TestMethod]
	public void ApplyNewBalance_IncreasingBalance_LogsError()
	{
		var logger = new TestReceiver();
		((decimal?)5m).ApplyNewBalance(10m, 1, logger);
		IsTrue(logger.Logs.Count > 0);
	}

	#endregion

	#region SafeGetVolume

	[TestMethod]
	public void SafeGetVolume_HasOrderVolume_ReturnsIt()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderVolume = 10m,
		};

		msg.SafeGetVolume().AssertEqual(10m);
	}

	[TestMethod]
	public void SafeGetVolume_HasTradeVolume_ReturnsIt()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Ticks,
			TradeVolume = 5m,
		};

		msg.SafeGetVolume().AssertEqual(5m);
	}

	[TestMethod]
	public void SafeGetVolume_NoVolume_Throws()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		};

		Throws<ArgumentOutOfRangeException>(() => msg.SafeGetVolume());
	}

	#endregion

	#region IsAllSecurity

	[TestMethod]
	public void IsAllSecurity_Default_ReturnsTrue()
	{
		default(SecurityId).IsAllSecurity().AssertTrue();
	}

	[TestMethod]
	public void IsAllSecurity_AssociatedBoard_ReturnsTrue()
	{
		var secId = new SecurityId
		{
			SecurityCode = SecurityId.AssociatedBoardCode,
			BoardCode = SecurityId.AssociatedBoardCode,
		};
		secId.IsAllSecurity().AssertTrue();
	}

	[TestMethod]
	public void IsAllSecurity_Specific_ReturnsFalse()
	{
		Helper.CreateSecurityId().IsAllSecurity().AssertFalse();
	}

	#endregion

	#region IsLookupAll

	[TestMethod]
	public void IsLookupAll_EmptyCriteria_ReturnsTrue()
	{
		var msg = new SecurityLookupMessage();
		msg.IsLookupAll().AssertTrue();
	}

	[TestMethod]
	public void IsLookupAll_WithSecurityType_ReturnsFalse()
	{
		var msg = new SecurityLookupMessage { SecurityType = SecurityTypes.Stock };
		msg.IsLookupAll().AssertFalse();
	}

	[TestMethod]
	public void IsLookupAll_WithName_ReturnsFalse()
	{
		var msg = new SecurityLookupMessage { Name = "test" };
		msg.IsLookupAll().AssertFalse();
	}

	#endregion

	#region ToMessageType2

	[TestMethod]
	public void ToMessageType2_Level1()
	{
		DataType.Level1.ToMessageType2().AssertEqual(MessageTypes.Level1Change);
	}

	[TestMethod]
	public void ToMessageType2_MarketDepth()
	{
		DataType.MarketDepth.ToMessageType2().AssertEqual(MessageTypes.QuoteChange);
	}

	[TestMethod]
	public void ToMessageType2_Ticks()
	{
		DataType.Ticks.ToMessageType2().AssertEqual(MessageTypes.Execution);
	}

	[TestMethod]
	public void ToMessageType2_News()
	{
		DataType.News.ToMessageType2().AssertEqual(MessageTypes.News);
	}

	[TestMethod]
	public void ToMessageType2_Securities()
	{
		DataType.Securities.ToMessageType2().AssertEqual(MessageTypes.Security);
	}

	#endregion

	#region GetPlazaTimeInForce

	[TestMethod]
	public void GetPlazaTimeInForce_Bit1_PutInQueue()
	{
		0x1L.GetPlazaTimeInForce().AssertEqual(TimeInForce.PutInQueue);
	}

	[TestMethod]
	public void GetPlazaTimeInForce_Bit2_CancelBalance()
	{
		0x2L.GetPlazaTimeInForce().AssertEqual(TimeInForce.CancelBalance);
	}

	[TestMethod]
	public void GetPlazaTimeInForce_Bit80000_MatchOrCancel()
	{
		0x80000L.GetPlazaTimeInForce().AssertEqual(TimeInForce.MatchOrCancel);
	}

	[TestMethod]
	public void GetPlazaTimeInForce_NoBits_ReturnsNull()
	{
		0L.GetPlazaTimeInForce().AssertNull();
	}

	#endregion

	#region LastTradeDay

	[TestMethod]
	public void LastTradeDay_AlreadyTradeDay_ReturnsSame()
	{
		var monday = new DateTime(2025, 1, 6); // Monday
		var board = new BoardMessage
		{
			WorkingTime = new WorkingTime
			{
				IsEnabled = true,
				Periods =
				[
					new WorkingTimePeriod
					{
						Till = new DateTime(2026, 1, 1),
						Times = [new Range<TimeSpan>(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0))],
					}
				],
			},
		};

		board.LastTradeDay(monday, true).AssertEqual(monday);
	}

	[TestMethod]
	public void LastTradeDay_Sunday_ReturnsFriday()
	{
		var sunday = new DateTime(2025, 1, 5); // Sunday
		var board = new BoardMessage
		{
			WorkingTime = new WorkingTime
			{
				IsEnabled = true,
				Periods =
				[
					new WorkingTimePeriod
					{
						Till = new DateTime(2026, 1, 1),
						Times = [new Range<TimeSpan>(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0))],
					}
				],
			},
		};

		board.LastTradeDay(sunday, true).AssertEqual(new DateTime(2025, 1, 3));
	}

	#endregion

	// ===== Tier 3 =====

	#region ToReadableString (DataType)

	[TestMethod]
	public void ToReadableString_Days()
	{
		var dt = Extensions.TimeFrame(TimeSpan.FromDays(1));
		var str = dt.ToReadableString();
		IsTrue(str.Length > 0);
	}

	[TestMethod]
	public void ToReadableString_Minutes()
	{
		var dt = Extensions.TimeFrame(TimeSpan.FromMinutes(5));
		var str = dt.ToReadableString();
		IsTrue(str.Length > 0);
	}

	#endregion

	#region Join (OrderBook)

	[TestMethod]
	public void Join_MergesAndSortsBidsAsks()
	{
		var secId = Helper.CreateSecurityId();
		var original = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10), new QuoteChange(98m, 5)],
			Asks = [new QuoteChange(101m, 10), new QuoteChange(103m, 5)],
		};
		var rare = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(99m, 3)],
			Asks = [new QuoteChange(102m, 3)],
		};

		var result = original.Join(rare);

		result.SecurityId.AssertEqual(secId);
		// Bids sorted descending: 100, 99, 98
		result.Bids.Length.AssertEqual(3);
		result.Bids[0].Price.AssertEqual(100m);
		result.Bids[1].Price.AssertEqual(99m);
		result.Bids[2].Price.AssertEqual(98m);
		// Asks sorted ascending: 101, 102, 103
		result.Asks.Length.AssertEqual(3);
		result.Asks[0].Price.AssertEqual(101m);
		result.Asks[1].Price.AssertEqual(102m);
		result.Asks[2].Price.AssertEqual(103m);
	}

	[TestMethod]
	public void Join_EmptyRare_ReturnsOriginal()
	{
		var secId = Helper.CreateSecurityId();
		var original = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
		};
		var rare = new QuoteChangeMessage
		{
			SecurityId = secId,
			ServerTime = DateTime.UtcNow,
			Bids = [],
			Asks = [],
		};

		var result = original.Join(rare);

		result.Bids.Length.AssertEqual(1);
		result.Asks.Length.AssertEqual(1);
	}

	#endregion

	#region IsHalfEmpty

	[TestMethod]
	public void IsHalfEmpty_OnlyBids_ReturnsTrue()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [],
		};

		msg.IsHalfEmpty().AssertTrue();
	}

	[TestMethod]
	public void IsHalfEmpty_OnlyAsks_ReturnsTrue()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [],
			Asks = [new QuoteChange(101m, 10)],
		};

		msg.IsHalfEmpty().AssertTrue();
	}

	[TestMethod]
	public void IsHalfEmpty_BothSides_ReturnsFalse()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
		};

		msg.IsHalfEmpty().AssertFalse();
	}

	[TestMethod]
	public void IsHalfEmpty_Empty_ReturnsFalse()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [],
			Asks = [],
		};

		msg.IsHalfEmpty().AssertFalse();
	}

	#endregion
}
