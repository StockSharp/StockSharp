namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// DIMinus is a component of the Directional Movement System developed by Welles Wilder.
	/// </summary>
	public class DiMinus : DiPart
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DiMinus"/>.
		/// </summary>
		public DiMinus()
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
			if (current.LowPrice < prev.LowPrice && current.HighPrice - prev.HighPrice < prev.LowPrice - current.LowPrice)
				return prev.LowPrice - current.LowPrice;
			else
				return 0;
		}
	}
}