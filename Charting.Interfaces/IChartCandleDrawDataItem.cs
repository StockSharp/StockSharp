namespace StockSharp.Charting;

using static IChartDrawData;

/// <summary>
/// Chart drawing data item capable of receiving complete candle data.
/// </summary>
public interface IChartCandleDrawDataItem : IChartDrawDataItem
{
	/// <summary>
	/// Put the candle data.
	/// </summary>
	/// <param name="element">The chart element representing a candle.</param>
	/// <param name="data">Candle data.</param>
	/// <returns><see cref="IChartDrawDataItem"/> instance.</returns>
	IChartDrawDataItem Add(IChartCandleElement element, ChartCandleDrawData data);
}
