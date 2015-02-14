namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using System.Linq;
	using StockSharp.Localization;

	/// <summary>
	/// Сумма N последних значений.
	/// </summary>
	[DisplayName("Sum")]
	[DescriptionLoc(LocalizedStrings.Str751Key)]
	public class Sum : LengthIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="Sum"/>.
		/// </summary>
		public Sum()
			: base(typeof(decimal))
		{
			Length = 15;
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
			{
				return new DecimalIndicatorValue(this, Buffer.Sum());
			}
			else
			{
				return new DecimalIndicatorValue(this, (Buffer.Skip(1).Sum() + newValue));
			}
		}
	}
}