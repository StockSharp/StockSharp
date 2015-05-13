namespace StockSharp.BitStamp
{
	using StockSharp.Messages;

	static class Extensions
	{
		public static QuoteChange ToStockSharp(this double[] vp, Sides side)
		{
			return new QuoteChange(side, (decimal)vp[0], (decimal)vp[1]);
		}

		public static Sides ToStockSharp(this int type)
		{
			return type == 0 ? Sides.Buy : Sides.Sell;
		}
	}
}