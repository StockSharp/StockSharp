namespace StockSharp.Charting;

/// <summary>
/// Styles of the candles chart drawing.
/// </summary>
public enum ChartCandleDrawStyles
{
	/// <summary>
	/// Japanese candles.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CandleStickKey)]
	CandleStick,

	/// <summary>
	/// Bars.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BarsKey)]
	Ohlc,

	/// <summary>
	/// Line (open).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LineOpenKey)]
	LineOpen,

	/// <summary>
	/// Line (high).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LineHighKey)]
	LineHigh,

	/// <summary>
	/// Line (low).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LineLowKey)]
	LineLow,

	/// <summary>
	/// Line (close).
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.LineCloseKey)]
	LineClose,

	/// <summary>
	/// Box volumes.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BoxChartKey)]
	BoxVolume,

	/// <summary>
	/// Cluster profile.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.ClusterProfileKey)]
	ClusterProfile,

	/// <summary>
	/// Area.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.AreaKey)]
	Area,

	/// <summary>
	/// X0 candle.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PnFCandleKey)]
	PnF,
}