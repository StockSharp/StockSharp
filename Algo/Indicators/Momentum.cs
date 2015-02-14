namespace StockSharp.Algo.Indicators
{
	using System.ComponentModel;
	using StockSharp.Localization;

	/// <summary>
	/// Моментум.
	/// </summary>
	/// <remarks>
	/// Momentum Simple = C - C-n
	/// Где C- цена закрытия текущего периода.
	/// Где С-n - цена закрытия N периодов назад.
	/// </remarks>
	[DisplayName("Momentum")]
	[DescriptionLoc(LocalizedStrings.Str769Key)]
	public class Momentum : LengthIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="Momentum"/>.
		/// </summary>
		public Momentum()
			: base(typeof(decimal))
		{
			Length = 5;
		}

		/// <summary>
		/// Сформирован ли индикатор.
		/// </summary>
		public override bool IsFormed
		{
			get { return Buffer.Count > Length; }
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
				
				if ((Buffer.Count - 1) > Length)
					Buffer.RemoveAt(0);
			}

			if (Buffer.Count == 0)
				return new DecimalIndicatorValue(this);

			return new DecimalIndicatorValue(this, newValue - Buffer[0]);
		}
	}
}