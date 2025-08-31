namespace StockSharp.Messages;

/// <summary>
/// Statistic types.
/// </summary>
public enum StatisticParameterTypes
{
	/// <summary>
	/// Number of trades won (whose profit is greater than 0).
	/// </summary>
	WinningTrades,

	/// <summary>
	/// Total number of trades.
	/// </summary>
	TradeCount,

	/// <summary>
	/// Total number of closing trades.
	/// </summary>
	RoundtripCount,

	/// <summary>
	/// Average trade profit.
	/// </summary>
	AverageTradeProfit,

	/// <summary>
	/// Average winning trade.
	/// </summary>
	AverageWinTrades,

	/// <summary>
	/// Average losing trade.
	/// </summary>
	AverageLossTrades,

	/// <summary>
	/// Number of trades lost with zero profit (whose profit is less than or equal to 0).
	/// </summary>
	LossingTrades,

	/// <summary>
	/// Maximum long position size.
	/// </summary>
	MaxLongPosition,

	/// <summary>
	/// Maximum short position size.
	/// </summary>
	MaxShortPosition,

	/// <summary>
	/// The maximal profit value for the entire period.
	/// </summary>
	MaxProfit,

	/// <summary>
	/// Maximum absolute drawdown during the whole period.
	/// </summary>
	MaxDrawdown,

	/// <summary>
	/// Maximum relative equity drawdown during the whole period.
	/// </summary>
	MaxRelativeDrawdown,

	/// <summary>
	/// Relative income for the whole time period.
	/// </summary>
	Return,

	/// <summary>
	/// Recovery factor (net profit / maximum drawdown).
	/// </summary>
	RecoveryFactor,

	/// <summary>
	/// Net profit for whole time period.
	/// </summary>
	NetProfit,

	/// <summary>
	/// The maximal value of the order registration delay.
	/// </summary>
	MaxLatencyRegistration,

	/// <summary>
	/// The maximal value of the order cancelling delay.
	/// </summary>
	MaxLatencyCancellation,

	/// <summary>
	/// The minimal value of order registration delay.
	/// </summary>
	MinLatencyRegistration,

	/// <summary>
	/// The minimal value of order cancelling delay.
	/// </summary>
	MinLatencyCancellation,

	/// <summary>
	/// Total number of orders.
	/// </summary>
	OrderCount,

	/// <summary>
	/// Total number of errors during order registration.
	/// </summary>
	OrderErrorCount,

	/// <summary>
	/// Total number of insufficient fund error orders.
	/// </summary>
	OrderInsufficientFundErrorCount,

	/// <summary>
	/// Average trades count per one month.
	/// </summary>
	PerMonthTrades,

	/// <summary>
	/// Average trades count per one day.
	/// </summary>
	PerDayTrades,

	/// <summary>
	/// Date of maximum absolute drawdown during the whole period.
	/// </summary>
	MaxDrawdownDate,

	/// <summary>
	/// Date of maximum profit value for the entire period.
	/// </summary>
	MaxProfitDate,

	/// <summary>
	/// Total commission.
	/// </summary>
	Commission,

	/// <summary>
	/// Maximum absolute drawdown during the whole period in percent.
	/// </summary>
	MaxDrawdownPercent,

	/// <summary>
	/// Net profit for whole time period in percent.
	/// </summary>
	NetProfitPercent,

	/// <summary>
	/// Sharpe ratio (annualized return - risk-free rate / annualized standard deviation).
	/// </summary>
	SharpeRatio,

	/// <summary>
	/// Sortino ratio (annualized return - risk-free rate / annualized downside deviation).
	/// </summary>
	SortinoRatio,

	/// <summary>
	/// The ratio of the average profit of winning trades to the average loss of losing trades.
	/// </summary>
	ProfitFactor,

	/// <summary>
	/// The average profit of winning trades minus the average loss of losing trades.
	/// </summary>
	Expectancy,

	/// <summary>
	/// Calmar ratio (annualized net profit / max drawdown).
	/// </summary>
	CalmarRatio,

	/// <summary>
	/// Sterling ratio (annualized net profit / average drawdown).
	/// </summary>
	SterlingRatio,

	/// <summary>
	/// Average drawdown during the whole period.
	/// </summary>
	AverageDrawdown,

	/// <summary>
	/// Total number of errors during order cancellation.
	/// </summary>
	OrderCancelErrorCount,
}