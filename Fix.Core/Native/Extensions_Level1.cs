namespace StockSharp.Fix.Native;

static partial class Extensions
{
	/// <summary>
	/// <see cref="Level1Fields.PriceStep"/>.
	/// </summary>
	public const char PriceStep = '#';
	/// <summary>
	/// <see cref="Level1Fields.VolumeStep"/>.
	/// </summary>
	public const char VolumeStep = '&';
	/// <summary>
	/// <see cref="Level1Fields.Multiplier"/>.
	/// </summary>
	public const char Multiplier = '@';
	/// <summary>
	/// <see cref="Level1Fields.StepPrice"/>.
	/// </summary>
	public const char StepPrice = '$';
	/// <summary>
	/// <see cref="Level1Fields.MarginBuy"/>.
	/// </summary>
	public const char MarginBuy = '<';
	/// <summary>
	/// <see cref="Level1Fields.MarginSell"/>.
	/// </summary>
	public const char MarginSell = '>';
	/// <summary>
	/// <see cref="Level1Fields.ImpliedVolatility"/>.
	/// </summary>
	public const char ImpliedVolatility = '/';
	/// <summary>
	/// <see cref="Level1Fields.TheorPrice"/>.
	/// </summary>
	public const char TheorPrice = '\\';
	/// <summary>
	/// <see cref="Level1Fields.LastTradePrice"/>.
	/// </summary>
	public const char LastTradePrice = 'p';
	/// <summary>
	/// <see cref="Level1Fields.LastTradeVolume"/>.
	/// </summary>
	public const char LastTradeVolume = 'v';
	/// <summary>
	/// <see cref="Level1Fields.BestBidPrice"/>.
	/// </summary>
	public const char BestBidPrice = 'b';
	/// <summary>
	/// <see cref="Level1Fields.BestBidVolume"/>.
	/// </summary>
	public const char BestBidVolume = 'j';
	/// <summary>
	/// <see cref="Level1Fields.BestAskPrice"/>.
	/// </summary>
	public const char BestAskPrice = 'c';
	/// <summary>
	/// <see cref="Level1Fields.BestAskVolume"/>.
	/// </summary>
	public const char BestAskVolume = 'm';
	/// <summary>
	/// <see cref="Level1Fields.BestBidTime"/>.
	/// </summary>
	public const char BestBidTime = 'h';
	/// <summary>
	/// <see cref="Level1Fields.BestAskTime"/>.
	/// </summary>
	public const char BestAskTime = 'K';
	/// <summary>
	/// <see cref="Level1Fields.BidsCount"/>.
	/// </summary>
	public const char BidsCount = 'q';
	/// <summary>
	/// <see cref="Level1Fields.BidsVolume"/>.
	/// </summary>
	public const char BidsVolume = 'w';
	/// <summary>
	/// <see cref="Level1Fields.AsksVolume"/>.
	/// </summary>
	public const char AsksVolume = 'e';
	/// <summary>
	/// <see cref="Level1Fields.AsksCount"/>.
	/// </summary>
	public const char AsksCount = 'r';
	/// <summary>
	/// <see cref="Level1Fields.Change"/>.
	/// </summary>
	public const char Change = 't';
	/// <summary>
	/// <see cref="Level1Fields.MaxPrice"/>.
	/// </summary>
	public const char MaxPrice = 'y';
	/// <summary>
	/// <see cref="Level1Fields.MinPrice"/>.
	/// </summary>
	public const char MinPrice = 'u';
	//public const char Vwap = 'i';
	/// <summary>
	/// <see cref="Level1Fields.Yield"/>.
	/// </summary>
	public const char Yield = 'o';
	/// <summary>
	/// <see cref="Level1Fields.AccruedCouponIncome"/>.
	/// </summary>
	public const char AccruedCouponIncome = 's';
	/// <summary>
	/// <see cref="Level1Fields.TradesCount"/>.
	/// </summary>
	public const char TradesCount = 'd';
	/// <summary>
	/// <see cref="Level1Fields.State"/>.
	/// </summary>
	public const char State = 'f';

	/// <summary>
	/// <see cref="Level1Fields.LastTradeId"/>.
	/// </summary>
	public const char LastTradeId = 'U';
	/// <summary>
	/// <see cref="Level1Fields.LastTradeTime"/>.
	/// </summary>
	public const char LastTradeTime = 'P';
	/// <summary>
	/// <see cref="Level1Fields.LastTradeOrigin"/>.
	/// </summary>
	public const char LastTradeOrigin = 'T';
	/// <summary>
	/// <see cref="Level1Fields.LastTradeUpDown"/>.
	/// </summary>
	public const char LastTradeUpDown = 'D';
	/// <summary>
	/// <see cref="Level1Fields.ShortRatio"/>.
	/// </summary>
	public const char ShortRatio = 'E';
	/// <summary>
	/// <see cref="Level1Fields.AverageTrueRange"/>.
	/// </summary>
	public const char AverageTrueRange = 'F';
	/// <summary>
	/// <see cref="Level1Fields.AveragePrice"/>.
	/// </summary>
	public const char AveragePrice = 'G';
	/// <summary>
	/// <see cref="Level1Fields.CurrentRatio"/>.
	/// </summary>
	public const char CurrentRatio = '~';
	/// <summary>
	/// <see cref="Level1Fields.SharesFloat"/>.
	/// </summary>
	public const char SharesFloat = 'Z';

	/// <summary>
	/// <see cref="Level1Fields.Duration"/>.
	/// </summary>
	public const char Duration = 'A';
	/// <summary>
	/// <see cref="Level1Fields.IssueSize"/>.
	/// </summary>
	public const char IssueSize = 'k';
	/// <summary>
	/// <see cref="Level1Fields.BuyBackDate"/>.
	/// </summary>
	public const char BuyBackDate = 'Q';
	/// <summary>
	/// <see cref="Level1Fields.BuyBackPrice"/>.
	/// </summary>
	public const char BuyBackPrice = 'g';
	/// <summary>
	/// <see cref="Level1Fields.Turnover"/>.
	/// </summary>
	public const char Turnover = 'a';

	/// <summary>
	/// <see cref="Level1Fields.OperatingMargin"/>.
	/// </summary>
	public const char OperatingMargin = 'M';
	/// <summary>
	/// <see cref="Level1Fields.GrossMargin"/>.
	/// </summary>
	public const char GrossMargin = 'L';

	/// <summary>
	/// <see cref="Level1Fields.Dividend"/>.
	/// </summary>
	public const char Dividend = '(';

	/// <summary>
	/// <see cref="Level1Fields.CouponValue"/>.
	/// </summary>
	public const char CouponValue = ')';
	/// <summary>
	/// <see cref="Level1Fields.CouponDate"/>.
	/// </summary>
	public const char CouponDate = 'l';
	/// <summary>
	/// <see cref="Level1Fields.CouponPeriod"/>.
	/// </summary>
	public const char CouponPeriod = '?';
	/// <summary>
	/// <see cref="Level1Fields.MarketPriceYesterday"/>.
	/// </summary>
	public const char MarketPriceYesterday = 'Y';
	/// <summary>
	/// <see cref="Level1Fields.MarketPriceToday"/>.
	/// </summary>
	public const char MarketPriceToday = '+';
	/// <summary>
	/// <see cref="Level1Fields.VWAPPrev"/>.
	/// </summary>
	public const char VWAPPrev = 'S';
	/// <summary>
	/// <see cref="Level1Fields.YieldVWAP"/>.
	/// </summary>
	public const char YieldVWAP = '-';
	/// <summary>
	/// <see cref="Level1Fields.YieldVWAPPrev"/>.
	/// </summary>
	public const char YieldVWAPPrev = 'X';

	/// <summary>
	/// <see cref="Level1Fields.HistoricalVolatility"/>.
	/// </summary>
	public const char HistoricalVolatility = (char)128;
	/// <summary>
	/// <see cref="Level1Fields.Delta"/>.
	/// </summary>
	public const char Delta = (char)129;
	/// <summary>
	/// <see cref="Level1Fields.Gamma"/>.
	/// </summary>
	public const char Gamma = (char)130;
	/// <summary>
	/// <see cref="Level1Fields.Vega"/>.
	/// </summary>
	public const char Vega = (char)131;
	/// <summary>
	/// <see cref="Level1Fields.Theta"/>.
	/// </summary>
	public const char Theta = (char)132;
	/// <summary>
	/// <see cref="Level1Fields.Rho"/>.
	/// </summary>
	public const char Rho = (char)133;
	/// <summary>
	/// <see cref="Level1Fields.PriceEarnings"/>.
	/// </summary>
	public const char PriceEarnings = (char)134;
	/// <summary>
	/// <see cref="Level1Fields.ForwardPriceEarnings"/>.
	/// </summary>
	public const char ForwardPriceEarnings = (char)135;
	/// <summary>
	/// <see cref="Level1Fields.PriceEarningsGrowth"/>.
	/// </summary>
	public const char PriceEarningsGrowth = (char)136;
	/// <summary>
	/// <see cref="Level1Fields.PriceSales"/>.
	/// </summary>
	public const char PriceSales = (char)137;
	/// <summary>
	/// <see cref="Level1Fields.PriceBook"/>.
	/// </summary>
	public const char PriceBook = (char)138;
	/// <summary>
	/// <see cref="Level1Fields.PriceCash"/>.
	/// </summary>
	public const char PriceCash = (char)139;
	/// <summary>
	/// <see cref="Level1Fields.PriceFreeCash"/>.
	/// </summary>
	public const char PriceFreeCash = (char)140;
	/// <summary>
	/// <see cref="Level1Fields.Payout"/>.
	/// </summary>
	public const char Payout = (char)141;
	/// <summary>
	/// <see cref="Level1Fields.SharesOutstanding"/>.
	/// </summary>
	public const char SharesOutstanding = (char)142;
	/// <summary>
	/// <see cref="Level1Fields.FloatShort"/>.
	/// </summary>
	public const char FloatShort = (char)143;
	/// <summary>
	/// <see cref="Level1Fields.ReturnOnAssets"/>.
	/// </summary>
	public const char ReturnOnAssets = (char)144;
	/// <summary>
	/// <see cref="Level1Fields.ReturnOnEquity"/>.
	/// </summary>
	public const char ReturnOnEquity = (char)145;
	/// <summary>
	/// <see cref="Level1Fields.ReturnOnInvestment"/>.
	/// </summary>
	public const char ReturnOnInvestment = (char)146;
	/// <summary>
	/// <see cref="Level1Fields.QuickRatio"/>.
	/// </summary>
	public const char QuickRatio = (char)147;
	/// <summary>
	/// <see cref="Level1Fields.LongTermDebtEquity"/>.
	/// </summary>
	public const char LongTermDebtEquity = (char)148;
	/// <summary>
	/// <see cref="Level1Fields.TotalDebtEquity"/>.
	/// </summary>
	public const char TotalDebtEquity = (char)149;
	/// <summary>
	/// <see cref="Level1Fields.ProfitMargin"/>.
	/// </summary>
	public const char ProfitMargin = (char)150;
	/// <summary>
	/// <see cref="Level1Fields.Beta"/>.
	/// </summary>
	public const char Beta = (char)151;
	/// <summary>
	/// <see cref="Level1Fields.HistoricalVolatilityWeek"/>.
	/// </summary>
	public const char HistoricalVolatilityWeek = (char)152;
	/// <summary>
	/// <see cref="Level1Fields.HistoricalVolatilityMonth"/>.
	/// </summary>
	public const char HistoricalVolatilityMonth = (char)153;
	/// <summary>
	/// <see cref="Level1Fields.IsSystem"/>.
	/// </summary>
	public const char IsSystem = (char)154;
	/// <summary>
	/// <see cref="Level1Fields.Decimals"/>.
	/// </summary>
	public const char Decimals = (char)155;
	/// <summary>
	/// <see cref="Level1Fields.AfterSplit"/>.
	/// </summary>
	public const char AfterSplit = (char)156;
	/// <summary>
	/// <see cref="Level1Fields.BeforeSplit"/>.
	/// </summary>
	public const char BeforeSplit = (char)157;
	/// <summary>
	/// <see cref="Level1Fields.CommissionTaker"/>.
	/// </summary>
	public const char CommissionTaker = (char)158;
	/// <summary>
	/// <see cref="Level1Fields.CommissionMaker"/>.
	/// </summary>
	public const char CommissionMaker = (char)159;
	/// <summary>
	/// <see cref="Level1Fields.MinVolume"/>.
	/// </summary>
	public const char MinVolume = (char)160;
	/// <summary>
	/// <see cref="Level1Fields.UnderlyingMinVolume"/>.
	/// </summary>
	public const char UnderlyingMinVolume = (char)161;
	/// <summary>
	/// <see cref="Level1Fields.Index"/>.
	/// </summary>
	public const char Index = (char)162;
	/// <summary>
	/// <see cref="Level1Fields.Imbalance"/>.
	/// </summary>
	public const char Imbalance = (char)163;
	/// <summary>
	/// <see cref="Level1Fields.UnderlyingPrice"/>.
	/// </summary>
	public const char UnderlyingPrice = (char)164;
	/// <summary>
	/// <see cref="Level1Fields.MaxVolume"/>.
	/// </summary>
	public const char MaxVolume = (char)165;
	/// <summary>
	/// <see cref="Level1Fields.LowBidPrice"/>.
	/// </summary>
	public const char LowBidPrice = (char)166;
	/// <summary>
	/// <see cref="Level1Fields.HighAskPrice"/>.
	/// </summary>
	public const char HighAskPrice = (char)167;
	/// <summary>
	/// <see cref="Level1Fields.LastTradeVolumeLow"/>.
	/// </summary>
	public const char LastTradeVolumeLow = (char)168;
	/// <summary>
	/// <see cref="Level1Fields.LastTradeVolumeHigh"/>.
	/// </summary>
	public const char LastTradeVolumeHigh = (char)169;
	/// <summary>
	/// <see cref="Level1Fields.LowBidVolume"/>.
	/// </summary>
	public const char LowBidVolume = (char)170;
	/// <summary>
	/// <see cref="Level1Fields.HighAskVolume"/>.
	/// </summary>
	public const char HighAskVolume = (char)171;
	/// <summary>
	/// <see cref="Level1Fields.UnderlyingBestBidPrice"/>.
	/// </summary>
	public const char UnderlyingBestBidPrice = (char)172;
	/// <summary>
	/// <see cref="Level1Fields.UnderlyingBestAskPrice"/>.
	/// </summary>
	public const char UnderlyingBestAskPrice = (char)173;
	/// <summary>
	/// <see cref="Level1Fields.MedianPrice"/>.
	/// </summary>
	public const char MedianPrice = (char)174;
	/// <summary>
	/// <see cref="Level1Fields.HighPrice52Week"/>.
	/// </summary>
	public const char HighPrice52Week = (char)175;
	/// <summary>
	/// <see cref="Level1Fields.LowPrice52Week"/>.
	/// </summary>
	public const char LowPrice52Week = (char)176;
	/// <summary>
	/// <see cref="Level1Fields.LastTradeStringId"/>.
	/// </summary>
	public const char LastTradeStringId = (char)177;

	/// <summary>
	/// Convert <see cref="Level1Fields"/> to <see cref="MDEntryType"/> value.
	/// </summary>
	/// <param name="field"><see cref="Level1Fields"/> value.</param>
	/// <returns><see cref="MDEntryType"/> value.</returns>
	public static char ToFix(this Level1Fields field)
	{
		return field switch
		{
			Level1Fields.OpenPrice => MDEntryType.OpeningPrice,
			Level1Fields.HighPrice => MDEntryType.TradingSessionHighPrice,
			Level1Fields.LowPrice => MDEntryType.TradingSessionLowPrice,
			Level1Fields.ClosePrice => MDEntryType.ClosingPrice,
			Level1Fields.StepPrice => StepPrice,
			Level1Fields.ImpliedVolatility => ImpliedVolatility,
			Level1Fields.TheorPrice => TheorPrice,
			Level1Fields.HistoricalVolatility => HistoricalVolatility,
			Level1Fields.Delta => Delta,
			Level1Fields.Gamma => Gamma,
			Level1Fields.Vega => Vega,
			Level1Fields.Theta => Theta,
			Level1Fields.Rho => Rho,
			Level1Fields.OpenInterest => MDEntryType.OpenInterest,
			Level1Fields.MinPrice => MinPrice,
			Level1Fields.MaxPrice => MaxPrice,
			Level1Fields.BidsVolume => BidsVolume,
			Level1Fields.BidsCount => BidsCount,
			Level1Fields.AsksVolume => AsksVolume,
			Level1Fields.AsksCount => AsksCount,
			Level1Fields.MarginBuy => MarginBuy,
			Level1Fields.MarginSell => MarginSell,
			Level1Fields.PriceStep => PriceStep,
			Level1Fields.VolumeStep => VolumeStep,
			Level1Fields.Multiplier => Multiplier,
			Level1Fields.State => State,
			Level1Fields.LastTradeId => LastTradeId,
			Level1Fields.LastTradeOrigin => LastTradeOrigin,
			Level1Fields.LastTradeTime => LastTradeTime,
			Level1Fields.LastTradeUpDown => LastTradeUpDown,
			Level1Fields.LastTradePrice => LastTradePrice,
			Level1Fields.LastTradeVolume => LastTradeVolume,
			Level1Fields.BestBidPrice => BestBidPrice,
			Level1Fields.BestBidVolume => BestBidVolume,
			Level1Fields.BestAskPrice => BestAskPrice,
			Level1Fields.BestAskVolume => BestAskVolume,
			Level1Fields.BestBidTime => BestBidTime,
			Level1Fields.BestAskTime => BestAskTime,
			Level1Fields.Change => Change,
			Level1Fields.HighBidPrice => MDEntryType.SessionHighBid,
			Level1Fields.LowAskPrice => MDEntryType.SessionLowOffer,
			Level1Fields.VWAP => MDEntryType.TradingSessionVWAPPrice,
			Level1Fields.Yield => Yield,
			Level1Fields.AccruedCouponIncome => AccruedCouponIncome,
			Level1Fields.SettlementPrice => MDEntryType.SettlementPrice,
			Level1Fields.Volume => MDEntryType.TradeVolume,
			Level1Fields.TradesCount => TradesCount,
			Level1Fields.ShortRatio => ShortRatio,
			Level1Fields.AverageTrueRange => AverageTrueRange,
			Level1Fields.AveragePrice => AveragePrice,
			Level1Fields.CurrentRatio => CurrentRatio,
			Level1Fields.SharesFloat => SharesFloat,
			Level1Fields.Duration => Duration,
			Level1Fields.IssueSize => IssueSize,
			Level1Fields.BuyBackPrice => BuyBackPrice,
			Level1Fields.BuyBackDate => BuyBackDate,
			Level1Fields.Turnover => Turnover,
			Level1Fields.OperatingMargin => OperatingMargin,
			Level1Fields.GrossMargin => GrossMargin,
			Level1Fields.Dividend => Dividend,
			Level1Fields.CouponDate => CouponDate,
			Level1Fields.CouponPeriod => CouponPeriod,
			Level1Fields.CouponValue => CouponValue,
			Level1Fields.MarketPriceToday => MarketPriceToday,
			Level1Fields.MarketPriceYesterday => MarketPriceYesterday,
			Level1Fields.VWAPPrev => VWAPPrev,
			Level1Fields.YieldVWAP => YieldVWAP,
			Level1Fields.YieldVWAPPrev => YieldVWAPPrev,
			Level1Fields.SpreadMiddle => MDEntryType.MidPrice,
			Level1Fields.PriceEarnings => PriceEarnings,
			Level1Fields.ForwardPriceEarnings => ForwardPriceEarnings,
			Level1Fields.PriceEarningsGrowth => PriceEarningsGrowth,
			Level1Fields.PriceSales => PriceSales,
			Level1Fields.PriceBook => PriceBook,
			Level1Fields.PriceCash => PriceCash,
			Level1Fields.PriceFreeCash => PriceFreeCash,
			Level1Fields.Payout => Payout,
			Level1Fields.SharesOutstanding => SharesOutstanding,
			Level1Fields.FloatShort => FloatShort,
			Level1Fields.ReturnOnAssets => ReturnOnAssets,
			Level1Fields.ReturnOnEquity => ReturnOnEquity,
			Level1Fields.ReturnOnInvestment => ReturnOnInvestment,
			Level1Fields.QuickRatio => QuickRatio,
			Level1Fields.LongTermDebtEquity => LongTermDebtEquity,
			Level1Fields.TotalDebtEquity => TotalDebtEquity,
			Level1Fields.ProfitMargin => ProfitMargin,
			Level1Fields.Beta => Beta,
			Level1Fields.HistoricalVolatilityWeek => HistoricalVolatilityWeek,
			Level1Fields.HistoricalVolatilityMonth => HistoricalVolatilityMonth,
			Level1Fields.IsSystem => IsSystem,
			Level1Fields.Decimals => Decimals,
			Level1Fields.AfterSplit => AfterSplit,
			Level1Fields.BeforeSplit => BeforeSplit,
			Level1Fields.CommissionTaker => CommissionTaker,
			Level1Fields.CommissionMaker => CommissionMaker,
			Level1Fields.MinVolume => MinVolume,
			Level1Fields.UnderlyingMinVolume => UnderlyingMinVolume,
			Level1Fields.Index => Index,
			Level1Fields.Imbalance => Imbalance,
			Level1Fields.UnderlyingPrice => UnderlyingPrice,
			Level1Fields.MaxVolume => MaxVolume,
			Level1Fields.LowBidPrice => LowBidPrice,
			Level1Fields.HighAskPrice => HighAskPrice,
			Level1Fields.LastTradeVolumeLow => LastTradeVolumeLow,
			Level1Fields.LastTradeVolumeHigh => LastTradeVolumeHigh,
			Level1Fields.LowBidVolume => LowBidVolume,
			Level1Fields.HighAskVolume => HighAskVolume,
			Level1Fields.UnderlyingBestBidPrice => UnderlyingBestBidPrice,
			Level1Fields.UnderlyingBestAskPrice => UnderlyingBestAskPrice,
			Level1Fields.MedianPrice => MedianPrice,
			Level1Fields.HighPrice52Week => HighPrice52Week,
			Level1Fields.LowPrice52Week => LowPrice52Week,
			Level1Fields.LastTradeStringId => LastTradeStringId,
			_ => throw new ArgumentOutOfRangeException(nameof(field), field, LocalizedStrings.InvalidValue),
		};
	}

	/// <summary>
	/// Convert <see cref="MDEntryType"/> to <see cref="Level1Fields"/> value.
	/// </summary>
	/// <param name="entryType"><see cref="MDEntryType"/> value.</param>
	/// <returns><see cref="Level1Fields"/> value.</returns>
	public static Level1Fields ToLevel1(this char entryType)
	{
		switch (entryType)
		{
			case MDEntryType.OpeningPrice:
				return Level1Fields.OpenPrice;
			case MDEntryType.TradingSessionHighPrice:
				return Level1Fields.HighPrice;
			case MDEntryType.TradingSessionLowPrice:
				return Level1Fields.LowPrice;
			case MDEntryType.ClosingPrice:
				return Level1Fields.ClosePrice;
			case MDEntryType.OpenInterest:
				return Level1Fields.OpenInterest;
			case MDEntryType.SessionLowOffer:
				return Level1Fields.LowAskPrice;
			case MDEntryType.SessionHighBid:
				return Level1Fields.HighBidPrice;
			case PriceStep:
				return Level1Fields.PriceStep;
			case VolumeStep:
				return Level1Fields.VolumeStep;
			case Multiplier:
				return Level1Fields.Multiplier;
			case StepPrice:
				return Level1Fields.StepPrice;
			case MarginBuy:
				return Level1Fields.MarginBuy;
			case MarginSell:
				return Level1Fields.MarginSell;
			case ImpliedVolatility:
				return Level1Fields.ImpliedVolatility;
			case TheorPrice:
				return Level1Fields.TheorPrice;
			case HistoricalVolatility:
				return Level1Fields.HistoricalVolatility;
			case Delta:
				return Level1Fields.Delta;
			case Gamma:
				return Level1Fields.Gamma;
			case Vega:
				return Level1Fields.Vega;
			case Theta:
				return Level1Fields.Theta;
			case Rho:
				return Level1Fields.Rho;
			case LastTradePrice:
				return Level1Fields.LastTradePrice;
			case LastTradeVolume:
				return Level1Fields.LastTradeVolume;
			case BestBidPrice:
				return Level1Fields.BestBidPrice;
			case BestBidVolume:
				return Level1Fields.BestBidVolume;
			case BestAskPrice:
				return Level1Fields.BestAskPrice;
			case BestAskVolume:
				return Level1Fields.BestAskVolume;
			case BestBidTime:
				return Level1Fields.BestBidTime;
			case BestAskTime:
				return Level1Fields.BestAskTime;
			case BidsVolume:
				return Level1Fields.BidsVolume;
			case BidsCount:
				return Level1Fields.BidsCount;
			case AsksVolume:
				return Level1Fields.AsksVolume;
			case AsksCount:
				return Level1Fields.AsksCount;
			case Change:
				return Level1Fields.Change;
			case MaxPrice:
				return Level1Fields.MaxPrice;
			case MinPrice:
				return Level1Fields.MinPrice;
			case MDEntryType.TradingSessionVWAPPrice:
				return Level1Fields.VWAP;
			case Yield:
				return Level1Fields.Yield;
			case AccruedCouponIncome:
				return Level1Fields.AccruedCouponIncome;
			case TradesCount:
				return Level1Fields.TradesCount;
			case MDEntryType.SettlementPrice:
				return Level1Fields.SettlementPrice;
			case MDEntryType.TradeVolume:
				return Level1Fields.Volume;
			case State:
				return Level1Fields.State;
			case ShortRatio:
				return Level1Fields.ShortRatio;
			case AverageTrueRange:
				return Level1Fields.AverageTrueRange;
			case AveragePrice:
				return Level1Fields.AveragePrice;
			case CurrentRatio:
				return Level1Fields.CurrentRatio;
			case SharesFloat:
				return Level1Fields.SharesFloat;
			case LastTradeId:
				return Level1Fields.LastTradeId;
			case LastTradeOrigin:
				return Level1Fields.LastTradeOrigin;
			case LastTradeTime:
				return Level1Fields.LastTradeTime;
			case LastTradeUpDown:
				return Level1Fields.LastTradeUpDown;
			case Duration:
				return Level1Fields.Duration;
			case IssueSize:
				return Level1Fields.IssueSize;
			case BuyBackPrice:
				return Level1Fields.BuyBackPrice;
			case BuyBackDate:
				return Level1Fields.BuyBackDate;
			case Turnover:
				return Level1Fields.Turnover;
			case OperatingMargin:
				return Level1Fields.OperatingMargin;
			case GrossMargin:
				return Level1Fields.GrossMargin;
			case Dividend:
				return Level1Fields.Dividend;
			case CouponDate:
				return Level1Fields.CouponDate;
			case CouponPeriod:
				return Level1Fields.CouponPeriod;
			case CouponValue:
				return Level1Fields.CouponValue;
			case MarketPriceToday:
				return Level1Fields.MarketPriceToday;
			case MarketPriceYesterday:
				return Level1Fields.MarketPriceYesterday;
			case VWAPPrev:
				return Level1Fields.VWAPPrev;
			case YieldVWAP:
				return Level1Fields.YieldVWAP;
			case YieldVWAPPrev:
				return Level1Fields.YieldVWAPPrev;
			case MDEntryType.MidPrice:
				return Level1Fields.SpreadMiddle;
			case PriceEarnings:
				return Level1Fields.PriceEarnings;
			case ForwardPriceEarnings:
				return Level1Fields.ForwardPriceEarnings;
			case PriceEarningsGrowth:
				return Level1Fields.PriceEarningsGrowth;
			case PriceSales:
				return Level1Fields.PriceSales;
			case PriceBook:
				return Level1Fields.PriceBook;
			case PriceCash:
				return Level1Fields.PriceCash;
			case PriceFreeCash:
				return Level1Fields.PriceFreeCash;
			case Payout:
				return Level1Fields.Payout;
			case SharesOutstanding:
				return Level1Fields.SharesOutstanding;
			case FloatShort:
				return Level1Fields.FloatShort;
			case ReturnOnAssets:
				return Level1Fields.ReturnOnAssets;
			case ReturnOnEquity:
				return Level1Fields.ReturnOnEquity;
			case ReturnOnInvestment:
				return Level1Fields.ReturnOnInvestment;
			case QuickRatio:
				return Level1Fields.QuickRatio;
			case LongTermDebtEquity:
				return Level1Fields.LongTermDebtEquity;
			case TotalDebtEquity:
				return Level1Fields.TotalDebtEquity;
			case ProfitMargin:
				return Level1Fields.ProfitMargin;
			case Beta:
				return Level1Fields.Beta;
			case HistoricalVolatilityWeek:
				return Level1Fields.HistoricalVolatilityWeek;
			case HistoricalVolatilityMonth:
				return Level1Fields.HistoricalVolatilityMonth;
			case IsSystem:
				return Level1Fields.IsSystem;
			case Decimals:
				return Level1Fields.Decimals;
			case AfterSplit:
				return Level1Fields.AfterSplit;
			case BeforeSplit:
				return Level1Fields.BeforeSplit;
			case CommissionTaker:
				return Level1Fields.CommissionTaker;
			case CommissionMaker:
				return Level1Fields.CommissionMaker;
			case MinVolume:
				return Level1Fields.MinVolume;
			case UnderlyingMinVolume:
				return Level1Fields.UnderlyingMinVolume;
			case Index:
				return Level1Fields.Index;
			case Imbalance:
				return Level1Fields.Imbalance;
			case UnderlyingPrice:
				return Level1Fields.UnderlyingPrice;
			case MaxVolume:
				return Level1Fields.MaxVolume;
			case LowBidPrice:
				return Level1Fields.LowBidPrice;
			case HighAskPrice:
				return Level1Fields.HighAskPrice;
			case LastTradeVolumeLow:
				return Level1Fields.LastTradeVolumeLow;
			case LastTradeVolumeHigh:
				return Level1Fields.LastTradeVolumeHigh;
			case LowBidVolume:
				return Level1Fields.LowBidVolume;
			case HighAskVolume:
				return Level1Fields.HighAskVolume;
			case UnderlyingBestBidPrice:
				return Level1Fields.UnderlyingBestBidPrice;
			case UnderlyingBestAskPrice:
				return Level1Fields.UnderlyingBestAskPrice;
			case MedianPrice:
				return Level1Fields.MedianPrice;
			case HighPrice52Week:
				return Level1Fields.HighPrice52Week;
			case LowPrice52Week:
				return Level1Fields.LowPrice52Week;
			case LastTradeStringId:
				return Level1Fields.LastTradeStringId;
#if DEBUG
			case Level1:
			case News:
			case CandleTimeFrame:
			case CandleVolume:
			case CandleTick:
			case CandleRange:
			case CandleRenko:
			case CandlePnF:
			case CandleHeikin:
			case OrderLog:
			case MDEntryType.Bid:
			case MDEntryType.Offer:
			case MDEntryType.Trade:
			//case MDEntryType.IndexValue:
			//case MDEntryType.Imbalance:
			//case MDEntryType.CompositeUnderlyingPrice:
			//case MDEntryType.SimulatedSellPrice:
			//case MDEntryType.SimulatedBuyPrice:
			//case MDEntryType.MarginRate:
			case MDEntryType.EmptyBook:
			//case MDEntryType.SettleHighPrice:
			//case MDEntryType.SettleLowPrice:
			//case MDEntryType.PriorSettlePrice:
			//case MDEntryType.EarlyPrices:
			//case MDEntryType.AuctionClearingPrice:
			//case MDEntryType.SwapValueFactor:
			//case MDEntryType.DailyValueAdjustmentForLongPositions:
			//case MDEntryType.CumulativeValueAdjustmentForLongPositions:
			//case MDEntryType.DailyValueAdjustmentForShortPositions:
			//case MDEntryType.CumulativeValueAdjustmentForShortPositions:
			//case MDEntryType.RecoveryRate:
			//case MDEntryType.RecoveryRateForLong:
			//case MDEntryType.RecoveryRateForShort:
			//case MDEntryType.FixingPrice:
			//case MDEntryType.CashRate:
#endif
			default:
				throw new ArgumentOutOfRangeException(nameof(entryType), entryType, LocalizedStrings.InvalidValue);
		}
	}
}