namespace StockSharp.Messages;

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
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OpenPriceKey)]
	OpenPrice,

	/// <summary>
	/// Highest price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.HighPriceKey)]
	HighPrice,

	/// <summary>
	/// Lowest price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LowPriceKey)]
	LowPrice,

	/// <summary>
	/// Closing price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ClosingPriceKey)]
	ClosePrice,

	/// <summary>
	/// Last trade.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LastTradeKey)]
	[Obsolete]
	LastTrade,

	/// <summary>
	/// Step price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StepPriceKey)]
	StepPrice,

	/// <summary>
	/// Best bid.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BestBidKey)]
	[Obsolete]
	BestBid,

	/// <summary>
	/// Best ask.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BestAskKey)]
	[Obsolete]
	BestAsk,

	/// <summary>
	/// Volatility (implied).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ImpliedVolatilityKey)]
	ImpliedVolatility,

	/// <summary>
	/// Theoretical price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TheorPriceKey)]
	TheorPrice,

	/// <summary>
	/// Open interest.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OpenInterestKey)]
	OpenInterest,

	/// <summary>
	/// Price (min).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PriceMinKey)]
	MinPrice,

	/// <summary>
	/// Price (max).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PriceMaxKey)]
	MaxPrice,

	/// <summary>
	/// Bids volume.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BidsVolumeKey)]
	BidsVolume,

	/// <summary>
	/// Number of bids.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BidsCountKey)]
	BidsCount,

	/// <summary>
	/// Ask volume.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AsksVolumeKey)]
	AsksVolume,

	/// <summary>
	/// Number of asks.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AsksCountKey)]
	AsksCount,

	/// <summary>
	/// Volatility (historical).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.HistoricalVolatilityKey)]
	HistoricalVolatility,

	/// <summary>
	/// Delta.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DeltaKey)]
	Delta,

	/// <summary>
	/// Gamma.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.GammaKey)]
	Gamma,

	/// <summary>
	/// Vega.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VegaKey)]
	Vega,

	/// <summary>
	/// Theta.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ThetaKey)]
	Theta,

	/// <summary>
	/// Initial margin to buy.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarginBuyKey)]
	MarginBuy,

	/// <summary>
	/// Initial margin to sell.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarginSellKey)]
	MarginSell,

	/// <summary>
	/// Minimum price step.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PriceStepKey)]
	PriceStep,

	/// <summary>
	/// Minimum volume step.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VolumeStepKey)]
	VolumeStep,

	/// <summary>
	/// Extended information.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ExtendedInfoKey)]
	[Obsolete]
	ExtensionInfo,

	/// <summary>
	/// State.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StateKey)]
	State,

	/// <summary>
	/// Last trade price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LastTradePriceKey)]
	LastTradePrice,

	/// <summary>
	/// Last trade volume.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LastTradeVolumeKey)]
	LastTradeVolume,

	/// <summary>
	/// Volume per session.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VolumePerSessionKey)]
	Volume,

	/// <summary>
	/// Average price per session.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AveragePricePerSessionKey)]
	AveragePrice,

	/// <summary>
	/// Settlement price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SettlementPriceKey)]
	SettlementPrice,

	/// <summary>
	/// Change,%.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ChangeKey)]
	Change,

	/// <summary>
	/// Best bid price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BestBidPriceKey)]
	BestBidPrice,

	/// <summary>
	/// Best buy volume.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BestBidVolumeKey)]
	BestBidVolume,

	/// <summary>
	/// Best ask price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BestAskPriceKey)]
	BestAskPrice,

	/// <summary>
	/// Best sell volume.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BestAskVolumeKey)]
	BestAskVolume,

	/// <summary>
	/// Rho.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RhoKey)]
	Rho,

	/// <summary>
	/// Accrued coupon income (ACI).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AccruedCouponIncomeKey)]
	AccruedCouponIncome,

	/// <summary>
	/// Maximum bid during the session.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BidMaxKey)]
	HighBidPrice,

	/// <summary>
	/// Minimum ask during the session.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AskMinKey)]
	LowAskPrice,

	/// <summary>
	/// Yield.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.YieldKey)]
	Yield,

	/// <summary>
	/// Time of last trade.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LastTradeTimeKey)]
	LastTradeTime,

	/// <summary>
	/// Number of trades.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NumOfTradesKey)]
	TradesCount,

	/// <summary>
	/// Average price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AveragePriceKey)]
	VWAP,

	/// <summary>
	/// Last trade ID.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LastTradeIdKey)]
	LastTradeId,

	/// <summary>
	/// Best bid time.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BestBidTimeKey)]
	BestBidTime,

	/// <summary>
	/// Best ask time.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BestAskTimeKey)]
	BestAskTime,

	/// <summary>
	/// Is tick ascending or descending in price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TrendKey)]
	LastTradeUpDown,

	/// <summary>
	/// Initiator of the last trade (buyer or seller).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.InitiatorKey)]
	LastTradeOrigin,

	/// <summary>
	/// Lot multiplier.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LotKey)]
	Multiplier,

	/// <summary>
	/// Price/profit.
	/// </summary>
	[EnumMember]
	[Display(Name = "P/E")]
	PriceEarnings,

	/// <summary>
	/// Price target/profit.
	/// </summary>
	[EnumMember]
	[Display(Name = "Forward P/E")]
	ForwardPriceEarnings,

	/// <summary>
	/// Price/profit (increase).
	/// </summary>
	[EnumMember]
	[Display(Name = "PEG")]
	PriceEarningsGrowth,

	/// <summary>
	/// Price/buy.
	/// </summary>
	[EnumMember]
	[Display(Name = "P/S")]
	PriceSales,

	/// <summary>
	/// Price/sell.
	/// </summary>
	[EnumMember]
	[Display(Name = "P/B")]
	PriceBook,

	/// <summary>
	/// Price/amount.
	/// </summary>
	[EnumMember]
	[Display(Name = "P/CF")]
	PriceCash,

	/// <summary>
	/// Price/amount (free).
	/// </summary>
	[EnumMember]
	[Display(Name = "P/FCF")]
	PriceFreeCash,

	/// <summary>
	/// Payments.
	/// </summary>
	[EnumMember]
	[Display(Name = "Payout")]
	Payout,

	/// <summary>
	/// Number of shares.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NumOfSharesKey)]
	SharesOutstanding,

	/// <summary>
	/// Shares Float.
	/// </summary>
	[EnumMember]
	[Display(Name = "Shares Float")]
	SharesFloat,

	/// <summary>
	/// Float Short.
	/// </summary>
	[EnumMember]
	[Display(Name = "Float Short")]
	FloatShort,

	/// <summary>
	/// Short.
	/// </summary>
	[EnumMember]
	[Display(Name = "Short")]
	ShortRatio,

	/// <summary>
	/// Return on assets.
	/// </summary>
	[EnumMember]
	[Display(Name = "ROA")]
	ReturnOnAssets,

	/// <summary>
	/// Return on equity.
	/// </summary>
	[EnumMember]
	[Display(Name = "ROE")]
	ReturnOnEquity,

	/// <summary>
	/// Return on investment.
	/// </summary>
	[EnumMember]
	[Display(Name = "ROI")]
	ReturnOnInvestment,

	/// <summary>
	/// Liquidity (current).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CurrentRatioKey)]
	CurrentRatio,

	/// <summary>
	/// Liquidity (instantaneous).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.QuickRatioKey)]
	QuickRatio,

	/// <summary>
	/// Capital (long-term debt).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LongTermDebtEquityKey)]
	LongTermDebtEquity,

	/// <summary>
	/// Capital (debt).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TotalDebtEquityKey)]
	TotalDebtEquity,

	/// <summary>
	/// Assets margin (gross).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.GrossMarginKey)]
	GrossMargin,

	/// <summary>
	/// Assets margin.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OperatingMarginKey)]
	OperatingMargin,

	/// <summary>
	/// Profit margin.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ProfitMarginKey)]
	ProfitMargin,

	/// <summary>
	/// Beta.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BetaKey)]
	Beta,

	/// <summary>
	/// ATR.
	/// </summary>
	[EnumMember]
	[Display(Name = "ATR")]
	AverageTrueRange,

	/// <summary>
	/// Volatility (week).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VolatilityWeekKey)]
	HistoricalVolatilityWeek,

	/// <summary>
	/// Volatility (month).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VolatilityMonthKey)]
	HistoricalVolatilityMonth,

	/// <summary>
	/// System info.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SystemKey)]
	IsSystem,

	/// <summary>
	/// Number of digits in price after coma.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DecimalsKey)]
	Decimals,

	/// <summary>
	/// Duration.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DurationKey)]
	Duration,

	/// <summary>
	/// Number of issued contracts.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IssueSizeKey)]
	IssueSize,

	/// <summary>
	/// BuyBack date.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BuyBackDateKey)]
	BuyBackDate,

	/// <summary>
	/// BuyBack price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BuyBackPriceKey)]
	BuyBackPrice,

	/// <summary>
	/// Turnover.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TurnoverKey)]
	Turnover,

	/// <summary>
	/// The middle of spread.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SpreadKey)]
	SpreadMiddle,

	/// <summary>
	/// The dividend amount on shares.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DividendKey)]
	Dividend,

	/// <summary>
	/// Price after split.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AfterSplitKey)]
	AfterSplit,

	/// <summary>
	/// Price before split.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BeforeSplitKey)]
	BeforeSplit,

	/// <summary>
	/// Commission (taker).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CommissionTakerKey)]
	CommissionTaker,

	/// <summary>
	/// Commission (maker).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CommissionMakerKey)]
	CommissionMaker,

	/// <summary>
	/// Minimum volume allowed in order.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MinVolumeKey)]
	MinVolume,

	/// <summary>
	/// Minimum volume allowed in order for underlying security.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UnderlyingMinVolumeKey)]
	UnderlyingMinVolume,

	/// <summary>
	/// Coupon value.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CouponValueKey)]
	CouponValue,

	/// <summary>
	/// Coupon date.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CouponDateKey)]
	CouponDate,

	/// <summary>
	/// Coupon period.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CouponPeriodKey)]
	CouponPeriod,

	/// <summary>
	/// Market price (yesterday).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarketPriceYesterdayKey)]
	MarketPriceYesterday,

	/// <summary>
	/// Market price (today).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarketPriceTodayKey)]
	MarketPriceToday,

	/// <summary>
	/// VWAP (prev).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VWAPPrevKey)]
	VWAPPrev,

	/// <summary>
	/// Yield by VWAP.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.YieldVWAPKey)]
	YieldVWAP,

	/// <summary>
	/// Yield by VWAP (prev).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.YieldVWAPPrevKey)]
	YieldVWAPPrev,

	/// <summary>
	/// Index.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IndexKey)]
	Index,

	/// <summary>
	/// Imbalance.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ImbalanceKey)]
	Imbalance,

	/// <summary>
	/// Underlying price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UnderlyingKey)]
	UnderlyingPrice,

	/// <summary>
	/// Maximum volume allowed in order.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxVolumeKey)]
	MaxVolume,

	/// <summary>
	/// Lowest bid during the session.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LowBidPriceKey, Description = LocalizedStrings.LowBidPriceDescKey)]
	LowBidPrice,

	/// <summary>
	/// Highest ask during the session.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.HighAskPriceKey, Description = LocalizedStrings.HighAskPriceDescKey)]
	HighAskPrice,

	/// <summary>
	/// Lowest last trade volume.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LastTradeVolumeLowKey, Description = LocalizedStrings.LastTradeVolumeLowDescKey)]
	LastTradeVolumeLow,

	/// <summary>
	/// Highest last trade volume.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LastTradeVolumeHighKey, Description = LocalizedStrings.LastTradeVolumeHighDescKey)]
	LastTradeVolumeHigh,

	/// <summary>
	/// Option margin leverage.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OptionMarginKey, Description = LocalizedStrings.OptionMarginDescKey)]
	OptionMargin,

	/// <summary>
	/// Synthetic option position margin leverage.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OptionSyntheticMarginKey, Description = LocalizedStrings.OptionSyntheticMarginDescKey)]
	OptionSyntheticMargin,

	/// <summary>
	/// Volume of the lowest bid.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LowBidVolumeKey, Description = LocalizedStrings.LowBidVolumeDescKey)]
	LowBidVolume,
	
	/// <summary>
	/// Volume of the highest ask.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.HighAskVolumeKey, Description = LocalizedStrings.HighAskVolumeDescKey)]
	HighAskVolume,
	
	/// <summary>
	/// Underlying asset best bid price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UnderlyingBestBidPriceKey, Description = LocalizedStrings.UnderlyingBestBidPriceDescKey)]
	UnderlyingBestBidPrice,
	
	/// <summary>
	/// Underlying asset best ask price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UnderlyingBestAskPriceKey, Description = LocalizedStrings.UnderlyingBestAskPriceDescKey)]
	UnderlyingBestAskPrice,
	
	/// <summary>
	/// Median price.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MedianKey, Description = LocalizedStrings.MedianPriceKey)]
	MedianPrice,
	
	/// <summary>
	/// The highest price for 52 weeks.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.HighPrice52WeekKey, Description = LocalizedStrings.HighPrice52WeekDescKey)]
	HighPrice52Week,
	
	/// <summary>
	/// The lowest price for 52 weeks.
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LowPrice52WeekKey, Description = LocalizedStrings.LowPrice52WeekDescKey)]
	LowPrice52Week,
	
	/// <summary>
	/// Last trade ID (string).
	/// </summary>
	[EnumMember]
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LastTradeStringIdKey, Description = LocalizedStrings.LastTradeStringIdDescKey)]
	LastTradeStringId,
}

/// <summary>
/// The message containing the level1 market data.
/// </summary>
[DataContract]
[Serializable]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.Level1Key,
	Description = LocalizedStrings.Level1MarketDataKey)]
public class Level1ChangeMessage : BaseChangeMessage<Level1ChangeMessage, Level1Fields>,
	ISecurityIdMessage, ISeqNumMessage
{
	/// <inheritdoc />
	[DataMember]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityKey,
		Description = LocalizedStrings.SecurityIdKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public SecurityId SecurityId { get; set; }

	/// <inheritdoc />
	[DataMember]
	public long SeqNum { get; set; }

	/// <inheritdoc />
	public override DataType DataType => DataType.Level1;

	/// <summary>
	/// Initializes a new instance of the <see cref="Level1ChangeMessage"/>.
	/// </summary>
	public Level1ChangeMessage()
		: base(MessageTypes.Level1Change)
	{
	}

	/// <inheritdoc />
	public override void CopyTo(Level1ChangeMessage destination)
	{
		base.CopyTo(destination);

		destination.SecurityId = SecurityId;
		destination.SeqNum = SeqNum;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		var str = base.ToString() + $",Sec={SecurityId},Changes={Changes.Select(c => c.ToString()).JoinComma()}";

		if (SeqNum != default)
			str += $",SQ={SeqNum}";

		return str;
	}
}