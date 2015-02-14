namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// Линия Chinkou.
	/// </summary>
	public class IchimokuChinkouLine : LengthIndicator<decimal>
	{
		/// <summary>
		/// Создать <see cref="IchimokuChinkouLine"/>.
		/// </summary>
		public IchimokuChinkouLine()
			: base(typeof(Candle))
		{
		}

		/// <summary>
		/// Обработать входное значение.
		/// </summary>
		/// <param name="input">Входное значение.</param>
		/// <returns>Результирующее значение.</returns>
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var price = input.GetValue<Candle>().ClosePrice;

			if (Buffer.Count > Length)
				Buffer.RemoveAt(0);

			if (input.IsFinal)
				Buffer.Add(price);

			return new DecimalIndicatorValue(this, price);
		}
	}
}