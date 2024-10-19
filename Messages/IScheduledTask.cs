namespace StockSharp.Messages;

/// <summary>
/// Interface described schedule task.
/// </summary>
public interface IScheduledTask
{
	/// <summary>
	/// Working schedule.
	/// </summary>
	WorkingTime WorkingTime { get; }

	/// <summary>
	/// Can start (not disabled, not already started).
	/// </summary>
	bool CanStart { get; }

	/// <summary>
	/// Can stop.
	/// </summary>
	bool CanStop { get; }
}