namespace StockSharp.Btce.Native;

static class Extensions
{
	public static string ToBtce(this Sides side)
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
			"sell" or "ask" => Sides.Sell,
			"buy" or "bid" => Sides.Buy,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};
	}

	public static OrderStates ToOrderState(this int status)
	{
		return status switch
		{
			0 => OrderStates.Active,
			// executed
			1 or 2 or 3 => OrderStates.Done,
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, LocalizedStrings.InvalidValue),
		};
	}

	public static string ToCurrency(this SecurityId securityId)
	{
		return securityId.SecurityCode.Replace('/', '_').ToLowerInvariant();
	}

	public static SecurityId ToStockSharp(this string currency)
	{
		return new SecurityId
		{
			SecurityCode = currency.Replace('_', '/').ToUpperInvariant(),
			BoardCode = BoardCodes.Btce,
		};
	}

	//public static string ToStockSharpCode(this string btceCode)
	//{
	//	return btceCode.Replace('_', '/').ToUpperInvariant();
	//}

	//public static string ToBtceCode(this string stockSharpCode)
	//{
	//	return stockSharpCode.Replace('/', '_').ToLowerInvariant();
	//}
}