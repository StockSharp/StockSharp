namespace StockSharp.Coinbase.Native;

static class Extensions
{
	public static string ToNative(this Sides side)
	{
		return side switch
		{
			Sides.Buy => "buy",
			Sides.Sell => "sell",
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};
	}

	public static Sides ToSide(this string side)
	{
		return side switch
		{
			"buy" => Sides.Buy,
			"sell" => Sides.Sell,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};
	}

	public static string ToNative(this OrderTypes? type)
	{
		return type switch
		{
			null => null,
			OrderTypes.Limit => "limit",
			OrderTypes.Market => "market",
			OrderTypes.Conditional => "stop",
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};
	}

	public static OrderTypes ToOrderType(this string type)
	{
		return type switch
		{
			"limit" => OrderTypes.Limit,
			"market" => OrderTypes.Market,
			"stop" => OrderTypes.Conditional,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};
	}

	public static OrderStates ToOrderState(this string type)
	{
		return type switch
		{
			"pending" or "received" => OrderStates.Pending,
			"open" or "active" => OrderStates.Active,
			"filled" or "done" or "canceled" or "settled" => OrderStates.Done,
			"rejected" => OrderStates.Failed,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};
	}

	public static string ToNative(this TimeInForce? tif, DateTimeOffset? tillDate)
	{
		return tif switch
		{
			null => null,
			TimeInForce.PutInQueue => tillDate == null ? "GTC" : "GTT",
			TimeInForce.CancelBalance => "IOC",
			TimeInForce.MatchOrCancel => "FOK",
			_ => throw new ArgumentOutOfRangeException(nameof(tif), tif, LocalizedStrings.InvalidValue),
		};
	}

	public static TimeInForce? ToTimeInForce(this string tif)
	{
		return tif switch
		{
			null => null,
			"GTC" or "GTT" => (TimeInForce?)TimeInForce.PutInQueue,
			"IOC" => (TimeInForce?)TimeInForce.CancelBalance,
			"FOK" => (TimeInForce?)TimeInForce.MatchOrCancel,
			_ => throw new ArgumentOutOfRangeException(nameof(tif), tif, LocalizedStrings.InvalidValue),
		};
	}

	public static decimal GetBalance(this Order order)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		return (order.Size - order.FilledSize ?? 0).ToDecimal().Value;
	}

	public static string ToSymbol(this SecurityId securityId)
	{
		return securityId.SecurityCode.ToUpperInvariant();
	}

	public static SecurityId ToStockSharp(this string symbol)
	{
		return new SecurityId
		{
			SecurityCode = symbol.ToUpperInvariant(),
			BoardCode = BoardCodes.Coinbase,
		};
	}

	public static SecurityTypes? ToSecurityType(this string secType)
		=> secType?.ToLowerInvariant() switch
		{
			"spot" => SecurityTypes.CryptoCurrency,
			"futures" => SecurityTypes.Future,
			_ => null,
		};

	public static readonly PairSet<TimeSpan, string> TimeFrames = new()
	{
		{ TimeSpan.FromMinutes(1), "ONE_MINUTE" },
		{ TimeSpan.FromMinutes(5), "FIVE_MINUTE" },
		{ TimeSpan.FromMinutes(15), "FIFTEEN_MINUTE" },
		{ TimeSpan.FromMinutes(30), "THIRTY_MINUTE" },
		{ TimeSpan.FromHours(1), "ONE_HOUR" },
		{ TimeSpan.FromHours(2), "TWO_HOUR" },
		{ TimeSpan.FromHours(6), "SIX_HOUR" },
		{ TimeSpan.FromDays(1), "ONE_DAY" },
	};

	public static string ToNative(this TimeSpan timeFrame)
		=> TimeFrames.TryGetValue(timeFrame) ?? throw new ArgumentOutOfRangeException(nameof(timeFrame), timeFrame, LocalizedStrings.InvalidValue);

	public static TimeSpan ToTimeFrame(this string name)
		=> TimeFrames.TryGetKey2(name) ?? throw new ArgumentOutOfRangeException(nameof(name), name, LocalizedStrings.InvalidValue);

}