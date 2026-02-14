namespace StockSharp.Tests;

[TestClass]
public class HeartbeatReconnectScheduleTests : BaseTestClass
{
	private const MessageTypes ReconnectType = (MessageTypes)(-12);

	private sealed class TimeControlledPassThroughMessageAdapter : PassThroughMessageAdapter
	{
		public TimeControlledPassThroughMessageAdapter()
			: base(new IncrementalIdGenerator())
		{
		}

		public DateTime Time { get; set; } = new(2025, 1, 6, 12, 0, 0);

		public override DateTime CurrentTime => Time;

		public override IMessageAdapter Clone()
			=> new TimeControlledPassThroughMessageAdapter
			{
				Time = Time,
			};
	}

	[TestMethod]
	public async Task Reconnect_InsideWorkingTime_SendsReconnectMessage()
	{
		var now = new DateTime(2025, 1, 6, 12, 0, 0);
		var wt = CreateDailyWorkingTime(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
		var (adapter, _, state, outMessages) = CreateSut(now, wt, attempts: 3);

		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromSeconds(1));

		AreEqual(1, outMessages.Count(m => m.Type == ReconnectType));
		AreEqual(2, state.ConnectingAttemptCount);
		AreEqual(adapter.ReConnectionSettings.Interval, state.ConnectionTimeOut);
	}

	[TestMethod]
	public async Task Reconnect_OutsideWorkingTime_SkipsReconnect()
	{
		var now = new DateTime(2025, 1, 6, 20, 0, 0);
		var wt = CreateDailyWorkingTime(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
		var (adapter, _, state, outMessages) = CreateSut(now, wt, attempts: 3);

		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromSeconds(1));

		AreEqual(0, outMessages.Count(m => m.Type == ReconnectType));
		AreEqual(TimeSpan.FromMinutes(1), state.ConnectionTimeOut);
	}

	[TestMethod]
	public async Task Reconnect_WorkingTimeDisabled_AlwaysReconnects()
	{
		var now = new DateTime(2025, 1, 6, 23, 0, 0);
		var wt = new WorkingTime { IsEnabled = false };
		var (adapter, _, state, outMessages) = CreateSut(now, wt, attempts: 2);

		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromSeconds(1));

		AreEqual(1, outMessages.Count(m => m.Type == ReconnectType));
		AreEqual(1, state.ConnectingAttemptCount);
	}

	[TestMethod]
	public async Task Reconnect_OutsideWorkingTime_DoesNotDecrementAttempts()
	{
		var now = new DateTime(2025, 1, 6, 21, 0, 0);
		var wt = CreateDailyWorkingTime(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
		var (adapter, _, state, _) = CreateSut(now, wt, attempts: 5);

		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromSeconds(1));

		AreEqual(5, state.ConnectingAttemptCount);
	}

	[TestMethod]
	public async Task Reconnect_TransitionFromOutsideToInsideWorkingTime()
	{
		var wt = CreateDailyWorkingTime(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0));
		var (adapter, inner, state, outMessages) = CreateSut(new DateTime(2025, 1, 6, 20, 0, 0), wt, attempts: 4);

		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromSeconds(1));
		AreEqual(0, outMessages.Count(m => m.Type == ReconnectType));
		AreEqual(TimeSpan.FromMinutes(1), state.ConnectionTimeOut);

		inner.Time = new DateTime(2025, 1, 7, 12, 0, 0);
		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromMinutes(1));

		AreEqual(1, outMessages.Count(m => m.Type == ReconnectType));
		AreEqual(3, state.ConnectingAttemptCount);
	}

	[TestMethod]
	public async Task Reconnect_SpecialHoliday_SkipsReconnect()
	{
		var holiday = new DateTime(2025, 1, 6);
		var wt = CreateDailyWorkingTime(new TimeSpan(9, 0, 0), new TimeSpan(18, 0, 0), holiday);
		var (adapter, _, state, outMessages) = CreateSut(new DateTime(2025, 1, 6, 12, 0, 0), wt, attempts: 3);

		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromSeconds(1));

		AreEqual(0, outMessages.Count(m => m.Type == ReconnectType));
		AreEqual(TimeSpan.FromMinutes(1), state.ConnectionTimeOut);
		AreEqual(3, state.ConnectingAttemptCount);
	}

	private static (HeartbeatMessageAdapter adapter, TimeControlledPassThroughMessageAdapter inner, HeartbeatManagerState state, List<Message> outMessages) CreateSut(DateTime now, WorkingTime workingTime, int attempts)
	{
		var inner = new TimeControlledPassThroughMessageAdapter
		{
			Time = now,
		};

		var state = new HeartbeatManagerState
		{
			CurrentState = ConnectionStates.Reconnecting,
			PreviousState = ConnectionStates.Connected,
			ConnectingAttemptCount = attempts,
			ConnectionTimeOut = TimeSpan.Zero,
			IsFirstTimeConnect = false,
		};

		var adapter = new HeartbeatMessageAdapter(inner, state);
		adapter.ReConnectionSettings.Interval = TimeSpan.FromSeconds(10);
		adapter.ReConnectionSettings.WorkingTime = workingTime;

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (message, _) =>
		{
			outMessages.Add(message);
			return default;
		};

		return (adapter, inner, state, outMessages);
	}

	private static WorkingTime CreateDailyWorkingTime(TimeSpan from, TimeSpan to, params DateTime[] specialHolidays)
	{
		return new WorkingTime
		{
			IsEnabled = true,
			Periods =
			[
				new WorkingTimePeriod
				{
					Till = new DateTime(2100, 1, 1),
					Times = [new Range<TimeSpan>(from, to)],
				}
			],
			SpecialHolidays = specialHolidays?.Select(h => h.Date).ToArray() ?? [],
		};
	}

	private static async ValueTask InvokeProcessReconnectionAsync(HeartbeatMessageAdapter adapter, TimeSpan diff)
		=> await adapter.ProcessReconnection(diff, CancellationToken.None);

}
