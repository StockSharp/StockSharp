namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Localization;

	/// <summary>
	/// Weighted moving average.
	/// </summary>
	[DisplayName("WMA")]
	[DescriptionLoc(LocalizedStrings.Str824Key)]
	public class WeightedMovingAverage : LengthIndicator<decimal>
	{
		private decimal _denominator = 1;

		/// <summary>
		/// Initializes a new instance of the <see cref="WeightedMovingAverage"/>.
		/// </summary>
		public WeightedMovingAverage()
		{
			Length = 32;
		}

		/// <summary>
		/// To reset the indicator status to initial. The method is called each time when initial settings are changed (for example, the length of period).
		/// </summary>
		public override void Reset()
		{
			base.Reset();

			_denominator = 0;

			for (var i = 1; i <= Length; i++)
				_denominator += i;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			if (input.IsFinal)
			{
				Buffer.Add(newValue);

				if (Buffer.Count > Length)
					Buffer.RemoveAt(0);
			}

			var buff = Buffer;
			if (!input.IsFinal)
			{
				buff = new List<decimal>();
				buff.AddRange(Buffer.Skip(1));
				buff.Add(newValue);
			}

			var w = 1;
			return new DecimalIndicatorValue(this, buff.Sum(v => w++ * v) / _denominator);
		}
	}
}