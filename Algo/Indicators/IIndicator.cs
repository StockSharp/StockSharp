#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: IIndicator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	/// <summary>
	/// The interface describing indicator.
	/// </summary>
	public interface IIndicator : IPersistable, ICloneable<IIndicator>
	{
		/// <summary>
		/// Unique ID.
		/// </summary>
		Guid Id { get; }

		/// <summary>
		/// Indicator name.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		bool IsFormed { get; }

		/// <summary>
		/// The container storing indicator data.
		/// </summary>
		IIndicatorContainer Container { get; }

		/// <summary>
		/// Input values type.
		/// </summary>
		Type InputType { get; }

		/// <summary>
		/// Result values type.
		/// </summary>
		Type ResultType { get; }

		/// <summary>
		/// The indicator change event (for example, a new value is added).
		/// </summary>
		event Action<IIndicatorValue, IIndicatorValue> Changed;

		/// <summary>
		/// The event of resetting the indicator status to initial. The event is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		event Action Reseted;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The new value of the indicator.</returns>
		IIndicatorValue Process(IIndicatorValue input);

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		void Reset();
	}
}
