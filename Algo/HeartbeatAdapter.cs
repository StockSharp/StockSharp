#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: HeartbeatAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Threading;

	using Ecng.Common;

	using StockSharp.Messages;

	class RestoredConnectMessage : ConnectMessage
	{
	}

	/// <summary>
	/// The messages adapter controlling the connection.
	/// </summary>
	public class HeartbeatAdapter : MessageAdapterWrapper
	{
		private readonly SyncObject _timeSync = new SyncObject();

		// дополнительные состояния для ConnectionStates
		private const ConnectionStates _none = (ConnectionStates)0 - 1;
		private const ConnectionStates _reConnecting = (ConnectionStates)10;
		private const ConnectionStates _reStartingExport = (ConnectionStates)11;

		private ConnectionStates _currState = _none;
		private ConnectionStates _prevState = _none;

		private int _connectingAttemptCount;
		private TimeSpan _connectionTimeOut;

		private Timer _heartBeatTimer;

		private readonly TimeMessage _timeMessage = new TimeMessage();
		private bool _canSendTime;

		private ReConnectionSettings _reConnectionSettings;

		private bool _isFirstTimeConnect = true;

		/// <summary>
		/// Initializes a new instance of the <see cref="HeartbeatAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public HeartbeatAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
			_reConnectionSettings = InnerAdapter.ReConnectionSettings;
		}

		/// <summary>
		/// Process <see cref="MessageAdapterWrapper.InnerAdapter"/> output message.
		/// </summary>
		/// <param name="message">The message.</param>
		protected override void OnInnerAdapterNewOutMessage(Message message)
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
						if (InnerAdapter.HeartbeatInterval == TimeSpan.Zero)
							break;

						lock (_timeSync)
						{
							_canSendTime = true;
							_heartBeatTimer = ThreadingHelper.Timer(OnHeartbeatTimer).Interval(InnerAdapter.HeartbeatInterval);	
						}
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
					{
						_prevState = _currState = ConnectionStates.Disconnected;

						if (_heartBeatTimer != null)
						{
							_heartBeatTimer.Dispose();
							_heartBeatTimer = null;
						}
					}
					
					break;
				}

				case MessageTypes.Time:
				{
					if (message == _timeMessage)
					{
						lock (_timeSync)
							_canSendTime = true;

						return;
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					if (_isFirstTimeConnect)
						_isFirstTimeConnect = false;
					else
						base.SendInMessage(new ResetMessage());

				//	lock (_timeSync)
				//	{
				//		_currState = ConnectionStates.Connecting;
				//	}

				//	if (_prevState == _none)
				//	{
				//		_connectionTimeOut = _reConnectionSettings.TimeOutInterval;
				//		_connectingAttemptCount = _reConnectionSettings.AttemptCount;
				//	}
				//	else
				//		_connectionTimeOut = _reConnectionSettings.Interval;

					break;
				}
				case MessageTypes.Disconnect:
				{
					//lock (_timeSync)
					//	_currState = ConnectionStates.Disconnecting;

					//_connectionTimeOut = _reConnectionSettings.TimeOutInterval;

					lock (_timeSync)
					{
						_canSendTime = false;

						if (_heartBeatTimer != null)
						{
							_heartBeatTimer.Dispose();
							_heartBeatTimer = null;
						}
					}

					break;
				}
			}

			base.SendInMessage(message);
		}

		private void OnHeartbeatTimer()
		{
			lock (_timeSync)
			{
				if (_currState != ConnectionStates.Connected)
					return;

				if (!_canSendTime)
					return;

				_canSendTime = false;
			}

			_timeMessage.IsBack = true;
			_timeMessage.TransactionId = InnerAdapter.TransactionIdGenerator.GetNextId();
			InnerAdapter.SendInMessage(_timeMessage);
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

		/// <summary>
		/// Create a copy of <see cref="HeartbeatAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new HeartbeatAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}