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

	private const ConnectionStates _none = (ConnectionStates)(-1);

	private readonly AsyncLock _sync = new();
	private readonly TimeMessage _timeMessage = new() { OfflineMode = MessageOfflineModes.Ignore };

	private readonly ReConnectionSettings _reConnectionSettings;

	private ConnectionStates _currState = _none;
	private ConnectionStates _prevState = _none;

	private int _connectingAttemptCount;
	private TimeSpan _connectionTimeOut;
	private ControllablePeriodicTimer _timer;
	private bool _canSendTime;
	private bool _isFirstTimeConnect = true;
	private bool _suppressDisconnectError;

	/// <summary>
	/// Initializes a new instance of the <see cref="HeartbeatMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	public HeartbeatMessageAdapter(IMessageAdapter innerAdapter)
		: base(innerAdapter)
	{
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
					isRestored = _currState == ConnectionStates.Connecting && (_prevState == ConnectionStates.Failed || _prevState == ConnectionStates.Reconnecting);

					using (await _sync.LockAsync(cancellationToken))
					{
						_prevState = _currState = ConnectionStates.Connected;

						_connectionTimeOut = _reConnectionSettings.Interval;
						_connectingAttemptCount = _reConnectionSettings.ReAttemptCount;
					}
				}
				else
				{
					using (await _sync.LockAsync(cancellationToken))
					{
						if (_connectingAttemptCount != 0)
						{
							isReconnectionStarted = _prevState == ConnectionStates.Connected;

							_prevState = _currState == ConnectionStates.Connected
								? ConnectionStates.Reconnecting
								: ConnectionStates.Failed;

							_currState = ConnectionStates.Reconnecting;
							isReconnecting = true;
						}
						else
							_prevState = _currState = ConnectionStates.Failed;
					}

					this.AddLog(LogLevels.Warning, () => $"RCM: got error, new state={_currState}\n{connectMsg.Error}");
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
						_prevState = _currState = ConnectionStates.Disconnected;
				}
				else
				{
					Message errorMsg = null;

					using (await _sync.LockAsync(cancellationToken))
					{
						if (_suppressDisconnectError)
						{
							_suppressDisconnectError = false;
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
				_prevState = _none;

				using (await _sync.LockAsync(cancellationToken))
				{
					_currState = _none;

					StopTimer();

					_connectingAttemptCount = 0;
					_connectionTimeOut = default;
					_canSendTime = false;
					_suppressDisconnectError = false;
				}

				break;
			}

			case MessageTypes.Connect:
			{
				if (_isFirstTimeConnect)
					_isFirstTimeConnect = false;
				else
					await base.OnSendInMessageAsync(new ResetMessage(), cancellationToken);

				using (await _sync.LockAsync(cancellationToken))
				{
					_currState = ConnectionStates.Connecting;

					if (_prevState == _none)
					{
						_connectionTimeOut = _reConnectionSettings.TimeOutInterval;
						_connectingAttemptCount = _reConnectionSettings.AttemptCount;
					}

					isStartTimer = true;
				}

				break;
			}
			case MessageTypes.Disconnect:
			{
				using (await _sync.LockAsync(cancellationToken))
				{
					_suppressDisconnectError = _timer != null;

					_currState = ConnectionStates.Disconnecting;
					_connectionTimeOut = _reConnectionSettings.TimeOutInterval;

					StopTimer();
					_canSendTime = false;
				}

				break;
			}

			case MessageTypes.Time:
			{
				if (_timeMessage == message)
				{
					using (await _sync.LockAsync(cancellationToken))
					{
						if (_currState is ConnectionStates.Disconnecting or ConnectionStates.Disconnected)
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
				if (isStartTimer && (_currState == ConnectionStates.Connecting || _currState == ConnectionStates.Connected))
					StartTimer(cancellationToken);
			}

			return;
		}
		finally
		{
			if (message == _timeMessage)
			{
				using (await _sync.LockAsync(cancellationToken))
					_canSendTime = true;
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

		var time = CurrentTimeUtc;
		var lastHeartBeatTime = time;

		var sync = new Lock();
		var isProcessing = false;

		if (needHeartbeat)
		{
			_canSendTime = true;
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
					var now = CurrentTimeUtc;
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
						await RaiseNewOutMessageAsync(new TimeMessage { LocalTime = CurrentTimeUtc }, cancellationToken);
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
		switch (_currState)
		{
			case ConnectionStates.Disconnecting:
			case ConnectionStates.Connecting:
			{
				_connectionTimeOut -= diff;

				if (_connectionTimeOut <= TimeSpan.Zero)
				{
					LogWarning("RCM: Connecting Timeout Left {0}.", _connectionTimeOut);

					switch (_currState)
					{
						case ConnectionStates.Connecting:
							if (!SuppressReconnectingErrors || _connectingAttemptCount == 0)
								await RaiseNewOutMessageAsync(new ConnectMessage { Error = new TimeoutException(LocalizedStrings.ConnectionTimeout) }, cancellationToken);
							break;
						case ConnectionStates.Disconnecting:
							await RaiseNewOutMessageAsync(new DisconnectMessage { Error = new TimeoutException(LocalizedStrings.DisconnectTimeout) }, cancellationToken);
							break;
					}

					if (_prevState != _none)
					{
						LogInfo("RCM: Connecting AttemptError.");

						using (await _sync.LockAsync(cancellationToken))
							_currState = _prevState;
					}
					else
					{
						if (_currState == ConnectionStates.Connecting && _connectingAttemptCount != 0)
						{
							using (await _sync.LockAsync(cancellationToken))
								_currState = ConnectionStates.Reconnecting;

							LogInfo("RCM: To Reconnecting Attempts {0} Timeout {1}.", _connectingAttemptCount, _connectionTimeOut);
						}
						else
						{
							using (await _sync.LockAsync(cancellationToken))
								_currState = _none;
						}
					}
				}

				break;
			}
			case ConnectionStates.Reconnecting:
			{
				if (_connectingAttemptCount == 0)
				{
					LogWarning("RCM: Reconnecting attempts {0} PrevState {1}.", _connectingAttemptCount, FormatState(_prevState));

					using (await _sync.LockAsync(cancellationToken))
						_currState = _none;

					break;
				}

				_connectionTimeOut -= diff;

				if (_connectionTimeOut > TimeSpan.Zero)
					break;

				if (_reConnectionSettings.WorkingTime.IsTradeTime(CurrentTimeUtc, out _, out _))
				{
					LogInfo("RCM: To Connecting. CurrState {0} PrevState {1} Attempts {2}.", FormatState(_currState), FormatState(_prevState), _connectingAttemptCount);

					if (_connectingAttemptCount != -1)
						_connectingAttemptCount--;

					_connectionTimeOut = _reConnectionSettings.Interval;

					await RaiseNewOutMessageAsync(new ReconnectMessage().LoopBack(this), cancellationToken);
				}
				else
				{
					LogWarning("RCM: Out of trade time. CurrState {0}.", FormatState(_currState));
					_connectionTimeOut = TimeSpan.FromMinutes(1);
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

			case _none:
				return LocalizedStrings.None;

			default:
				return state.GetDisplayName();
		}
	}

	private async ValueTask ProcessHeartbeat(CancellationToken cancellationToken)
	{
		using (await _sync.LockAsync(cancellationToken))
		{
			if (_currState != ConnectionStates.Connected && !InnerAdapter.HeartbeatBeforConnect)
				return;

			if (!_canSendTime)
				return;

			_canSendTime = false;
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
