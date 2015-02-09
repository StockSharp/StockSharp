namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;
	using StockSharp.Localization;

	/// <summary>
	/// Простая скользящая средняя.
	/// </summary>
	[DisplayName("SMA")]
	[DescriptionLoc(LocalizedStrings.Str818Key)]
	public class SimpleMovingAverage : LengthIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="SimpleMovingAverage"/>.
		/// </summary>
		public SimpleMovingAverage()
			: base(typeof(decimal))
		{
			Length = 32;
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

			if (input.IsFinal)
				return new DecimalIndicatorValue(this, Buffer.Sum() / Length);

			return new DecimalIndicatorValue(this, (Buffer.Skip(1).Sum() + newValue) / Length);
		}
	}
}