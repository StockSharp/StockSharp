namespace StockSharp.Bibox.Native;

static class Extensions
{
	public static int ToNative(this Sides side)
	{
		return side switch
		{
			Sides.Buy => 1,
			Sides.Sell => 2,
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

	public static int ToNative(this OrderTypes? type)
	{
		return type switch
		{
			null or OrderTypes.Limit => 2,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};
	}

	public static OrderTypes ToOrderType(this int type)
	{
		return type switch
		{
			2 => OrderTypes.Limit,
			_ => throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue),
		};
	}

	public static PairSet<TimeSpan, string> TimeFrames { get; } = new PairSet<TimeSpan, string>
	{
		{ TimeSpan.FromMinutes(1), "1m" },
		{ TimeSpan.FromMinutes(3), "3m" },
		{ TimeSpan.FromMinutes(5), "5m" },
		{ TimeSpan.FromMinutes(15), "15m" },
		{ TimeSpan.FromMinutes(30), "30m" },
		{ TimeSpan.FromHours(1), "1h" },
		{ TimeSpan.FromHours(2), "2h" },
		{ TimeSpan.FromHours(4), "4h" },
		{ TimeSpan.FromHours(6), "6h" },
		{ TimeSpan.FromHours(12), "12h" },
		{ TimeSpan.FromDays(1), "1d" },
		{ TimeSpan.FromDays(7), "1w" },
		{ TimeSpan.FromTicks(TimeHelper.TicksPerMonth), "1M" },
	};

	public static string ToNative(this TimeSpan timeFrame)
	{
		return TimeFrames.TryGetValue(timeFrame) ?? throw new ArgumentOutOfRangeException(nameof(timeFrame), timeFrame, LocalizedStrings.InvalidValue);
	}

	public static TimeSpan ToTimeFrame(this string name)
	{
		return TimeFrames.TryGetKey2(name) ?? throw new ArgumentOutOfRangeException(nameof(name), name, LocalizedStrings.InvalidValue);
	}

	public static string ToSymbol(this SecurityId securityId)
	{
		return securityId.SecurityCode.ToUpperInvariant();
	}

	public static SecurityId ToStockSharp(this string currency)
	{
		return new SecurityId
		{
			SecurityCode = currency.ToUpperInvariant(),
			BoardCode = BoardCodes.Bibox,
		};
	}

	public static OrderStates ToOrderState(this int status)
	{
		return status switch
		{
			// to be dealt
			1 => OrderStates.Active,
			// dealt partly
			2 => OrderStates.Active,
			// dealt totally
			3 => OrderStates.Done,
			// cancelled partly
			4 => OrderStates.Done,
			// cancelled totally
			5 => OrderStates.Done,
			// to be cancelled
			6 => OrderStates.Active,
			_ => throw new ArgumentOutOfRangeException(nameof(status), status, LocalizedStrings.InvalidValue),
		};
	}

	private static readonly Dictionary<int, string> _errorsIds = new()
	{
		{ 2003, "Cookie expired" },
		{ 2027, "Insufficient balance available" },
		{ 2033, "Operation failed! Order completed or canceled" },
		{ 2034, "Operation failed! Please check parameter" },
		{ 2040, "Operation failed! No record" },
		{ 2064, "Canceling. Unable to cancel again" },
		{ 2065, "Precatory price is exorbitant, please reset" },
		{ 2066, "Precatory price is low , please reset" },
		{ 2067, "Limit Order Only" },
		{ 2068, "Min Amount:0.0001" },
		{ 2069, "Market order can not be canceled" },
		{ 2078, "Invalid order price" },
		{ 2085, "the trade amount is low" },
		{ 2086, "Abnormal account assets, trade is forbidden" },
		{ 2091, "request is too frequency, please try again later" },
		{ 2092, "Minimum amount not met" },
		{ 3000, "Requested parameter incorrect" },
		{ 3002, "Parameter cannot be null" },
		{ 3009, "Illegal subscription channel" },
		{ 3010, "websocket connection error" },
		{ 3011, "Interface does not support apikey request method" },
		{ 3012, "Invalid apikey" },
		{ 3016, "Trading pair error" },
		{ 3017, "Illegal subscribe event" },
		{ 3024, "apikey authorization insufficient" },
		{ 3025, "apikey signature verification failed" },
		{ 3026, "apikey ip is restricted" },
		{ 3027, "No apikey in your account" },
		{ 3028, "Account apikey has exceeded the limit amount" },
		{ 3029, "apikey ip has exceeded the limit amount" },
		{ 3033, "query allow only one cmd" },
		{ 3034, "maximum cmds" },
		{ 3035, "too many cmds" },
		{ 4000, "the network is unstable now, please try again later" },
		{ 4003, "The server is busy, please try again later" },
	};

	public static string ToErrorText(this int code)
	{
		return _errorsIds.TryGetValue(code) ?? code.To<string>();
	}
}