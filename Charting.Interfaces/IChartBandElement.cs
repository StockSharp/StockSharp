namespace StockSharp.Charting;

/// <summary>
/// The chart element representing a band.
/// </summary>
public interface IChartBandElement : IChartElement
{
	/// <summary>
	/// The band drawing style. The default is <see cref="DrawStyles.Band"/>. Can also be <see cref="DrawStyles.BandOneValue"/>.
	/// </summary>
	DrawStyles Style { get; set; }

	/// <summary>
	/// <see cref="Line1"/>.
	/// </summary>
	IChartLineElement Line1 { get; }

	/// <summary>
	/// <see cref="Line2"/>.
	/// </summary>
	IChartLineElement Line2 { get; }
}