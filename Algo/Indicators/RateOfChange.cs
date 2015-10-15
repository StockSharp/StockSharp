namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Rate of change.
	/// </summary>
	[DisplayName("ROC")]
	[DescriptionLoc(LocalizedStrings.Str732Key)]
	public class RateOfChange : Momentum
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RateOfChange"/>.
		/// </summary>
		public RateOfChange()
		{
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var result = base.OnProcess(input);

			if (Buffer.Count > 0 && Buffer[0] != 0)
				return new DecimalIndicatorValue(this, result.GetValue<decimal>() / Buffer[0] * 100);
			
			return new DecimalIndicatorValue(this);
		}
	}
}