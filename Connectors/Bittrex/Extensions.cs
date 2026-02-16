namespace StockSharp.Bittrex;

static class Extensions
{
	public static Sides ToSide(this string side)
	{
		if (side.IsEmpty())
			throw new ArgumentNullException(nameof(side));

		switch (side?.ToUpperInvariant())
		{
			case "BUY":
			case "LIMIT_BUY":
				return Sides.Buy;
			case "SELL":
			case "LIMIT_SELL":
				return Sides.Sell;
			default:
				throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue);
		}
	}

	public static readonly PairSet<TimeSpan, string> TimeFrames = new()
	{
		{ TimeSpan.FromMinutes(1), "oneMin" },
		{ TimeSpan.FromMinutes(5), "fiveMin" },
		{ TimeSpan.FromMinutes(30), "thirtyMin" },
		{ TimeSpan.FromHours(1), "hour" },
		{ TimeSpan.FromDays(1), "day" },
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
		var timeFrame = TimeFrames.TryGetKey2(name);

		if (timeFrame == null)
			throw new ArgumentOutOfRangeException(nameof(name), name, LocalizedStrings.InvalidValue);

		return timeFrame.Value;
	}

	public static string ToSymbol(this SecurityId securityId)
	{
		var parts = securityId.SecurityCode.Split('/');

		if (parts.Length != 2)
			throw new ArgumentException(securityId.ToString());

		return $"{parts[1]}-{parts[0]}".ToUpperInvariant();
	}

	public static SecurityId ToStockSharp(this string symbol)
	{
		var parts = symbol.Split('-');

		if (parts.Length == 2)
			symbol = $"{parts[1]}/{parts[0]}";

		return new SecurityId
		{
			SecurityCode = symbol.ToUpperInvariant(),
			BoardCode = BoardCodes.Bittrex,
		};
	}

	public static OrderStates ToOrderState(this int state)
	{
		switch (state)
		{
			case 0: // OPEN
			case 1: // PARTIAL
				return OrderStates.Active;
			case 2: // FILL
			case 3: // CANCEL
				return OrderStates.Done;
			default:
				throw new ArgumentOutOfRangeException(nameof(state), state, LocalizedStrings.InvalidValue);
		}
	}
}