#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Messages.Messages
File: Level1ChangeMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Messages
{
	using System;
	using System.Linq;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Level1 fields of market-data.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum Level1Fields
	{
		/// <summary>
		/// Opening price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str79Key)]
		OpenPrice,

		/// <summary>
		/// Greatest price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str287Key)]
		HighPrice,

		/// <summary>
		/// Lowest price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str288Key)]
		LowPrice,

		/// <summary>
		/// Closing price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ClosingPriceKey)]
		ClosePrice,

		/// <summary>
		/// Last trade.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str289Key)]
		LastTrade,

		/// <summary>
		/// Step price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str290Key)]
		StepPrice,

		/// <summary>
		/// Best bid.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str291Key)]
		BestBid,

		/// <summary>
		/// Best ask.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str292Key)]
		BestAsk,

		/// <summary>
		/// Volatility (implied).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str293Key)]
		ImpliedVolatility,

		/// <summary>
		/// Theoretical price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str294Key)]
		TheorPrice,

		/// <summary>
		/// Open interest.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str150Key)]
		OpenInterest,

		/// <summary>
		/// Minimum price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str83Key)]
		MinPrice,

		/// <summary>
		/// Maximum price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str81Key)]
		MaxPrice,

		/// <summary>
		/// Bids volume.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str295Key)]
		BidsVolume,

		/// <summary>
		/// Number of bids.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str296Key)]
		BidsCount,

		/// <summary>
		/// Ask volume.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str297Key)]
		AsksVolume,

		/// <summary>
		/// Number of asks.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str298Key)]
		AsksCount,

		/// <summary>
		/// Volatility (historic).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str299Key)]
		HistoricalVolatility,

		/// <summary>
		/// Delta.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.DeltaKey)]
		Delta,

		/// <summary>
		/// Gamma.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.GammaKey)]
		Gamma,

		/// <summary>
		/// Vega.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.VegaKey)]
		Vega,

		/// <summary>
		/// Theta.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ThetaKey)]
		Theta,

		/// <summary>
		/// Initial margin (buy).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str304Key)]
		MarginBuy,

		/// <summary>
		/// Initial margin (sell).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str305Key)]
		MarginSell,

		/// <summary>
		/// Minimum price step.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str306Key)]
		PriceStep,

		/// <summary>
		/// Minimum volume step.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str307Key)]
		VolumeStep,

		/// <summary>
		/// Extended information.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.ExtendedInfoKey)]
		ExtensionInfo,

		/// <summary>
		/// State.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.StateKey)]
		State,

		/// <summary>
		/// Last trade price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str308Key)]
		LastTradePrice,

		/// <summary>
		/// Last trade volume.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str309Key)]
		LastTradeVolume,

		/// <summary>
		/// Volume per session.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str310Key)]
		Volume,

		/// <summary>
		/// Average price per session.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str311Key)]
		AveragePrice,

		/// <summary>
		/// Settlement price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str312Key)]
		SettlementPrice,

		/// <summary>
		/// Change,%.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Change,%")]
		Change,

		/// <summary>
		/// Best bid price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str313Key)]
		BestBidPrice,

		/// <summary>
		/// Best buy volume.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str314Key)]
		BestBidVolume,

		/// <summary>
		/// Best ask price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str315Key)]
		BestAskPrice,

		/// <summary>
		/// Best sell volume.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str316Key)]
		BestAskVolume,

		/// <summary>
		/// Rho.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.RhoKey)]
		Rho,

		/// <summary>
		/// Accrued coupon income (ACI).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str318Key)]
		AccruedCouponIncome,

		/// <summary>
		/// Maximum bid during the session.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str319Key)]
		HighBidPrice,

		/// <summary>
		/// Maximum ask during the session.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str320Key)]
		LowAskPrice,

		/// <summary>
		/// Yield.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str321Key)]
		Yield,

		/// <summary>
		/// Time of last trade.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str322Key)]
		LastTradeTime,

		/// <summary>
		/// Number of trades.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str323Key)]
		TradesCount,

		/// <summary>
		/// Average price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.AveragePriceKey)]
		VWAP,

		/// <summary>
		/// Last trade ID.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str325Key)]
		LastTradeId,

		/// <summary>
		/// Best bid time.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str326Key)]
		BestBidTime,

		/// <summary>
		/// Best ask time.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str327Key)]
		BestAskTime,

		/// <summary>
		/// Is tick ascending or descending in price.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str328Key)]
		LastTradeUpDown,

		/// <summary>
		/// Initiator of the last trade (buyer or seller).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str329Key)]
		LastTradeOrigin,

		/// <summary>
		/// Lot multiplier.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str330Key)]
		Multiplier,

		/// <summary>
		/// Price/profit.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("P/E")]
		PriceEarnings,

		/// <summary>
		/// Price target/profit.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Forward P/E")]
		ForwardPriceEarnings,

		/// <summary>
		/// Price/profit (increase).
		/// </summary>
		[EnumMember]
		[EnumDisplayName("PEG")]
		PriceEarningsGrowth,

		/// <summary>
		/// Price/buy.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("P/S")]
		PriceSales,

		/// <summary>
		/// Price/sell.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("P/B")]
		PriceBook,

		/// <summary>
		/// Price/amount.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("P/CF")]
		PriceCash,

		/// <summary>
		/// Price/amount (free).
		/// </summary>
		[EnumMember]
		[EnumDisplayName("P/FCF")]
		PriceFreeCash,

		/// <summary>
		/// Payments.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Payout")]
		Payout,

		/// <summary>
		/// Number of shares.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str331Key)]
		SharesOutstanding,

		/// <summary>
		/// Shares Float.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Shares Float")]
		SharesFloat,

		/// <summary>
		/// Float Short.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Float Short")]
		FloatShort,

		/// <summary>
		/// Short.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("Short")]
		ShortRatio,

		/// <summary>
		/// Return on assets.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("ROA")]
		ReturnOnAssets,

		/// <summary>
		/// Return on equity.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("ROE")]
		ReturnOnEquity,

		/// <summary>
		/// Return on investment.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("ROI")]
		ReturnOnInvestment,

		/// <summary>
		/// Liquidity (current).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str332Key)]
		CurrentRatio,

		/// <summary>
		/// Liquidity (instantaneous).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str333Key)]
		QuickRatio,

		/// <summary>
		/// Capital (longterm debt).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str334Key)]
		LongTermDebtEquity,

		/// <summary>
		/// Capital (debt).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str335Key)]
		TotalDebtEquity,

		/// <summary>
		/// Assets margin (gross).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str336Key)]
		GrossMargin,

		/// <summary>
		/// Assets margin.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str337Key)]
		OperatingMargin,

		/// <summary>
		/// Profit margin.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str338Key)]
		ProfitMargin,

		/// <summary>
		/// Beta.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.BetaKey)]
		Beta,

		/// <summary>
		/// ATR.
		/// </summary>
		[EnumMember]
		[EnumDisplayName("ATR")]
		AverageTrueRange,

		/// <summary>
		/// Volatility (week).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str340Key)]
		HistoricalVolatilityWeek,

		/// <summary>
		/// Volatility (month).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str341Key)]
		HistoricalVolatilityMonth,

		/// <summary>
		/// System info.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str342Key)]
		IsSystem,

		/// <summary>
		/// Number of digits in price after coma.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.DecimalsKey)]
		Decimals
	}

	/// <summary>
	/// The message containing the level1 market data.
	/// </summary>
	[DataContract]
	[Serializable]
	public class Level1ChangeMessage : BaseChangeMessage<Level1Fields>
	{
		/// <summary>
		/// Security ID.
		/// </summary>
		[DataMember]
		[DisplayNameLoc(LocalizedStrings.SecurityKey)]
		[DescriptionLoc(LocalizedStrings.SecurityIdKey, true)]
		[MainCategory]
		public SecurityId SecurityId { get; set; }

		/// <summary>
		/// Initializes a new instance of the <see cref="Level1ChangeMessage"/>.
		/// </summary>
		public Level1ChangeMessage()
			: base(MessageTypes.Level1Change)
		{
		}

		/// <summary>
		/// Create a copy of <see cref="Level1ChangeMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			var msg = new Level1ChangeMessage
			{
				LocalTime = LocalTime,
				SecurityId = SecurityId,
				ServerTime = ServerTime,
			};

			msg.Changes.AddRange(Changes);

			return msg;
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return base.ToString() + $",Sec={SecurityId},Changes={Changes.Select(c => c.ToString()).Join(",")}";
		}
	}
}