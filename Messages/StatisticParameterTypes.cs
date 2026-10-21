namespace StockSharp.Messages;

/// <summary>
/// Statistic types.
/// </summary>
public enum StatisticParameterTypes
{
	/// <summary>
	/// Number of trades won (whose profit is greater than 0).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ProfitTradesKey)]
	WinningTrades,

	/// <summary>
	/// Total number of trades.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TotalTradesKey)]
	TradeCount,

	/// <summary>
	/// Total number of closing trades.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ClosingTradesKey)]
	RoundtripCount,

	/// <summary>
	/// Average trade profit.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AverageProfitKey)]
	AverageTradeProfit,

	/// <summary>
	/// Average winning trade.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AverageWinKey)]
	AverageWinTrades,

	/// <summary>
	/// Average losing trade.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AverageLossKey)]
	AverageLossTrades,

	/// <summary>
	/// Number of trades lost with zero profit (whose profit is less than or equal to 0).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LossTradesKey)]
	LossingTrades,

	/// <summary>
	/// Maximum long position size.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxLongPosKey)]
	MaxLongPosition,

	/// <summary>
	/// Maximum short position size.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxShortPosKey)]
	MaxShortPosition,

	/// <summary>
	/// The maximal profit value for the entire period.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxProfitKey)]
	MaxProfit,

	/// <summary>
	/// Maximum absolute drawdown during the whole period.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxDrawdownKey)]
	MaxDrawdown,

	/// <summary>
	/// Maximum relative equity drawdown during the whole period.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RelativeDrawdownKey)]
	MaxRelativeDrawdown,

	/// <summary>
	/// Relative income for the whole time period.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RelativeIncomeKey)]
	Return,

	/// <summary>
	/// Recovery factor (net profit / maximum drawdown).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.RecoveryFactorKey)]
	RecoveryFactor,

	/// <summary>
	/// Net profit for whole time period.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NetProfitKey)]
	NetProfit,

	/// <summary>
	/// The maximal value of the order registration delay.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxLatencyRegistrationKey)]
	MaxLatencyRegistration,

	/// <summary>
	/// The maximal value of the order cancelling delay.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxLatencyCancellationKey)]
	MaxLatencyCancellation,

	/// <summary>
	/// The minimal value of order registration delay.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MinLatencyRegistrationKey)]
	MinLatencyRegistration,

	/// <summary>
	/// The minimal value of order cancelling delay.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MinLatencyCancellationKey)]
	MinLatencyCancellation,

	/// <summary>
	/// Total number of orders.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TotalOrdersKey)]
	OrderCount,

	/// <summary>
	/// Total number of errors during order registration.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ErrorsKey)]
	OrderErrorCount,

	/// <summary>
	/// Total number of insufficient fund error orders.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.IFEKey)]
	OrderInsufficientFundErrorCount,

	/// <summary>
	/// Average trades count per one month.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PerMonthTradesKey)]
	PerMonthTrades,

	/// <summary>
	/// Average trades count per one day.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PerDayTradesKey)]
	PerDayTrades,

	/// <summary>
	/// Date of maximum absolute drawdown during the whole period.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxDrawdownDateKey)]
	MaxDrawdownDate,

	/// <summary>
	/// Date of maximum profit value for the entire period.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxProfitDateKey)]
	MaxProfitDate,

	/// <summary>
	/// Total commission.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CommissionKey)]
	Commission,

	/// <summary>
	/// Maximum absolute drawdown during the whole period in percent.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxDrawdownPercentKey)]
	MaxDrawdownPercent,

	/// <summary>
	/// Net profit for whole time period in percent.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NetProfitPercentKey)]
	NetProfitPercent,

	/// <summary>
	/// Sharpe ratio (annualized return - risk-free rate / annualized standard deviation).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SharpeRatioKey)]
	SharpeRatio,

	/// <summary>
	/// Sortino ratio (annualized return - risk-free rate / annualized downside deviation).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SortinoRatioKey)]
	SortinoRatio,

	/// <summary>
	/// The ratio of the average profit of winning trades to the average loss of losing trades.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ProfitFactorKey)]
	ProfitFactor,

	/// <summary>
	/// The average profit of winning trades minus the average loss of losing trades.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ExpectancyKey)]
	Expectancy,

	/// <summary>
	/// Calmar ratio (annualized net profit / max drawdown).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CalmarRatioKey)]
	CalmarRatio,

	/// <summary>
	/// Sterling ratio (annualized net profit / average drawdown).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SterlingRatioKey)]
	SterlingRatio,

	/// <summary>
	/// Average drawdown during the whole period.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AverageDrawdownKey)]
	AverageDrawdown,

	/// <summary>
	/// Total number of errors during order cancellation.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ErrorCancellingKey)]
	OrderCancelErrorCount,

	/// <summary>
	/// Total currency amount of all completed losing trades.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.GrossLossKey)]
	GrossLoss,

	/// <summary>
	/// Total currency amount of all completed winning trades.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.GrossProfitKey)]
	GrossProfit,

	/// <summary>
	/// Maximum profit value for the period, expressed as a percentage.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxProfitPercentKey)]
	MaxProfitPercent,

	/// <summary>
	/// The share of the observed time a position was open, expressed as a percentage.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TimeInMarketKey)]
	TimeInMarket,

	/// <summary>
	/// How long a position is held on average.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AverageHoldingTimeKey)]
	AverageHoldingTime,

	/// <summary>
	/// The share of the net result made by the single most profitable trade, expressed as a percentage.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LargestTradeShareKey)]
	LargestTradeShare,
}