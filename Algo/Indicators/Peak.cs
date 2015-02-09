namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Пик.
	/// </summary>
	[DisplayName("Peak")]
	[DescriptionLoc(LocalizedStrings.Str816Key)]
	public sealed class Peak : ZigZagEquis
	{
		/// <summary>
		/// Создать индикатор <see cref="Peak"/>.
		/// </summary>
		public Peak()
		{
			ByPrice = c => c.HighPrice;
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var value = base.OnProcess(input);

			if (IsFormed && !value.IsEmpty)
			{
				if (CurrentValue < value.GetValue<decimal>())
				{
					return value;
				}

				var lastValue = this.GetCurrentValue();
				IsFormed = !lastValue.IsEmpty;
				return IsFormed ? new ShiftedIndicatorValue(this, lastValue.Shift + 1, lastValue.Value) : lastValue;
			}

			IsFormed = false;

			return value;
		}
	}
}