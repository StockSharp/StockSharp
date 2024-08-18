namespace StockSharp.Algo.Statistics;

using System;
using System.ComponentModel.DataAnnotations;

using Ecng.Common;

using StockSharp.Localization;
using StockSharp.Messages;

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

	/// <summary>
	/// To add the new position value to the parameter.
	/// </summary>
	/// <param name="marketTime">The exchange time.</param>
	/// <param name="position">The new position value.</param>
	public void Add(DateTimeOffset marketTime, decimal position)
	{
		if (position > 0 && position > Value)
			Value = position;
	}
}

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

	/// <summary>
	/// To add the new position value to the parameter.
	/// </summary>
	/// <param name="marketTime">The exchange time.</param>
	/// <param name="position">The new position value.</param>
	public void Add(DateTimeOffset marketTime, decimal position)
	{
		if (position < 0 && position.Abs() > Value)
			Value = position;
	}
}