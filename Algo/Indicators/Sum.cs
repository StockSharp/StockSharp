namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;

	using StockSharp.Localization;

	/// <summary>
	/// Sum of N last values.
	/// </summary>
	[DisplayName("Sum")]
	[DescriptionLoc(LocalizedStrings.Str751Key)]
	public class Sum : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Sum"/>.
		/// </summary>
		public Sum()
		{
			Length = 15;
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
			{
				return new DecimalIndicatorValue(this, Buffer.Sum());
			}
			else
			{
				return new DecimalIndicatorValue(this, (Buffer.Skip(1).Sum() + newValue));
			}
		}
	}
}