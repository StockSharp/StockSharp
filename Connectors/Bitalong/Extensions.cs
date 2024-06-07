namespace StockSharp.Bitalong
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
			switch (status?.ToLowerInvariant())
			{
				case "open":
					return OrderStates.Active;
				case "filled":
					return OrderStates.Done;
				case "cancelled":
					return OrderStates.Done;
				default:
					throw new ArgumentOutOfRangeException(nameof(status), status, LocalizedStrings.InvalidValue);
			}
		}
	}
}