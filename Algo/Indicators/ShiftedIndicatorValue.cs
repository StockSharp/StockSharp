#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: ShiftedIndicatorValue.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;

	/// <summary>
	/// The shifted value of the indicator.
	/// </summary>
	public class ShiftedIndicatorValue : SingleIndicatorValue<IIndicatorValue>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ShiftedIndicatorValue"/>.
		/// </summary>
		/// <param name="indicator">Indicator.</param>
		public ShiftedIndicatorValue(IIndicator indicator)
			: base(indicator)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ShiftedIndicatorValue"/>.
		/// </summary>
		/// <param name="shift">The shift of the indicator value.</param>
		/// <param name="value">Indicator value.</param>
		/// <param name="indicator">Indicator.</param>
		public ShiftedIndicatorValue(IIndicator indicator, int shift, IIndicatorValue value)
			: base(indicator, value)
		{
			Shift = shift;
		}

		/// <summary>
		/// The shift of the indicator value.
		/// </summary>
		public int Shift { get; }

		/// <inheritdoc />
		public override bool IsSupport(Type valueType) => !IsEmpty && Value.IsSupport(valueType);

		/// <inheritdoc />
		public override T GetValue<T>() => base.GetValue<IIndicatorValue>().GetValue<T>();

		/// <inheritdoc />
		public override IIndicatorValue SetValue<T>(IIndicator indicator, T value) => throw new NotSupportedException();
	}
}