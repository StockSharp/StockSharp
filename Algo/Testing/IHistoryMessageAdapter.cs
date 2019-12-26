namespace StockSharp.Algo.Testing
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Historical message adapter.
	/// </summary>
	public interface IHistoryMessageAdapter : IMessageAdapter
	{
		/// <summary>
		/// The interval of message <see cref="TimeMessage"/> generation. By default, it is equal to 1 sec.
		/// </summary>
		TimeSpan MarketTimeChangedInterval { get; set; }

		/// <summary>
		/// Date in history for starting the paper trading.
		/// </summary>
		DateTimeOffset StartDate { get; set; }

		/// <summary>
		/// Date in history to stop the paper trading (date is included).
		/// </summary>
		DateTimeOffset StopDate { get; set; }

		/// <summary>
		/// Send next outgoing message.
		/// </summary>
		/// <returns><see langword="true" />, if message was sent, otherwise, <see langword="false" />.</returns>
		bool SendOutMessage();

		/// <summary>
		/// Send outgoing message and raise <see cref="IMessageChannel.NewOutMessage"/> event.
		/// </summary>
		/// <param name="message">Message.</param>
		void SendOutMessage(Message message);
	}

	/// <summary>
	/// Custom implementation of the <see cref="IHistoryMessageAdapter"/>.
	/// </summary>
	public class CustomHistoryMessageAdapter : MessageAdapterWrapper, IHistoryMessageAdapter
	{
		private readonly ISecurityProvider _securityProvider;
		private readonly SynchronizedSet<long> _subscriptions = new SynchronizedSet<long>();
		private readonly Queue<Message> _outMessages = new Queue<Message>();

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomHistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public CustomHistoryMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
			this.AddSupportedMessage(ExtendedMessageTypes.EmulationState, null);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CustomHistoryMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		public CustomHistoryMessageAdapter(IMessageAdapter innerAdapter, ISecurityProvider securityProvider)
			: this(innerAdapter)
		{
			_securityProvider = securityProvider ?? throw new ArgumentNullException(nameof(securityProvider));
		}

		/// <inheritdoc />
		public TimeSpan MarketTimeChangedInterval { get; set; }

		/// <inheritdoc />
		public DateTimeOffset StartDate { get; set; }

		/// <inheritdoc />
		public DateTimeOffset StopDate { get; set; }

		/// <inheritdoc />
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_subscriptions.Clear();
					break;
				}

				case MessageTypes.SecurityLookup:
				{
					if (_securityProvider != null)
					{
						var lookupMsg = (SecurityLookupMessage)message;

						var securities = lookupMsg.SecurityId == default
							? _securityProvider.LookupAll() 
							: _securityProvider.Lookup(lookupMsg);

						foreach (var security in securities)
						{
							SendOutMessage(security.Board.ToMessage());
							SendOutMessage(security.ToMessage(originalTransactionId: lookupMsg.TransactionId));
						}

						SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = lookupMsg.TransactionId });
						return;
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
						_subscriptions.Add(mdMsg.TransactionId);

					break;
				}

				case ExtendedMessageTypes.EmulationState:
					SendOutMessage(message);
					return;
			}

			base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		bool IHistoryMessageAdapter.SendOutMessage()
		{
			if (_outMessages.Count == 0)
				return false;

			SendOutMessage(_outMessages.Dequeue());
			return true;
		}

		/// <inheritdoc />
		public void SendOutMessage(Message message)
		{
			LastMessage lastMsg = null;

			void TryRemoveSubscription(long id, bool isError)
			{
				lock (_subscriptions.SyncRoot)
				{
					if (_subscriptions.Remove(id) && _subscriptions.Count == 0)
					{
						lastMsg = new LastMessage
						{
							LocalTime = StopDate,
							IsError = isError,
							Adapter = this,
						};
					}
				}
			}

			switch (message.Type)
			{
				case MessageTypes.SubscriptionResponse:
				{
					var response = (SubscriptionResponseMessage)message;

					if (!response.IsOk())
						TryRemoveSubscription(response.OriginalTransactionId, true);

					break;
				}

				case MessageTypes.SubscriptionFinished:
				{
					TryRemoveSubscription(((SubscriptionFinishedMessage)message).OriginalTransactionId, false);
					break;
				}
			}

			message.Adapter = this;

			RaiseNewOutMessage(message);

			if (lastMsg != null)
				RaiseNewOutMessage(lastMsg);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			if (message is ConnectMessage)
				message.LocalTime = StartDate;
			else if (message is IServerTimeMessage timeMsg)
				message.LocalTime = timeMsg.ServerTime;
			else
				message.LocalTime = default;

			_outMessages.Enqueue(message);
		}

		/// <summary>
		/// Create a copy of <see cref="CustomHistoryMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new CustomHistoryMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}