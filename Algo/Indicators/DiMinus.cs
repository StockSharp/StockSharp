namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// DIMinus is a component of the Directional Movement System developed by Welles Wilder.
	/// </summary>
	public class DiMinus : DiPart
	{
		/// <summary>
		/// Создать <see cref="DiMinus"/>.
		/// </summary>
		public DiMinus()
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
			if (current.LowPrice < prev.LowPrice && current.HighPrice - prev.HighPrice < prev.LowPrice - current.LowPrice)
				return prev.LowPrice - current.LowPrice;
			else
				return 0;
		}
	}
}