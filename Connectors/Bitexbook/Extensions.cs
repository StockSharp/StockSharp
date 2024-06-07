namespace StockSharp.Bitexbook
{
	static class Extensions
	{
		public static string ToNative(this Sides side)
		{
			switch (side)
			{
				case Sides.Buy:
					return "buy";
				case Sides.Sell:
					return "sell";
				default:
					throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue);
			}
		}

		public static Sides ToSide(this string side)
		{
			switch (side)
			{
				case "buy":
					return Sides.Buy;
				case "sell":
					return Sides.Sell;
				default:
					throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue);
			}
		}

		public static Sides ToSide(this int side)
		{
			switch (side)
			{
				case 1:
					return Sides.Buy;
				case 2:
					return Sides.Sell;
				default:
					throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue);
			}
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
			var timeFrame = TimeFrames.TryGetKey2(name);

			if (timeFrame == null)
				throw new ArgumentOutOfRangeException(nameof(name), name, LocalizedStrings.InvalidValue);

			return timeFrame.Value;
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
}