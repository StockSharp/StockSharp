namespace StockSharp.Charting;

/// <summary>
/// The chart element representing a candle.
/// </summary>
public interface IChartCandleElement : IChartElement
{
	/// <summary>
	/// Style of candles rendering.
	/// </summary>
	ChartCandleDrawStyles DrawStyle { get; set; }

	#region candle properties

	/// <summary>
	/// Body color of decreasing candle.
	/// </summary>
	Color DownFillColor { get; set; }

	/// <summary>
	/// Body color of increasing candle.
	/// </summary>
	Color UpFillColor { get; set; }

	/// <summary>
	/// Border color of decreasing candle.
	/// </summary>
	Color DownBorderColor { get; set; }

	/// <summary>
	/// Border color of increasing candle.
	/// </summary>
	Color UpBorderColor { get; set; }

	/// <summary>
	/// Line width (bar or border), with which candle will be drawn on chart.
	/// </summary>
	int StrokeThickness { get; set; }

	/// <summary>
	/// Candles rendering smoothing (enabled by default).
	/// </summary>
	bool AntiAliasing { get; set; }

	/// <summary>
	/// Line color for <see cref="DrawStyle"/> Line*.
	/// </summary>
	Color? LineColor { get; set; }

	/// <summary>
	/// Area color for <see cref="DrawStyle"/> set as <see cref="ChartCandleDrawStyles.Area"/>.
	/// </summary>
	Color? AreaColor { get; set; }

	/// <summary>
	/// Show Y-axis marker.
	/// </summary>
	bool ShowAxisMarker { get; set; }

	/// <summary>
	/// Custom elements colorer.
	/// </summary>
	new Func<DateTimeOffset, bool, bool, Color?> Colorer { get; set; }

	#endregion

	#region volume charts properties

	/// <summary>
	/// Timeframe #2 multiplier.
	/// </summary>
	int? Timeframe2Multiplier { get; set; }

	/// <summary>
	/// Timeframe #3 multiplier.
	/// </summary>
	int? Timeframe3Multiplier { get; set; }

	/// <summary>
	/// Font color.
	/// </summary>
	Color? FontColor { get; set; }

	/// <summary>
	/// Timeframe #2 color.
	/// </summary>
	Color? Timeframe2Color { get; set; }

	/// <summary>
	/// Timeframe #2 frame color.
	/// </summary>
	Color? Timeframe2FrameColor { get; set; }

	/// <summary>
	/// Timeframe #3 color.
	/// </summary>
	Color? Timeframe3Color { get; set; }

	/// <summary>
	/// Max volume color.
	/// </summary>
	Color? MaxVolumeColor { get; set; }

	/// <summary>
	/// Cluster profile line color.
	/// </summary>
	Color? ClusterLineColor { get; set; }

	/// <summary>
	/// Cluster profile separator line color.
	/// </summary>
	Color? ClusterSeparatorLineColor { get; set; }

	/// <summary>
	/// Cluster text color.
	/// </summary>
	Color? ClusterTextColor { get; set; }

	/// <summary>
	/// Cluster color.
	/// </summary>
	Color? ClusterColor { get; set; }

	/// <summary>
	/// Cluster max color.
	/// </summary>
	Color? ClusterMaxColor { get; set; }

	/// <summary>
	/// Show horizontal volume.
	/// </summary>
	bool ShowHorizontalVolumes { get; set; }

	/// <summary>
	/// Local horizontal volume.
	/// </summary>
	bool LocalHorizontalVolumes { get; set; }

	/// <summary>
	/// Horizontal volume width fraction.
	/// </summary>
	double HorizontalVolumeWidthFraction { get; set; }

	/// <summary>
	/// Horizontal volume color.
	/// </summary>
	Color? HorizontalVolumeColor { get; set; }

	/// <summary>
	/// Horizontal volume font color.
	/// </summary>
	Color? HorizontalVolumeFontColor { get; set; }

	/// <summary>
	/// Price step for group volumes. <see langword="null"/> means no grouping.
	/// </summary>
	decimal? PriceStep { get; set; }

	/// <summary>
	/// Draw <see cref="Sides.Buy"/> and <see cref="Sides.Sell"/> volumes separately.
	/// </summary>
	bool DrawSeparateVolumes { get; set; }

	/// <summary>
	/// Buy volume color.
	/// </summary>
	Color? BuyColor { get; set; }

	/// <summary>
	/// Sell volume color.
	/// </summary>
	Color? SellColor { get; set; }

	#endregion
}