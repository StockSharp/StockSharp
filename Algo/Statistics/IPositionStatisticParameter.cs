namespace StockSharp.Algo.Statistics;

/// <summary>
/// The interface, describing statistic parameter, calculated based on position.
/// </summary>
public interface IPositionStatisticParameter : IStatisticParameter
{
	/// <summary>
	/// To add the new position value to the parameter.
	/// </summary>
	/// <param name="marketTime">The exchange time.</param>
	/// <param name="position">The new position value.</param>
	void Add(DateTimeOffset marketTime, decimal position);
}