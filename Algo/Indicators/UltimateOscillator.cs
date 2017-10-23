#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: UltimateOscillator.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// Last oscillator.
	/// </summary>
	/// <remarks>
	/// http://www2.wealth-lab.com/WL5Wiki/UltimateOsc.ashx http://stockcharts.com/school/doku.php?id=chart_school:technical_indicators:ultimate_oscillator.
	/// </remarks>
	[DisplayName("UltimateOsc")]
	[DescriptionLoc(LocalizedStrings.Str776Key)]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public class UltimateOscillator : BaseIndicator
	{
		private const decimal _stoProcentov = 100m;

		private const int _period7 = 7;
		private const int _period14 = 14;
		private const int _period28 = 28;

		private const int _weight1 = 1;
		private const int _weight2 = 2;
		private const int _weight4 = 4;

		private readonly Sum _period7BpSum;
		private readonly Sum _period14BpSum;
		private readonly Sum _period28BpSum;

		private readonly Sum _period7TrSum;
		private readonly Sum _period14TrSum;
		private readonly Sum _period28TrSum;

		private decimal? _previouseClosePrice;

		/// <summary>
		/// To create the indicator <see cref="UltimateOscillator"/>.
		/// </summary>
		public UltimateOscillator()
		{
			_period7BpSum = new Sum { Length = _period7 };
			_period14BpSum = new Sum { Length = _period14 };
			_period28BpSum = new Sum { Length = _period28 };

			_period7TrSum = new Sum { Length = _period7 };
			_period14TrSum = new Sum { Length = _period14 };
			_period28TrSum = new Sum { Length = _period28 };
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => _period7BpSum.IsFormed && _period14BpSum.IsFormed &&
		                                 _period28BpSum.IsFormed && _period7TrSum.IsFormed &&
		                                 _period14TrSum.IsFormed && _period28TrSum.IsFormed;

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			if (_previouseClosePrice != null)
			{
				var min = _previouseClosePrice.Value < candle.LowPrice ? _previouseClosePrice.Value : candle.LowPrice;
				var max = _previouseClosePrice.Value > candle.HighPrice ? _previouseClosePrice.Value : candle.HighPrice;

				input = input.SetValue(this, candle.ClosePrice - min);

				var p7BpValue = _period7BpSum.Process(input).GetValue<decimal>();
				var p14BpValue = _period14BpSum.Process(input).GetValue<decimal>();
				var p28BpValue = _period28BpSum.Process(input).GetValue<decimal>();

				input = input.SetValue(this, max - min);

				var p7TrValue = _period7TrSum.Process(input).GetValue<decimal>();
				var p14TrValue = _period14TrSum.Process(input).GetValue<decimal>();
				var p28TrValue = _period28TrSum.Process(input).GetValue<decimal>();

				if (input.IsFinal)
					_previouseClosePrice = candle.ClosePrice;

				if (p7TrValue != 0 && p14TrValue != 0 && p28TrValue != 0)
				{
					var average7 = p7BpValue / p7TrValue;
					var average14 = p14BpValue / p14TrValue;
					var average28 = p28BpValue / p28TrValue;
					return new DecimalIndicatorValue(this, _stoProcentov * (_weight4 * average7 + _weight2 * average14 + _weight1 * average28) / (_weight4 + _weight2 + _weight1));
				}

				return new DecimalIndicatorValue(this);
			}

			if (input.IsFinal)
				_previouseClosePrice = candle.ClosePrice;

			return new DecimalIndicatorValue(this);
		}
	}
}