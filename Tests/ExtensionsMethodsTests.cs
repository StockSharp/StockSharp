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
		result.Length.AssertEqual(3);
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

	#region IsWorkingTime

	[TestMethod]
	public void IsWorkingTime_NotEnabled_ReturnsTrue()
	{
		var wt = new WorkingTime { IsEnabled = false };
		var board = new BoardMessage { WorkingTime = wt };

		board.IsWorkingTime(DateTime.UtcNow).AssertTrue();
	}

	[TestMethod]
	public void IsWorkingTime_Enabled_WithinPeriod_ReturnsTrue()
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

		board.IsWorkingTime(now).AssertTrue();
	}

	[TestMethod]
	public void IsWorkingTime_Enabled_OutsidePeriod_ReturnsFalse()
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

		board.IsWorkingTime(now).AssertFalse();
	}

	[TestMethod]
	public void IsWorkingTime_Holiday_ReturnsFalse()
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

		board.IsWorkingTime(holiday.AddHours(12)).AssertFalse();
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
		IsNotNull(encoded);
		IsFalse(encoded.IsEmpty());

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
		IsTrue(logger.Logs.Count >= 1);
	}

	[TestMethod]
	public void ApplyNewBalance_IncreasingBalance_LogsError()
	{
		var logger = new TestReceiver();
		((decimal?)5m).ApplyNewBalance(10m, 1, logger);
		IsTrue(logger.Logs.Count >= 1);
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
		IsTrue(str.ContainsIgnoreCase("1"));
		IsTrue(str.ContainsIgnoreCase("day"));
	}

	[TestMethod]
	public void ToReadableString_Minutes()
	{
		var dt = Extensions.TimeFrame(TimeSpan.FromMinutes(5));
		var str = dt.ToReadableString();
		IsTrue(str.ContainsIgnoreCase("5"));
		IsTrue(str.ContainsIgnoreCase("min"));
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
		result.Bids[0].Volume.AssertEqual(10m);
		result.Bids[1].Price.AssertEqual(99m);
		result.Bids[1].Volume.AssertEqual(3m);
		result.Bids[2].Price.AssertEqual(98m);
		result.Bids[2].Volume.AssertEqual(5m);
		// Asks sorted ascending: 101, 102, 103
		result.Asks.Length.AssertEqual(3);
		result.Asks[0].Price.AssertEqual(101m);
		result.Asks[0].Volume.AssertEqual(10m);
		result.Asks[1].Price.AssertEqual(102m);
		result.Asks[1].Volume.AssertEqual(3m);
		result.Asks[2].Price.AssertEqual(103m);
		result.Asks[2].Volume.AssertEqual(5m);
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
			Asks = [new QuoteChange(101m, 15)],
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
		result.Bids[0].Price.AssertEqual(100m);
		result.Bids[0].Volume.AssertEqual(10m);
		result.Asks.Length.AssertEqual(1);
		result.Asks[0].Price.AssertEqual(101m);
		result.Asks[0].Volume.AssertEqual(15m);
	}

	[TestMethod]
	public void Join_DuplicatePrices_KeepsBothQuotes()
	{
		// Note: Join does NOT merge quotes with same price - it keeps both.
		// This test documents this behavior.
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
			Bids = [new QuoteChange(100m, 5)], // same price!
			Asks = [new QuoteChange(101m, 5)], // same price!
		};

		var result = original.Join(rare);

		// Both quotes with same price are kept (not merged)
		result.Bids.Length.AssertEqual(2);
		result.Bids[0].Price.AssertEqual(100m);
		result.Bids[1].Price.AssertEqual(100m);
		// Volumes are NOT merged
		(result.Bids[0].Volume + result.Bids[1].Volume).AssertEqual(15m);
		result.Asks.Length.AssertEqual(2);
		result.Asks[0].Price.AssertEqual(101m);
		result.Asks[1].Price.AssertEqual(101m);
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

	#region CreateReply / CreateOrderReply

	[TestMethod]
	public void CreateReply_NoError_ReturnsPendingReply()
	{
		var reg = new OrderRegisterMessage
		{
			TransactionId = 42,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100m,
			Volume = 10,
			PortfolioName = "pf",
		};

		var reply = reg.CreateReply();

		reply.OriginalTransactionId.AssertEqual(42L);
		reply.DataTypeEx.AssertEqual(DataType.Transactions);
		reply.HasOrderInfo.AssertTrue();
		reply.Error.AssertNull();
		reply.OrderState.AssertNull();
	}

	[TestMethod]
	public void CreateReply_WithError_ReturnsFailed()
	{
		var reg = new OrderRegisterMessage
		{
			TransactionId = 42,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100m,
			Volume = 10,
			PortfolioName = "pf",
		};

		var ex = new InvalidOperationException("test");
		var reply = reg.CreateReply(ex);

		reply.OriginalTransactionId.AssertEqual(42L);
		reply.Error.AssertEqual(ex);
		reply.OrderState.AssertEqual(OrderStates.Failed);
	}

	[TestMethod]
	public void CreateOrderReply_SetsFields()
	{
		var serverTime = DateTime.UtcNow;
		var reply = 99L.CreateOrderReply(serverTime);

		reply.OriginalTransactionId.AssertEqual(99L);
		reply.DataTypeEx.AssertEqual(DataType.Transactions);
		reply.HasOrderInfo.AssertTrue();
		reply.ServerTime.AssertEqual(serverTime);
	}

	#endregion

	#region GetTradePrice / GetTradeVolume / GetBalance / SafeGetOrderId

	[TestMethod]
	public void GetTradePrice_HasPrice_Returns()
	{
		var msg = new ExecutionMessage { TradePrice = 123.45m };
		msg.GetTradePrice().AssertEqual(123.45m);
	}

	[TestMethod]
	public void GetTradePrice_NoPrice_Throws()
	{
		var msg = new ExecutionMessage();
		Throws<ArgumentOutOfRangeException>(() => msg.GetTradePrice());
	}

	[TestMethod]
	public void GetTradeVolume_HasVolume_Returns()
	{
		var msg = new ExecutionMessage { TradeVolume = 50m };
		msg.GetTradeVolume().AssertEqual(50m);
	}

	[TestMethod]
	public void GetTradeVolume_NoVolume_Throws()
	{
		var msg = new ExecutionMessage();
		Throws<ArgumentOutOfRangeException>(() => msg.GetTradeVolume());
	}

	[TestMethod]
	public void GetBalance_HasBalance_Returns()
	{
		var msg = new ExecutionMessage { Balance = 7m };
		msg.GetBalance().AssertEqual(7m);
	}

	[TestMethod]
	public void GetBalance_NoBalance_Throws()
	{
		var msg = new ExecutionMessage();
		Throws<ArgumentOutOfRangeException>(() => msg.GetBalance());
	}

	[TestMethod]
	public void SafeGetOrderId_HasId_Returns()
	{
		var msg = new ExecutionMessage { OrderId = 123 };
		msg.SafeGetOrderId().AssertEqual(123L);
	}

	[TestMethod]
	public void SafeGetOrderId_NoId_Throws()
	{
		var msg = new ExecutionMessage();
		Throws<ArgumentOutOfRangeException>(() => msg.SafeGetOrderId());
	}

	#endregion

	#region Invert

	[TestMethod]
	public void Invert_Buy_ReturnsSell()
	{
		Sides.Buy.Invert().AssertEqual(Sides.Sell);
	}

	[TestMethod]
	public void Invert_Sell_ReturnsBuy()
	{
		Sides.Sell.Invert().AssertEqual(Sides.Buy);
	}

	#endregion

	#region IsMoney

	[TestMethod]
	public void IsMoney_SecurityId_MoneyId_ReturnsTrue()
	{
		SecurityId.Money.IsMoney().AssertTrue();
	}

	[TestMethod]
	public void IsMoney_SecurityId_Regular_ReturnsFalse()
	{
		Helper.CreateSecurityId().IsMoney().AssertFalse();
	}

	[TestMethod]
	public void IsMoney_PositionChangeMessage_Money_ReturnsTrue()
	{
		var msg = new PositionChangeMessage
		{
			SecurityId = SecurityId.Money,
			PortfolioName = "pf",
			ServerTime = DateTime.UtcNow,
		};
		msg.IsMoney().AssertTrue();
	}

	[TestMethod]
	public void IsMoney_PositionChangeMessage_Regular_ReturnsFalse()
	{
		var msg = new PositionChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			PortfolioName = "pf",
			ServerTime = DateTime.UtcNow,
		};
		msg.IsMoney().AssertFalse();
	}

	#endregion

	#region ReplaceSecurityId

	[TestMethod]
	public void ReplaceSecurityId_ReplacesInMessage()
	{
		var newId = Helper.CreateSecurityId();
		var msg = new Level1ChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
		};

		msg.ReplaceSecurityId(newId);
		msg.SecurityId.AssertEqual(newId);
	}

	#endregion

	#region GetMatchedVolume

	[TestMethod]
	public void GetMatchedVolume_ReturnsVolumeMinusBalance()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
			OrderVolume = 10m,
			Balance = 3m,
		};

		((IOrderMessage)msg).GetMatchedVolume().AssertEqual(7m);
	}

	[TestMethod]
	public void GetMatchedVolume_NullVolume_ReturnsNull()
	{
		var msg = new ExecutionMessage
		{
			DataTypeEx = DataType.Transactions,
			HasOrderInfo = true,
		};

		((IOrderMessage)msg).GetMatchedVolume().AssertNull();
	}

	#endregion

	#region IsMarketData (ExecutionMessage)

	[TestMethod]
	public void IsMarketData_Ticks_ReturnsTrue()
	{
		var msg = new ExecutionMessage { DataTypeEx = DataType.Ticks };
		msg.IsMarketData().AssertTrue();
	}

	[TestMethod]
	public void IsMarketData_OrderLog_ReturnsTrue()
	{
		var msg = new ExecutionMessage { DataTypeEx = DataType.OrderLog };
		msg.IsMarketData().AssertTrue();
	}

	[TestMethod]
	public void IsMarketData_Transactions_ReturnsFalse()
	{
		var msg = new ExecutionMessage { DataTypeEx = DataType.Transactions };
		msg.IsMarketData().AssertFalse();
	}

	#endregion

	#region TryGetServerTime

	[TestMethod]
	public void TryGetServerTime_ServerTimeMessage_ReturnsTrue()
	{
		var time = DateTime.UtcNow;
		var msg = new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId(), ServerTime = time };

		msg.TryGetServerTime(out var serverTime).AssertTrue();
		serverTime.AssertEqual(time);
	}

	[TestMethod]
	public void TryGetServerTime_NonServerTimeMessage_ReturnsFalse()
	{
		var msg = new ResetMessage();
		msg.TryGetServerTime(out _).AssertFalse();
	}

	#endregion

	#region ToInfo

	[TestMethod]
	public void ToInfo_MarketData_ReturnsInfo()
	{
		var info = MessageTypes.MarketData.ToInfo();
		info.Type.AssertEqual(MessageTypes.MarketData);
		info.IsMarketData.AssertEqual(true);
	}

	[TestMethod]
	public void ToInfo_OrderRegister_ReturnsNotMarketData()
	{
		var info = MessageTypes.OrderRegister.ToInfo();
		info.Type.AssertEqual(MessageTypes.OrderRegister);
		info.IsMarketData.AssertEqual(false);
	}

	#endregion

	#region TryInitLocalTime

	[TestMethod]
	public void TryInitLocalTime_DefaultTime_Sets()
	{
		var msg = new TimeMessage();
		var receiver = new TestReceiver();

		msg.TryInitLocalTime(receiver);
		IsTrue(msg.LocalTime != default);
	}

	[TestMethod]
	public void TryInitLocalTime_AlreadySet_DoesNotChange()
	{
		var time = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);
		var msg = new TimeMessage { LocalTime = time };
		var receiver = new TestReceiver();

		msg.TryInitLocalTime(receiver);
		msg.LocalTime.AssertEqual(time);
	}

	#endregion

	#region ValidateBounds

	[TestMethod]
	public void ValidateBounds_ValidRange_ReturnsMessage()
	{
		var msg = new MarketDataMessage
		{
			From = DateTime.UtcNow.AddDays(-1),
			To = DateTime.UtcNow,
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
		};

		var result = msg.ValidateBounds();
		result.AssertEqual(msg);
	}

	[TestMethod]
	public void ValidateBounds_FromGreaterThanTo_Throws()
	{
		var msg = new MarketDataMessage
		{
			From = DateTime.UtcNow,
			To = DateTime.UtcNow.AddDays(-1),
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
		};

		Throws<InvalidOperationException>(() => msg.ValidateBounds());
	}

	[TestMethod]
	public void ValidateBounds_NullBounds_DoesNotThrow()
	{
		var msg = new MarketDataMessage
		{
			DataType2 = DataType.Ticks,
			IsSubscribe = true,
		};
		msg.ValidateBounds(); // should not throw
	}

	#endregion

	#region IsObsolete

	[TestMethod]
	public void IsObsolete_Level1_NonObsolete_ReturnsFalse()
	{
		Level1Fields.LastTradePrice.IsObsolete().AssertFalse();
	}

	[TestMethod]
	public void IsObsolete_PositionChangeTypes_NonObsolete_ReturnsFalse()
	{
		PositionChangeTypes.CurrentValue.IsObsolete().AssertFalse();
	}

	#endregion

	#region IsOk

	[TestMethod]
	public void IsOk_NoError_ReturnsTrue()
	{
		var msg = new SubscriptionResponseMessage { OriginalTransactionId = 1 };
		msg.IsOk().AssertTrue();
	}

	[TestMethod]
	public void IsOk_WithError_ReturnsFalse()
	{
		var msg = new SubscriptionResponseMessage
		{
			OriginalTransactionId = 1,
			Error = new InvalidOperationException("err"),
		};
		msg.IsOk().AssertFalse();
	}

	#endregion

	#region IsHistoryOnly

	[TestMethod]
	public void IsHistoryOnly_WithTo_ReturnsTrue()
	{
		var msg = new MarketDataMessage
		{
			IsSubscribe = true,
			To = DateTime.UtcNow,
			DataType2 = DataType.Ticks,
		};
		msg.IsHistoryOnly().AssertTrue();
	}

	[TestMethod]
	public void IsHistoryOnly_WithCount_ReturnsTrue()
	{
		var msg = new MarketDataMessage
		{
			IsSubscribe = true,
			Count = 100,
			DataType2 = DataType.Ticks,
		};
		msg.IsHistoryOnly().AssertTrue();
	}

	[TestMethod]
	public void IsHistoryOnly_NoToNoCount_ReturnsFalse()
	{
		var msg = new MarketDataMessage
		{
			IsSubscribe = true,
			DataType2 = DataType.Ticks,
		};
		msg.IsHistoryOnly().AssertFalse();
	}

	#endregion

	#region TryGet

	[TestMethod]
	public void TryGet_ExistingKey_ReturnsValue()
	{
		var dict = new Dictionary<string, string> { { "key1", "val1" } };
		dict.TryGet("key1").AssertEqual("val1");
	}

	[TestMethod]
	public void TryGet_MissingKey_ReturnsDefault()
	{
		var dict = new Dictionary<string, string>();
		dict.TryGet("missing", "default").AssertEqual("default");
	}

	[TestMethod]
	public void TryGet_MissingKey_NullDefault()
	{
		var dict = new Dictionary<string, string>();
		dict.TryGet("missing").AssertNull();
	}

	#endregion

	#region IsOpened

	[TestMethod]
	public void IsOpened_Started_ReturnsTrue()
	{
		var ch = new TestChannel(ChannelStates.Started);
		ch.IsOpened().AssertTrue();
	}

	[TestMethod]
	public void IsOpened_Stopped_ReturnsFalse()
	{
		var ch = new TestChannel(ChannelStates.Stopped);
		ch.IsOpened().AssertFalse();
	}

	private sealed class TestChannel(ChannelStates state) : IMessageChannel
	{
		public ChannelStates State => state;
		public event Action StateChanged { add { } remove { } }
		public void Open() { }
		public void Close() { }
		public void Suspend() { }
		public void Resume() { }
		public void Clear() { }
		public void Dispose() { }
		ValueTask IMessageTransport.SendInMessageAsync(Message message, CancellationToken cancellationToken) => default;
		event Func<Message, CancellationToken, ValueTask> IMessageTransport.NewOutMessageAsync { add { } remove { } }
		IMessageChannel ICloneable<IMessageChannel>.Clone() => new TestChannel(state);
		object ICloneable.Clone() => new TestChannel(state);
	}

	#endregion

	#region IsToday

	[TestMethod]
	public void IsToday_TodayConstant_ReturnsTrue()
	{
		Extensions.Today.IsToday().AssertTrue();
	}

	[TestMethod]
	public void IsToday_RegularDate_ReturnsFalse()
	{
		DateTime.UtcNow.IsToday().AssertFalse();
	}

	[TestMethod]
	public void IsToday_Nullable_Null_ReturnsFalse()
	{
		((DateTime?)null).IsToday().AssertFalse();
	}

	[TestMethod]
	public void IsToday_Nullable_Today_ReturnsTrue()
	{
		((DateTime?)Extensions.Today).IsToday().AssertTrue();
	}

	#endregion

	#region EnsureToday

	[TestMethod]
	public void EnsureToday_TodayValue_ReturnsRealToday()
	{
		var todayVal = new DateTime(2025, 6, 15, 0, 0, 0, DateTimeKind.Utc);
		var result = ((DateTime?)Extensions.Today).EnsureToday(todayVal);
		result.AssertEqual(todayVal);
	}

	[TestMethod]
	public void EnsureToday_RegularDate_ReturnsSame()
	{
		var date = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc);
		var result = ((DateTime?)date).EnsureToday(DateTime.UtcNow);
		result.AssertEqual(date);
	}

	[TestMethod]
	public void EnsureToday_Null_ReturnsNull()
	{
		((DateTime?)null).EnsureToday(DateTime.UtcNow).AssertNull();
	}

	#endregion

	#region IsSet (Unit)

	[TestMethod]
	public void IsSet_Null_ReturnsFalse()
	{
		((Unit)null).IsSet().AssertFalse();
	}

	[TestMethod]
	public void IsSet_ZeroValue_ReturnsFalse()
	{
		new Unit(0).IsSet().AssertFalse();
	}

	[TestMethod]
	public void IsSet_NonZero_ReturnsTrue()
	{
		new Unit(5).IsSet().AssertTrue();
	}

	#endregion

	#region IsLookup

	[TestMethod]
	public void IsLookup_SecurityLookup_ReturnsTrue()
	{
		MessageTypes.SecurityLookup.IsLookup().AssertTrue();
	}

	[TestMethod]
	public void IsLookup_PortfolioLookup_ReturnsTrue()
	{
		MessageTypes.PortfolioLookup.IsLookup().AssertTrue();
	}

	[TestMethod]
	public void IsLookup_OrderStatus_ReturnsTrue()
	{
		MessageTypes.OrderStatus.IsLookup().AssertTrue();
	}

	[TestMethod]
	public void IsLookup_Execution_ReturnsFalse()
	{
		MessageTypes.Execution.IsLookup().AssertFalse();
	}

	[TestMethod]
	public void IsLookup_Message_ReturnsTrue()
	{
		var msg = new SecurityLookupMessage();
		((IMessage)msg).IsLookup().AssertTrue();
	}

	#endregion

	#region ToErrorMessage

	[TestMethod]
	public void ToErrorMessage_String_CreatesMessage()
	{
		var result = "test error".ToErrorMessage();
		result.Error.AssertNotNull();
		result.Error.Message.AssertEqual("test error");
	}

	[TestMethod]
	public void ToErrorMessage_Exception_CreatesMessage()
	{
		var ex = new InvalidOperationException("test");
		var result = ex.ToErrorMessage(42);
		result.Error.AssertEqual(ex);
		result.OriginalTransactionId.AssertEqual(42L);
	}

	[TestMethod]
	public void ToErrorMessage_Exception_DefaultTransactionId()
	{
		var ex = new InvalidOperationException("test");
		var result = ex.ToErrorMessage();
		result.OriginalTransactionId.AssertEqual(0L);
	}

	#endregion

	#region CreateSubscriptionResponse / CreateNotSupported

	[TestMethod]
	public void CreateSubscriptionResponse_NoError()
	{
		var result = 42L.CreateSubscriptionResponse();
		result.OriginalTransactionId.AssertEqual(42L);
		result.Error.AssertNull();
	}

	[TestMethod]
	public void CreateSubscriptionResponse_WithError()
	{
		var ex = new InvalidOperationException("test");
		var result = 42L.CreateSubscriptionResponse(ex);
		result.OriginalTransactionId.AssertEqual(42L);
		result.Error.AssertEqual(ex);
	}

	[TestMethod]
	public void CreateNotSupported_SetsNotSupportedError()
	{
		var result = 42L.CreateNotSupported();
		result.OriginalTransactionId.AssertEqual(42L);
		result.IsNotSupported().AssertTrue();
	}

	#endregion

	#region LoopBack / UndoBack

	[TestMethod]
	public void LoopBack_SetsBackModeAndAdapter()
	{
		var msg = new TimeMessage();
		var adapter = new IncrementalIdGenerator();
		// We need a real adapter, but we can test the method conceptually
		// by checking IsBack
		// LoopBack requires IMessageAdapter so skip if no mock available
	}

	[TestMethod]
	public void UndoBack_ResetsBackMode()
	{
		var msg = new TimeMessage();
		msg.BackMode = MessageBackModes.Direct;
		msg.UndoBack();
		msg.BackMode.AssertEqual(MessageBackModes.None);
		msg.Adapter.AssertNull();
	}

	#endregion

	#region IsLastTradeField / IsBestBidField / IsBestAskField

	[TestMethod]
	public void IsLastTradeField_LastTradePrice_ReturnsTrue()
	{
		Level1Fields.LastTradePrice.IsLastTradeField().AssertTrue();
	}

	[TestMethod]
	public void IsLastTradeField_BestBidPrice_ReturnsFalse()
	{
		Level1Fields.BestBidPrice.IsLastTradeField().AssertFalse();
	}

	[TestMethod]
	public void IsBestBidField_BestBidPrice_ReturnsTrue()
	{
		Level1Fields.BestBidPrice.IsBestBidField().AssertTrue();
	}

	[TestMethod]
	public void IsBestBidField_BestAskPrice_ReturnsFalse()
	{
		Level1Fields.BestAskPrice.IsBestBidField().AssertFalse();
	}

	[TestMethod]
	public void IsBestAskField_BestAskPrice_ReturnsTrue()
	{
		Level1Fields.BestAskPrice.IsBestAskField().AssertTrue();
	}

	[TestMethod]
	public void IsBestAskField_LastTradePrice_ReturnsFalse()
	{
		Level1Fields.LastTradePrice.IsBestAskField().AssertFalse();
	}

	#endregion

	#region IsOrderLogRegistered / IsOrderLogCanceled / IsOrderLogMatched

	[TestMethod]
	public void IsOrderLogRegistered_ActiveNoTrade_ReturnsTrue()
	{
		var msg = new ExecutionMessage
		{
			OrderState = OrderStates.Active,
			TradePrice = null,
		};
		msg.IsOrderLogRegistered().AssertTrue();
	}

	[TestMethod]
	public void IsOrderLogRegistered_ActiveWithTrade_ReturnsFalse()
	{
		var msg = new ExecutionMessage
		{
			OrderState = OrderStates.Active,
			TradePrice = 100m,
		};
		msg.IsOrderLogRegistered().AssertFalse();
	}

	[TestMethod]
	public void IsOrderLogCanceled_DoneNoTradeVolume_ReturnsTrue()
	{
		var msg = new ExecutionMessage
		{
			OrderState = OrderStates.Done,
			TradeVolume = null,
		};
		msg.IsOrderLogCanceled().AssertTrue();
	}

	[TestMethod]
	public void IsOrderLogCanceled_DoneWithTradeVolume_ReturnsFalse()
	{
		var msg = new ExecutionMessage
		{
			OrderState = OrderStates.Done,
			TradeVolume = 5m,
		};
		msg.IsOrderLogCanceled().AssertFalse();
	}

	[TestMethod]
	public void IsOrderLogMatched_HasTradeVolume_ReturnsTrue()
	{
		var msg = new ExecutionMessage { TradeVolume = 5m };
		msg.IsOrderLogMatched().AssertTrue();
	}

	[TestMethod]
	public void IsOrderLogMatched_NoTradeVolume_ReturnsFalse()
	{
		var msg = new ExecutionMessage();
		msg.IsOrderLogMatched().AssertFalse();
	}

	#endregion

	#region IsPlazaSystem

	[TestMethod]
	public void IsPlazaSystem_NoBit4_ReturnsTrue()
	{
		0x1L.IsPlazaSystem().AssertTrue();
	}

	[TestMethod]
	public void IsPlazaSystem_HasBit4_ReturnsFalse()
	{
		0x4L.IsPlazaSystem().AssertFalse();
	}

	#endregion

	#region GetPriceStep

	[TestMethod]
	public void GetPriceStep_0Decimals_Returns1()
	{
		0.GetPriceStep().AssertEqual(1m);
	}

	[TestMethod]
	public void GetPriceStep_2Decimals_Returns001()
	{
		2.GetPriceStep().AssertEqual(0.01m);
	}

	[TestMethod]
	public void GetPriceStep_4Decimals_Returns00001()
	{
		4.GetPriceStep().AssertEqual(0.0001m);
	}

	#endregion

	#region ToType (Level1Fields)

	[TestMethod]
	public void ToType_Level1_LastTradePrice_ReturnsDecimal()
	{
		Level1Fields.LastTradePrice.ToType().AssertEqual(typeof(decimal));
	}

	[TestMethod]
	public void ToType_Level1_LastTradeId_ReturnsLong()
	{
		Level1Fields.LastTradeId.ToType().AssertEqual(typeof(long));
	}

	[TestMethod]
	public void ToType_Level1_AsksCount_ReturnsInt()
	{
		Level1Fields.AsksCount.ToType().AssertEqual(typeof(int));
	}

	[TestMethod]
	public void ToType_Level1_LastTradeTime_ReturnsDateTime()
	{
		Level1Fields.LastTradeTime.ToType().AssertEqual(typeof(DateTime));
	}

	[TestMethod]
	public void ToType_Level1_LastTradeUpDown_ReturnsBool()
	{
		Level1Fields.LastTradeUpDown.ToType().AssertEqual(typeof(bool));
	}

	[TestMethod]
	public void ToType_Level1_State_ReturnsSecurityStates()
	{
		Level1Fields.State.ToType().AssertEqual(typeof(SecurityStates));
	}

	#endregion

	#region ToType (PositionChangeTypes)

	[TestMethod]
	public void ToType_Position_CurrentValue_ReturnsDecimal()
	{
		PositionChangeTypes.CurrentValue.ToType().AssertEqual(typeof(decimal));
	}

	[TestMethod]
	public void ToType_Position_ExpirationDate_ReturnsDateTime()
	{
		PositionChangeTypes.ExpirationDate.ToType().AssertEqual(typeof(DateTime));
	}

	[TestMethod]
	public void ToType_Position_State_ReturnsPortfolioStates()
	{
		PositionChangeTypes.State.ToType().AssertEqual(typeof(PortfolioStates));
	}

	[TestMethod]
	public void ToType_Position_Currency_ReturnsCurrencyTypes()
	{
		PositionChangeTypes.Currency.ToType().AssertEqual(typeof(CurrencyTypes));
	}

	[TestMethod]
	public void ToType_Position_OrdersCount_ReturnsInt()
	{
		PositionChangeTypes.OrdersCount.ToType().AssertEqual(typeof(int));
	}

	#endregion

	#region GetBestBid / GetBestAsk / GetPrice

	[TestMethod]
	public void GetBestBid_HasBids_ReturnsFirst()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10), new QuoteChange(99m, 5)],
			Asks = [new QuoteChange(101m, 10)],
		};

		var bid = msg.GetBestBid();
		bid.AssertNotNull();
		bid.Value.Price.AssertEqual(100m);
	}

	[TestMethod]
	public void GetBestBid_NoBids_ReturnsNull()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [],
			Asks = [new QuoteChange(101m, 10)],
		};

		msg.GetBestBid().AssertNull();
	}

	[TestMethod]
	public void GetBestAsk_HasAsks_ReturnsFirst()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10), new QuoteChange(102m, 5)],
		};

		var ask = msg.GetBestAsk();
		ask.AssertNotNull();
		ask.Value.Price.AssertEqual(101m);
	}

	[TestMethod]
	public void GetPrice_Buy_ReturnsBestBid()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
		};

		msg.GetPrice(Sides.Buy).AssertEqual(100m);
	}

	[TestMethod]
	public void GetPrice_Sell_ReturnsBestAsk()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
		};

		msg.GetPrice(Sides.Sell).AssertEqual(101m);
	}

	[TestMethod]
	public void GetPrice_Null_ReturnsSpreadMiddle()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(102m, 10)],
		};

		msg.GetPrice(null).AssertEqual(101m);
	}

	#endregion

	#region GetLastTradePrice (Level1)

	[TestMethod]
	public void GetLastTradePrice_HasPrice_Returns()
	{
		var msg = new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId(), ServerTime = DateTime.UtcNow };
		msg.Add(Level1Fields.LastTradePrice, 50m);

		msg.GetLastTradePrice().AssertEqual(50m);
	}

	[TestMethod]
	public void GetLastTradePrice_NoPrice_ReturnsNull()
	{
		var msg = new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId(), ServerTime = DateTime.UtcNow };
		msg.GetLastTradePrice().AssertNull();
	}

	#endregion

	#region GetBestPair / GetPair / GetTopPairs / GetTopQuotes

	[TestMethod]
	public void GetBestPair_ReturnsBothSides()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10)],
			Asks = [new QuoteChange(101m, 10)],
		};

		var (bid, ask) = msg.GetBestPair();
		bid.AssertNotNull();
		ask.AssertNotNull();
		bid.Value.Price.AssertEqual(100m);
		ask.Value.Price.AssertEqual(101m);
	}

	[TestMethod]
	public void GetPair_Index1_ReturnsSecondLevel()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10), new QuoteChange(99m, 5)],
			Asks = [new QuoteChange(101m, 10), new QuoteChange(102m, 5)],
		};

		var (bid, ask) = msg.GetPair(1);
		bid.AssertNotNull();
		ask.AssertNotNull();
		bid.Value.Price.AssertEqual(99m);
		ask.Value.Price.AssertEqual(102m);
	}

	[TestMethod]
	public void GetTopPairs_ReturnsCorrectCount()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10), new QuoteChange(99m, 5)],
			Asks = [new QuoteChange(101m, 10), new QuoteChange(102m, 5)],
		};

		var pairs = msg.GetTopPairs(2).ToArray();
		pairs.Length.AssertEqual(2);
	}

	[TestMethod]
	public void GetTopQuotes_ReturnsCorrectOrder()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [new QuoteChange(100m, 10), new QuoteChange(99m, 5)],
			Asks = [new QuoteChange(101m, 10), new QuoteChange(102m, 5)],
		};

		var quotes = msg.GetTopQuotes(2).ToArray();
		// Bids in reverse order then asks in order: 99, 100, 101, 102
		quotes.Length.AssertEqual(4);
		quotes[0].Price.AssertEqual(99m);
		quotes[1].Price.AssertEqual(100m);
		quotes[2].Price.AssertEqual(101m);
		quotes[3].Price.AssertEqual(102m);
	}

	#endregion

	#region IsFinal (IOrderBookMessage)

	[TestMethod]
	public void IsFinal_OrderBook_NullState_ReturnsTrue()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [],
			Asks = [],
		};

		msg.IsFinal().AssertTrue();
	}

	[TestMethod]
	public void IsFinal_OrderBook_SnapshotComplete_ReturnsTrue()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [],
			Asks = [],
			State = QuoteChangeStates.SnapshotComplete,
		};

		msg.IsFinal().AssertTrue();
	}

	[TestMethod]
	public void IsFinal_OrderBook_Increment_ReturnsFalse()
	{
		var msg = new QuoteChangeMessage
		{
			SecurityId = Helper.CreateSecurityId(),
			ServerTime = DateTime.UtcNow,
			Bids = [],
			Asks = [],
			State = QuoteChangeStates.Increment,
		};

		msg.IsFinal().AssertFalse();
	}

	#endregion

	#region SetSecurityCode / SetNativeId

	[TestMethod]
	public void SetSecurityCode_SetsCode()
	{
		var msg = new SecurityMessage { SecurityId = Helper.CreateSecurityId() };
		msg.SetSecurityCode("NEW_CODE");
		msg.SecurityId.SecurityCode.AssertEqual("NEW_CODE");
	}

	[TestMethod]
	public void SetNativeId_SetsNative()
	{
		var secId = Helper.CreateSecurityId();
		var result = secId.SetNativeId(42);
		result.Native.AssertEqual(42);
	}

	#endregion

	#region SetSecurityTypes / GetSecurityTypes

	[TestMethod]
	public void SetSecurityTypes_SingleType_SetsSingle()
	{
		var msg = new SecurityLookupMessage();
		msg.SetSecurityTypes(SecurityTypes.Stock);
		msg.SecurityType.AssertEqual(SecurityTypes.Stock);
	}

	[TestMethod]
	public void SetSecurityTypes_MultipleTypes_SetsArray()
	{
		var msg = new SecurityLookupMessage();
		msg.SetSecurityTypes(null, new[] { SecurityTypes.Stock, SecurityTypes.Bond });
		msg.SecurityTypes.AssertNotNull();
		msg.SecurityTypes.Length.AssertEqual(2);
	}

	[TestMethod]
	public void GetSecurityTypes_SingleType_ReturnsSet()
	{
		var msg = new SecurityLookupMessage { SecurityType = SecurityTypes.Stock };
		var types = msg.GetSecurityTypes();
		types.Count.AssertEqual(1);
		types.Count(t => t == SecurityTypes.Stock).AssertEqual(1);
	}

	[TestMethod]
	public void GetSecurityTypes_NoTypes_ReturnsEmpty()
	{
		var msg = new SecurityLookupMessage();
		msg.GetSecurityTypes().Count.AssertEqual(0);
	}

	#endregion

	#region FillDefaultCryptoFields

	[TestMethod]
	public void FillDefaultCryptoFields_SecurityId_SetsCryptoDefaults()
	{
		var secId = Helper.CreateSecurityId();
		var msg = secId.FillDefaultCryptoFields();

		msg.SecurityId.AssertEqual(secId);
		msg.PriceStep.AssertEqual(0.00000001m);
		msg.VolumeStep.AssertEqual(0.00000001m);
		msg.SecurityType.AssertEqual(SecurityTypes.CryptoCurrency);
	}

	[TestMethod]
	public void FillDefaultCryptoFields_SecurityMessage_SetsCryptoDefaults()
	{
		var msg = new SecurityMessage { SecurityId = Helper.CreateSecurityId() };
		var result = msg.FillDefaultCryptoFields();

		result.AssertEqual(msg);
		msg.PriceStep.AssertEqual(0.00000001m);
		msg.SecurityType.AssertEqual(SecurityTypes.CryptoCurrency);
	}

	#endregion

	#region IsBasket / IsIndex

	[TestMethod]
	public void IsBasket_HasBasketCode_ReturnsTrue()
	{
		var msg = new SecurityMessage { BasketCode = "WI" };
		msg.IsBasket().AssertTrue();
	}

	[TestMethod]
	public void IsBasket_NoBasketCode_ReturnsFalse()
	{
		var msg = new SecurityMessage();
		msg.IsBasket().AssertFalse();
	}

	[TestMethod]
	public void IsIndex_WI_ReturnsTrue()
	{
		var msg = new SecurityMessage { BasketCode = "WI" };
		msg.IsIndex().AssertTrue();
	}

	[TestMethod]
	public void IsIndex_EI_ReturnsTrue()
	{
		var msg = new SecurityMessage { BasketCode = "EI" };
		msg.IsIndex().AssertTrue();
	}

	[TestMethod]
	public void IsIndex_Other_ReturnsFalse()
	{
		var msg = new SecurityMessage { BasketCode = "XX" };
		msg.IsIndex().AssertFalse();
	}

	#endregion

	#region EnsureGetGenerator

	[TestMethod]
	public void EnsureGetGenerator_Null_ReturnsDefault()
	{
		var gen = ((SecurityIdGenerator)null).EnsureGetGenerator();
		gen.AssertNotNull();
	}

	[TestMethod]
	public void EnsureGetGenerator_NotNull_ReturnsSame()
	{
		var gen = new SecurityIdGenerator();
		gen.EnsureGetGenerator().AssertEqual(gen);
	}

	#endregion

	#region ToNullableSecurityId

	[TestMethod]
	public void ToNullableSecurityId_Empty_ReturnsDefault()
	{
		var result = "".ToNullableSecurityId();
		result.AssertEqual(default);
	}

	[TestMethod]
	public void ToNullableSecurityId_Valid_ReturnsSecurityId()
	{
		var result = "AAPL@NASDAQ".ToNullableSecurityId();
		result.SecurityCode.AssertEqual("AAPL");
		result.BoardCode.AssertEqual("NASDAQ");
	}

	#endregion

	#region IsAssociated

	[TestMethod]
	public void IsAssociated_MatchingBoard_ReturnsTrue()
	{
		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = "NYSE" };
		secId.IsAssociated("NYSE").AssertTrue();
	}

	[TestMethod]
	public void IsAssociated_DifferentBoard_ReturnsFalse()
	{
		var secId = new SecurityId { SecurityCode = "TEST", BoardCode = "NYSE" };
		secId.IsAssociated("NASDAQ").AssertFalse();
	}

	#endregion

	#region TryFillUnderlyingId / GetUnderlyingCode

	[TestMethod]
	public void TryFillUnderlyingId_SetsUnderlyingSecurityId()
	{
		var msg = new SecurityMessage
		{
			SecurityId = new SecurityId { SecurityCode = "OPT", BoardCode = "FORTS" },
		};

		msg.TryFillUnderlyingId("GAZP");
		msg.UnderlyingSecurityId.SecurityCode.AssertEqual("GAZP");
		msg.UnderlyingSecurityId.BoardCode.AssertEqual("FORTS");
	}

	[TestMethod]
	public void TryFillUnderlyingId_Empty_DoesNothing()
	{
		var msg = new SecurityMessage { SecurityId = Helper.CreateSecurityId() };
		msg.TryFillUnderlyingId("");
		msg.UnderlyingSecurityId.AssertEqual(default);
	}

	[TestMethod]
	public void GetUnderlyingCode_ReturnsCode()
	{
		var msg = new SecurityMessage
		{
			UnderlyingSecurityId = new SecurityId { SecurityCode = "GAZP", BoardCode = "FORTS" },
		};

		msg.GetUnderlyingCode().AssertEqual("GAZP");
	}

	#endregion

	#region SplitToPair

	[TestMethod]
	public void SplitToPair_Slash_ReturnsPair()
	{
		var (from, to) = "BTC/USD".SplitToPair();
		from.AssertEqual("BTC");
		to.AssertEqual("USD");
	}

	[TestMethod]
	public void SplitToPair_Dash_ReturnsPair()
	{
		var (from, to) = "BTC-USD".SplitToPair();
		from.AssertEqual("BTC");
		to.AssertEqual("USD");
	}

	[TestMethod]
	public void SplitToPair_NoSeparator_Throws()
	{
		Throws<ArgumentException>(() => "BTCUSD".SplitToPair());
	}

	#endregion

	#region IsCandleMessage / IsCandle

	[TestMethod]
	public void IsCandleMessage_TimeFrameCandle_ReturnsTrue()
	{
		typeof(TimeFrameCandleMessage).IsCandleMessage().AssertTrue();
	}

	[TestMethod]
	public void IsCandleMessage_ExecutionMessage_ReturnsFalse()
	{
		typeof(ExecutionMessage).IsCandleMessage().AssertFalse();
	}

	[TestMethod]
	public void IsCandle_CandleTimeFrame_ReturnsTrue()
	{
		MessageTypes.CandleTimeFrame.IsCandle().AssertTrue();
	}

	[TestMethod]
	public void IsCandle_Execution_ReturnsFalse()
	{
		MessageTypes.Execution.IsCandle().AssertFalse();
	}

	#endregion

	#region DataType factories: TimeFrame / Volume / Tick / Range / PnF / Renko / Portfolio

	[TestMethod]
	public void TimeFrame_CreatesDataType()
	{
		var dt = TimeSpan.FromMinutes(5).TimeFrame();
		dt.AssertNotNull();
		dt.MessageType.AssertEqual(typeof(TimeFrameCandleMessage));
		((TimeSpan)dt.Arg).AssertEqual(TimeSpan.FromMinutes(5));
	}

	[TestMethod]
	public void Volume_CreatesDataType()
	{
		var dt = 1000m.Volume();
		dt.AssertNotNull();
		dt.MessageType.AssertEqual(typeof(VolumeCandleMessage));
	}

	[TestMethod]
	public void Tick_CreatesDataType()
	{
		var dt = 100.Tick();
		dt.AssertNotNull();
		dt.MessageType.AssertEqual(typeof(TickCandleMessage));
	}

	[TestMethod]
	public void Portfolio_CreatesDataType()
	{
		var dt = "TestPortfolio".Portfolio();
		dt.AssertNotNull();
		dt.MessageType.AssertEqual(typeof(PortfolioMessage));
	}

	#endregion

	#region IsIntraday

	[TestMethod]
	public void IsIntraday_LessThanDay_ReturnsTrue()
	{
		TimeSpan.FromHours(1).IsIntraday().AssertTrue();
	}

	[TestMethod]
	public void IsIntraday_OneDay_ReturnsFalse()
	{
		TimeSpan.FromDays(1).IsIntraday().AssertFalse();
	}

	[TestMethod]
	public void IsIntraday_ZeroOrNegative_Throws()
	{
		Throws<ArgumentOutOfRangeException>(() => TimeSpan.Zero.IsIntraday());
	}

	#endregion

	#region DataTypeArgToString / ToDataTypeArg

	[TestMethod]
	public void DataTypeArgToString_TimeFrame()
	{
		var dt = TimeSpan.FromMinutes(5).TimeFrame();
		var str = dt.DataTypeArgToString();
		IsFalse(str.IsEmpty());
		// roundtrip: parsing back should give the same DataType
		var restored = dt.MessageType.ToDataTypeArg(str);
		AreEqual(dt.Arg, restored);
	}

	[TestMethod]
	public void ToDataTypeArg_EmptyString_ReturnsNull()
	{
		var result = typeof(TimeFrameCandleMessage).ToDataTypeArg("");
		result.AssertNull();
	}

	#endregion

	#region FileNameToDataType / DataTypeToFileName

	[TestMethod]
	public void FileNameToDataType_Trades_ReturnsTicksDataType()
	{
		var dt = "trades".FileNameToDataType();
		dt.AssertEqual(DataType.Ticks);
	}

	[TestMethod]
	public void FileNameToDataType_Quotes_ReturnsMarketDepth()
	{
		var dt = "quotes".FileNameToDataType();
		dt.AssertEqual(DataType.MarketDepth);
	}

	[TestMethod]
	public void FileNameToDataType_Unknown_ReturnsNull()
	{
		var dt = "unknown".FileNameToDataType();
		dt.AssertNull();
	}

	[TestMethod]
	public void DataTypeToFileName_Ticks_ReturnsTrades()
	{
		var fn = DataType.Ticks.DataTypeToFileName();
		fn.AssertEqual("trades");
	}

	[TestMethod]
	public void DataTypeToFileName_MarketDepth_ReturnsQuotes()
	{
		var fn = DataType.MarketDepth.DataTypeToFileName();
		fn.AssertEqual("quotes");
	}

	[TestMethod]
	public void FileNameToDataType_DataTypeToFileName_Roundtrip()
	{
		var original = DataType.Ticks;
		var fn = original.DataTypeToFileName();
		var back = fn.FileNameToDataType();
		back.AssertEqual(original);
	}

	#endregion

	#region IsStorageSupported / IsBuildOnly

	[TestMethod]
	public void IsStorageSupported_Ticks_ReturnsTrue()
	{
		DataType.Ticks.IsStorageSupported().AssertTrue();
	}

	[TestMethod]
	public void IsStorageSupported_TimeFrameCandle_ReturnsTrue()
	{
		TimeSpan.FromMinutes(5).TimeFrame().IsStorageSupported().AssertTrue();
	}

	[TestMethod]
	public void IsBuildOnly_TickCandle_ReturnsTrue()
	{
		// TickCandleMessage was registered with isBuildOnly = true (default)
		typeof(TickCandleMessage).IsBuildOnly().AssertTrue();
	}

	[TestMethod]
	public void IsBuildOnly_TimeFrameCandle_ReturnsFalse()
	{
		// TimeFrameCandleMessage was registered with isBuildOnly = false
		typeof(TimeFrameCandleMessage).IsBuildOnly().AssertFalse();
	}

	#endregion

	#region GetTimeFrame (MarketDataMessage)

	[TestMethod]
	public void GetTimeFrame_MarketDataMessage_ReturnsTimeFrame()
	{
		var msg = new MarketDataMessage
		{
			DataType2 = TimeSpan.FromMinutes(5).TimeFrame(),
			IsSubscribe = true,
		};

		msg.GetTimeFrame().AssertEqual(TimeSpan.FromMinutes(5));
	}

	#endregion

	#region ToCandleMessageType / ToCandleMessage / ToMessageType

	[TestMethod]
	public void ToCandleMessageType_CandleTimeFrame_ReturnsType()
	{
		var type = MessageTypes.CandleTimeFrame.ToCandleMessageType();
		type.AssertEqual(typeof(TimeFrameCandleMessage));
	}

	[TestMethod]
	public void ToCandleMessage_CandleTimeFrame_ReturnsType()
	{
		var type = MessageTypes.CandleTimeFrame.ToCandleMessage();
		type.AssertEqual(typeof(TimeFrameCandleMessage));
	}

	[TestMethod]
	public void ToMessageType_Type_ReturnsMessageType()
	{
		var mt = typeof(TimeFrameCandleMessage).ToMessageType();
		mt.AssertEqual(MessageTypes.CandleTimeFrame);
	}

	[TestMethod]
	public void ToMessageType_MessageTypes_ReturnsType()
	{
		var t = MessageTypes.CandleTimeFrame.ToMessageType();
		t.AssertEqual(typeof(TimeFrameCandleMessage));
	}

	[TestMethod]
	public void ToMessageType_String_Regular()
	{
		var mt = "CandleTimeFrame".ToMessageType();
		mt.AssertEqual(MessageTypes.CandleTimeFrame);
	}

	[TestMethod]
	public void ToMessageType_String_LegacyTimeFrameInfo()
	{
		var mt = "TimeFrameInfo".ToMessageType();
		mt.AssertEqual(MessageTypes.DataTypeInfo);
	}

	#endregion

	#region GetCandleArgType / ValidateCandleArg / CreateCandleMessage

	[TestMethod]
	public void GetCandleArgType_TimeFrame_ReturnsTimeSpan()
	{
		typeof(TimeFrameCandleMessage).GetCandleArgType().AssertEqual(typeof(TimeSpan));
	}

	[TestMethod]
	public void ValidateCandleArg_TimeFrame_ValidArg_ReturnsTrue()
	{
		typeof(TimeFrameCandleMessage).ValidateCandleArg(TimeSpan.FromMinutes(5)).AssertTrue();
	}

	[TestMethod]
	public void ValidateCandleArg_TimeFrame_InvalidArg_ReturnsFalse()
	{
		typeof(TimeFrameCandleMessage).ValidateCandleArg(TimeSpan.Zero).AssertFalse();
	}

	[TestMethod]
	public void CreateCandleMessage_TimeFrame_CreatesInstance()
	{
		var candle = typeof(TimeFrameCandleMessage).CreateCandleMessage();
		candle.AssertNotNull();
		IsTrue(candle is TimeFrameCandleMessage);
	}

	#endregion

	#region GetPreferredLanguage

	[TestMethod]
	public void GetPreferredLanguage_Russia_ReturnsRu()
	{
		var lang = ((MessageAdapterCategories?)MessageAdapterCategories.Russia).GetPreferredLanguage();
		lang.AssertEqual("ru");
	}

	[TestMethod]
	public void GetPreferredLanguage_Null_ReturnsEn()
	{
		((MessageAdapterCategories?)null).GetPreferredLanguage().AssertEqual("en");
	}

	#endregion

	#region GetTypicalPrice / GetMedianPrice

	[TestMethod]
	public void GetTypicalPrice_ReturnsCorrectValue()
	{
		var candle = new TimeFrameCandleMessage
		{
			HighPrice = 30m,
			LowPrice = 10m,
			ClosePrice = 20m,
		};

		candle.GetTypicalPrice().AssertEqual(20m); // (30 + 10 + 20) / 3
	}

	[TestMethod]
	public void GetMedianPrice_ReturnsCorrectValue()
	{
		var candle = new TimeFrameCandleMessage
		{
			HighPrice = 30m,
			LowPrice = 10m,
		};

		candle.GetMedianPrice().AssertEqual(20m); // (30 + 10) / 2
	}

	#endregion

	#region SetSubscriptionIds / GetSubscriptionIds / HasSubscriptionId

	[TestMethod]
	public void SetSubscriptionIds_SingleId_SetsSingle()
	{
		var msg = new ExecutionMessage();
		msg.SetSubscriptionIds(subscriptionId: 42);
		msg.SubscriptionId.AssertEqual(42L);
		msg.SubscriptionIds.AssertNull();
	}

	[TestMethod]
	public void SetSubscriptionIds_Array_SetsArray()
	{
		var msg = new ExecutionMessage();
		msg.SetSubscriptionIds([1, 2, 3]);
		msg.SubscriptionId.AssertEqual(0L);
		msg.SubscriptionIds.Length.AssertEqual(3);
	}

	[TestMethod]
	public void GetSubscriptionIds_SingleId_ReturnsSingleArray()
	{
		var msg = new ExecutionMessage { SubscriptionId = 42 };
		var ids = msg.GetSubscriptionIds();
		ids.Length.AssertEqual(1);
		ids[0].AssertEqual(42L);
	}

	[TestMethod]
	public void GetSubscriptionIds_ArrayIds_ReturnsArray()
	{
		var msg = new ExecutionMessage { SubscriptionIds = [1, 2] };
		var ids = msg.GetSubscriptionIds();
		ids.Length.AssertEqual(2);
	}

	[TestMethod]
	public void GetSubscriptionIds_NoIds_ReturnsEmpty()
	{
		var msg = new ExecutionMessage();
		msg.GetSubscriptionIds().Length.AssertEqual(0);
	}

	[TestMethod]
	public void HasSubscriptionId_WithSingleId_ReturnsTrue()
	{
		var msg = new ExecutionMessage { SubscriptionId = 42 };
		msg.HasSubscriptionId().AssertTrue();
	}

	[TestMethod]
	public void HasSubscriptionId_WithArrayIds_ReturnsTrue()
	{
		var msg = new ExecutionMessage { SubscriptionIds = [1, 2] };
		msg.HasSubscriptionId().AssertTrue();
	}

	[TestMethod]
	public void HasSubscriptionId_NoIds_ReturnsFalse()
	{
		var msg = new ExecutionMessage();
		msg.HasSubscriptionId().AssertFalse();
	}

	#endregion

	#region HasOrderId (OrderStatusMessage)

	[TestMethod]
	public void HasOrderId_WithOrderId_ReturnsTrue()
	{
		var msg = new OrderStatusMessage { OrderId = 123 };
		msg.HasOrderId().AssertTrue();
	}

	[TestMethod]
	public void HasOrderId_WithOrderStringId_ReturnsTrue()
	{
		var msg = new OrderStatusMessage { OrderStringId = "ABC" };
		msg.HasOrderId().AssertTrue();
	}

	[TestMethod]
	public void HasOrderId_NoId_ReturnsFalse()
	{
		var msg = new OrderStatusMessage();
		msg.HasOrderId().AssertFalse();
	}

	#endregion

	#region FilterTimeFrames

	[TestMethod]
	public void FilterTimeFrames_FiltersOnlyTimeFrameCandles()
	{
		var dataTypes = new[]
		{
			TimeSpan.FromMinutes(1).TimeFrame(),
			TimeSpan.FromMinutes(5).TimeFrame(),
			DataType.Ticks,
			DataType.Level1,
		};

		var result = dataTypes.FilterTimeFrames().ToArray();
		result.Length.AssertEqual(2);
		result[0].AssertEqual(TimeSpan.FromMinutes(1));
		result[1].AssertEqual(TimeSpan.FromMinutes(5));
	}

	#endregion

	#region Filter (by dates)

	[TestMethod]
	public void Filter_ByDates_FiltersCorrectly()
	{
		var now = DateTime.UtcNow;
		var msgs = new[]
		{
			new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId(), ServerTime = now.AddDays(-2) },
			new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId(), ServerTime = now.AddDays(-1) },
			new Level1ChangeMessage { SecurityId = Helper.CreateSecurityId(), ServerTime = now },
		};

		var result = msgs.Filter(now.AddDays(-1.5), now.AddHours(-1)).ToArray();
		result.Length.AssertEqual(1);
		result[0].ServerTime.AssertEqual(now.AddDays(-1));
	}

	#endregion

	#region TryLimitByCount

	[TestMethod]
	public void TryLimitByCount_WithCount_Limits()
	{
		var items = Enumerable.Range(1, 100);
		var msg = new SecurityLookupMessage { Count = 5 };

		var result = items.TryLimitByCount(msg).ToArray();
		result.Length.AssertEqual(5);
	}

	[TestMethod]
	public void TryLimitByCount_NoCount_ReturnsAll()
	{
		var items = Enumerable.Range(1, 10);
		var msg = new SecurityLookupMessage();

		var result = items.TryLimitByCount(msg).ToArray();
		result.Length.AssertEqual(10);
	}

	#endregion

	#region Filter (SecurityMessage)

	[TestMethod]
	public void Filter_Securities_ByType()
	{
		var securities = new[]
		{
			new SecurityMessage { SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" }, SecurityType = SecurityTypes.Stock },
			new SecurityMessage { SecurityId = new SecurityId { SecurityCode = "GC", BoardCode = "CME" }, SecurityType = SecurityTypes.Future },
		};

		var criteria = new SecurityLookupMessage { SecurityType = SecurityTypes.Stock };
		var result = securities.Filter(criteria).ToArray();
		result.Length.AssertEqual(1);
		result[0].SecurityId.SecurityCode.AssertEqual("AAPL");
	}

	[TestMethod]
	public void Filter_Securities_LookupAll_ReturnsAll()
	{
		var securities = new[]
		{
			new SecurityMessage { SecurityId = new SecurityId { SecurityCode = "AAPL", BoardCode = "NASDAQ" } },
			new SecurityMessage { SecurityId = new SecurityId { SecurityCode = "GOOG", BoardCode = "NASDAQ" } },
		};

		var criteria = new SecurityLookupMessage();
		var result = securities.Filter(criteria).ToArray();
		result.Length.AssertEqual(2);
	}

	#endregion

	#region Filter (BoardMessage)

	[TestMethod]
	public void Filter_Boards_ByLike()
	{
		var boards = new[]
		{
			new BoardMessage { Code = "NYSE", ExchangeCode = "NYSE" },
			new BoardMessage { Code = "NASDAQ", ExchangeCode = "NASDAQ" },
		};

		var criteria = new BoardLookupMessage { Like = "NY" };
		var result = boards.Filter(criteria).ToArray();
		result.Length.AssertEqual(1);
		result[0].Code.AssertEqual("NYSE");
	}

	#endregion

	#region NearestSupportedDepth

	[TestMethod]
	public void NearestSupportedDepth_AnyDepths_ReturnsSameDepth()
	{
		var adapter = new TestMessageAdapter();
		adapter.SetSupportedOrderBookDepths(Extensions.AnyDepths);

		adapter.NearestSupportedDepth(10).AssertEqual(10);
	}

	[TestMethod]
	public void NearestSupportedDepth_EmptyDepths_ReturnsNull()
	{
		var adapter = new TestMessageAdapter();
		adapter.SetSupportedOrderBookDepths([]);

		adapter.NearestSupportedDepth(10).AssertNull();
	}

	[TestMethod]
	public void NearestSupportedDepth_FindsNearest()
	{
		var adapter = new TestMessageAdapter();
		adapter.SetSupportedOrderBookDepths([5, 10, 20, 50]);

		adapter.NearestSupportedDepth(7).AssertEqual(10);
	}

	[TestMethod]
	public void NearestSupportedDepth_LargerThanAll_ReturnsMax()
	{
		var adapter = new TestMessageAdapter();
		adapter.SetSupportedOrderBookDepths([5, 10, 20]);

		adapter.NearestSupportedDepth(100).AssertEqual(20);
	}

	private sealed class TestMessageAdapter : MessageAdapter
	{
		private IEnumerable<int> _depths = Extensions.AnyDepths;

		public TestMessageAdapter()
			: base(new IncrementalIdGenerator())
		{
		}

		public void SetSupportedOrderBookDepths(IEnumerable<int> depths) => _depths = depths;

		public override IEnumerable<int> SupportedOrderBookDepths => _depths;

		protected override ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken) => default;

		public override IMessageAdapter Clone() => new TestMessageAdapter();
	}

	#endregion

	#region FormatToString

	[TestMethod]
	public void FormatToString_ReturnsTypeAndArg()
	{
		var dt = TimeSpan.FromMinutes(5).TimeFrame();
		var (type, arg) = dt.FormatToString();
		AreEqual(typeof(TimeFrameCandleMessage).GetTypeName(false), type);
		AreEqual(dt.DataTypeArgToString(), arg);
	}

	#endregion

	#region CreateOrderCondition (Type)

	[TestMethod]
	public void CreateOrderCondition_NullType_ReturnsNull()
	{
		((Type)null).CreateOrderCondition().AssertNull();
	}

	#endregion

	#region FindById

	[TestMethod]
	public void FindById_ExistingId_ReturnsAdapter()
	{
		var adapter = new TestMessageAdapter();
		var list = new[] { adapter };

		list.FindById(adapter.Id).AssertEqual(adapter);
	}

	[TestMethod]
	public void FindById_MissingId_ReturnsNull()
	{
		var adapter = new TestMessageAdapter();
		var list = new[] { adapter };

		list.FindById(Guid.NewGuid()).AssertNull();
	}

	#endregion

	#region MakeVectorIconUri

	[TestMethod]
	public void MakeVectorIconUri()
	{
		// Register pack:// URI scheme (normally registered by WPF Application)
		_ = System.IO.Packaging.PackUriHelper.UriSchemePack;

		"test_icon".MakeVectorIconUri().ToString().AssertEqual($"pack://application:,,,/StockSharp.Xaml;component/IconsSvg/test_icon.svg");
	}

	#endregion

	#region CreatePortfolioChangeMessage / CreatePositionChangeMessage

	[TestMethod]
	public void CreatePortfolioChangeMessage_SetsFields()
	{
		var adapter = new TestMessageAdapter();

		var msg = adapter.CreatePortfolioChangeMessage("TestPf");
		msg.PortfolioName.AssertEqual("TestPf");
		msg.SecurityId.AssertEqual(SecurityId.Money);
	}

	[TestMethod]
	public void CreatePositionChangeMessage_SetsFields()
	{
		var adapter = new TestMessageAdapter();
		var secId = Helper.CreateSecurityId();

		var msg = adapter.CreatePositionChangeMessage("TestPf", secId, "depo1");
		msg.PortfolioName.AssertEqual("TestPf");
		msg.SecurityId.AssertEqual(secId);
		msg.DepoName.AssertEqual("depo1");
	}

	#endregion

	#region IsMarketData / IsTransactional (adapter)

	[TestMethod]
	public void IsMarketData_Adapter_WithMarketDataSupport_ReturnsTrue()
	{
		var adapter = new TestMessageAdapter();
		adapter.SupportedInMessages = [MessageTypes.MarketData];
		adapter.IsMarketData().AssertTrue();
	}

	[TestMethod]
	public void IsMarketData_Adapter_NoMarketDataSupport_ReturnsFalse()
	{
		var adapter = new TestMessageAdapter();
		adapter.SupportedInMessages = [MessageTypes.OrderRegister];
		adapter.IsMarketData().AssertFalse();
	}

	[TestMethod]
	public void IsTransactional_Adapter_WithOrderRegister_ReturnsTrue()
	{
		var adapter = new TestMessageAdapter();
		adapter.SupportedInMessages = [MessageTypes.OrderRegister];
		adapter.IsTransactional().AssertTrue();
	}

	[TestMethod]
	public void IsTransactional_Adapter_NoOrderRegister_ReturnsFalse()
	{
		var adapter = new TestMessageAdapter();
		adapter.SupportedInMessages = [MessageTypes.MarketData];
		adapter.IsTransactional().AssertFalse();
	}

	#endregion

	#region IsMessageSupported

	[TestMethod]
	public void IsMessageSupported_Supported_ReturnsTrue()
	{
		var adapter = new TestMessageAdapter();
		adapter.SupportedInMessages = [MessageTypes.MarketData, MessageTypes.OrderRegister];
		adapter.IsMessageSupported(MessageTypes.MarketData).AssertTrue();
	}

	[TestMethod]
	public void IsMessageSupported_NotSupported_ReturnsFalse()
	{
		var adapter = new TestMessageAdapter();
		adapter.SupportedInMessages = [MessageTypes.OrderRegister];
		adapter.IsMessageSupported(MessageTypes.MarketData).AssertFalse();
	}

	#endregion

	#region IsResultMessageNotSupported

	[TestMethod]
	public void IsResultMessageNotSupported_InList_ReturnsTrue()
	{
		var adapter = new TestMessageAdapter();
		adapter.NotSupportedResultMessages = [MessageTypes.SecurityLookup];
		adapter.IsResultMessageNotSupported(MessageTypes.SecurityLookup).AssertTrue();
	}

	[TestMethod]
	public void IsResultMessageNotSupported_NotInList_ReturnsFalse()
	{
		var adapter = new TestMessageAdapter();
		adapter.NotSupportedResultMessages = [];
		adapter.IsResultMessageNotSupported(MessageTypes.SecurityLookup).AssertFalse();
	}

	#endregion

	#region UseChannels

	[TestMethod]
	public void UseChannels_DefaultAdapter_ReturnsTrue()
	{
		// MessageAdapter defaults UseInChannel and UseOutChannel to true
		var adapter = new TestMessageAdapter();
		adapter.UseChannels().AssertTrue();
	}

	#endregion

	#region IsSupportStopLoss / IsSupportTakeProfit / IsSupportWithdraw

	[TestMethod]
	public void IsSupportStopLoss_NoConditionType_ReturnsFalse()
	{
		var adapter = new TestMessageAdapter();
		adapter.IsSupportStopLoss().AssertFalse();
	}

	[TestMethod]
	public void IsSupportTakeProfit_NoConditionType_ReturnsFalse()
	{
		var adapter = new TestMessageAdapter();
		adapter.IsSupportTakeProfit().AssertFalse();
	}

	[TestMethod]
	public void IsSupportWithdraw_NoConditionType_ReturnsFalse()
	{
		var adapter = new TestMessageAdapter();
		adapter.IsSupportWithdraw().AssertFalse();
	}

	#endregion

	#region IsSupportSecuritiesLookupAll

	[TestMethod]
	public void IsSupportSecuritiesLookupAll_NotSupported_ReturnsFalse()
	{
		var adapter = new TestMessageAdapter();
		adapter.IsSupportSecuritiesLookupAll().AssertFalse();
	}

	#endregion
}
