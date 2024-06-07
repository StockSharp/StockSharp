namespace StockSharp.Coinbase;

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

	public static decimal GetBalance(this Native.Model.Order order)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		return (order.Size - order.FilledSize ?? 0).ToDecimal().Value;
	}

	public static string ToCurrency(this SecurityId securityId)
	{
		return securityId.SecurityCode.Replace("/", "-").ToUpperInvariant();
	}

	public static SecurityId ToStockSharp(this string currency)
	{
		return new SecurityId
		{
			SecurityCode = currency.Replace("-", "/").ToUpperInvariant(),
			BoardCode = BoardCodes.Coinbase,
		};
	}
}