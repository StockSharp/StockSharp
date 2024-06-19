namespace StockSharp.Bitexbook.Native;

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

	public static Sides ToSide(this int side)
	{
		return side switch
		{
			1 => Sides.Buy,
			2 => Sides.Sell,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};
	}

	public static readonly PairSet<TimeSpan, string> TimeFrames = new()
	{
		{ TimeSpan.FromMinutes(1), "1m" },
		{ TimeSpan.FromMinutes(5), "5m" },
		{ TimeSpan.FromMinutes(15), "15m" },
		{ TimeSpan.FromHours(1), "1h" },
		{ TimeSpan.FromDays(1), "1d" },
	};

	public static string ToNative(this TimeSpan timeFrame)
	{
		var name = TimeFrames.TryGetValue(timeFrame);

		if (name == null)
			throw new ArgumentOutOfRangeException(nameof(timeFrame), timeFrame, LocalizedStrings.InvalidValue);

		return name;
	}

	public static TimeSpan ToTimeFrame(this string name)
	{
		return TimeFrames.TryGetKey2(name) ?? throw new ArgumentOutOfRangeException(nameof(name), name, LocalizedStrings.InvalidValue);
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
			BoardCode = BoardCodes.Bitexbook,
		};
	}

	public static decimal GetBalance(this Order order)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		return (decimal)order.Volume;
	}
}