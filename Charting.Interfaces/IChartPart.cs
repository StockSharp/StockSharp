namespace StockSharp.Charting;

/// <summary>
/// The interfaces that describes the part of the chart.
/// </summary>
/// <typeparam name="T">The chart element type.</typeparam>
public interface IChartPart<T> : INotifyPropertyChanging, INotifyPropertyChanged, IPersistable
	where T : IChartPart<T>
{
	/// <summary>
	/// Unique ID.
	/// </summary>
	Guid Id { get; }
}