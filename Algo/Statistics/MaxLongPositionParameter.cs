namespace StockSharp.Algo.Statistics;

/// <summary>
/// Maximum long position size.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MaxLongPosKey,
	Description = LocalizedStrings.MaxLongPosDescKey,
	GroupName = LocalizedStrings.PositionsKey,
	Order = 200
)]
public class MaxLongPositionParameter : BaseStatisticParameter<decimal>, IPositionStatisticParameter
{
	/// <summary>
	/// Initialize <see cref="MaxLongPositionParameter"/>.
	/// </summary>
	public MaxLongPositionParameter()
		: base(StatisticParameterTypes.MaxLongPosition)
	{
	}

	/// <inheritdoc/>
	public void Add(DateTimeOffset marketTime, decimal position)
	{
		if (position > 0 && position > Value)
			Value = position;
	}
}
