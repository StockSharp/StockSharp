namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Trough.
	/// </summary>
	[DisplayName("Trough")]
	[DescriptionLoc(LocalizedStrings.Str821Key)]
	public sealed class Trough : ZigZagEquis
	{
		/// <summary>
		/// To create the indicator <see cref="Trough"/>.
		/// </summary>
		public Trough()
		{
			ByPrice = c => c.LowPrice;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = base.OnProcess(input);

			if (IsFormed && !value.IsEmpty)
			{
				if (CurrentValue > value.GetValue<decimal>())
				{
					return value;
				}
				else
				{
					var lastValue = this.GetCurrentValue<ShiftedIndicatorValue>();

					if (input.IsFinal)
						IsFormed = !lastValue.IsEmpty;

					return IsFormed ? new ShiftedIndicatorValue(this, lastValue.Shift + 1, lastValue.Value) : lastValue;
				}
			}

			IsFormed = false;

			return value;
		}
	}
}