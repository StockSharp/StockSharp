namespace StockSharp.Algo.Statistics
{
	using System;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// The interface, describing statistic parameter, calculated based on position.
	/// </summary>
	public interface IPositionStatisticParameter
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
	[DisplayNameLoc(LocalizedStrings.Str970Key)]
	[DescriptionLoc(LocalizedStrings.Str971Key)]
	[CategoryLoc(LocalizedStrings.Str972Key)]
	public class MaxLongPositionParameter : BaseStatisticParameter<decimal>, IPositionStatisticParameter
	{
		/// <summary>
		/// To add the new position value to the parameter.
		/// </summary>
		/// <param name="marketTime">The exchange time.</param>
		/// <param name="position">The new position value.</param>
		public void Add(DateTimeOffset marketTime, decimal position)
		{
			if (position > 0)
				Value = position.Max(Value);
		}
	}

	/// <summary>
	/// Maximum short position size.
	/// </summary>
	[DisplayNameLoc(LocalizedStrings.Str973Key)]
	[DescriptionLoc(LocalizedStrings.Str974Key)]
	[CategoryLoc(LocalizedStrings.Str972Key)]
	public class MaxShortPositionParameter : BaseStatisticParameter<decimal>, IPositionStatisticParameter
	{
		/// <summary>
		/// To add the new position value to the parameter.
		/// </summary>
		/// <param name="marketTime">The exchange time.</param>
		/// <param name="position">The new position value.</param>
		public void Add(DateTimeOffset marketTime, decimal position)
		{
			if (position < 0)
				Value = position.Abs().Max(Value);
		}
	}
}