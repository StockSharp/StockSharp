namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;
	using System;

	using StockSharp.Localization;

	/// <summary>
	/// Maximum value for a period.
	/// </summary>
	[DisplayName("Highest")]
	[DescriptionLoc(LocalizedStrings.Str733Key)]
	public class Highest : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="Highest"/>.
		/// </summary>
		public Highest()
		{
			Length = 5;
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			var lastValue = Buffer.Count == 0 ? newValue : this.GetCurrentValue();

			// добавляем новое начало
			if (input.IsFinal)
				Buffer.Add(newValue);

			if (newValue > lastValue)
			{
				// Новое значение и есть экстремум 
				lastValue = newValue;
			}

			if (Buffer.Count > Length)
			{
				var first = Buffer[0];

				// удаляем хвостовое значение
				if (input.IsFinal)
					Buffer.RemoveAt(0);

				// удаляется экстремум, для поиска нового значения необходим проход по всему буфферу
				if (first == lastValue && lastValue != newValue)
				{
					// ищем новый экстремум
					lastValue = Buffer.Aggregate(newValue, (current, t) => Math.Max(t, current));
				}
			}

			return new DecimalIndicatorValue(this, lastValue);
		}
	}
}