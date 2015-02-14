namespace StockSharp.Algo.Indicators
{
	/// <summary>
	/// Сигнальная часть индикатора <see cref="RelativeVigorIndex"/>.
	/// </summary>
	public class RelativeVigorIndexSignal : LengthIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="RelativeVigorIndexSignal"/>.
		/// </summary>
		public RelativeVigorIndexSignal()
			: base(typeof(decimal))
		{
			Length = 4;
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var newValue = input.GetValue<decimal>();

			if (input.IsFinal)
			{
				Buffer.Add(newValue);

				if (Buffer.Count > Length)
					Buffer.RemoveAt(0);
			}

			if (IsFormed)
			{
				return input.IsFinal
					? new DecimalIndicatorValue(this, (Buffer[0] + 2 * Buffer[1] + 2 * Buffer[2] + Buffer[3]) / 6m)
					: new DecimalIndicatorValue(this, (Buffer[1] + 2 * Buffer[2] + 2 * Buffer[3] + newValue) / 6m);
			}

			return new DecimalIndicatorValue(this);
		}
	}
}