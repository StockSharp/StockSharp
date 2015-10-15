namespace StockSharp.Algo.Indicators
{
	using System;
	using System.ComponentModel;
	using System.Linq;

	using StockSharp.Algo.Candles;
	using StockSharp.Localization;

	/// <summary>
	/// True range.
	/// </summary>
	[DisplayName("TR")]
	[DescriptionLoc(LocalizedStrings.Str775Key)]
	public class TrueRange : BaseIndicator
	{
		private Candle _prevCandle;

		/// <summary>
		/// Initializes a new instance of the <see cref="TrueRange"/>.
		/// </summary>
		public TrueRange()
		{
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();
			_prevCandle = null;
		}

		/// <summary>
		/// To get price components to select the maximal value.
		/// </summary>
		/// <param name="currentCandle">The current candle.</param>
		/// <param name="prevCandle">The previous candle.</param>
		/// <returns>Price components.</returns>
		protected virtual decimal[] GetPriceMovements(Candle currentCandle, Candle prevCandle)
		{
			return new[]
			{
				Math.Abs(currentCandle.HighPrice - currentCandle.LowPrice),
				Math.Abs(prevCandle.ClosePrice - currentCandle.HighPrice),
				Math.Abs(prevCandle.ClosePrice - currentCandle.LowPrice)
			};
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var candle = input.GetValue<Candle>();

			if (_prevCandle != null)
			{
				if (input.IsFinal)
					IsFormed = true;

				var priceMovements = GetPriceMovements(candle, _prevCandle);

				if (input.IsFinal)
					_prevCandle = candle;

				return new DecimalIndicatorValue(this, priceMovements.Max());
			}

			if (input.IsFinal)
				_prevCandle = candle;

			return new DecimalIndicatorValue(this, candle.HighPrice - candle.LowPrice);
		}
	}
}