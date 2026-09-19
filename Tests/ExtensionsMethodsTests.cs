namespace StockSharp.Tests;

using StockSharp.Fix;
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
		var result = ((decimal?)10m).ApplyNewBalance(-1m, 1, logger);

		result.AssertEqual(-1m);
		logger.Logs.Count.AssertEqual(1);
		logger.Logs[0].Level.AssertEqual(LogLevels.Error);
		logger.Logs[0].Message.Contains("-1").AssertTrue();
	}

	[TestMethod]
	public void ApplyNewBalance_IncreasingBalance_LogsError()
	{
		var logger = new TestReceiver();
		var result = ((decimal?)5m).ApplyNewBalance(10m, 1, logger);

		result.AssertEqual(10m);
		logger.Logs.Count.AssertEqual(1);
		logger.Logs[0].Level.AssertEqual(LogLevels.Error);
		logger.Logs[0].Message.Contains("5").AssertTrue();
		logger.Logs[0].Message.Contains("10").AssertTrue();
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

	#region MicexCurrencyName

	[TestMethod]
	public void ToMicexCurrencyName_Rub_ReturnsSur()
	{
		CurrencyTypes.RUB.ToMicexCurrencyName().AssertEqual("SUR");
	}

	[TestMethod]
	public void ToMicexCurrencyName_Other_ReturnsIsoCode()
	{
		CurrencyTypes.USD.ToMicexCurrencyName().AssertEqual("USD");
	}

	[TestMethod]
	public void FromMicexCurrencyName_Sur_ReturnsRub()
	{
		"SUR".FromMicexCurrencyName().currency.AssertEqual(CurrencyTypes.RUB);
	}

	[TestMethod]
	public void FromMicexCurrencyName_Rur_ReturnsRub()
	{
		"RUR".FromMicexCurrencyName().currency.AssertEqual(CurrencyTypes.RUB);
	}

	[TestMethod]
	public void FromMicexCurrencyName_IsoCode_ReturnsSameCurrency()
	{
		"USD".FromMicexCurrencyName().currency.AssertEqual(CurrencyTypes.USD);
	}

	[TestMethod]
	public void FromMicexCurrencyName_Empty_ReturnsNothing()
	{
		var (currency, error) = string.Empty.FromMicexCurrencyName();

		currency.AssertNull();
		error.AssertNull();
	}

	/// <summary>
	/// The pair carries the rouble under a name of its own, so what one writes the other has to read back.
	/// </summary>
	[TestMethod]
	public void MicexCurrencyName_RoundTrips()
	{
		foreach (var currency in new[] { CurrencyTypes.RUB, CurrencyTypes.USD, CurrencyTypes.EUR })
			currency.ToMicexCurrencyName().FromMicexCurrencyName().currency.AssertEqual(currency);
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
	public void CreateReply_NullErrorThrows()
	{
		// The method builds a rejection, and a rejection without a reason is not one. A reply that
		// carries no error is CreateOrderReply.
		var reg = new OrderRegisterMessage
		{
			TransactionId = 42,
			SecurityId = Helper.CreateSecurityId(),
			Side = Sides.Buy,
			Price = 100m,
			Volume = 10,
			PortfolioName = "pf",
		};

		Throws<ArgumentNullException>(() => reg.CreateReply(null));
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
	public void CreateReply_OrderStatusThrows()
	{
		// OrderStatusMessage derives from OrderCancelMessage, so it reaches this method as a cancel
		// would. It asks for a snapshot and names no order: its refusal is a subscription response,
		// and building an execution for it hands the receiver an order nobody placed.
		var status = new OrderStatusMessage
		{
			TransactionId = 44,
			SecurityId = Helper.CreateSecurityId(),
			PortfolioName = "pf",
		};

		Throws<ArgumentException>(() => status.CreateReply(new InvalidOperationException("test")));
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
		using var adapter = new RecordingMessageAdapter();

		var result = msg.LoopBack(adapter);

		result.AssertSame(msg);
		msg.BackMode.AssertEqual(MessageBackModes.Direct);
		msg.Adapter.AssertSame(adapter);
		msg.IsBack().AssertTrue();
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

	#region GetAnsweredSubscriptionIds

	[TestMethod]
	public void GetAnsweredSubscriptionIds_PrefersSubscriptionIds()
	{
		var msg = new TimeFrameCandleMessage { OriginalTransactionId = 11, SubscriptionId = 22, SubscriptionIds = [77] };

		msg.GetAnsweredSubscriptionIds().AssertEqual([77L]);
	}

	[TestMethod]
	public void GetAnsweredSubscriptionIds_ReturnsEveryTaggedSubscription()
	{
		var msg = new TimeFrameCandleMessage { SubscriptionIds = [1L, 2L, 3L] };

		msg.GetAnsweredSubscriptionIds().AssertEqual([1L, 2L, 3L]);
	}

	[TestMethod]
	public void GetAnsweredSubscriptionIds_FallsBackToSubscriptionId()
	{
		var msg = new TimeFrameCandleMessage { OriginalTransactionId = 11, SubscriptionId = 22 };

		msg.GetAnsweredSubscriptionIds().AssertEqual([22L]);
	}

	[TestMethod]
	public void GetAnsweredSubscriptionIds_FallsBackToOriginalTransactionId()
	{
		var msg = new TimeFrameCandleMessage { OriginalTransactionId = 11 };

		msg.GetAnsweredSubscriptionIds().AssertEqual([11L]);
	}

	[TestMethod]
	public void GetAnsweredSubscriptionIds_NotSubscriptionIdMessage_UsesOriginalTransactionId()
	{
		var msg = new SubscriptionOnlineMessage { OriginalTransactionId = 5 };

		msg.GetAnsweredSubscriptionIds().AssertEqual([5L]);
	}

	[TestMethod]
	public void GetAnsweredSubscriptionIds_NoIds_ReturnsEmpty()
	{
		new TimeFrameCandleMessage().GetAnsweredSubscriptionIds().Length.AssertEqual(0);
	}

	[TestMethod]
	public void GetAnsweredSubscriptionIds_Null_Throws()
	{
		Throws<ArgumentNullException>(() => ((Message)null).GetAnsweredSubscriptionIds());
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

	#region ReRegisterClone

	// An order carrying a distinct, non-default value in EVERY writable property: the ones that
	// describe HOW it is to be executed, the ones only the exchange fills in once it lives there, and
	// the bookkeeping the caller attaches to it. Sweeps compare it against a blank order first, so a
	// property added to Order and not given a value here is reported rather than silently skipped.
	private static Order CreateLivingOrder() => new()
	{
		Security = Helper.CreateSecurity(),
		Portfolio = Helper.CreatePortfolio(),
		Side = Sides.Sell,
		Type = OrderTypes.Limit,
		Price = 101.5m,
		Volume = 20m,
		TimeInForce = TimeInForce.MatchOrCancel,
		ExpiryDate = new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc),
		VisibleVolume = 5m,
		MinVolume = 2m,
		PostOnly = true,
		PositionEffect = OrderPositionEffects.CloseOnly,
		MarginMode = MarginModes.Isolated,
		Leverage = 10,
		BrokerCode = "BRK",
		ClientCode = "CLI",
		StrategyId = "STRAT",
		IsManual = true,
		IsMarketMaker = true,
		Condition = new FixOrderCondition { StopLoss = 95m, TakeProfit = 110m },

		// What the exchange, not the caller, put on the order.
		Id = 777,
		StringId = "EXCH-777",
		BoardId = "BRD-777",
		TransactionId = 42,
		State = OrderStates.Active,
		Balance = 12m,
		Status = 5,
		IsSystem = false,
		Time = new DateTime(2025, 6, 15, 10, 30, 2, DateTimeKind.Utc),
		ServerTime = new DateTime(2025, 6, 15, 10, 31, 0, DateTimeKind.Utc),
		LocalTime = new DateTime(2025, 6, 15, 10, 31, 1, DateTimeKind.Utc),
		MatchedTime = new DateTime(2025, 6, 15, 10, 32, 0, DateTimeKind.Utc),
		CancelledTime = new DateTime(2025, 6, 15, 10, 33, 0, DateTimeKind.Utc),
		AveragePrice = 101.4m,
		MarketPrice = 101.6m,
		Slippage = 0.3m,
		Yield = 4.5m,
		Commission = 0.7m,
		CommissionCurrency = "USD",
		Currency = CurrencyTypes.USD,
		LatencyRegistration = TimeSpan.FromMilliseconds(15),
		LatencyCancellation = TimeSpan.FromMilliseconds(16),
		LatencyEdition = TimeSpan.FromMilliseconds(17),
		SeqNum = 9,

		// What the caller attached to it for its own bookkeeping.
		Comment = "the reason this order exists",
		UserOrderId = "USER-777",
	};

	[TestMethod]
	public void ReRegisterClone_CarriesEveryExecutionInstruction()
	{
		// The replacement is sent to the exchange in place of the original, so every constraint the
		// original was accepted under has to travel with it. A dropped one is not a lost annotation:
		// a post-only order that comes back as an ordinary limit crosses the spread and pays the
		// taker fee, and a close-only order that loses PositionEffect opens a fresh position.
		var old = CreateLivingOrder();
		var clone = old.ReRegisterClone();

		clone.Security.AssertSame(old.Security);
		clone.Portfolio.AssertSame(old.Portfolio);
		clone.Side.AssertEqual(old.Side);
		clone.Type.AssertEqual(old.Type);
		clone.Price.AssertEqual(old.Price);
		clone.Volume.AssertEqual(old.Volume);
		clone.TimeInForce.AssertEqual(old.TimeInForce);
		clone.ExpiryDate.AssertEqual(old.ExpiryDate);
		clone.VisibleVolume.AssertEqual(old.VisibleVolume);
		clone.MinVolume.AssertEqual(old.MinVolume);
		clone.PostOnly.AssertEqual(old.PostOnly);
		clone.PositionEffect.AssertEqual(old.PositionEffect);
		clone.MarginMode.AssertEqual(old.MarginMode);
		clone.Leverage.AssertEqual(old.Leverage);
		clone.BrokerCode.AssertEqual(old.BrokerCode);
		clone.ClientCode.AssertEqual(old.ClientCode);
		clone.StrategyId.AssertEqual(old.StrategyId);
		clone.IsManual.AssertEqual(old.IsManual);
		clone.IsMarketMaker.AssertEqual(old.IsMarketMaker);
	}

	[TestMethod]
	public void ReRegisterClone_LeavesTheExchangeLifeOfTheOldOrderBehind()
	{
		// The clone has not been sent anywhere yet: it has no identity at the exchange and no
		// execution history. Carrying either over would make the new order look already registered
		// and partly filled - Balance 12 of Volume 20 says 8 lots are done that never traded.
		var clone = CreateLivingOrder().ReRegisterClone();

		clone.Id.AssertNull();
		clone.StringId.AssertNull();
		clone.BoardId.AssertNull();
		clone.TransactionId.AssertEqual(0L);
		clone.State.AssertEqual(OrderStates.None);
		clone.Balance.AssertEqual(0m);
		clone.Status.AssertNull();
		clone.ServerTime.AssertEqual(default(DateTime));
		clone.LocalTime.AssertEqual(default(DateTime));
		clone.MatchedTime.AssertNull();
		clone.CancelledTime.AssertNull();
		clone.AveragePrice.AssertNull();
		clone.Commission.AssertNull();
		clone.LatencyRegistration.AssertNull();
		clone.SeqNum.AssertEqual(0L);
	}

	[TestMethod]
	public void ReRegisterClone_GivesTheNewOrderItsOwnCondition()
	{
		// A condition is mutable and the caller edits the clone's before sending it - that is the
		// point of taking a copy. Sharing one instance would rewrite the live order's stop level
		// while it still sits at the exchange under the old one.
		var old = CreateLivingOrder();
		var clone = old.ReRegisterClone();

		var cloned = (FixOrderCondition)clone.Condition;
		cloned.AssertNotSame(old.Condition);
		cloned.StopLoss.AssertEqual(95m);
		cloned.TakeProfit.AssertEqual(110m);

		cloned.StopLoss = 90m;
		((FixOrderCondition)old.Condition).StopLoss.AssertEqual(95m);
	}

	[TestMethod]
	public void ReRegisterClone_ReplacesOnlyThePriceAndVolumeAsked()
	{
		// The two arguments are the whole reason to re-register; an omitted one keeps the original
		// value rather than resetting the order to nothing.
		var old = CreateLivingOrder();

		var both = old.ReRegisterClone(102.5m, 30m);
		both.Price.AssertEqual(102.5m);
		both.Volume.AssertEqual(30m);

		var priceOnly = old.ReRegisterClone(newPrice: 102.5m);
		priceOnly.Price.AssertEqual(102.5m);
		priceOnly.Volume.AssertEqual(20m);

		var volumeOnly = old.ReRegisterClone(newVolume: 30m);
		volumeOnly.Price.AssertEqual(101.5m);
		volumeOnly.Volume.AssertEqual(30m);
	}

	#endregion

	#region ToOrder

	// One execution report about one order, as the exchange sends them: an acknowledgement, then a
	// fill, then the close. Reports after the first identify the order by id and need not repeat the
	// account, so portfolioName is left free for the caller to omit.
	private static ExecutionMessage CreateOrderReport(Security security, string portfolioName, OrderStates state, decimal balance) => new()
	{
		DataTypeEx = DataType.Transactions,
		HasOrderInfo = true,
		SecurityId = security.ToSecurityId(),
		OrderId = 777,
		TransactionId = 42,
		PortfolioName = portfolioName,
		Side = Sides.Buy,
		OrderPrice = 100m,
		OrderVolume = 10m,
		Balance = balance,
		OrderState = state,
		ServerTime = new DateTime(2025, 6, 15, 10, 31, 0, DateTimeKind.Utc),
	};

	[TestMethod]
	public void ToOrder_KeepsThePortfolioEntityTheOrderWasGiven()
	{
		// The order handed in already carries the Portfolio it was registered against, and that
		// object is the account itself - balances live on it and every consumer holds it by
		// reference. A report about that same order must fill the order in, not swap its account
		// for a fresh stand-in that knows no balance and nobody else is holding.
		var security = Helper.CreateSecurity();
		var portfolio = Helper.CreatePortfolio();
		var order = new Order { Security = security, Portfolio = portfolio };

		CreateOrderReport(security, portfolio.Name, OrderStates.Active, 10m).ToOrder(order);

		order.Portfolio.AssertSame(portfolio);
	}

	[TestMethod]
	public void ToOrder_GivesTwoReportsOnOneOrderTheSamePortfolio()
	{
		// Whatever portfolio the first report produced is the order's account from then on. A
		// second report naming the same account must find that one, not build a second object for
		// it: two Portfolio instances under one name split every position and P&L total keyed on
		// the account, and neither half is the truth.
		var security = Helper.CreateSecurity();
		var order = new Order { Security = security };

		CreateOrderReport(security, "PF-1", OrderStates.Active, 10m).ToOrder(order);
		var afterAck = order.Portfolio;

		CreateOrderReport(security, "PF-1", OrderStates.Done, 0m).ToOrder(order);

		order.Portfolio.AssertSame(afterAck);
	}

	[TestMethod]
	public void ToOrder_DoesNotDropThePortfolioWhenTheReportNamesNone()
	{
		// A fill or cancel report names the order, not the account. Saying nothing about the
		// portfolio means it did not change - it does not mean the order has no account. An order
		// left holding a nameless portfolio can no longer be cancelled or attributed to anyone.
		var security = Helper.CreateSecurity();
		var portfolio = Helper.CreatePortfolio();
		var order = new Order { Security = security, Portfolio = portfolio };

		CreateOrderReport(security, null, OrderStates.Done, 0m).ToOrder(order);

		order.Portfolio.AssertSame(portfolio);
	}

	[TestMethod]
	public void ToOrder_WithoutSecurityDoesNotLeakNullReference()
	{
		// ToOrder(message, order) is public and states no requirement on the order's Security, yet
		// it dereferences it to stamp a board on the portfolio it builds. Whether the right answer
		// is to refuse the argument or to leave the board unset is the library's to choose; a
		// NullReferenceException is neither, and tells the caller nothing about what was wrong.
		var order = new Order();
		var report = CreateOrderReport(Helper.CreateSecurity(), "PF-1", OrderStates.Active, 10m);

		try
		{
			report.ToOrder(order);
		}
		catch (NullReferenceException)
		{
			Fail($"{nameof(EntitiesExtensions.ToOrder)} threw NullReferenceException for an order with no Security instead of naming the argument at fault.");
		}
		catch (ArgumentException)
		{
			// Refusing the argument is a legitimate answer. The unhandled dereference is not.
		}
	}

	/// <summary>
	/// Done is a terminal state: the exchange has closed the order and nothing reopens it. A report
	/// that says otherwise is either about a different order or corrupt, and an order that quietly
	/// goes back to Active is worse than either - it is shown to the user as live, it is cancelled
	/// and amended as live, and the money behind it is counted twice. The transition table refuses
	/// Done -> Active, so this conversion must either refuse the report or leave the state as it
	/// found it. It cannot do neither: nothing on this path is given a log receiver, so "applied and
	/// warned about" is not among the answers a caller can see.
	/// </summary>
	[TestMethod]
	public void ToOrder_InvalidStateTransition_IsNotAppliedSilently()
	{
		var security = Helper.CreateSecurity();
		var order = new Order
		{
			Security = security,
			Portfolio = Helper.CreatePortfolio(),
			State = OrderStates.Done,
		};

		try
		{
			CreateOrderReport(security, "PF-1", OrderStates.Active, 10m).ToOrder(order);
		}
		catch (InvalidOperationException)
		{
			// Refusing the report outright is a legitimate answer.
			return;
		}

		order.State.AssertEqual(OrderStates.Done, $"{nameof(EntitiesExtensions.ToOrder)} moved an order from {OrderStates.Done} back to {OrderStates.Active} - a transition the state table refuses - without refusing the report and without anywhere to report it.");
	}

	// Properties of Order that no execution report has a field for, so a round trip through one
	// cannot be asked to bring them back. Everything else Order declares is compared below.
	private static readonly HashSet<string> _orderFieldsNoReportCarries =
	[
		// Both travel as identifiers - SecurityId and PortfolioName - and the entity behind the
		// identifier is the receiver's to resolve rather than the report's to carry.
		nameof(Order.Security),
		nameof(Order.Portfolio),

		// The report timestamps itself (ServerTime, LocalTime). It has no field for when the order
		// was originally placed, nor for when it was matched or cancelled.
		nameof(Order.Time),
		nameof(Order.MatchedTime),
		nameof(Order.CancelledTime),

		// The report carries a single Latency and does not say which of the three it measured.
		nameof(Order.LatencyRegistration),
		nameof(Order.LatencyCancellation),
		nameof(Order.LatencyEdition),
	];

	/// <summary>
	/// An order becomes an execution report whenever it has to cross a boundary - the snapshot a new
	/// transaction subscription is answered with is built exactly this way - and the far side rebuilds
	/// the order from that report. Whatever the report leaves behind, the far side never learns: a
	/// stop order that loses its condition arrives as a plain limit order, and a client cannot see the
	/// stop level of its own live order. The two halves of the conversion live in two different
	/// methods, so a field added to one and forgotten in the other stays invisible until something
	/// downstream reads a null. This walks every writable property of an order and requires each one
	/// either to survive the round trip or to be named as one no report has a field for.
	/// </summary>
	[TestMethod]
	public void OrderMessageRoundTripFillsEveryScalarProperty()
	{
		var order = CreateLivingOrder();

		var compared = typeof(Order)
			.GetModifiableProps()
			.Where(p => !_orderFieldsNoReportCarries.Contains(p.Name))
			.OrderBy(p => p.Name)
			.ToArray();

		// A property left at its default compares equal however the conversion behaves, so the
		// fixture is required to have given every compared property a value of its own first.
		var blank = new Order();

		var unfilled = compared
			.Where(p => Equals(p.GetValue(order), p.GetValue(blank)))
			.Select(p => p.Name)
			.ToArray();

		unfilled.IsEmpty().AssertTrue($"{nameof(CreateLivingOrder)} leaves these properties at their default, so the round trip below proves nothing about them: {unfilled.JoinComma()}");

		var restored = order.ToMessage().ToOrder(new Order { Security = order.Security });
		restored.Condition.AssertNotSame(order.Condition, "the report and the restored order must not share the caller's mutable condition");

		var lost = compared
			.Where(p => !OrderPropertyEquals(p, order, restored))
			.Select(p => $"{p.Name}: {p.GetValue(order) ?? "null"} -> {p.GetValue(restored) ?? "null"}")
			.ToArray();

		lost.IsEmpty().AssertTrue($"{lost.Length} of {compared.Length} properties did not survive Order -> ExecutionMessage -> Order:{Environment.NewLine}{lost.JoinN()}");
	}

	private static bool OrderPropertyEquals(PropertyInfo property, Order expected, Order actual)
	{
		var expectedValue = property.GetValue(expected);
		var actualValue = property.GetValue(actual);

		if (expectedValue is not OrderCondition expectedCondition || actualValue is not OrderCondition actualCondition)
			return Equals(expectedValue, actualValue);

		return expectedCondition.GetType() == actualCondition.GetType()
			&& expectedCondition.Parameters.Count == actualCondition.Parameters.Count
			&& expectedCondition.Parameters.All(p => actualCondition.Parameters.TryGetValue(p.Key, out var value) && Equals(p.Value, value));
	}

	#endregion

	#region Order transaction messages

	/// <summary>
	/// This message is the order: what it says is what the exchange is asked to do, and the order
	/// object stays behind on this side. An instruction that does not make it into the message is
	/// not a lost annotation - a post-only order sent as an ordinary limit crosses the spread and
	/// pays the taker fee, and a close-only order that loses its position effect opens a new
	/// position instead of closing one.
	/// </summary>
	[TestMethod]
	public void CreateRegisterMessage_CarriesEveryInstructionTheOrderWasGiven()
	{
		var order = CreateLivingOrder();

		var msg = order.CreateRegisterMessage();

		msg.TransactionId.AssertEqual(order.TransactionId);
		msg.SecurityId.AssertEqual(order.Security.ToSecurityId());
		msg.PortfolioName.AssertEqual(order.Portfolio.Name);

		msg.Side.AssertEqual(order.Side);
		msg.OrderType.AssertEqual(order.Type);
		msg.Price.AssertEqual(order.Price);
		msg.Volume.AssertEqual(order.Volume);
		msg.VisibleVolume.AssertEqual(order.VisibleVolume);
		msg.MinOrderVolume.AssertEqual(order.MinVolume);
		msg.TimeInForce.AssertEqual(order.TimeInForce);
		msg.TillDate.AssertEqual(order.ExpiryDate);
		msg.PostOnly.AssertEqual(order.PostOnly);
		msg.PositionEffect.AssertEqual(order.PositionEffect);
		msg.MarginMode.AssertEqual(order.MarginMode);
		msg.Leverage.AssertEqual(order.Leverage);
		msg.IsMarketMaker.AssertEqual(order.IsMarketMaker);
		msg.IsManual.AssertEqual(order.IsManual);
		msg.Slippage.AssertEqual(order.Slippage);

		msg.Comment.AssertEqual(order.Comment);
		msg.UserOrderId.AssertEqual(order.UserOrderId);
		msg.StrategyId.AssertEqual(order.StrategyId);
		msg.BrokerCode.AssertEqual(order.BrokerCode);
		msg.ClientCode.AssertEqual(order.ClientCode);
	}

	/// <summary>
	/// The currency the order is placed in is checked separately from the sweep above because it is
	/// the one instruction an order and an instrument both have an opinion about. The caller set it
	/// on the order, so that is the answer; an instrument that says nothing about its currency
	/// cannot be the reason the order is sent without one.
	/// </summary>
	[TestMethod]
	public void CreateRegisterMessage_KeepsTheCurrencyTheOrderNames()
	{
		var order = CreateLivingOrder();

		order.Security.Currency.AssertNull($"{nameof(Helper.CreateSecurity)} now names a currency, so this test no longer separates the order's answer from the instrument's");

		order.CreateRegisterMessage().Currency.AssertEqual(order.Currency, "the order names the currency it is placed in and the instrument names none, so the instrument cannot be the one that decides");
	}

	/// <summary>
	/// A condition is mutable and the caller goes on holding the order after sending it - moving a
	/// stop level is an ordinary thing to do. The message is already on its way, so it has to carry
	/// its own copy; sharing one would rewrite an instruction the exchange has already been given.
	/// </summary>
	[TestMethod]
	public void CreateRegisterMessage_GivesTheMessageItsOwnCopyOfTheCondition()
	{
		var order = CreateLivingOrder();

		var msg = order.CreateRegisterMessage();

		msg.Condition.AssertNotNull();
		msg.Condition.AssertNotSame(order.Condition);

		((FixOrderCondition)order.Condition).StopLoss = 1m;

		((FixOrderCondition)msg.Condition).StopLoss.AssertEqual(95m, "editing the order after it was sent must not rewrite the instruction already given");
	}

	/// <summary>
	/// A cancellation is itself a transaction: it has its own number, by which its acknowledgement
	/// or rejection comes back, and it names the order to cancel by that order's number. Confusing
	/// the two leaves the user with a cancel whose fate cannot be tracked, or with a cancel aimed at
	/// nothing. The identity of the order is filled in first and the instrument's fields are copied
	/// over the message afterwards, so what is under test is that the target survives that copy.
	/// </summary>
	[TestMethod]
	public void CreateCancelMessage_NamesTheOrderToCancelAndCarriesItsOwnTransaction()
	{
		var order = CreateLivingOrder();
		var securityId = order.Security.ToSecurityId();

		const long cancelTransactionId = 4242;

		var msg = order.CreateCancelMessage(securityId, cancelTransactionId);

		msg.TransactionId.AssertEqual(cancelTransactionId);
		msg.OriginalTransactionId.AssertEqual(order.TransactionId);

		msg.OrderId.AssertEqual(order.Id);
		msg.OrderStringId.AssertEqual(order.StringId);
		msg.SecurityId.AssertEqual(securityId);
		msg.PortfolioName.AssertEqual(order.Portfolio.Name);

		msg.OrderType.AssertEqual(order.Type);
		msg.Side.AssertEqual(order.Side);
		msg.Volume.AssertEqual(order.Volume);
		msg.Balance.AssertEqual(order.Balance);
		msg.MarginMode.AssertEqual(order.MarginMode);

		msg.UserOrderId.AssertEqual(order.UserOrderId);
		msg.StrategyId.AssertEqual(order.StrategyId);
		msg.BrokerCode.AssertEqual(order.BrokerCode);
		msg.ClientCode.AssertEqual(order.ClientCode);
	}

	/// <summary>
	/// A replace is one transaction naming two orders: the one to withdraw and the one to put in its
	/// place. Taking the terms from the wrong side of that pair sends the old price and volume back
	/// to the exchange - the amendment appears to be accepted and changes nothing - while naming the
	/// wrong order to withdraw leaves the original live alongside its replacement.
	/// </summary>
	[TestMethod]
	public void CreateReplaceMessage_NamesTheOrderItWithdrawsAndCarriesTheNewTerms()
	{
		var old = CreateLivingOrder();

		var replacement = old.ReRegisterClone(102.5m, 30m);
		replacement.TransactionId = old.TransactionId + 1;

		var securityId = old.Security.ToSecurityId();

		var msg = old.CreateReplaceMessage(replacement, securityId);

		// The order being withdrawn.
		msg.OriginalTransactionId.AssertEqual(old.TransactionId);
		msg.OldOrderId.AssertEqual(old.Id);
		msg.OldOrderStringId.AssertEqual(old.StringId);
		msg.OldOrderPrice.AssertEqual(old.Price);
		msg.OldOrderVolume.AssertEqual(old.Volume);

		// The order taking its place.
		msg.TransactionId.AssertEqual(replacement.TransactionId);
		msg.SecurityId.AssertEqual(securityId);
		msg.PortfolioName.AssertEqual(replacement.Portfolio.Name);
		msg.Price.AssertEqual(102.5m);
		msg.Volume.AssertEqual(30m);
		msg.Side.AssertEqual(replacement.Side);
		msg.OrderType.AssertEqual(replacement.Type);
		msg.VisibleVolume.AssertEqual(replacement.VisibleVolume);
		msg.MinOrderVolume.AssertEqual(replacement.MinVolume);
		msg.TimeInForce.AssertEqual(replacement.TimeInForce);
		msg.TillDate.AssertEqual(replacement.ExpiryDate);
		msg.PostOnly.AssertEqual(replacement.PostOnly);
		msg.PositionEffect.AssertEqual(replacement.PositionEffect);
		msg.MarginMode.AssertEqual(replacement.MarginMode);
		msg.Leverage.AssertEqual(replacement.Leverage);
		msg.IsMarketMaker.AssertEqual(replacement.IsMarketMaker);
		msg.IsManual.AssertEqual(replacement.IsManual);
		msg.Slippage.AssertEqual(replacement.Slippage);

		// Bookkeeping follows the order being amended.
		msg.UserOrderId.AssertEqual(old.UserOrderId);
		msg.StrategyId.AssertEqual(old.StrategyId);
		msg.BrokerCode.AssertEqual(old.BrokerCode);
		msg.ClientCode.AssertEqual(old.ClientCode);
	}

	/// <summary>
	/// The same promise the registration message keeps, for the same reason: the replacement's
	/// condition is the caller's to go on editing, and the message has already been handed over.
	/// </summary>
	[TestMethod]
	public void CreateReplaceMessage_GivesTheMessageItsOwnCopyOfTheCondition()
	{
		var old = CreateLivingOrder();

		var replacement = old.ReRegisterClone();
		replacement.TransactionId = old.TransactionId + 1;

		var msg = old.CreateReplaceMessage(replacement, old.Security.ToSecurityId());

		msg.Condition.AssertNotNull();
		msg.Condition.AssertNotSame(replacement.Condition);

		((FixOrderCondition)replacement.Condition).StopLoss = 1m;

		((FixOrderCondition)msg.Condition).StopLoss.AssertEqual(95m, "editing the replacement after it was sent must not rewrite the instruction already given");
	}

	#endregion

	#region Lookup criteria

	/// <summary>
	/// A criteria object and a lookup message are the same request in two shapes - the user fills in
	/// one on a search panel, the adapter is handed the other - so a field that does not survive the
	/// trip quietly widens the search: asking for stocks named "Sber" on one board and receiving
	/// every instrument the venue lists is not the answer to the question asked.
	/// </summary>
	[TestMethod]
	public void ASecurityLookupSurvivesTheTripThroughACriteriaObject()
	{
		var secId = "SBER@TQBR".ToSecurityId();

		var message = new SecurityLookupMessage
		{
			SecurityId = secId,
			Name = "Sberbank",
			SecurityType = SecurityTypes.Stock,
			Currency = CurrencyTypes.RUB,
		};

		var criteria = message.ToLookupCriteria(new InMemoryExchangeInfoProvider());

		criteria.Code.AssertEqual(secId.SecurityCode);
		criteria.Board.AssertNotNull();
		criteria.Board.Code.AssertEqual(secId.BoardCode);
		criteria.Name.AssertEqual(message.Name);
		criteria.Type.AssertEqual(message.SecurityType);
		criteria.Currency.AssertEqual(message.Currency);

		var back = criteria.ToLookupMessage();

		back.SecurityId.AssertEqual(secId);
		back.Name.AssertEqual(message.Name);
		back.SecurityType.AssertEqual(message.SecurityType);
		back.Currency.AssertEqual(message.Currency);
	}

	/// <summary>
	/// A criteria carrying nothing means "everything", and that has to be recognised as such: a
	/// request that instead travels as a filter matching nothing answers a user who asked for the
	/// whole instrument list with an empty one.
	/// </summary>
	[TestMethod]
	public void AnEmptySecurityLookupAsksForEverything()
	{
		var criteria = Helper.LookupAll.ToLookupCriteria(new InMemoryExchangeInfoProvider());

		criteria.IsLookupAll().AssertTrue();
		criteria.ToLookupMessage().IsLookupAll().AssertTrue();
	}

	/// <summary>
	/// A criteria that names the instrument outright asks for that one instrument. The identifier is
	/// the whole request here, so it has to be split into code and board rather than travel as text
	/// nothing downstream compares against.
	/// </summary>
	[TestMethod]
	public void ToLookupMessage_ACriteriaNamingOneInstrumentAsksForThatOne()
	{
		var secId = "SBER@TQBR".ToSecurityId();

		var message = new Security { Id = secId.ToStringId() }.ToLookupMessage();

		message.SecurityId.AssertEqual(secId);
		message.IsLookupAll().AssertFalse("a criteria naming one instrument is not a request for every instrument");
	}

	/// <summary>
	/// The account filter is what limits a portfolio request to the accounts the user asked about,
	/// and it is a subscription rather than a one-off read: a request that forgets to say so is
	/// answered once and never again, and the balances on screen stop moving.
	/// </summary>
	[TestMethod]
	public void ToLookupCriteria_APortfolioFilterKeepsEveryFieldItFiltersOn()
	{
		var criteria = new Portfolio
		{
			Name = "PF-1",
			Board = ExchangeBoard.Test,
			Currency = CurrencyTypes.EUR,
			ClientCode = "CLI",
		};

		var message = criteria.ToLookupCriteria();

		message.IsSubscribe.AssertTrue();
		message.PortfolioName.AssertEqual(criteria.Name);
		message.BoardCode.AssertEqual(criteria.Board.Code);
		message.Currency.AssertEqual(criteria.Currency);
		message.ClientCode.AssertEqual(criteria.ClientCode);
	}

	/// <summary>
	/// The order filter is how a client asks "what of mine is still live": every field it names
	/// narrows the answer, and the volume and side are given separately because an order object has
	/// no way to say "either side". A dropped field returns somebody else's orders, or the whole
	/// book of them.
	/// </summary>
	[TestMethod]
	public void ToLookupCriteria_AnOrderFilterKeepsEveryFieldItFiltersOn()
	{
		var criteria = CreateLivingOrder();

		var message = criteria.ToLookupCriteria(15m, Sides.Buy);

		message.IsSubscribe.AssertTrue();
		message.SecurityId.AssertEqual(criteria.Security.ToSecurityId());
		message.PortfolioName.AssertEqual(criteria.Portfolio.Name);
		message.OrderId.AssertEqual(criteria.Id);
		message.OrderStringId.AssertEqual(criteria.StringId);
		message.OrderType.AssertEqual(criteria.Type);
		message.UserOrderId.AssertEqual(criteria.UserOrderId);
		message.StrategyId.AssertEqual(criteria.StrategyId);
		message.BrokerCode.AssertEqual(criteria.BrokerCode);
		message.ClientCode.AssertEqual(criteria.ClientCode);

		// The two the caller states separately rather than through the order.
		message.Volume.AssertEqual(15m);
		message.Side.AssertEqual(Sides.Buy);
	}

	#endregion

	#region Portfolio and news conversions

	/// <summary>
	/// The account object is held by reference all over the application - balances hang off it and
	/// every position points at it - so an update has to be applied to the one the caller passed in,
	/// not to a fresh copy nobody else can see.
	/// </summary>
	[TestMethod]
	public void ToPortfolio_UpdatesTheAccountItWasGiven()
	{
		var portfolio = new Portfolio { Name = "PF-1" };

		var message = new PortfolioMessage
		{
			PortfolioName = "PF-1",
			BoardCode = ExchangeBoard.Test.Code,
			Currency = CurrencyTypes.EUR,
			ClientCode = "CLI",
		};

		var updated = message.ToPortfolio(portfolio, new InMemoryExchangeInfoProvider());

		updated.AssertSame(portfolio);

		portfolio.Board.AssertNotNull();
		portfolio.Board.Code.AssertEqual(ExchangeBoard.Test.Code);
		portfolio.Currency.AssertEqual(CurrencyTypes.EUR);
		portfolio.ClientCode.AssertEqual("CLI");
	}

	/// <summary>
	/// An update that says nothing about a field is not an instruction to clear it. Portfolio
	/// messages arrive repeatedly and most of them carry only what changed, so treating silence as
	/// "erase" would strip the account of its board and currency on the second message.
	/// </summary>
	[TestMethod]
	public void ToPortfolio_LeavesAloneWhatTheMessageSaysNothingAbout()
	{
		var portfolio = new Portfolio
		{
			Name = "PF-1",
			Board = ExchangeBoard.Test,
			Currency = CurrencyTypes.EUR,
			ClientCode = "CLI",
		};

		new PortfolioMessage { PortfolioName = "PF-1" }.ToPortfolio(portfolio, new InMemoryExchangeInfoProvider());

		portfolio.Board.AssertSame(ExchangeBoard.Test);
		portfolio.Currency.AssertEqual(CurrencyTypes.EUR);
		portfolio.ClientCode.AssertEqual("CLI");
	}

	/// <summary>
	/// A news item is read, filtered and stored by what it carries: the headline and story the user
	/// sees, the time it is filed under, the source it is attributed to, and the sequence number by
	/// which a gap in the feed is noticed. A field lost here is lost for good - nothing downstream
	/// goes back to the message for it.
	/// </summary>
	[TestMethod]
	public void ToNews_CarriesEveryFieldTheServerSent()
	{
		var serverTime = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Utc);

		var message = new NewsMessage
		{
			Id = "N-1",
			Source = "Some agency",
			Headline = "Rates unchanged",
			Story = "The full text of the story.",
			Url = "https://example.com/n1",
			BoardCode = ExchangeBoard.Test.Code,
			ServerTime = serverTime,
			LocalTime = serverTime.AddMilliseconds(30),
			Priority = NewsPriorities.High,
			Language = "en",
			ExpiryDate = serverTime.AddDays(1),
			SeqNum = 9,
		};

		var news = message.ToNews(new InMemoryExchangeInfoProvider());

		news.Id.AssertEqual(message.Id);
		news.Source.AssertEqual(message.Source);
		news.Headline.AssertEqual(message.Headline);
		news.Story.AssertEqual(message.Story);
		news.Url.AssertEqual(message.Url);
		news.ServerTime.AssertEqual(message.ServerTime);
		news.LocalTime.AssertEqual(message.LocalTime);
		news.Priority.AssertEqual(message.Priority);
		news.Language.AssertEqual(message.Language);
		news.ExpiryDate.AssertEqual(message.ExpiryDate);
		news.SeqNum.AssertEqual(message.SeqNum);

		news.Board.AssertNotNull();
		news.Board.Code.AssertEqual(message.BoardCode);
	}

	/// <summary>
	/// News attached to an instrument is shown next to that instrument and filtered by it. The
	/// instrument is named by code and board together, and a code on its own is a different
	/// instrument: the same ticker trades on more than one board, and an identifier missing its
	/// board resolves to the associated-board instrument instead of the one the story is about.
	/// </summary>
	[TestMethod]
	public void ToNews_NamesTheInstrumentTheServerSent()
	{
		var secId = "SBER@TQBR".ToSecurityId();

		var news = new NewsMessage { Id = "N-1", SecurityId = secId }.ToNews(new InMemoryExchangeInfoProvider());

		news.Security.AssertNotNull();
		news.Security.Id.AssertEqual(secId.ToStringId(), "the instrument the story is about must come back as the one the server named");
		news.Security.ToSecurityId().AssertEqual(secId);
	}

	#endregion
}
