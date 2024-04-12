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
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.ComponentModel;

	/// <summary>
	/// Optimal Tracking.
	/// </summary>
	/// <remarks>
	/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/optimal_tracking.html
	/// </remarks>
	[DisplayName("OptimalTracking")]
	[Description("Optimal Tracking Filter published by John Ehlers")]
	[IndicatorIn(typeof(CandleIndicatorValue))]
	[Doc("topics/api/indicators/list_of_indicators/optimal_tracking.html")]
	public sealed class OptimalTracking : LengthIndicator<decimal>
	{
		private static readonly decimal _smoothConstant1 = (decimal)Math.Exp(-0.25);
		private static readonly decimal _smoothConstant = 1 - _smoothConstant1;

		private class CalcBuffer
		{
			private decimal _lambda;
			private decimal _alpha;

			private decimal _value1Old;
			private decimal _value2Old;
			private decimal _resultOld;

			public CalcBuffer Clone() => (CalcBuffer)MemberwiseClone();

			public decimal Calculate(OptimalTracking ind, IList<decimal> buff, decimal average, decimal halfRange)
			{
				if (ind.IsFormed)
				{
					//Сглаженное приращение ****************************************************************************
					var avgDiff = buff[buff.Count - 1] - buff[buff.Count - 2];
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

					return result;
				}

				_value2Old = halfRange;
				_resultOld = average;

				return _resultOld;
			}

			public void Reset()
			{
				_value1Old = 0;
				_value2Old = 0;
				_resultOld = 0;

				_lambda = 0;
				_alpha = 0;
			}
		}

		private readonly CalcBuffer _buf = new();

		/// <summary>
		/// Initializes a new instance of the <see cref="OptimalTracking"/>.
		/// </summary>
		public OptimalTracking()
		{
			Length = 2; //только 2 т.к текущая и пред свеча.
		}

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();
			_buf.Reset();
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var (_, high, low, _) = input.GetOhlc();

			var average = (high + low) / 2;
			var halfRange = (high - low) / 2;

			if (input.IsFinal)
				Buffer.AddEx(average);

			var buff = input.IsFinal ? Buffer : (IList<decimal>)Buffer.Skip(Buffer.Count >= Length ? 1 : 0).Append(average).ToArray();

			var b = input.IsFinal ? _buf : _buf.Clone();

			var result = b.Calculate(this, buff, average, halfRange);

			return new DecimalIndicatorValue(this, result);
		}
	}
}