namespace StockSharp.OpenECry
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using OEC.Data;

	using StockSharp.Messages;
	using StockSharp.Localization;

	static class Extensions
	{
		public static SecurityTypes GetSecurityType(this OEC.API.Contract contract)
		{
			if (contract == null)
				throw new ArgumentNullException("contract");

			if (contract.IsCompound)
				return SecurityTypes.Index;

			if (contract.IsContinuous)
				return SecurityTypes.Future;

			if (contract.IsEquityAsset)
				return SecurityTypes.Stock;

			if (contract.IsForex)
				return SecurityTypes.Currency;

			if (contract.IsFuture)
				return SecurityTypes.Future;

			if (contract.IsOption)
				return SecurityTypes.Option;
			
			return SecurityTypes.Stock;
		}

		public static decimal Cast(this OEC.API.Contract contract, double value)
		{
			var d = value.SafeCast();

			if (d == 0)
				return 0;

			var priceStep = (decimal)contract.TickSize;
			return MathHelper.Round((decimal)value, priceStep, priceStep.GetCachedDecimals());
		}

		public static decimal SafeCast(this double value)
		{
			if (value.IsNaN())
				return 0;

			return (decimal)value;
		}

		public static decimal SafeCast(this float value)
		{
			if (value.IsNaN())
				return 0;

			return (decimal)value;
		}

		public static CurrencyTypes ToCurrency(this string str)
		{
			return str.Replace("*", string.Empty).To<CurrencyTypes>();
		}

		public static SecurityId ToSecurityId(this OEC.API.Contract contract)
		{
			return new SecurityId
			{
				SecurityCode = contract.Symbol,
				BoardCode = contract.Exchange.Name,
			};
		}

		/// <summary>
		/// Получить <see cref="SecurityStates"/> для инструмента, соответствующего контракту <paramref name="contract"/>.
		/// </summary>
		/// <param name="contract">Контракт OEC.</param>
		/// <returns>Состояние инструмента <see cref="SecurityStates"/>.</returns>
		public static SecurityStates GetSecurityState(this OEC.API.Contract contract)
		{
			var times = contract.GetWorkingTimesUtc();
			var now = DateTime.UtcNow.TimeOfDay;
			return times.IsEmpty() || times.Any(range => range.Contains(now)) ? SecurityStates.Trading : SecurityStates.Stoped;
		}

		/// <summary>
		/// Получить время торгов по инструменту.
		/// </summary>
		/// <param name="contract">Контракт OEC.</param>
		/// <returns>Список диапазонов времени дня (UTC), в течение которых инструмент торгуется.</returns>
		public static Range<TimeSpan>[] GetWorkingTimesUtc(this OEC.API.Contract contract)
		{
			// sometimes OEC returns timespans for working time > 24 hours (some internal oec date conversion problem)
			// all OEC StartTimes and StopTimes are in local timezone

			// convert back to UTC
			var offset = TimeZone.CurrentTimeZone.GetUtcOffset(DateTime.Now);
			var cstart = new TimeSpan(contract.StartTime.Hours + 24, contract.StartTime.Minutes, contract.StartTime.Seconds) -
			             offset;
			var cstop = new TimeSpan(contract.StopTime.Hours + 24, contract.StopTime.Minutes, contract.StopTime.Seconds) - offset;

			cstart = new TimeSpan(cstart.Hours, cstart.Minutes, cstart.Seconds);
			cstop = new TimeSpan(cstop.Hours, cstop.Minutes, cstop.Seconds);

			if (cstart == cstop)
				return new Range<TimeSpan>[0];

			if (cstart < cstop)
			{
				return new[] { new Range<TimeSpan>(cstart, cstop) };
			}

			var times = new List<Range<TimeSpan>>();
			var daybegin = new TimeSpan(0);

			if (cstop > daybegin)
				times.Add(new Range<TimeSpan>(daybegin, cstop));
			times.Add(new Range<TimeSpan>(cstart, new TimeSpan(24, 0, 0)));

			return times.ToArray();
		}

		public static OrderSide ToOec(this Sides od)
		{
			return od == Sides.Buy ? OrderSide.Buy : od == Sides.Sell ? OrderSide.Sell : OrderSide.None;
		}

		public static Sides ToStockSharp(this OrderSide os)
		{
			switch (os)
			{
				case OrderSide.Buy:
				case OrderSide.BuyToCover:
					return Sides.Buy;
				case OrderSide.Sell:
				case OrderSide.SellShort:
					return Sides.Sell;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static Level1Fields ToStockSharp(this TriggerType type)
		{
			switch (type)
			{
				case TriggerType.Last:
					return Level1Fields.LastTradePrice;
				case TriggerType.Bid:
					return Level1Fields.BestBidPrice;
				case TriggerType.Ask:
					return Level1Fields.BestAskPrice;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static TriggerType ToOec(this Level1Fields type)
		{
			switch (type)
			{
				case Level1Fields.LastTradePrice:
					return TriggerType.Last;
				case Level1Fields.BestBidPrice:
					return TriggerType.Bid;
				case Level1Fields.BestAskPrice:
					return TriggerType.Ask;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static OrderType ToOec(this OrderTypes type)
		{
			switch (type)
			{
				case OrderTypes.Limit:
					return OrderType.Limit;
				case OrderTypes.Market:
					return OrderType.Market;
				default:
					throw new ArgumentException(LocalizedStrings.Str2579Params.Put(type));
			}
		}

		public static OrderTypes ToStockSharp(this OrderType type)
		{
			switch (type)
			{
				case OrderType.Market:
					return OrderTypes.Market;
				case OrderType.Limit:
					return OrderTypes.Limit;
				case OrderType.Stop:
				case OrderType.StopLimit:
				case OrderType.MarketIfTouched:
				case OrderType.MarketToLimit:
				case OrderType.MarketOnOpen:
				case OrderType.MarketOnClose:
				case OrderType.MarketOnPitOpen:
				case OrderType.MarketOnPitClose:
				case OrderType.TrailingStopLoss:
				case OrderType.TrailingStopLimit:
					return OrderTypes.Conditional;
				case OrderType.Iceberg:
					return OrderTypes.Limit;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static string GetDescription(this FailReason reason)
		{
			switch (reason)
			{
				case FailReason.DataError:
					return LocalizedStrings.Str2580;
				case FailReason.Disabled:
					return LocalizedStrings.Str2581;
				case FailReason.DisconnectedByOwner:
					return LocalizedStrings.Str2582;
				case FailReason.Expired:
					return LocalizedStrings.Str2583;
				case FailReason.InvalidClientVersion:
					return LocalizedStrings.Str2584;
				case FailReason.InvalidUserOrPassword:
					return LocalizedStrings.Str2585;
				case FailReason.Locked:
					return LocalizedStrings.Str2586;
				case FailReason.SoftwareNotPermitted:
					return LocalizedStrings.Str2587;
				case FailReason.UserAlreadyConnected:
					return LocalizedStrings.Str2588;
			}

			return reason.ToString();
		}
	}
}