namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;
	using System;
	using StockSharp.Localization;

	/// <summary>
	/// Максимальное значение за период.
	/// </summary>
	[DisplayName("Highest")]
	[DescriptionLoc(LocalizedStrings.Str733Key)]
	public class Highest : LengthIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="Highest"/>.
		/// </summary>
		public Highest()
			: base(typeof(decimal))
		{
			Length = 5;
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
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