namespace StockSharp.Charting;

/// <summary>
/// The chart axis.
/// </summary>
public interface IChartAxis : IPersistable, INotifyPropertyChangedEx, INotifyPropertyChanged, INotifyPropertyChanging
{
	/// <summary>
	/// Chart area.
	/// </summary>
	IChartArea ChartArea { get; }

	/// <summary>
	/// Unique ID.
	/// </summary>
	string Id { get; set; }

	/// <summary>
	/// Axis visibility.
	/// </summary>
	bool IsVisible { get; set; }

	/// <summary>
	/// Header.
	/// </summary>
	string Title { get; set; }

	/// <summary>
	/// Group.
	/// </summary>
	string Group { get; set; }

	/// <summary>
	/// Whether to use alternative axis alignment.
	/// </summary>
	bool SwitchAxisLocation { get; set; }

	/// <summary>
	/// Axis type.
	/// </summary>
	ChartAxisType AxisType { get; set; }

	/// <summary>
	/// Auto range.
	/// </summary>
	bool AutoRange { get; set; }

	/// <summary>
	/// Flip coordinates.
	/// </summary>
	bool FlipCoordinates { get; set; }

	/// <summary>
	/// Show main grid lines on the axis.
	/// </summary>
	bool DrawMajorTicks { get; set; }

	/// <summary>
	/// Show main grid lines.
	/// </summary>
	bool DrawMajorGridLines { get; set; }

	/// <summary>
	/// Show extra grid lines on the axis.
	/// </summary>
	bool DrawMinorTicks { get; set; }

	/// <summary>
	/// Show extra grid lines.
	/// </summary>
	bool DrawMinorGridLines { get; set; }

	/// <summary>
	/// Show labels on the axis.
	/// </summary>
	bool DrawLabels { get; set; }

	/// <summary>
	/// Labels format.
	/// </summary>
	string TextFormatting { get; set; }

	/// <summary>
	/// Cursor labels format.
	/// </summary>
	string CursorTextFormatting { get; set; }

	/// <summary>
	/// The format of X-axis labels within the day.
	/// </summary>
	string SubDayTextFormatting { get; set; }

	/// <summary>
	/// Time zone for this axis.
	/// </summary>
	TimeZoneInfo TimeZone { get; set; }
}