namespace StockSharp.Tinkoff;

static class TinkoffExtensions
{
	public static Sides? ToSide(this TradeDirection side)
		=> side switch
		{
			TradeDirection.Buy => Sides.Buy,
			TradeDirection.Sell => Sides.Sell,
			TradeDirection.Unspecified => null,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};

	public static OrderDirection ToNative(this Sides side)
		=> side switch
		{
			Sides.Buy => OrderDirection.Buy,
			Sides.Sell => OrderDirection.Sell,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};

	public static StopOrderDirection ToStopNative(this Sides side)
		=> side switch
		{
			Sides.Buy => StopOrderDirection.Buy,
			Sides.Sell => StopOrderDirection.Sell,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};

	public static Sides ToSide(this OrderDirection side)
		=> side switch
		{
			OrderDirection.Buy => Sides.Buy,
			OrderDirection.Sell => Sides.Sell,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};

	public static Sides ToSide(this StopOrderDirection side)
		=> side switch
		{
			StopOrderDirection.Buy => Sides.Buy,
			StopOrderDirection.Sell => Sides.Sell,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};

	public static OrderType ToNative(this OrderTypes? type)
		=> type switch
		{
			null => OrderType.Unspecified,
			OrderTypes.Limit => OrderType.Limit,
			OrderTypes.Market => OrderType.Market,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};

	public static OrderTypes? ToOrderType(this OrderType type)
		=> type switch
		{
			OrderType.Unspecified => null,
			OrderType.Limit => OrderTypes.Limit,
			OrderType.Market => OrderTypes.Market,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};

	public static IEnumerable<TimeSpan> TimeFrames => _timeFrames.Keys;

	private static readonly PairSet<TimeSpan, SubscriptionInterval> _timeFrames = new()
	{
		{ TimeSpan.FromMinutes(1), SubscriptionInterval.OneMinute },
		{ TimeSpan.FromMinutes(5), SubscriptionInterval.FiveMinutes },
	};

	private static readonly PairSet<TimeSpan, CandleInterval> _timeFrames2 = new()
	{
		{ TimeSpan.FromMinutes(1), CandleInterval._1Min },
		{ TimeSpan.FromMinutes(5), CandleInterval._5Min },
	};

	public static SubscriptionInterval ToNative(this TimeSpan timeFrame)
	{
		if (!_timeFrames.TryGetValue(timeFrame, out var native))
			throw new ArgumentOutOfRangeException(nameof(timeFrame), timeFrame, LocalizedStrings.InvalidValue);

		return native;
	}

	public static CandleInterval ToNative2(this TimeSpan timeFrame)
	{
		if (!_timeFrames2.TryGetValue(timeFrame, out var native))
			throw new ArgumentOutOfRangeException(nameof(timeFrame), timeFrame, LocalizedStrings.InvalidValue);

		return native;
	}

	public static TimeSpan ToTimeFrame(this SubscriptionInterval native)
	{
		if (!_timeFrames.TryGetKey(native, out var tf))
			throw new ArgumentOutOfRangeException(nameof(native), native, LocalizedStrings.InvalidValue);

		return tf;
	}

	public static SecurityTypes? ToSecurityType(this string type)
		=> type?.Remove("*").ToLowerInvariant() switch
		{
			"future" => SecurityTypes.Future,
			"commodity" => SecurityTypes.Commodity,
			"currency" => SecurityTypes.Currency,
			"security" => SecurityTypes.Stock,
			"index" => SecurityTypes.Index,
			_ => null,
		};

	public static OptionTypes? ToOptionType(this OptionDirection dir)
		=> dir switch
		{
			OptionDirection.Unspecified => null,
			OptionDirection.Put => OptionTypes.Put,
			OptionDirection.Call => OptionTypes.Call,
			_ => throw new ArgumentOutOfRangeException(nameof(dir), dir, LocalizedStrings.InvalidValue),
		};

	public static OptionStyles? ToOptionStyle(this OptionStyle style)
		=> style switch
		{
			OptionStyle.Unspecified => null,
			OptionStyle.American => OptionStyles.American,
			OptionStyle.European => OptionStyles.European,
			_ => throw new ArgumentOutOfRangeException(nameof(style), style, LocalizedStrings.InvalidValue),
		};

	public static SettlementTypes? ToSettlementType(this OptionSettlementType type)
		=> type switch
		{
			OptionSettlementType.OptionExecutionTypeUnspecified => null,
			OptionSettlementType.OptionExecutionTypePhysicalDelivery => SettlementTypes.Delivery,
			OptionSettlementType.OptionExecutionTypeCashSettlement => SettlementTypes.Cash,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};

	public static SettlementTypes? ToSettlementType(this string type)
		=> type?.Remove("*").ToLowerInvariant() switch
		{
			"physical_delivery" => SettlementTypes.Delivery,
			"cash_settlement" => SettlementTypes.Cash,
			_ => null,
		};

	public static StopOrderExpirationType ToNative(this TimeInForce? tif, DateTimeOffset? till)
		=> tif switch
		{
			null => StopOrderExpirationType.Unspecified,
			TimeInForce.PutInQueue => till is null ? StopOrderExpirationType.GoodTillCancel : StopOrderExpirationType.GoodTillDate,
			_ => throw new ArgumentOutOfRangeException(nameof(tif), tif, LocalizedStrings.InvalidValue),
		};

	public static OrderStates ToOrderState(this OrderExecutionReportStatus status)
		=> status switch
		{
			OrderExecutionReportStatus.ExecutionReportStatusNew or OrderExecutionReportStatus.ExecutionReportStatusPartiallyfill
				=> OrderStates.Active,
			OrderExecutionReportStatus.ExecutionReportStatusCancelled or OrderExecutionReportStatus.ExecutionReportStatusFill
				=> OrderStates.Done,
			OrderExecutionReportStatus.ExecutionReportStatusRejected
				=> OrderStates.Failed,

			_ => throw new ArgumentOutOfRangeException(nameof(status), status, LocalizedStrings.InvalidValue),
		};

	public static string GetInstrumentId(this SecurityMessage secMsg)
	{
		if (secMsg is null)
			throw new ArgumentNullException(nameof(secMsg));

		var secId = secMsg.SecurityId;

		if (secId.Native is string s)
			return s;

		throw new ArgumentException(LocalizedStrings.NoSystemId.Put(secId), nameof(secMsg));
	}

	public static SecurityId FromInstrumentIdToSecId(this string instrumentId)
	{
		if (instrumentId.IsEmpty())
			throw new ArgumentNullException(nameof(instrumentId));

		return new() { Native = instrumentId };
	}

	public static SecurityStates? ToState(this SecurityTradingStatus status)
		=> status switch
		{
			SecurityTradingStatus.Unspecified => null,

			SecurityTradingStatus.NotAvailableForTrading or SecurityTradingStatus.OpeningPeriod or
			SecurityTradingStatus.ClosingPeriod or SecurityTradingStatus.BreakInTrading or
			SecurityTradingStatus.ClosingAuction or SecurityTradingStatus.SessionClose or
			SecurityTradingStatus.DealerBreakInTrading or SecurityTradingStatus.DealerNotAvailableForTrading
				=> SecurityStates.Stoped,

			SecurityTradingStatus.NormalTrading or SecurityTradingStatus.DarkPoolAuction or
			SecurityTradingStatus.DiscreteAuction or SecurityTradingStatus.OpeningAuctionPeriod or
			SecurityTradingStatus.TradingAtClosingAuctionPrice or SecurityTradingStatus.SessionAssigned or
			SecurityTradingStatus.SessionOpen or SecurityTradingStatus.DealerNormalTrading
				=> SecurityStates.Trading,

			_ => throw new ArgumentOutOfRangeException(nameof(status), status, LocalizedStrings.InvalidValue),
		};

	public static Timestamp ToTimestamp(this DateTimeOffset dto)
		=> Timestamp.FromDateTimeOffset(dto);
}