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

	private readonly SyncObject _timeSync = new();
	private readonly TimeMessage _timeMessage = new() { OfflineMode = MessageOfflineModes.Ignore };

	private readonly ReConnectionSettings _reConnectionSettings;

	private ConnectionStates _currState = _none;
	private ConnectionStates _prevState = _none;

	private int _connectingAttemptCount;
	private TimeSpan _connectionTimeOut;
	private Timer _timer;
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
	protected override void OnInnerAdapterNewOutMessage(Message message)
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

					lock (_timeSync)
					{
						_prevState = _currState = ConnectionStates.Connected;

						_connectionTimeOut = _reConnectionSettings.Interval;
						_connectingAttemptCount = _reConnectionSettings.ReAttemptCount;
					}
				}
				else
				{
					lock (_timeSync)
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
						RaiseNewOutMessage(new ConnectionRestoredMessage { IsResetState = true, Adapter = message.Adapter });
					else
						RaiseNewOutMessage(new RestoredConnectMessage { Adapter = message.Adapter });
				}
				else
				{
					if (connectMsg.IsOk() || !SuppressReconnectingErrors || !isReconnecting)
						base.OnInnerAdapterNewOutMessage(message);
					else if (isReconnectionStarted)
					{
						LogInfo(LocalizedStrings.Reconnecting);
						base.OnInnerAdapterNewOutMessage(new ConnectionLostMessage { IsResetState = true, Adapter = message.Adapter });
					}
				}

				break;
			}
			case MessageTypes.Disconnect:
			{
				var disconnectMsg = (DisconnectMessage)message;

				if (disconnectMsg.IsOk())
				{
					lock (_timeSync)
						_prevState = _currState = ConnectionStates.Disconnected;
				}
				else
				{
					lock (_timeSync)
					{
						if (_suppressDisconnectError)
						{
							_suppressDisconnectError = false;
							RaiseNewOutMessage(disconnectMsg.Error.ToErrorMessage());
							disconnectMsg.Error = null;
						}
					}
				}

				base.OnInnerAdapterNewOutMessage(message);
				break;
			}

			default:
				base.OnInnerAdapterNewOutMessage(message);
				break;
		}
	}

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		var isStartTimer = false;

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_prevState = _none;

				lock (_timeSync)
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
					base.OnSendInMessage(new ResetMessage());

				lock (_timeSync)
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
				lock (_timeSync)
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
					lock (_timeSync)
					{
						if (_currState is ConnectionStates.Disconnecting or ConnectionStates.Disconnected)
							return true;
					}
				}

				break;
			}

			case ExtendedMessageTypes.Reconnect:
			{
				return OnSendInMessage(new ConnectMessage());
			}
		}

		try
		{
			var result = base.OnSendInMessage(message);

			lock (_timeSync)
			{
				if (isStartTimer && (_currState == ConnectionStates.Connecting || _currState == ConnectionStates.Connected))
					StartTimer();
			}

			return result;
		}
		finally
		{
			if (message == _timeMessage)
			{
				lock (_timeSync)
					_canSendTime = true;
			}
		}
	}

	private void StartTimer()
	{
		if (_timer != null)
			return;

		var period = ReConnectionSettings.Interval;
		var heartbeat = HeartbeatInterval;
		var needHeartbeat = heartbeat > TimeSpan.Zero;

		var time = CurrentTime;
		var lastHeartBeatTime = time;

		var sync = new SyncObject();
		var isProcessing = false;

		if (needHeartbeat)
		{
			_canSendTime = true;
			period = period.Min(heartbeat);
		}

		var outMsgIntervalInitial = TimeSpan.FromSeconds(5);
		var outMsgInterval = outMsgIntervalInitial;

		_timer = ThreadingHelper
		    .Timer(() =>
		    {
			    lock (sync)
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
					    ProcessHeartbeat();
					    lastHeartBeatTime = now;
				    }

				    ProcessReconnection(diff);

					outMsgInterval -= diff;

					if (outMsgInterval <= TimeSpan.Zero)
					{
						outMsgInterval = outMsgIntervalInitial;
						RaiseNewOutMessage(new TimeMessage { LocalTime = CurrentTime });
					}

				    time = now;
			    }
			    catch (Exception ex)
			    {
				    this.AddErrorLog(ex);
			    }
			    finally
			    {
				    lock (sync)
					    isProcessing = false;
			    }
		    })
		    .Interval(period.Min(outMsgIntervalInitial).Max(TimeSpan.FromSeconds(1)));
	}

	private void StopTimer()
	{
		if (_timer == null)
			return;

		_timer.Dispose();
		_timer = null;
	}

	private void ProcessReconnection(TimeSpan diff)
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
								RaiseNewOutMessage(new ConnectMessage { Error = new TimeoutException(LocalizedStrings.ConnectionTimeout) });
							break;
						case ConnectionStates.Disconnecting:
							RaiseNewOutMessage(new DisconnectMessage { Error = new TimeoutException(LocalizedStrings.DisconnectTimeout) });
							break;
					}

					if (_prevState != _none)
					{
						LogInfo("RCM: Connecting AttemptError.");

						lock (_timeSync)
							_currState = _prevState;
					}
					else
					{
						if (_currState == ConnectionStates.Connecting && _connectingAttemptCount != 0)
						{
							lock (_timeSync)
								_currState = ConnectionStates.Reconnecting;

							LogInfo("RCM: To Reconnecting Attempts {0} Timeout {1}.", _connectingAttemptCount, _connectionTimeOut);
						}
						else
						{
							lock (_timeSync)
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

					lock (_timeSync)
						_currState = _none;

					break;
				}

				_connectionTimeOut -= diff;

				if (_connectionTimeOut > TimeSpan.Zero)
					break;

				if (_reConnectionSettings.WorkingTime.IsTradeTime(CurrentTime.UtcDateTime, out _, out _))
				{
					LogInfo("RCM: To Connecting. CurrState {0} PrevState {1} Attempts {2}.", FormatState(_currState), FormatState(_prevState), _connectingAttemptCount);

					if (_connectingAttemptCount != -1)
						_connectingAttemptCount--;

					_connectionTimeOut = _reConnectionSettings.Interval;

					RaiseNewOutMessage(new ReconnectMessage().LoopBack(this));
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

	private void ProcessHeartbeat()
	{
		lock (_timeSync)
		{
			if (_currState != ConnectionStates.Connected && !InnerAdapter.HeartbeatBeforConnect)
				return;

			if (!_canSendTime)
				return;

			_canSendTime = false;
		}

		_timeMessage.LoopBack(this);
		_timeMessage.TransactionId = TransactionIdGenerator.GetNextId();

		RaiseNewOutMessage(_timeMessage);
	}

	/// <summary>
	/// Create a copy of <see cref="HeartbeatMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new HeartbeatMessageAdapter(InnerAdapter.TypedClone()) { SuppressReconnectingErrors = SuppressReconnectingErrors };
	}
}
