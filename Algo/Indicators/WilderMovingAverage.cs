namespace StockSharp.Algo.Indicators
{
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;
	using Ecng.Collections;
	using StockSharp.Localization;

	/// <summary>
	/// Welles Wilder Moving Average.
	/// </summary>
	[DisplayName("WilderMA")]
	[DescriptionLoc(LocalizedStrings.Str825Key)]
	public class WilderMovingAverage : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="WilderMovingAverage"/>.
		/// </summary>
		public WilderMovingAverage()
		{
			Length = 32;
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

			return new DecimalIndicatorValue(this, (this.GetCurrentValue() * (buff.Count - 1) + newValue) / buff.Count);
		}
	}
}