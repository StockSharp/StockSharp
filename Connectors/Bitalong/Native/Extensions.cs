namespace StockSharp.Bitalong.Native;

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

	public static string ToNative(this SecurityId securityId)
	{
		return securityId.SecurityCode.Replace('/', '_').ToLowerInvariant();
	}

	public static SecurityId ToStockSharp(this string symbol)
	{
		return new SecurityId
		{
			SecurityCode = symbol.Replace('_', '/').ToUpperInvariant(),
			BoardCode = BoardCodes.Bitalong,
		};
	}

	public static OrderStates ToOrderState(this string status)
	{
		return (status?.ToLowerInvariant()) switch
		{
			"open" => OrderStates.Active,
			"filled" => OrderStates.Done,
			"cancelled" => OrderStates.Done,
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, LocalizedStrings.InvalidValue),
		};
	}
}