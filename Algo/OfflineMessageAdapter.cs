namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter keeping message until connection will be done.
	/// </summary>
	public class OfflineMessageAdapter : MessageAdapterWrapper
	{
		private bool _connected;
		private readonly SyncObject _syncObject = new SyncObject();
		private readonly List<Message> _pendingMessages = new List<Message>();
		private readonly PairSet<long, MarketDataMessage> _subscriptions = new PairSet<long, MarketDataMessage>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OfflineMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OfflineMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		private int _maxMessageCount = 10000;

		/// <summary>
		/// Max message queue count. The default value is 10000.
		/// </summary>
		/// <remarks>
		/// Value setted to -1 corresponds to the size without limitations.
		/// </remarks>
		public int MaxMessageCount
		{
			get => _maxMessageCount;
			set
			{
				if (value < -1)
					throw new ArgumentOutOfRangeException();

				_maxMessageCount = value;
			}
		}

		/// <summary>
		/// Send message.
		/// </summary>
		/// <param name="message">Message.</param>
		public override void SendInMessage(Message message)
		{
			if (message.IsBack)
			{
				if (message.Adapter == this)
				{
					message.Adapter = null;
					message.IsBack = false;
				}

				base.SendInMessage(message);
				return;
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_connected = false;

					lock (_syncObject)
					{
						_pendingMessages.Clear();
						_subscriptions.Clear();
					}

					base.SendInMessage(message);
					break;
				}
				case MessageTypes.Connect:
				case MessageTypes.Disconnect:
					break;
				case MessageTypes.Time:
				{
					if (!_connected)
						return;

					break;
				}
				case MessageTypes.MarketData:
				{
					if (!_connected)
					{
						var mdMsg = (MarketDataMessage)message;

						lock (_syncObject)
						{
							if (mdMsg.IsSubscribe)
							{
								var clone = (MarketDataMessage)mdMsg.Clone();

								if (mdMsg.TransactionId != 0)
									_subscriptions.Add(mdMsg.TransactionId, clone);

								StoreMessage(clone);
							}
							else
							{
								if (mdMsg.OriginalTransactionId != 0)
								{
									var originMsg = _subscriptions.TryGetValue(mdMsg.OriginalTransactionId);

									if (originMsg != null)
									{
										_subscriptions.Remove(mdMsg.OriginalTransactionId);
										_pendingMessages.Remove(originMsg);
										return;
									}
								}
								
								StoreMessage(message.Clone());
							}
						}

						return;
					}

					break;
				}
				default:
				{
					if (!_connected)
					{
						lock (_syncObject)
							StoreMessage(message.Clone());

						return;
					}

					break;
				}
			}

			base.SendInMessage(message);
		}

		private void StoreMessage(Message message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			if (_maxMessageCount > 0 && _pendingMessages.Count == _maxMessageCount)
				throw new InvalidOperationException(LocalizedStrings.MaxMessageCountExceed);

			_pendingMessages.Add(message);
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
					var connectMsg = (ConnectMessage)message;
					_connected = connectMsg.Error == null;
					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);

			if (message.Type == MessageTypes.Connect && _connected)
			{
				Message[] msgs;

				lock (_syncObject)
				{
					msgs = _pendingMessages.CopyAndClear();

					foreach (var msg in msgs)
					{
						if (msg is MarketDataMessage mdMsg)
							_subscriptions.RemoveByValue(mdMsg);
					}
				}

				foreach (var msg in msgs)
				{
					msg.IsBack = true;
					msg.Adapter = this;

					RaiseNewOutMessage(msg);
				}
			}
		}

		/// <summary>
		/// Create a copy of <see cref="OfflineMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OfflineMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}