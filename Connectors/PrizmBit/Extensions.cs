namespace StockSharp.PrizmBit;

static class Extensions
{
	public static string ToNative(this Sides side)
	{
		return side switch
		{
			Sides.Buy => "Bid",
			Sides.Sell => "Ask",
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

	public static Sides ToSide(this int side)
	{
		return side switch
		{
			0 => Sides.Buy,
			1 => Sides.Sell,
			_ => throw new ArgumentOutOfRangeException(nameof(side), side, LocalizedStrings.InvalidValue),
		};
	}

	public static readonly PairSet<TimeSpan, string> TimeFrames = new()
	{
		{ TimeSpan.FromMinutes(1), "1" },
		{ TimeSpan.FromMinutes(3), "3" },
		{ TimeSpan.FromMinutes(5), "5" },
		{ TimeSpan.FromMinutes(15), "15" },
		{ TimeSpan.FromMinutes(30), "30" },
		{ TimeSpan.FromHours(1), "1H" },
		{ TimeSpan.FromHours(2), "2H" },
		{ TimeSpan.FromHours(4), "4H" },
		{ TimeSpan.FromHours(6), "6H" },
		{ TimeSpan.FromHours(12), "12H" },
		{ TimeSpan.FromDays(1), "1D" },
		{ TimeSpan.FromDays(3), "3D" },
		{ TimeSpan.FromDays(7 * 3), "3W" },
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

	public static string ToNative(this SecurityId securityId)
	{
		return securityId.SecurityCode;
	}

	public static SecurityId ToStockSharp(this string symbol)
	{
		return new SecurityId
		{
			SecurityCode = symbol.ToUpperInvariant(),
			BoardCode = BoardCodes.PrizmBit,
		};
	}

	public static int ToNative(this TimeInForce? tif, bool? postOnly)
	{
		if (postOnly == null)
			return 3;

		return tif switch
		{
			null or TimeInForce.PutInQueue => 0,
			TimeInForce.MatchOrCancel => 2,
			TimeInForce.CancelBalance => 1,
			_ => throw new ArgumentOutOfRangeException(nameof(tif), tif, LocalizedStrings.InvalidValue),
		};
	}

	public static TimeInForce? ToTimeInForce(this int? tif, out bool? postOnly)
	{
		postOnly = null;

		switch (tif)
		{
			case 0:
				return TimeInForce.PutInQueue;
			case 1:
				return TimeInForce.CancelBalance;
			case 2:
				return TimeInForce.MatchOrCancel;
			case 3:
				postOnly = true;
				return null;
			default:
				throw new ArgumentOutOfRangeException(nameof(tif), tif, LocalizedStrings.InvalidValue);
		}
	}

	public static OrderStates ToOrderState(this string state)
	{
		return (state?.ToLowerInvariant()) switch
		{
			"new" or "partiallyfilled" or "awaiting" => OrderStates.Active,
			"filled" or "cancelled" or "inactive" or "deleted" => OrderStates.Done,
			_ => throw new ArgumentOutOfRangeException(nameof(state), state, LocalizedStrings.InvalidValue),
		};
	}

	public static OrderTypes ToOrderType(this string type, out TimeInForce? tif, out bool isTrailing)
	{
		tif = null;
		isTrailing = false;

		switch (type?.ToLowerInvariant())
		{
			case "limit":
				return OrderTypes.Limit;

			case "market":
				return OrderTypes.Market;

			case "fillorkill":
				tif = TimeInForce.MatchOrCancel;
				return OrderTypes.Limit;

			case "stop":
			case "stoplimit":
				return OrderTypes.Conditional;

			case "trailingstop":
				isTrailing = true;
				return OrderTypes.Conditional;

			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
		}
	}

	public static string ToOrderType(this OrderTypes? type, TimeInForce? tif, PrizmBitOrderCondition condition)
	{
		switch (type)
		{
			case null:
			case OrderTypes.Limit:
			{
				return tif switch
				{
					null or TimeInForce.PutInQueue => "Limit",
					TimeInForce.MatchOrCancel => "FillOrKill",
					//case TimeInForce.CancelBalance:
					//	break;
					_ => throw new ArgumentOutOfRangeException(nameof(tif), tif, LocalizedStrings.InvalidValue),
				};
			}
			case OrderTypes.Market:
				return "Market";
			case OrderTypes.Conditional:
			{
				if (condition == null)
					throw new ArgumentNullException(nameof(condition));

				if (condition.IsStopLossTrailing)
					return "TrailingStop";

				if (condition.StopLossClosePositionPrice == null)
					return "Stop";

				return "StopLimit";
			}
			default:
				throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.InvalidValue);
		}
	}
}