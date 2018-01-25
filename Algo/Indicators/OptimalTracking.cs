#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: OptimalTracking.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;

	using StockSharp.Algo.Candles;

	/// <summary>
	/// Optimal Tracking.
	/// </summary>
	/// <remarks>
	/// Based on a Kalman Filter (Dr. R. E. Kalman, 1960) and Kalatas Tracking Index (Paul. R. Kalata, 1984).
	/// </remarks>
	[DisplayName("OptimalTracking")]
	[Description("Optimal Tracking Filter published by John Ehlers")]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	public sealed class OptimalTracking : LengthIndicator<decimal>
	{
		//Fields

		//private int mult = 4;
		private decimal _lambda;
		private decimal _alpha;
		private const int _start = 1;

		private decimal _value1Old;
		private decimal _value2Old;
		private decimal _resultOld;

		private readonly decimal _smoothConstant1;
		private readonly decimal _smoothConstant;

		//methods

		/// <summary>
		/// Initializes a new instance of the <see cref="OptimalTracking"/>.
		/// </summary>
		public OptimalTracking()
		{
			Length = _start + 1; //только 2 т.к текущая и пред свеча.
			const double x = -0.25;
			_smoothConstant1 = (decimal)System.Math.Exp(x);
			_smoothConstant = 1 - _smoothConstant1;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_value1Old = 0;
			_value2Old = 0;
			_resultOld = 0;

			_lambda = 0;
			_alpha = 0;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();
			var average = (candle.HighPrice + candle.LowPrice) / 2;
			var halfRange = (candle.HighPrice - candle.LowPrice) / 2;

			Buffer.Add(average);
			//var Chec1 = Buffer[Buffer.Count - 1];

			if (IsFormed)
			{
				if (Buffer.Count > Length)
					Buffer.RemoveAt(0);
				//Сглаженное приращение ****************************************************************************
				var avgDiff = Buffer[Buffer.Count - 1] - Buffer[Buffer.Count - 2];
				var smoothDiff = _smoothConstant * avgDiff + _smoothConstant1 * _value1Old;
				_value1Old = smoothDiff;

				//Сглаженный Half Range *********************************************************************************

				var smoothRng = _smoothConstant * halfRange + _smoothConstant1 * _value2Old;
				_value2Old = smoothRng;

				//Tracking index ***********************************************************************************
				if (smoothRng != 0)
					_lambda = Math.Abs(smoothDiff / smoothRng);

				//Alfa для альфа фильтра ***************************************************************************
				_alpha = (-_lambda * _lambda + (decimal)Math.Sqrt((double)(_lambda * _lambda * _lambda * _lambda + 16 * _lambda * _lambda))) / 8;

				//Smoothed result **********************************************************************************
				var check2 = _alpha * average;
				var check3 = (1 - _alpha) * _resultOld;
				var result = check2 + check3;
				_resultOld = result;

				return new DecimalIndicatorValue(this, result);
			}

			_value2Old = halfRange;
			_resultOld = average;

			return new DecimalIndicatorValue(this, _resultOld);
		}
	}
}