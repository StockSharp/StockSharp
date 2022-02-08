namespace StockSharp.Charting
{
	using System.Drawing;

	/// <summary>
	/// The chart element representing orders.
	/// </summary>
	public interface IChartOrderElement : IChartTransactionElement<IChartOrderElement>
	{
		/// <summary>
		/// Fill color of transaction errors.
		/// </summary>
		Color ErrorColor { get; set; }

		/// <summary>
		/// Orders display filter.
		/// </summary>
		ChartOrderDisplayFilter Filter { get; set; }
	}
}