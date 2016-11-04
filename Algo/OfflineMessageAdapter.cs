namespace StockSharp.Algo
{
	using Ecng.Collections;

	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter keeping message until connection will be done.
	/// </summary>
	public class OfflineMessageAdapter : MessageAdapterWrapper
	{
		private bool _connected;
		private readonly SynchronizedList<Message> _pendingMessages = new SynchronizedList<Message>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OfflineMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OfflineMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
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
					_pendingMessages.Clear();

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
				default:
				{
					if (!_connected)
					{
						_pendingMessages.Add(message.Clone());
						return;
					}

					break;
				}
			}

			base.SendInMessage(message);
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
				var msgs = _pendingMessages.SyncGet(c => c.CopyAndClear());

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