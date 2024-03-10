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
	using System.Collections.Generic;
	
	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// The shifted value of the indicator.
	/// </summary>
	public class ShiftedIndicatorValue : SingleIndicatorValue<decimal>
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
		/// <param name="indicator">Indicator.</param>
		/// <param name="value">Indicator value.</param>
		/// <param name="shift">The shift of the indicator value.</param>
		public ShiftedIndicatorValue(IIndicator indicator, decimal value, int shift)
			: base(indicator, value)
		{
			Shift = shift;
		}

		private int _shift;

		/// <summary>
		/// The shift of the indicator value.
		/// </summary>
		public int Shift
		{
			get => _shift;
			private set
			{
				if (value < 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

				_shift = value;
			}
		}

		/// <inheritdoc />
		public override IIndicatorValue SetValue<T>(IIndicator indicator, T value)
			=> IsEmpty
				? new ShiftedIndicatorValue(indicator)
				: new ShiftedIndicatorValue(indicator, Value, Shift);

		/// <inheritdoc />
		public override IEnumerable<object> ToValues()
		{
			foreach (var v in base.ToValues())
				yield return v;

			if (!IsEmpty)
				yield return Shift;
		}

		/// <inheritdoc />
		public override void FromValues(object[] values)
		{
			base.FromValues(values);

			if (IsEmpty)
				return;

			Shift = values[1].To<int>();
		}
	}
}