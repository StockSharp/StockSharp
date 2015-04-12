namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Threading;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	partial class Connector
	{
		private class HeartbeatAdapter : IMessageAdapter
		{
			private readonly IMessageAdapter _adapter;
			private readonly ReConnectionSettings.Settings _settings;

			private readonly SyncObject _timeSync = new SyncObject();

			// дополнительные состояния для ConnectionStates
			private const ConnectionStates _none = (ConnectionStates)0 - 1;
			private const ConnectionStates _reConnecting = (ConnectionStates)10;
			private const ConnectionStates _reStartingExport = (ConnectionStates)11;

			private ConnectionStates _currState = _none;
			private ConnectionStates _prevState = _none;

			//private int _connectingAttemptCount;
			//private TimeSpan _connectionTimeOut;

			private Timer _heartBeatTimer;

			private readonly TimeMessage _timeMessage = new TimeMessage();
			private bool _isTimeMessageHandled;

			public HeartbeatAdapter(IMessageAdapter adapter, ReConnectionSettings.Settings settings)
			{
				if (adapter == null)
					throw new ArgumentNullException("adapter");

				if (settings == null)
					throw new ArgumentNullException("settings");

				_adapter = adapter;
				_settings = settings;

				_adapter.NewOutMessage += AdapterOnNewOutMessage;
			}

			private void AdapterOnNewOutMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					{
						if (((ConnectMessage)message).Error == null)
						{
							lock (_timeSync)
								_prevState = _currState = ConnectionStates.Connected;

							// heart beat is disabled
							if (_adapter.HeartbeatInterval == TimeSpan.Zero)
								return;

							_isTimeMessageHandled = true;
							_heartBeatTimer = ThreadingHelper.Timer(OnHeartbeatTimer).Interval(_adapter.HeartbeatInterval);
						}
						else
						{
							lock (_timeSync)
								_prevState = _currState = ConnectionStates.Failed;
						}

						break;
					}
					case MessageTypes.Disconnect:
					{
						lock (_timeSync)
							_prevState = _currState = ConnectionStates.Disconnected;

						_heartBeatTimer.Dispose();
						_heartBeatTimer = null;
						break;
					}

					case MessageTypes.Time:
					{
						if (message == _timeMessage)
						{
							lock (_timeSync)
								_isTimeMessageHandled = true;
						}

						break;
					}
				}
			}

			void IMessageChannel.SendInMessage(Message message)
			{
				//case MessageTypes.Connect:
				//{
				//	lock (_timeSync)
				//	{
				//		_canSendTimeIn = true;
				//		_currState = ConnectionStates.Connecting;
				//	}

				//	if (_prevState == _none)
				//	{
				//		_connectionTimeOut = ReConnectionSettings.TimeOutInterval;
				//		_connectingAttemptCount = ReConnectionSettings.AttemptCount;
				//	}
				//	else
				//		_connectionTimeOut = ReConnectionSettings.Interval;

				//	break;
				//}
				//case MessageTypes.Disconnect:
				//{
				//	lock (_timeSync)
				//		_currState = ConnectionStates.Disconnecting;

				//	_connectionTimeOut = ReConnectionSettings.TimeOutInterval;

				//	break;
				//}
				//case MessageTypes.Time:
				//{
				//	lock (_timeSync)
				//		_canSendTimeIn = true;

				//	break;
				//}

				_adapter.SendInMessage(message);
			}

			private void OnHeartbeatTimer()
			{
				lock (_timeSync)
				{
					if (_currState != ConnectionStates.Connected)
						return;

					if (!_isTimeMessageHandled)
						return;
				}

				_timeMessage.IsBack = true;
				_adapter.SendInMessage(_timeMessage);
			}

			//private void ProcessReconnection(TimeSpan diff)
			//{
			//	switch (_currState)
			//	{
			//		case ConnectionStates.Disconnecting:
			//		case ConnectionStates.Connecting:
			//		{
			//			_connectionTimeOut -= diff;

			//			if (_connectionTimeOut <= TimeSpan.Zero)
			//			{
			//				this.AddWarningLog("RCM: Connecting Timeout Left {0}.", _connectionTimeOut);

			//				switch (_currState)
			//				{
			//					case ConnectionStates.Connecting:
			//						SendOutMessage(new ConnectMessage { Error = new TimeoutException(LocalizedStrings.Str170) });
			//						break;
			//					case ConnectionStates.Disconnecting:
			//						SendOutMessage(new DisconnectMessage { Error = new TimeoutException(LocalizedStrings.Str171) });
			//						break;
			//				}

			//				if (_prevState != _none)
			//				{
			//					this.AddInfoLog("RCM: Connecting AttemptError.");

			//					//ReConnectionSettings.RaiseAttemptError(new TimeoutException(message));
			//					lock (_timeSync)
			//						_currState = _prevState;
			//				}
			//				else
			//				{
			//					//ReConnectionSettings.RaiseTimeOut();

			//					if (_currState == ConnectionStates.Connecting && _connectingAttemptCount > 0)
			//					{
			//						lock (_timeSync)
			//							_currState = _reConnecting;

			//						this.AddInfoLog("RCM: To Reconnecting Attempts {0} Timeout {1}.", _connectingAttemptCount, _connectionTimeOut);
			//					}
			//					else
			//					{
			//						lock (_timeSync)
			//							_currState = _none;
			//					}
			//				}
			//			}

			//			break;
			//		}
			//		case _reConnecting:
			//		{
			//			if (_connectingAttemptCount == 0)
			//			{
			//				this.AddWarningLog("RCM: Reconnecting attemts {0} PrevState {1}.", _connectingAttemptCount, _prevState);

			//				lock (_timeSync)
			//					_currState = _none;

			//				break;
			//			}

			//			_connectionTimeOut -= diff;

			//			if (_connectionTimeOut > TimeSpan.Zero)
			//				break;

			//			if (IsTradeTime())
			//			{
			//				this.AddInfoLog("RCM: To Connecting. CurrState {0} PrevState {1} Attempts {2}.", _currState, _prevState, _connectingAttemptCount);

			//				if (_connectingAttemptCount != -1)
			//					_connectingAttemptCount--;

			//				_connectionTimeOut = ReConnectionSettings.Interval;

			//				_prevState = _currState;
			//				SendInMessage(new ConnectMessage());
			//			}
			//			else
			//			{
			//				this.AddWarningLog("RCM: Out of trade time. CurrState {0}.", _currState);
			//				_connectionTimeOut = TimeSpan.FromMinutes(1);
			//			}

			//			break;
			//		}
			//	}
			//}

			private bool IsTradeTime()
			{
				// TODO
				return true;
				//return SessionHolder.ReConnectionSettings.WorkingTime.IsTradeTime(TimeHelper.Now);
			}

			bool IMessageChannel.IsOpened
			{
				get { return _adapter.IsOpened; }
			}

			void IDisposable.Dispose()
			{
				_adapter.NewOutMessage -= AdapterOnNewOutMessage;
				_adapter.Dispose();
			}

			void IMessageChannel.Open()
			{
				_adapter.Open();
			}

			void IMessageChannel.Close()
			{
				_adapter.Close();
			}

			event Action<Message> IMessageChannel.NewOutMessage
			{
				add { _adapter.NewOutMessage += value; }
				remove { _adapter.NewOutMessage -= value; }
			}

			void IPersistable.Load(SettingsStorage storage)
			{
				_adapter.Load(storage);
			}

			void IPersistable.Save(SettingsStorage storage)
			{
				_adapter.Save(storage);
			}

			Guid ILogSource.Id
			{
				get { return _adapter.Id; }
			}

			string ILogSource.Name
			{
				get { return _adapter.Name; }
			}

			ILogSource ILogSource.Parent
			{
				get { return _adapter.Parent; }
				set { _adapter.Parent = value; }
			}

			LogLevels ILogSource.LogLevel
			{
				get { return _adapter.LogLevel; }
				set { _adapter.LogLevel = value; }
			}

			DateTimeOffset ILogSource.CurrentTime
			{
				get { return _adapter.CurrentTime; }
			}

			event Action<LogMessage> ILogSource.Log
			{
				add { _adapter.Log += value; }
				remove { _adapter.Log -= value; }
			}

			void ILogReceiver.AddLog(LogMessage message)
			{
				_adapter.AddLog(message);
			}

			IdGenerator IMessageAdapter.TransactionIdGenerator
			{
				get { return _adapter.TransactionIdGenerator; }
			}

			bool IMessageAdapter.IsMarketDataEnabled
			{
				get { return _adapter.IsMarketDataEnabled; }
				set { _adapter.IsMarketDataEnabled = value; }
			}

			bool IMessageAdapter.IsTransactionEnabled
			{
				get { return _adapter.IsTransactionEnabled; }
				set { _adapter.IsTransactionEnabled = value; }
			}

			bool IMessageAdapter.IsValid
			{
				get { return _adapter.IsValid; }
			}

			IDictionary<string, RefPair<SecurityTypes, string>> IMessageAdapter.SecurityClassInfo
			{
				get { return _adapter.SecurityClassInfo; }
			}

			TimeSpan IMessageAdapter.HeartbeatInterval
			{
				get { return _adapter.HeartbeatInterval; }
				set { _adapter.HeartbeatInterval = value; }
			}

			bool IMessageAdapter.CreateAssociatedSecurity
			{
				get { return _adapter.CreateAssociatedSecurity; }
				set { _adapter.CreateAssociatedSecurity = value; }
			}

			bool IMessageAdapter.CreateDepthFromLevel1
			{
				get { return _adapter.CreateDepthFromLevel1; }
				set { _adapter.CreateDepthFromLevel1 = value; }
			}

			string IMessageAdapter.AssociatedBoardCode
			{
				get { return _adapter.AssociatedBoardCode; }
				set { _adapter.AssociatedBoardCode = value; }
			}

			bool IMessageAdapter.PortfolioLookupRequired
			{
				get { return _adapter.PortfolioLookupRequired; }
			}

			bool IMessageAdapter.SecurityLookupRequired
			{
				get { return _adapter.SecurityLookupRequired; }
			}

			bool IMessageAdapter.OrderStatusRequired
			{
				get { return _adapter.OrderStatusRequired; }
			}

			OrderCondition IMessageAdapter.CreateOrderCondition()
			{
				return _adapter.CreateOrderCondition();
			}
		}
	}
}