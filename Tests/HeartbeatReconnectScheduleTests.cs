namespace StockSharp.Tests;

[TestClass]
public class HeartbeatReconnectScheduleTests : BaseTestClass
{
	private const MessageTypes ReconnectType = (MessageTypes)(-12);

	private static readonly DateTime _midday = new(2025, 1, 6, 12, 0, 0);

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

	private async ValueTask InvokeProcessReconnectionAsync(HeartbeatMessageAdapter adapter, TimeSpan diff)
		=> await adapter.ProcessReconnection(diff, CancellationToken);

	#region Connection lifetime

	private static (HeartbeatMessageAdapter adapter, TimeControlledPassThroughMessageAdapter inner, List<Message> outMessages) CreateSut(DateTime now, HeartbeatManagerState state)
	{
		var inner = new TimeControlledPassThroughMessageAdapter
		{
			Time = now,
		};

		var adapter = new HeartbeatMessageAdapter(inner, state);
		adapter.ReConnectionSettings.Interval = TimeSpan.FromSeconds(10);
		adapter.ReConnectionSettings.WorkingTime = new WorkingTime { IsEnabled = false };

		var outMessages = new List<Message>();
		adapter.NewOutMessageAsync += (message, _) =>
		{
			outMessages.Add(message);
			return default;
		};

		return (adapter, inner, outMessages);
	}

	/// <summary>
	/// A connection attempt that never gets an answer has to end as a failed connection the caller
	/// can see, not as a wait with nothing on either side of it.
	/// </summary>
	[TestMethod]
	public async Task ConnectTimeout_IsReportedAsAConnectError()
	{
		var state = new HeartbeatManagerState
		{
			CurrentState = ConnectionStates.Connecting,
			PreviousState = HeartbeatManagerState.None,
			ConnectingAttemptCount = 3,
			ConnectionTimeOut = TimeSpan.FromSeconds(5),
			IsFirstTimeConnect = false,
		};

		var (adapter, _, outMessages) = CreateSut(_midday, state);

		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromSeconds(6));

		var connect = outMessages.OfType<ConnectMessage>().Single();
		IsNotNull(connect.Error, "the caller is told the connection did not happen");
		IsInstanceOfType<TimeoutException>(connect.Error);

		AreEqual(ConnectionStates.Reconnecting, state.CurrentState, "attempts are left, so the adapter goes on trying");
		AreEqual(3, state.ConnectingAttemptCount, "a timed out attempt is not counted here, it is counted when the next one is made");
	}

	/// <summary>
	/// The same for the way out: a disconnect that hangs is reported as a failed disconnect, so that
	/// whoever is shutting down is not left waiting on a connection that will never say goodbye.
	/// </summary>
	[TestMethod]
	public async Task DisconnectTimeout_IsReportedAsADisconnectError()
	{
		var state = new HeartbeatManagerState
		{
			CurrentState = ConnectionStates.Disconnecting,
			PreviousState = ConnectionStates.Connected,
			ConnectingAttemptCount = 3,
			ConnectionTimeOut = TimeSpan.FromSeconds(5),
			IsFirstTimeConnect = false,
		};

		var (adapter, _, outMessages) = CreateSut(_midday, state);

		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromSeconds(6));

		var disconnect = outMessages.OfType<DisconnectMessage>().Single();
		IsNotNull(disconnect.Error, "the caller is told the disconnect did not complete");
		IsInstanceOfType<TimeoutException>(disconnect.Error);
	}

	/// <summary>
	/// Suppressing reconnection errors is about not crying wolf while the adapter is quietly trying
	/// again. It is not about hiding the failure: the attempts that are left are what buys the
	/// silence.
	/// </summary>
	[TestMethod]
	public async Task WhileAttemptsRemain_ASuppressedTimeoutIsNotShownToTheUser()
	{
		var state = new HeartbeatManagerState
		{
			CurrentState = ConnectionStates.Connecting,
			PreviousState = HeartbeatManagerState.None,
			ConnectingAttemptCount = 3,
			ConnectionTimeOut = TimeSpan.FromSeconds(5),
			IsFirstTimeConnect = false,
		};

		var (adapter, _, outMessages) = CreateSut(_midday, state);
		adapter.SuppressReconnectingErrors = true;

		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromSeconds(6));

		outMessages.OfType<ConnectMessage>().Count().AssertEqual(0, "the adapter is still trying, so the user is not alarmed yet");
		AreEqual(ConnectionStates.Reconnecting, state.CurrentState, "and it really is still trying");
	}

	/// <summary>
	/// Once the attempts are used up there is nothing left to be quiet about, so even with errors
	/// suppressed the user learns that the connection is not coming back.
	/// </summary>
	[TestMethod]
	public async Task WhenTheAttemptsAreUsedUp_ASuppressedTimeoutIsStillReported()
	{
		var state = new HeartbeatManagerState
		{
			CurrentState = ConnectionStates.Connecting,
			PreviousState = HeartbeatManagerState.None,
			ConnectingAttemptCount = 0,
			ConnectionTimeOut = TimeSpan.FromSeconds(5),
			IsFirstTimeConnect = false,
		};

		var (adapter, _, outMessages) = CreateSut(_midday, state);
		adapter.SuppressReconnectingErrors = true;

		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromSeconds(6));

		var connect = outMessages.OfType<ConnectMessage>().Single();
		IsInstanceOfType<TimeoutException>(connect.Error, "the last attempt failed, so the failure is the user's to see");
	}

	/// <summary>
	/// A connection that comes back gets a full budget of attempts again. Without that, an adapter
	/// that reconnected after a rough day would drop for good at the next brief outage.
	/// </summary>
	[TestMethod]
	public async Task AConnectionThatComesBack_GetsItsReconnectionAttemptsBack()
	{
		var state = new HeartbeatManagerState
		{
			CurrentState = ConnectionStates.Connecting,
			PreviousState = ConnectionStates.Reconnecting,
			ConnectingAttemptCount = 0,
			ConnectionTimeOut = TimeSpan.Zero,
			IsFirstTimeConnect = false,
		};

		var (adapter, inner, outMessages) = CreateSut(_midday, state);
		adapter.ReConnectionSettings.ReAttemptCount = 7;

		await inner.SendOutMessageAsync(new ConnectMessage(), CancellationToken);

		AreEqual(ConnectionStates.Connected, state.CurrentState);
		AreEqual(7, state.ConnectingAttemptCount, "the next outage gets the whole budget of attempts again");
		AreEqual(adapter.ReConnectionSettings.Interval, state.ConnectionTimeOut);

		outMessages.OfType<ConnectMessage>().Count(m => m.IsOk()).AssertEqual(1, "and the caller is told the connection is up");
	}

	/// <summary>
	/// A reset means the caller has given up on this connection. Whatever the adapter was in the
	/// middle of, it must stop: a reconnect after a reset would revive a connection nobody asked for.
	/// </summary>
	[TestMethod]
	public async Task AResetInTheMiddleOfReconnecting_StopsTheReconnecting()
	{
		var state = new HeartbeatManagerState
		{
			CurrentState = ConnectionStates.Reconnecting,
			PreviousState = ConnectionStates.Connected,
			ConnectingAttemptCount = 5,
			ConnectionTimeOut = TimeSpan.Zero,
			IsFirstTimeConnect = false,
		};

		var (adapter, _, outMessages) = CreateSut(_midday, state);

		await adapter.SendInMessageAsync(new ResetMessage(), CancellationToken);

		AreEqual(HeartbeatManagerState.None, state.CurrentState);
		AreEqual(0, state.ConnectingAttemptCount);

		await InvokeProcessReconnectionAsync(adapter, TimeSpan.FromMinutes(5));

		AreEqual(0, outMessages.Count(m => m.Type == ReconnectType), "a reset connection is not reconnected behind the caller's back");
	}

	#endregion

	#region Probing only an idle link

	/// <summary>
	/// A link that is carrying messages has already answered the only question a heartbeat asks, so
	/// asking it again puts a probe on the wire for nothing - and on a protocol that runs its own
	/// session keepalive, a second one beside the adapter's.
	/// </summary>
	[TestMethod]
	public async Task ALinkThatIsCarryingMessagesIsNotProbed()
	{
		var (adapter, inner, outMessages) = CreateSut(_midday, new HeartbeatManagerState
		{
			CurrentState = ConnectionStates.Connected,
			CanSendTime = true,
		});

		adapter.HeartbeatInterval = TimeSpan.FromSeconds(10);

		await adapter.SendInMessageAsync(new TimeFrameCandleMessage(), CancellationToken);

		inner.Time = _midday.AddSeconds(5);
		outMessages.Clear();

		await adapter.ProcessHeartbeat(CancellationToken);

		IsFalse(outMessages.OfType<TimeMessage>().Any(), "the link spoke within the interval, so there is nothing to ask it");
	}

	/// <summary>
	/// A link that has gone quiet for longer than the interval is asked, which is what tells a live
	/// but silent connection from one that is gone.
	/// </summary>
	[TestMethod]
	public async Task ALinkThatHasGoneQuietIsProbed()
	{
		var (adapter, inner, outMessages) = CreateSut(_midday, new HeartbeatManagerState
		{
			CurrentState = ConnectionStates.Connected,
			CanSendTime = true,
		});

		adapter.HeartbeatInterval = TimeSpan.FromSeconds(10);

		await adapter.SendInMessageAsync(new TimeFrameCandleMessage(), CancellationToken);

		inner.Time = _midday.AddSeconds(30);
		outMessages.Clear();

		await adapter.ProcessHeartbeat(CancellationToken);

		IsTrue(outMessages.OfType<TimeMessage>().Any(), "nothing has come for three intervals, so the link has to be asked");
	}

	/// <summary>
	/// What the inner adapter sends up counts as the link speaking, the same as what is sent down it.
	/// </summary>
	[TestMethod]
	public async Task WhatArrivesFromTheVenueCountsAsTheLinkSpeaking()
	{
		var (adapter, inner, outMessages) = CreateSut(_midday, new HeartbeatManagerState
		{
			CurrentState = ConnectionStates.Connected,
			CanSendTime = true,
		});

		adapter.HeartbeatInterval = TimeSpan.FromSeconds(10);

		inner.Time = _midday.AddSeconds(30);
		await inner.SendOutMessageAsync(new TimeFrameCandleMessage(), CancellationToken);

		inner.Time = _midday.AddSeconds(35);
		outMessages.Clear();

		await adapter.ProcessHeartbeat(CancellationToken);

		IsFalse(outMessages.OfType<TimeMessage>().Any(), "the venue spoke within the interval, so there is nothing to ask it");
	}

	#endregion

}
