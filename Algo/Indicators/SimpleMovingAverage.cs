namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;

	using StockSharp.Localization;

	/// <summary>
	/// Simple moving average.
	/// </summary>
	[DisplayName("SMA")]
	[DescriptionLoc(LocalizedStrings.Str818Key)]
	public class SimpleMovingAverage : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleMovingAverage"/>.
		/// </summary>
		public SimpleMovingAverage()
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

			if (input.IsFinal)
				return new DecimalIndicatorValue(this, Buffer.Sum() / Length);

			return new DecimalIndicatorValue(this, (Buffer.Skip(1).Sum() + newValue) / Length);
		}
	}
}