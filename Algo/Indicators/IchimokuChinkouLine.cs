namespace StockSharp.Algo.Indicators
{
	using StockSharp.Algo.Candles;

	/// <summary>
	/// Chinkou line.
	/// </summary>
	public class IchimokuChinkouLine : LengthIndicator<decimal>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IchimokuChinkouLine"/>.
		/// </summary>
		public IchimokuChinkouLine()
		{
		}

		/// <summary>
		/// To handle the input value.
		/// </summary>
		/// <param name="input">The input value.</param>
		/// <returns>The resulting value.</returns>
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