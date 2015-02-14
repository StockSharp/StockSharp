namespace StockSharp.Transaq.Native
{
	using System;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	using StockSharp.Transaq.Native.Commands;
	using StockSharp.Transaq.Native.Responses;
	using StockSharp.Localization;

	static class TransaqHelper
	{
		public static NewOrderUnfilleds ToTransaq(this TimeInForce cond)
		{
			switch (cond)
			{
				case TimeInForce.CancelBalance:
					return NewOrderUnfilleds.CancelBalance;

				case TimeInForce.MatchOrCancel:
					return NewOrderUnfilleds.ImmOrCancel;

				case TimeInForce.PutInQueue:
					return NewOrderUnfilleds.PutInQueue;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static BuySells ToTransaq(this Sides side)
		{
			switch (side)
			{
				case Sides.Buy:
					return BuySells.B;

				case Sides.Sell:
					return BuySells.S;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static Sides FromTransaq(this BuySells op)
		{
			switch (op)
			{
				case BuySells.B:
					return Sides.Buy;

				case BuySells.S:
					return Sides.Sell;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		public static NewStopOrderElement CreateStopLoss(TransaqOrderCondition cond)
		{
			if (cond == null)
				throw new ArgumentNullException("cond");

			return new NewStopOrderElement
			{
				ActivationPrice = cond.StopLossActivationPrice,
				OrderPrice = cond.StopLossOrderPrice == null ? null : cond.StopLossOrderPrice.ToString(),
				ByMarket = cond.StopLossByMarket,
				Quantity = cond.StopLossVolume.ToString(),
				UseCredit = cond.StopLossUseCredit,
				GuardTime = cond.StopLossGuardTime,
				BrokerRef = cond.StopLossComment
			};
		}

		public static NewStopOrderElement CreateTakeProfit(TransaqOrderCondition cond)
		{
			if (cond == null)
				throw new ArgumentNullException("cond");

			return new NewStopOrderElement
			{
				ActivationPrice = cond.TakeProfitActivationPrice,
				ByMarket = cond.TakeProfitByMarket,
				Quantity = cond.TakeProfitVolume.ToString(),
				UseCredit = cond.TakeProfitUseCredit,
				GuardTime = cond.TakeProfitGuardTime,
				BrokerRef = cond.TakeProfitComment,
				Correction = cond.TakeProfitCorrection == null ? null : cond.TakeProfitCorrection.ToString(),
				Spread = cond.TakeProfitGuardSpread == null ? null : cond.TakeProfitGuardSpread.ToString()
			};
		}

		public static bool CheckConditionUnitType(this TransaqOrderCondition cond)
		{
			if (cond == null)
				throw new ArgumentNullException("cond");

			if ((cond.StopLossOrderPrice != null && cond.StopLossOrderPrice.Type != UnitTypes.Absolute & cond.StopLossOrderPrice.Type != UnitTypes.Percent) ||
				(cond.StopLossVolume != null && cond.StopLossVolume.Type != UnitTypes.Absolute & cond.StopLossVolume.Type != UnitTypes.Percent) ||
				(cond.TakeProfitVolume != null && cond.TakeProfitVolume.Type != UnitTypes.Absolute & cond.TakeProfitVolume.Type != UnitTypes.Percent) ||
				(cond.TakeProfitCorrection != null && cond.TakeProfitCorrection.Type != UnitTypes.Absolute & cond.TakeProfitCorrection.Type != UnitTypes.Percent) ||
				(cond.TakeProfitGuardSpread != null && cond.TakeProfitGuardSpread.Type != UnitTypes.Absolute & cond.TakeProfitGuardSpread.Type != UnitTypes.Percent))
			{
				return false;
			}

			return true;
		}

		public static OrderStates ToStockSharpState(this TransaqOrderStatus status)
		{
			switch (status)
			{
				//case TransaqOrderStatus.none:
				//	return OrderStates.None;

				case TransaqOrderStatus.active:
				case TransaqOrderStatus.wait:
				case TransaqOrderStatus.linkwait:
				case TransaqOrderStatus.watching:
				case TransaqOrderStatus.sl_guardtime:
				case TransaqOrderStatus.tp_guardtime:
				case TransaqOrderStatus.tp_correction:
				case TransaqOrderStatus.tp_correction_guardtime:
					return OrderStates.Active;

				case TransaqOrderStatus.forwarding:
				case TransaqOrderStatus.sl_forwarding:
				case TransaqOrderStatus.tp_forwarding:
					return OrderStates.Pending;

				case TransaqOrderStatus.rejected:
				case TransaqOrderStatus.refused:
				case TransaqOrderStatus.failed:
				case TransaqOrderStatus.denied:
				case TransaqOrderStatus.removed:
					return OrderStates.Failed;

				case TransaqOrderStatus.expired:
				case TransaqOrderStatus.disabled:
				case TransaqOrderStatus.cancelled:
				case TransaqOrderStatus.matched:
				case TransaqOrderStatus.sl_executed:
				case TransaqOrderStatus.tp_executed:
					return OrderStates.Done;

				default:
					return OrderStates.None;
			}
		}

		public static SecurityTypes? FromTransaq(this string type)
		{
			switch (type.ToUpperInvariant())
			{
				case "SHARE":
					return SecurityTypes.Stock;

				case "FUT":
				case "FOB":
					return SecurityTypes.Future;

				case "OPT":
					return SecurityTypes.Option;

				case "IDX":
					return SecurityTypes.Index;

				case "GKO":
				case "BOND":
					return SecurityTypes.Bond;

				case "ETS_SWAP":
					return SecurityTypes.Swap;

				case "ETS_CURRENCY":
				case "CURRENCY":
				case "MCT":
					return SecurityTypes.Currency;

				case "OIL":
				case "METAl":
					return SecurityTypes.Commodity;

				case "ERROR":
					throw new ArgumentException(LocalizedStrings.Str3569, "type");

				default:
					return null;
			}
		}

		public static OptionTypes FromTransaq(this SecInfoPutCalls type)
		{
			switch (type)
			{
				case SecInfoPutCalls.C:
					return OptionTypes.Call;
				case SecInfoPutCalls.P:
					return OptionTypes.Put;
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static string FixBoardName(this string board)
		{
			if (board.CompareIgnoreCase("FUT") || board.CompareIgnoreCase("OPT"))
				board = ExchangeBoard.Forts.Code;

			return board;
		}

		public static SecurityStates FromTransaq(this TransaqSecurityStatus status)
		{
			switch (status)
			{
				case TransaqSecurityStatus.A:
					return SecurityStates.Trading;
				case TransaqSecurityStatus.S:
					return SecurityStates.Stoped;
				default:
					throw new ArgumentOutOfRangeException("status");
			}
		}

		public static CurrencyTypes? ToCurrency(string name)
		{
			if (name.IsEmpty())
				return null;
			else if (name.CompareIgnoreCase("NA"))
				return null;
			else if (name.CompareIgnoreCase("RUR"))
				return CurrencyTypes.RUB;
			// http://stocksharp.com/forum/yaf_postst5355_API-4-2-25---Oshibka-i-zamiechaniia.aspx
			else if (name.CompareIgnoreCase("RURC"))
				return CurrencyTypes.RUB;
			else
				return name.To<CurrencyTypes>();
		}
	}
}