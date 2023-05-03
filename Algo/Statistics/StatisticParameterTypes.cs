namespace StockSharp.Algo.Statistics;

/// <summary>
/// <see cref="IStatisticParameter"/> types.
/// </summary>
public enum StatisticParameterTypes
{
	/// <summary>
	/// <see cref="WinningTradesParameter"/>
	/// </summary>
	WinningTrades,

	/// <summary>
	/// <see cref="TradeCountParameter"/>
	/// </summary>
	TradeCount,

	/// <summary>
	/// <see cref="RoundtripCountParameter"/>
	/// </summary>
	RoundtripCount,

	/// <summary>
	/// <see cref="AverageTradeProfitParameter"/>
	/// </summary>
	AverageTradeProfit,

	/// <summary>
	/// <see cref="AverageWinTradeParameter"/>
	/// </summary>
	AverageWinTrades,

	/// <summary>
	/// <see cref="AverageLossTradeParameter"/>
	/// </summary>
	AverageLossTrades,

	/// <summary>
	/// <see cref="LossingTradesParameter"/>
	/// </summary>
	LossingTrades,

	/// <summary>
	/// <see cref="MaxLongPositionParameter"/>
	/// </summary>
	MaxLongPosition,

	/// <summary>
	/// <see cref="MaxShortPositionParameter"/>
	/// </summary>
	MaxShortPosition,

	/// <summary>
	/// <see cref="MaxProfitParameter"/>
	/// </summary>
	MaxProfit,

	/// <summary>
	/// <see cref="MaxDrawdownParameter"/>
	/// </summary>
	MaxDrawdown,

	/// <summary>
	/// <see cref="MaxRelativeDrawdownParameter"/>
	/// </summary>
	MaxRelativeDrawdown,

	/// <summary>
	/// <see cref="ReturnParameter"/>
	/// </summary>
	Return,

	/// <summary>
	/// <see cref="RecoveryFactorParameter"/>
	/// </summary>
	RecoveryFactor,

	/// <summary>
	/// <see cref="NetProfitParameter"/>
	/// </summary>
	NetProfit,

	/// <summary>
	/// <see cref="MaxLatencyRegistrationParameter"/>
	/// </summary>
	MaxLatencyRegistration,

	/// <summary>
	/// <see cref="MaxLatencyCancellationParameter"/>
	/// </summary>
	MaxLatencyCancellation,

	/// <summary>
	/// <see cref="MinLatencyRegistrationParameter"/>
	/// </summary>
	MinLatencyRegistration,

	/// <summary>
	/// <see cref="MinLatencyCancellationParameter"/>
	/// </summary>
	MinLatencyCancellation,

	/// <summary>
	/// <see cref="OrderCountParameter"/>
	/// </summary>
	OrderCount,
}