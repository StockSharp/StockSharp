namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;

	using StockSharp.Localization;

	/// <summary>
	/// Smoothed Moving Average.
	/// </summary>
	[DisplayName("SMMA")]
	[DescriptionLoc(LocalizedStrings.Str819Key)]
	public class SmoothedMovingAverage : LengthIndicator<decimal>
	{
		private decimal _prevFinalValue;

		/// <summary>
		/// Initializes a new instance of the <see cref="SmoothedMovingAverage"/>.
		/// </summary>
		public SmoothedMovingAverage()
		{
			Length = 32;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			_prevFinalValue = 0;
			base.Reset();
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			if (!IsFormed)
			{
				if (input.IsFinal)
				{
					Buffer.Add(newValue);

					_prevFinalValue = Buffer.Sum() / Length;

					return new DecimalIndicatorValue(this, _prevFinalValue);
				}

				return new DecimalIndicatorValue(this, (Buffer.Skip(1).Sum() + newValue) / Length);
			}

			var curValue = (_prevFinalValue * (Length - 1) + newValue) / Length;

			if (input.IsFinal)
				_prevFinalValue = curValue;

			return new DecimalIndicatorValue(this, curValue);
		}
	}
}