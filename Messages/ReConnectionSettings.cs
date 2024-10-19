namespace StockSharp.Messages;

/// <summary>
/// Connection tracking settings <see cref="IMessageAdapter"/> with a server.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ReConnectionSettingsKey,
	Description = LocalizedStrings.ReConnectionDescKey)]
[TypeConverter(typeof(ExpandableObjectConverter))]
public class ReConnectionSettings : IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="ReConnectionSettings"/>.
	/// </summary>
	public ReConnectionSettings()
	{
	}

	private TimeSpan _interval = TimeSpan.FromSeconds(10);

	/// <summary>
	/// The interval at which attempts will establish a connection. The default value is 10 seconds.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IntervalKey,
		Description = LocalizedStrings.IntervalToConnectKey,
		GroupName = LocalizedStrings.ConnectionKey)]
	public TimeSpan Interval
	{
		get => _interval;
		set
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_interval = value;
		}
	}

	private int _attemptCount;

	/// <summary>
	/// The number of attempts to establish the initial connection, if it has not been established (timeout, network failure, etc.). The default value is 0. To establish infinite number uses -1.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.InitiallyKey,
		Description = LocalizedStrings.InitiallyConnectKey,
		GroupName = LocalizedStrings.ConnectionKey)]
	public int AttemptCount
	{
		get => _attemptCount;
		set
		{
			if (value < -1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_attemptCount = value;
		}
	}

	private int _reAttemptCount = 100;

	/// <summary>
	/// The number of attempts to reconnect if the connection was lost during the operation. The default value is 100. To establish infinite number uses -1.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ReconnectionKey,
		Description = LocalizedStrings.ReconnectAttemptsKey,
		GroupName = LocalizedStrings.ConnectionKey)]
	public int ReAttemptCount
	{
		get => _reAttemptCount;
		set
		{
			if (value < -1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_reAttemptCount = value;
		}
	}

	private TimeSpan _timeOutInterval = TimeSpan.FromSeconds(30);

	/// <summary>
	/// Timeout successful connection / disconnection. If the value is <see cref="TimeSpan.Zero"/>, the monitoring is performed. The default value is 30 seconds.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TimeOutKey,
		Description = LocalizedStrings.ConnectDisconnectTimeoutKey,
		GroupName = LocalizedStrings.ConnectionKey)]
	public TimeSpan TimeOutInterval
	{
		get => _timeOutInterval;
		set
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_timeOutInterval = value;
		}
	}

	private WorkingTime _workingTime = new();

	/// <summary>
	/// Schedule, during which it is necessary to make the connection. For example, there is no need to track connection when trading on the exchange finished.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.WorkScheduleKey,
		Description = LocalizedStrings.ReConnectWorkScheduleKey,
		GroupName = LocalizedStrings.ConnectionKey)]
	public WorkingTime WorkingTime
	{
		get => _workingTime;
		set => _workingTime = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		WorkingTime.Load(storage, nameof(WorkingTime));

		Interval = storage.GetValue<TimeSpan>(nameof(Interval));
		AttemptCount = storage.GetValue<int>(nameof(AttemptCount));
		ReAttemptCount = storage.GetValue<int>(nameof(ReAttemptCount));
		TimeOutInterval = storage.GetValue<TimeSpan>(nameof(TimeOutInterval));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(WorkingTime), WorkingTime.Save());
		storage.SetValue(nameof(Interval), Interval);
		storage.SetValue(nameof(AttemptCount), AttemptCount);
		storage.SetValue(nameof(ReAttemptCount), ReAttemptCount);
		storage.SetValue(nameof(TimeOutInterval), TimeOutInterval);
	}
}