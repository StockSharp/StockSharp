namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// DIPlus is a component of the Directional Movement System developed by Welles Wilder.
	/// </summary>
	public class DiPlus : DiPart
	{
		/// <summary>
		/// Создать <see cref="DiPlus"/>.
		/// </summary>
		public DiPlus()
		{
		}

		/// <summary>
		/// Получить значение части.
		/// </summary>
		/// <param name="current">Текущая свеча.</param>
		/// <param name="prev">Предыдущая свеча.</param>
		/// <returns>Значение.</returns>
		protected override decimal GetValue(Candle current, Candle prev)
		{
			if (current.HighPrice > prev.HighPrice && current.HighPrice - prev.HighPrice > prev.LowPrice - current.LowPrice)
				return current.HighPrice - prev.HighPrice;
			else
				return 0;
		}
	}
}