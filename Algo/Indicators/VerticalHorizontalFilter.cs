#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: VerticalHorizontalFilter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// The vertical-horizontal filter.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/VHF.ashx http://www.stator-afm.com/vertical-horizontal-filter.html http://www.incrediblecharts.com/indicators/vertical_horizontal_filter.php.
	/// </remarks>
	[DisplayName("VHF")]
	[DescriptionLoc(LocalizedStrings.Str754Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class VerticalHorizontalFilter : LengthIndicator<decimal>
	{
		// Текущее значение минимума
		private readonly Lowest _min = new Lowest();

		// Текущее значение максимума
		private readonly Highest _max = new Highest();

		// Текущее значение суммы
		private readonly Sum _sum = new Sum();

		// Цена закрытия предыдущего бара
		private decimal? _previousClosePrice;

		/// <summary>
		/// Initializes a new instance of the <see cref="VolumeWeightedMovingAverage"/>.
		/// </summary>
		public VerticalHorizontalFilter()
		{
			Length = 15;
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _sum.IsFormed && _max.IsFormed && _min.IsFormed;

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_sum.Length = _min.Length = _max.Length = Length;
			_previousClosePrice = null;
			base.Reset();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			// Находим минимум и максимум для заданного периода
			var minValue = _min.Process(input.SetValue(this, candle.LowPrice)).GetValue<decimal>();
			var maxValue = _max.Process(input.SetValue(this, candle.HighPrice)).GetValue<decimal>();

			var sumValue = 0m;

			// Вычисляем сумму модулей разности цен закрытия текущего и предыдущего дня для заданного периода
			if (_previousClosePrice != null)
				sumValue = _sum.Process(input.SetValue(this, Math.Abs(_previousClosePrice.Value - candle.ClosePrice))).GetValue<decimal>();

			if (input.IsFinal)
				_previousClosePrice = candle.ClosePrice;

			if (!IsFormed)
				return new DecimalIndicatorValue(this);

			// Вычисляем значение индикатора
			if (sumValue != 0)
				return new DecimalIndicatorValue(this, ((maxValue - minValue) / sumValue));

			return new DecimalIndicatorValue(this);
		}
	}
}