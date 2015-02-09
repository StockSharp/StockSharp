namespace StockSharp.CQG
{
	using System;

	using Ecng.Common;

	using global::CQG;

	using StockSharp.Messages;

	using StockSharp.Localization;

	static class Extensions
	{
		public static eOrderType ToCQG(this OrderTypes type, decimal? stopPrice)
		{
			switch (type)
			{
				case OrderTypes.Limit:
					return eOrderType.otLimit;
				case OrderTypes.Market:
					return eOrderType.otMarket;
				case OrderTypes.Conditional:
					return stopPrice == null ? eOrderType.otStop : eOrderType.otStopLimit;
				case OrderTypes.Repo:
				case OrderTypes.ExtRepo:
				case OrderTypes.Rps:
				case OrderTypes.Execute:
					throw new NotSupportedException();
				default:
					throw new ArgumentOutOfRangeException("type");
			}
		}

		public static eOrderSide ToCQG(this Sides side)
		{
			switch (side)
			{
				case Sides.Buy:
					return eOrderSide.osdBuy;
				case Sides.Sell:
					return eOrderSide.osdSell;
				default:
					throw new ArgumentOutOfRangeException("side");
			}
		}

		public static SecurityTypes ToStockSharp(this eInstrumentType type)
		{
			switch (type)
			{
				case eInstrumentType.itFuture:
					return SecurityTypes.Future;
				case eInstrumentType.itOptionPut:
					return SecurityTypes.Option;
				case eInstrumentType.itOptionCall:
					return SecurityTypes.Option;
				case eInstrumentType.itStock:
					return SecurityTypes.Stock;
				case eInstrumentType.itTreasure:
					return SecurityTypes.Bond;
				case eInstrumentType.itSyntheticStrategy:
					return SecurityTypes.Index;
				case eInstrumentType.itAllOptions:
					return SecurityTypes.Option;
				case eInstrumentType.itOther:
				case eInstrumentType.itUndefined:
				case eInstrumentType.itAllInstruments:
					throw new NotSupportedException(LocalizedStrings.UnsupportSecType.Put(type));
				default:
					throw new ArgumentOutOfRangeException("type", type, LocalizedStrings.Str1603);
			}
		}
	}
}