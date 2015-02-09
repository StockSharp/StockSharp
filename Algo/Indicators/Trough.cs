namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Впадина.
	/// </summary>
	[DisplayName("Trough")]
	[DescriptionLoc(LocalizedStrings.Str821Key)]
	public sealed class Trough : ZigZagEquis
	{
		/// <summary>
		/// Создать индикатор <see cref="Trough"/>.
		/// </summary>
		public Trough()
		{
			ByPrice = c => c.LowPrice;
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
				if (CurrentValue > value.GetValue<decimal>())
				{
					return value;
				}
				else
				{
					var lastValue = this.GetCurrentValue();
					IsFormed = !lastValue.IsEmpty;
					return IsFormed ? new ShiftedIndicatorValue(this, lastValue.Shift + 1, lastValue.Value) : lastValue;
				}
			}

			IsFormed = false;

			return value;
		}
	}
}