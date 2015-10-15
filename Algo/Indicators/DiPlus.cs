namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// DIPlus is a component of the Directional Movement System developed by Welles Wilder.
	/// </summary>
	public class DiPlus : DiPart
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DiPlus"/>.
		/// </summary>
		public DiPlus()
		{
		}

		/// <summary>
		/// To get the part value.
		/// </summary>
		/// <param name="current">The current candle.</param>
		/// <param name="prev">The previous candle.</param>
		/// <returns>Value.</returns>
		protected override decimal GetValue(Candle current, Candle prev)
		{
			if (current.HighPrice > prev.HighPrice && current.HighPrice - prev.HighPrice > prev.LowPrice - current.LowPrice)
				return current.HighPrice - prev.HighPrice;
			else
				return 0;
		}
	}
}