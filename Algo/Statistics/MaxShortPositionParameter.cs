namespace StockSharp.Algo.Statistics;

/// <summary>
/// Maximum short position size.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxShortPosKey,
	Description = LocalizedStrings.MaxShortPosDescKey,
	GroupName = LocalizedStrings.PositionsKey,
	Order = 201
)]
public class MaxShortPositionParameter : BaseStatisticParameter<decimal>, IPositionStatisticParameter
{
	/// <summary>
	/// Initialize <see cref="MaxShortPositionParameter"/>.
	/// </summary>
	public MaxShortPositionParameter()
		: base(StatisticParameterTypes.MaxShortPosition)
	{
	}

	/// <inheritdoc/>
	public void Add(DateTimeOffset marketTime, decimal position)
	{
		if (position < 0 && position.Abs() > Value.Abs())
			Value = position;
	}
}