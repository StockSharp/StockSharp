namespace StockSharp.Algo.Statistics;

/// <summary>
/// <see cref="IStatisticParameter"/> registry.
/// </summary>
public static class StatisticParameterRegistry
{
	/// <summary>
	/// Create all available <see cref="IStatisticParameter"/>.
	/// </summary>
	/// <returns>All available <see cref="IStatisticParameter"/>.</returns>
	public static IStatisticParameter[] CreateAll()
	{
		var maxPf = new MaxProfitParameter();
		var maxDd = new MaxDrawdownParameter();
		var netPf = new NetProfitParameter();
		var avgDd = new AverageDrawdownParameter();

		return
		[
			maxPf,
			new MaxProfitDateParameter(maxPf),
			maxDd,
			new MaxDrawdownDateParameter(maxDd),
			new MaxRelativeDrawdownParameter(),
			new MaxDrawdownPercentParameter(maxDd),
			new ReturnParameter(),
			netPf,
			new NetProfitPercentParameter(),
			new RecoveryFactorParameter(maxDd, netPf),
			new CommissionParameter(),
			new SharpeRatioParameter(),
			new SortinoRatioParameter(),
			new CalmarRatioParameter(netPf, maxDd),
			new SterlingRatioParameter(netPf, avgDd),
			avgDd,

			new WinningTradesParameter(),
			new AverageWinTradeParameter(),
			new LossingTradesParameter(),
			new AverageLossTradeParameter(),
			new PerMonthTradeParameter(),
			new PerDayTradeParameter(),
			new RoundtripCountParameter(),
			new AverageTradeProfitParameter(),
			new TradeCountParameter(),
			new ProfitFactorParameter(),
			new ExpectancyParameter(),

			new MaxLongPositionParameter(),
			new MaxShortPositionParameter(),

			new MaxLatencyRegistrationParameter(),
			new MinLatencyRegistrationParameter(),
			new MaxLatencyCancellationParameter(),
			new MinLatencyCancellationParameter(),
			new OrderCountParameter(),
			new OrderRegisterErrorCountParameter(),
			new OrderCancelErrorCountParameter(),
			new OrderInsufficientFundErrorCountParameter(),
		];
	}

	private static readonly Dictionary<StatisticParameterTypes, IStatisticParameter> _dict = [];

	static StatisticParameterRegistry()
    {
		All = CreateAll();

		foreach (var p in All)
			_dict.Add(p.Type, p);
	}

	/// <summary>
	/// Get <see cref="IStatisticParameter"/> by the specified type.
	/// </summary>
	/// <param name="type"><see cref="StatisticParameterTypes"/></param>
	/// <returns><see cref="IStatisticParameter"/></returns>
	public static IStatisticParameter ToParameter(this StatisticParameterTypes type)
		=> _dict[type];

	/// <summary>
	/// Return all available parameters.
	/// </summary>
	public static IStatisticParameter[] All { get; }
}