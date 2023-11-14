namespace StockSharp.Algo.Statistics;

using System;
using System.Collections.Generic;
using System.Linq;

using Ecng.Collections;

using StockSharp.Messages;

/// <summary>
/// <see cref="IStatisticParameter"/> registry.
/// </summary>
public static class StatisticParameterRegistry
{
	private static readonly Dictionary<StatisticParameterTypes, IStatisticParameter> _dict = new();

	static StatisticParameterRegistry()
    {
		var maxPf = new MaxProfitParameter();
		var maxDd = new MaxDrawdownParameter();
		var netPf = new NetProfitParameter();

		All = new IStatisticParameter[]
		{
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

			new WinningTradesParameter(),
			new AverageWinTradeParameter(),
			new LossingTradesParameter(),
			new AverageLossTradeParameter(),
			new PerMonthTradeParameter(),
			new PerDayTradeParameter(),
			new RoundtripCountParameter(),
			new AverageTradeProfitParameter(),
			new TradeCountParameter(),

			new MaxLongPositionParameter(),
			new MaxShortPositionParameter(),

			new MaxLatencyRegistrationParameter(),
			new MinLatencyRegistrationParameter(),
			new MaxLatencyCancellationParameter(),
			new MinLatencyCancellationParameter(),
			new OrderCountParameter(),
			new OrderErrorCountParameter(),
			new OrderInsufficientFundErrorCountParameter(),
		};

		foreach (var p in All)
			_dict.Add(p.Type, p);
	}

	/// <summary>
	/// Get <see cref="IStatisticParameter"/> by the specified type.
	/// </summary>
	/// <param name="type"><see cref="StatisticParameterTypes"/></param>
	/// <returns><see cref="IStatisticParameter"/></returns>
	public static IStatisticParameter GetByType(StatisticParameterTypes type)
		=> _dict[type];

	/// <summary>
	/// Return all available parameters.
	/// </summary>
	public static IStatisticParameter[] All { get; }

	/// <summary>
	/// Init by initial value.
	/// </summary>
	/// <typeparam name="TParam">Type of <see cref="IStatisticManager.Parameters"/>.</typeparam>
	/// <typeparam name="TValue">Type of <see cref="IStatisticParameter.ValueType"/>.</typeparam>
	/// <param name="manager"><see cref="IStatisticManager"/></param>
	/// <param name="beginValue">Initial value.</param>
	public static void Init<TParam, TValue>(this IStatisticManager manager, TValue beginValue)
		where TParam : IStatisticParameter
	{
		if (manager is null)
			throw new ArgumentNullException(nameof(manager));

		manager.Parameters.OfType<TParam>().Where(p => p.ValueType == typeof(TValue)).ForEach(p => p.Init(beginValue));
	}
}