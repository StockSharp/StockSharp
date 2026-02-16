namespace StockSharp.ZB;

static class Extensions
{
	public static int ToNativeAsInt(this Sides side)
	{
		return side switch
		{
			Sides.Buy => 1,
			Sides.Sell => 0,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};
	}

	public static Sides ToSide(this int side)
	{
		return side switch
		{
			1 => Sides.Buy,
			0 => Sides.Sell,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};
	}

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
		return (side?.ToLowerInvariant()) switch
		{
			"buy" or "bid" => Sides.Buy,
			"sell" or "ask" => Sides.Sell,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};
	}

	public static string ToSymbol(this SecurityId securityId, bool isSocket = true)
	{
		var secCode = securityId.SecurityCode;
		secCode = isSocket ? secCode.Remove("/") : secCode.Replace('/', '_');
		return secCode.ToLowerInvariant();
	}

	public static SecurityId ToStockSharp(this string symbol)
	{
		if (symbol.Contains("_"))
			symbol = symbol.Replace('_', '/');
		else if (symbol.ContainsIgnoreCase("usd"))
			symbol = symbol.Insert(symbol.IndexOfIgnoreCase("usd"), "/");
		else// if (symbol.Length == 6)
			symbol = symbol.Insert(3, "/");
		
		return new SecurityId
		{
			SecurityCode = symbol.ToUpperInvariant(),
			BoardCode = BoardCodes.ZB,
		};
	}

	public static decimal? GetBalance(this Order order)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		return (order.TotalAmount - order.TradeAmount)?.ToDecimal();
	}

	public static OrderStates ToOrderState(this int status)
	{
		switch (status)
		{
			case 1: // Cancel
			case 2: // Transaction completed
				return OrderStates.Done;

			case 3: // Pending/Pending transaction
				return OrderStates.Active;
			default:
				throw new ArgumentOutOfRangeException(nameof(status), status, LocalizedStrings.InvalidValue);
		}
	}
}