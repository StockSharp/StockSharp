namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;

	using StockSharp.Localization;

	/// <summary>
	/// Peak.
	/// </summary>
	[DisplayName("Peak")]
	[DescriptionLoc(LocalizedStrings.Str816Key)]
	public sealed class Peak : ZigZagEquis
	{
		/// <summary>
		/// To create the indicator <see cref="Peak"/>.
		/// </summary>
		public Peak()
		{
			ByPrice = c => c.HighPrice;
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
				if (CurrentValue < value.GetValue<decimal>())
				{
					return value;
				}

				var lastValue = this.GetCurrentValue<ShiftedIndicatorValue>();

				if (input.IsFinal)
					IsFormed = !lastValue.IsEmpty;

				return IsFormed ? new ShiftedIndicatorValue(this, lastValue.Shift + 1, lastValue.Value) : lastValue;
			}

			IsFormed = false;

			return value;
		}
	}
}