#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Indicators.Algo
File: DoubleExponentialMovingAverage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	/// <summary>
	/// Double Exponential Moving Average.
	/// </summary>
	/// <remarks>
	/// ((2 * EMA) – EMA of EMA).
	/// </remarks>
	[DisplayName("DEMA")]
	[Description("Double Exponential Moving Average")]
	public class DoubleExponentialMovingAverage : LengthIndicator<decimal>
	{
		private readonly ExponentialMovingAverage _ema1;
		private readonly ExponentialMovingAverage _ema2;

		/// <summary>
		/// Initializes a new instance of the <see cref="DoubleExponentialMovingAverage"/>.
		/// </summary>
		public DoubleExponentialMovingAverage()
		{
			_ema1 = new ExponentialMovingAverage();
			_ema2 = new ExponentialMovingAverage();

			Length = 32;
		}

		/// <inheritdoc />
		public override void Reset()
		{
			_ema2.Length = _ema1.Length = Length;
			base.Reset();
		}

		/// <inheritdoc />
		public override bool IsFormed => _ema1.IsFormed && _ema2.IsFormed;

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var ema1Value = _ema1.Process(input);

			if (!_ema1.IsFormed)
				return new DecimalIndicatorValue(this);

			var ema2Value = _ema2.Process(ema1Value);

			return new DecimalIndicatorValue(this, 2 * ema1Value.GetValue<decimal>() - ema2Value.GetValue<decimal>());
		}
	}
}
