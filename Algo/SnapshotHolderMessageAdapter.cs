namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Interface, described snapshot holder.
	/// </summary>
	public interface ISnapshotHolder
	{
		/// <summary>
		/// Get snapshot for the specified data type and security.
		/// </summary>
		/// <param name="subscription">Subscription.</param>
		/// <returns>Snapshot.</returns>
		IEnumerable<Message> GetSnapshot(ISubscriptionMessage subscription);
	}

	/// <summary>
	/// The message adapter snapshots holder.
	/// </summary>
	public class SnapshotHolderMessageAdapter : MessageAdapterWrapper
	{
		private readonly SyncObject _sync = new SyncObject();
		private readonly SynchronizedDictionary<long, ISubscriptionMessage> _pending = new SynchronizedDictionary<long, ISubscriptionMessage>();

		private readonly ISnapshotHolder _holder;

		/// <summary>
		/// Initializes a new instance of the <see cref="SnapshotHolderMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		/// <param name="holder">Snapshot holder.</param>
		public SnapshotHolderMessageAdapter(IMessageAdapter innerAdapter, ISnapshotHolder holder)
			: base(innerAdapter)
		{
			_holder = holder ?? throw new ArgumentNullException(nameof(holder));
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_sync)
					{
						_pending.Clear();
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						if (mdMsg.SecurityId == default || mdMsg.DoNotBuildOrderBookInrement || mdMsg.To != null)
							break;

						if (mdMsg.DataType2 != DataType.MarketDepth && mdMsg.DataType2 != DataType.Level1)
							break;

						lock (_sync)
							_pending[mdMsg.TransactionId] = mdMsg.TypedClone();
					}
					else
					{
						lock (_sync)
							_pending.Remove(mdMsg.OriginalTransactionId);
					}

					break;
				}

				case MessageTypes.OrderStatus:
				case MessageTypes.PortfolioLookup:
				{
					var subscrMsg = (ISubscriptionMessage)message;

					if (subscrMsg.IsSubscribe && !subscrMsg.IsHistoryOnly())
					{
						lock (_sync)
							_pending[subscrMsg.TransactionId] = subscrMsg.TypedClone();
					}

					break;
				}
			}

			return base.OnSendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			base.OnInnerAdapterNewOutMessage(message);

			switch (message.Type)
			{
				case MessageTypes.SubscriptionResponse:
				{
					var response = (SubscriptionResponseMessage)message;

					if (!response.IsOk())
					{
						lock (_sync)
							_pending.Remove(response.OriginalTransactionId);
					}

					break;
				}

				case MessageTypes.SubscriptionFinished:
				{
					var finished = (SubscriptionFinishedMessage)message;

					lock (_sync)
						_pending.Remove(finished.OriginalTransactionId);

					break;
				}

				case MessageTypes.SubscriptionOnline:
				{
					var online = (SubscriptionOnlineMessage)message;

					ISubscriptionMessage subscrMsg;

					lock (_sync)
					{
						if (!_pending.TryGetAndRemove(online.OriginalTransactionId, out subscrMsg))
							break;
					}

					foreach (var snapshot in _holder.GetSnapshot(subscrMsg))
					{
						if (snapshot is ISubscriptionIdMessage subscrIdMsg)
						{
							subscrIdMsg.OriginalTransactionId = online.OriginalTransactionId;
							subscrIdMsg.SetSubscriptionIds(subscriptionId: online.OriginalTransactionId);
						}

						base.OnInnerAdapterNewOutMessage(snapshot);
					}

					break;
				}

				default:
				{
					lock (_sync)
					{
						if (_pending.Count > 0 && message is ISubscriptionIdMessage subscrMsg)
						{
							foreach (var id in subscrMsg.GetSubscriptionIds())
								_pending.Remove(id);
						}
					}

					break;
				}
			}
		}

		/// <summary>
		/// Create a copy of <see cref="SnapshotHolderMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone() => new SnapshotHolderMessageAdapter(InnerAdapter.TypedClone(), _holder);
	}
}