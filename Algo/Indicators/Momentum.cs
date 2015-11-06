namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Momentum.
	/// </summary>
	/// <remarks>
	/// Momentum Simple = C - C-n Where C- closing price of previous period. Where �-n � closing price N periods ago.
	/// </remarks>
	[DisplayName("Momentum")]
	[DescriptionLoc(LocalizedStrings.Str769Key)]
	public class Momentum : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Momentum"/>.
		/// </summary>
		public Momentum()
		{
			Length = 5;
		}

		/// <summary>
		/// Whether the indicator is set.
		/// </summary>
		public override bool IsFormed => Buffer.Count > Length;

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
				
				if ((Buffer.Count - 1) > Length)
					Buffer.RemoveAt(0);
			}

			if (Buffer.Count == 0)
				return new DecimalIndicatorValue(this);

			return new DecimalIndicatorValue(this, newValue - Buffer[0]);
		}
	}
}