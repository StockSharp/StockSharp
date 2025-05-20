namespace StockSharp.Algo.Statistics;

/// <summary>
/// The interface, describing statistic parameter, calculated based on the profit-loss value (maximal contraction, Sharp coefficient etc.).
/// </summary>
public interface IPnLStatisticParameter : IStatisticParameter
{
	/// <summary>
	/// To add new data to the parameter.
	/// </summary>
	/// <param name="marketTime">The exchange time.</param>
	/// <param name="pnl">The profit-loss value.</param>
	/// <param name="commission">Commission.</param>
	void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission);
}

/// <summary>
/// The base profit-loss statistics parameter.
/// </summary>
/// <typeparam name="TValue">The type of the parameter value.</typeparam>
/// <remarks>
/// Initialize <see cref="BasePnLStatisticParameter{TValue}"/>.
/// </remarks>
/// <param name="type"><see cref="IStatisticParameter.Type"/></param>
public abstract class BasePnLStatisticParameter<TValue>(StatisticParameterTypes type) : BaseStatisticParameter<TValue>(type), IPnLStatisticParameter
	where TValue : IComparable<TValue>
{
	/// <inheritdoc />
	public virtual void Add(DateTimeOffset marketTime, decimal pnl, decimal? commission)
		=> throw new NotSupportedException();
}