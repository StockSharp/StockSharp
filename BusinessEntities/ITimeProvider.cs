namespace StockSharp.BusinessEntities;

/// <summary>
/// The interface describing the time provider.
/// </summary>
public interface ITimeProvider
{
	/// <summary>
	/// Current time.
	/// </summary>
	DateTime CurrentTimeUtc { get; }

	/// <summary>
	/// Server time changed <see cref="CurrentTimeUtc"/>. It passed the time difference since the last call of the event. The first time the event passes the value <see cref="TimeSpan.Zero"/>.
	/// </summary>
	event Action<TimeSpan> CurrentTimeChanged;
}