namespace StockSharp.Algo;

/// <summary>
/// The messages adapter controlling the connection.
/// </summary>
public class HeartbeatMessageAdapter : MessageAdapterWrapper
{
	private class RestoredConnectMessage : ConnectMessage
	{
	}

	private class ReconnectMessage : Message
	{
		public ReconnectMessage()
			: base(ExtendedMessageTypes.Reconnect)
		{
		}

		public override Message Clone()
		{
			return new ReconnectMessage();
		}
	}

	private readonly AsyncLock _sync = new();
	private readonly TimeMessage _timeMessage = new() { OfflineMode = MessageOfflineModes.Ignore };

	private readonly ReConnectionSettings _reConnectionSettings;
	private readonly IHeartbeatManagerState _state;

	private ControllablePeriodicTimer _timer;

	/// <summary>
	/// Initializes a new instance of the <see cref="HeartbeatMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	public HeartbeatMessageAdapter(IMessageAdapter innerAdapter)
		: this(innerAdapter, new HeartbeatManagerState())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="HeartbeatMessageAdapter"/> with explicit state.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	/// <param name="state">State storage.</param>
	public HeartbeatMessageAdapter(IMessageAdapter innerAdapter, IHeartbeatManagerState state)
		: base(innerAdapter)
	{
		_state = state ?? throw new ArgumentNullException(nameof(state));
		_reConnectionSettings = ReConnectionSettings;
		_timeMessage.Adapter = this;
	}

	/// <summary>
	/// Suppress reconnecting errors.
	/// </summary>
	public bool SuppressReconnectingErrors { get; set; }

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.Connect:
			{
				var connectMsg = (ConnectMessage)message;
				var isRestored = false;
				var isReconnecting = false;
				var isReconnectionStarted = false;

				if (connectMsg.IsOk())
				{
					isRestored = _state.CurrentState == ConnectionStates.Connecting && (_state.PreviousState == ConnectionStates.Failed || _state.PreviousState == ConnectionStates.Reconnecting);

					using (await _sync.LockAsync(cancellationToken))
					{
						_state.PreviousState = _state.CurrentState = ConnectionStates.Connected;

						_state.ConnectionTimeOut = _reConnectionSettings.Interval;
						_state.ConnectingAttemptCount = _reConnectionSettings.ReAttemptCount;
					}
				}
				else
				{
					using (await _sync.LockAsync(cancellationToken))
					{
						if (_state.ConnectingAttemptCount != 0)
						{
							isReconnectionStarted = _state.PreviousState == ConnectionStates.Connected;

							_state.PreviousState = _state.CurrentState == ConnectionStates.Connected
								? ConnectionStates.Reconnecting
								: ConnectionStates.Failed;

							_state.CurrentState = ConnectionStates.Reconnecting;
							isReconnecting = true;
						}
						else
							_state.PreviousState = _state.CurrentState = ConnectionStates.Failed;
					}

					this.AddLog(LogLevels.Warning, () => $"RCM: got error, new state={_state.CurrentState}\n{connectMsg.Error}");
				}

				if (isRestored)
				{
					LogInfo(LocalizedStrings.ConnectionRestored);

					if (SuppressReconnectingErrors)
						await RaiseNewOutMessageAsync(new ConnectionRestoredMessage { IsResetState = true, Adapter = message.Adapter }, cancellationToken);
					else
						await RaiseNewOutMessageAsync(new RestoredConnectMessage { Adapter = message.Adapter }, cancellationToken);
				}
				else
				{
					if (connectMsg.IsOk() || !SuppressReconnectingErrors || !isReconnecting)
						await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
					else if (isReconnectionStarted)
					{
						LogInfo(LocalizedStrings.Reconnecting);
						await base.OnInnerAdapterNewOutMessageAsync(new ConnectionLostMessage { IsResetState = true, Adapter = message.Adapter }, cancellationToken);
					}
				}

				break;
			}
			case MessageTypes.Disconnect:
			{
				var disconnectMsg = (DisconnectMessage)message;

				if (disconnectMsg.IsOk())
				{
					using (await _sync.LockAsync(cancellationToken))
						_state.PreviousState = _state.CurrentState = ConnectionStates.Disconnected;
				}
				else
				{
					Message errorMsg = null;

					using (await _sync.LockAsync(cancellationToken))
					{
						if (_state.SuppressDisconnectError)
						{
							_state.SuppressDisconnectError = false;
							errorMsg = disconnectMsg.Error.ToErrorMessage();
							disconnectMsg.Error = null;
						}
					}

					if (errorMsg != null)
						await RaiseNewOutMessageAsync(errorMsg, cancellationToken);
				}

				await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
				break;
			}

			default:
				await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
				break;
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var isStartTimer = false;

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_state.PreviousState = HeartbeatManagerState.None;

				using (await _sync.LockAsync(cancellationToken))
				{
					_state.CurrentState = HeartbeatManagerState.None;

					StopTimer();

					_state.ConnectingAttemptCount = 0;
					_state.ConnectionTimeOut = default;
					_state.CanSendTime = false;
					_state.SuppressDisconnectError = false;
				}

				break;
			}

			case MessageTypes.Connect:
			{
				if (_state.IsFirstTimeConnect)
					_state.IsFirstTimeConnect = false;
				else
					await base.OnSendInMessageAsync(new ResetMessage(), cancellationToken);

				using (await _sync.LockAsync(cancellationToken))
				{
					_state.CurrentState = ConnectionStates.Connecting;

					if (_state.PreviousState == HeartbeatManagerState.None)
					{
						_state.ConnectionTimeOut = _reConnectionSettings.TimeOutInterval;
						_state.ConnectingAttemptCount = _reConnectionSettings.AttemptCount;
					}

					isStartTimer = true;
				}

				break;
			}
			case MessageTypes.Disconnect:
			{
				using (await _sync.LockAsync(cancellationToken))
				{
					_state.SuppressDisconnectError = _timer != null;

					_state.CurrentState = ConnectionStates.Disconnecting;
					_state.ConnectionTimeOut = _reConnectionSettings.TimeOutInterval;

					StopTimer();
					_state.CanSendTime = false;
				}

				break;
			}

			case MessageTypes.Time:
			{
				if (_timeMessage == message)
				{
					using (await _sync.LockAsync(cancellationToken))
					{
						if (_state.CurrentState is ConnectionStates.Disconnecting or ConnectionStates.Disconnected)
							return;
					}
				}

				break;
			}

			case ExtendedMessageTypes.Reconnect:
			{
				await OnSendInMessageAsync(new ConnectMessage(), cancellationToken);
				return;
			}
		}

		try
		{
			await base.OnSendInMessageAsync(message, cancellationToken);

			using (await _sync.LockAsync(cancellationToken))
			{
				if (isStartTimer && (_state.CurrentState == ConnectionStates.Connecting || _state.CurrentState == ConnectionStates.Connected))
					StartTimer(cancellationToken);
			}

			return;
		}
		finally
		{
			if (message == _timeMessage)
			{
				using (await _sync.LockAsync(cancellationToken))
					_state.CanSendTime = true;
			}
		}
	}

	private void StartTimer(CancellationToken cancellationToken)
	{
		if (_timer != null)
			return;

		var period = ReConnectionSettings.Interval;
		var heartbeat = HeartbeatInterval;
		var needHeartbeat = heartbeat > TimeSpan.Zero;

		var time = CurrentTime;
		var lastHeartBeatTime = time;

		var sync = new Lock();
		var isProcessing = false;

		if (needHeartbeat)
		{
			_state.CanSendTime = true;
			period = period.Min(heartbeat);
		}

		var outMsgIntervalInitial = TimeSpan.FromSeconds(5);
		var outMsgInterval = outMsgIntervalInitial;

		_timer = AsyncHelper
			.CreatePeriodicTimer(async () =>
			{
				using (sync.EnterScope())
				{
					if (isProcessing)
						return;

					isProcessing = true;
				}

				try
				{
					var now = CurrentTime;
					var diff = now - time;

					if (needHeartbeat && (now - lastHeartBeatTime) >= heartbeat)
					{
						await ProcessHeartbeat(cancellationToken);
						lastHeartBeatTime = now;
					}

					await ProcessReconnection(diff, cancellationToken);

					outMsgInterval -= diff;

					if (outMsgInterval <= TimeSpan.Zero)
					{
						outMsgInterval = outMsgIntervalInitial;
						await RaiseNewOutMessageAsync(new TimeMessage { LocalTime = CurrentTime }, cancellationToken);
					}

					time = now;
				}
				catch (Exception ex)
				{
					if (!cancellationToken.IsCancellationRequested)
						this.AddErrorLog(ex);
				}
				finally
				{
					using (sync.EnterScope())
						isProcessing = false;
				}
			})
			.Start(period.Min(outMsgIntervalInitial).Max(TimeSpan.FromSeconds(1)), cancellationToken: cancellationToken);
	}

	private void StopTimer()
	{
		if (_timer == null)
			return;

		_timer.Dispose();
		_timer = null;
	}

	private async ValueTask ProcessReconnection(TimeSpan diff, CancellationToken cancellationToken)
	{
		switch (_state.CurrentState)
		{
			case ConnectionStates.Disconnecting:
			case ConnectionStates.Connecting:
			{
				_state.ConnectionTimeOut -= diff;

				if (_state.ConnectionTimeOut <= TimeSpan.Zero)
				{
					LogWarning("RCM: Connecting Timeout Left {0}.", _state.ConnectionTimeOut);

					switch (_state.CurrentState)
					{
						case ConnectionStates.Connecting:
							if (!SuppressReconnectingErrors || _state.ConnectingAttemptCount == 0)
								await RaiseNewOutMessageAsync(new ConnectMessage { Error = new TimeoutException(LocalizedStrings.ConnectionTimeout) }, cancellationToken);
							break;
						case ConnectionStates.Disconnecting:
							await RaiseNewOutMessageAsync(new DisconnectMessage { Error = new TimeoutException(LocalizedStrings.DisconnectTimeout) }, cancellationToken);
							break;
					}

					if (_state.PreviousState != HeartbeatManagerState.None)
					{
						LogInfo("RCM: Connecting AttemptError.");

						using (await _sync.LockAsync(cancellationToken))
							_state.CurrentState = _state.PreviousState;
					}
					else
					{
						if (_state.CurrentState == ConnectionStates.Connecting && _state.ConnectingAttemptCount != 0)
						{
							using (await _sync.LockAsync(cancellationToken))
								_state.CurrentState = ConnectionStates.Reconnecting;

							LogInfo("RCM: To Reconnecting Attempts {0} Timeout {1}.", _state.ConnectingAttemptCount, _state.ConnectionTimeOut);
						}
						else
						{
							using (await _sync.LockAsync(cancellationToken))
								_state.CurrentState = HeartbeatManagerState.None;
						}
					}
				}

				break;
			}
			case ConnectionStates.Reconnecting:
			{
				if (_state.ConnectingAttemptCount == 0)
				{
					LogWarning("RCM: Reconnecting attempts {0} PrevState {1}.", _state.ConnectingAttemptCount, FormatState(_state.PreviousState));

					using (await _sync.LockAsync(cancellationToken))
						_state.CurrentState = HeartbeatManagerState.None;

					break;
				}

				_state.ConnectionTimeOut -= diff;

				if (_state.ConnectionTimeOut > TimeSpan.Zero)
					break;

				if (_reConnectionSettings.WorkingTime.IsTradeTime(CurrentTime, out _, out _))
				{
					LogInfo("RCM: To Connecting. CurrState {0} PrevState {1} Attempts {2}.", FormatState(_state.CurrentState), FormatState(_state.PreviousState), _state.ConnectingAttemptCount);

					if (_state.ConnectingAttemptCount != -1)
						_state.ConnectingAttemptCount--;

					_state.ConnectionTimeOut = _reConnectionSettings.Interval;

					await RaiseNewOutMessageAsync(new ReconnectMessage().LoopBack(this), cancellationToken);
				}
				else
				{
					LogWarning("RCM: Out of trade time. CurrState {0}.", FormatState(_state.CurrentState));
					_state.ConnectionTimeOut = TimeSpan.FromMinutes(1);
				}

				break;
			}
		}
	}

	private static string FormatState(ConnectionStates state)
	{
		switch (state)
		{
			case ConnectionStates.Reconnecting:
				return LocalizedStrings.Reconnecting;

			case HeartbeatManagerState.None:
				return LocalizedStrings.None;

			default:
				return state.GetDisplayName();
		}
	}

	private async ValueTask ProcessHeartbeat(CancellationToken cancellationToken)
	{
		using (await _sync.LockAsync(cancellationToken))
		{
			if (_state.CurrentState != ConnectionStates.Connected && !InnerAdapter.HeartbeatBeforeConnect)
				return;

			if (!_state.CanSendTime)
				return;

			_state.CanSendTime = false;
		}

		_timeMessage.LoopBack(this);
		_timeMessage.TransactionId = TransactionIdGenerator.GetNextId();

		await RaiseNewOutMessageAsync(_timeMessage, cancellationToken);
	}

	/// <summary>
	/// Create a copy of <see cref="HeartbeatMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		return new HeartbeatMessageAdapter(InnerAdapter.TypedClone()) { SuppressReconnectingErrors = SuppressReconnectingErrors };
	}
}
