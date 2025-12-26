namespace StockSharp.Algo;

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
/// Interface for snapshot holder message processing logic.
/// </summary>
public interface ISnapshotHolderManager
{
	/// <summary>
	/// Process a message going into the inner adapter.
	/// </summary>
	/// <param name="message">Incoming message.</param>
	/// <returns>Processing result: messages to send to inner adapter and messages to send to output.</returns>
	(Message[] toInner, Message[] toOut) ProcessInMessage(Message message);

	/// <summary>
	/// Process a message coming from the inner adapter.
	/// </summary>
	/// <param name="message">Outgoing message.</param>
	/// <returns>Processing result: message to forward and extra messages to output.</returns>
	(Message forward, Message[] extraOut) ProcessOutMessage(Message message);
}

/// <summary>
/// Snapshot holder message processing implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SnapshotHolderManager"/>.
/// </remarks>
/// <param name="holder">Snapshot holder.</param>
public sealed class SnapshotHolderManager(ISnapshotHolder holder) : ISnapshotHolderManager
{
	private readonly Lock _sync = new();
	private readonly Dictionary<long, ISubscriptionMessage> _pending = [];

	private readonly ISnapshotHolder _holder = holder ?? throw new ArgumentNullException(nameof(holder));

	/// <inheritdoc />
	public (Message[] toInner, Message[] toOut) ProcessInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				using (_sync.EnterScope())
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
					if (mdMsg.SecurityId == default || mdMsg.DoNotBuildOrderBookIncrement || mdMsg.To != null)
						break;

					if (mdMsg.DataType2 != DataType.MarketDepth && mdMsg.DataType2 != DataType.Level1)
						break;

					using (_sync.EnterScope())
						_pending[mdMsg.TransactionId] = mdMsg.TypedClone();
				}
				else
				{
					using (_sync.EnterScope())
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
					using (_sync.EnterScope())
						_pending[subscrMsg.TransactionId] = subscrMsg.TypedClone();
				}

				break;
			}
		}

		return ([message], []);
	}

	/// <inheritdoc />
	public (Message forward, Message[] extraOut) ProcessOutMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.SubscriptionResponse:
			{
				var response = (SubscriptionResponseMessage)message;

				if (!response.IsOk())
				{
					using (_sync.EnterScope())
						_pending.Remove(response.OriginalTransactionId);
				}

				break;
			}

			case MessageTypes.SubscriptionFinished:
			{
				var finished = (SubscriptionFinishedMessage)message;

				using (_sync.EnterScope())
					_pending.Remove(finished.OriginalTransactionId);

				break;
			}

			case MessageTypes.SubscriptionOnline:
			{
				var online = (SubscriptionOnlineMessage)message;

				ISubscriptionMessage subscrMsg;

				using (_sync.EnterScope())
				{
					if (!_pending.TryGetAndRemove(online.OriginalTransactionId, out subscrMsg))
						break;
				}

				var extraOut = new List<Message>();

				foreach (var snapshot in _holder.GetSnapshot(subscrMsg))
				{
					if (snapshot is ISubscriptionIdMessage subscrIdMsg)
					{
						subscrIdMsg.OriginalTransactionId = online.OriginalTransactionId;
						subscrIdMsg.SetSubscriptionIds(subscriptionId: online.OriginalTransactionId);
					}

					extraOut.Add(snapshot);
				}

				return (message, [.. extraOut]);
			}

			default:
			{
				using (_sync.EnterScope())
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

		return (message, []);
	}
}

/// <summary>
/// The message adapter snapshots holder.
/// </summary>
public class SnapshotHolderMessageAdapter : MessageAdapterWrapper
{
	private readonly ISnapshotHolderManager _manager;
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
		_manager = new SnapshotHolderManager(holder);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SnapshotHolderMessageAdapter"/> with a custom manager.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	/// <param name="holder">Snapshot holder.</param>
	/// <param name="manager">Snapshot holder manager.</param>
	public SnapshotHolderMessageAdapter(IMessageAdapter innerAdapter, ISnapshotHolder holder, ISnapshotHolderManager manager)
		: base(innerAdapter)
	{
		_holder = holder ?? throw new ArgumentNullException(nameof(holder));
		_manager = manager ?? throw new ArgumentNullException(nameof(manager));
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (toInner, toOut) = _manager.ProcessInMessage(message);

		if (toInner.Length > 0)
		{
			foreach (var msg in toInner)
				await base.OnSendInMessageAsync(msg, cancellationToken);
		}

		if (toOut.Length > 0)
		{
			foreach (var sendOutMsg in toOut)
				await RaiseNewOutMessageAsync(sendOutMsg, cancellationToken);
		}
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		var (forward, extraOut) = _manager.ProcessOutMessage(message);

		if (forward != null)
			await base.OnInnerAdapterNewOutMessageAsync(forward, cancellationToken);

		if (extraOut.Length > 0)
		{
			foreach (var extra in extraOut)
				await base.OnInnerAdapterNewOutMessageAsync(extra, cancellationToken);
		}
	}

	/// <summary>
	/// Create a copy of <see cref="SnapshotHolderMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone() => new SnapshotHolderMessageAdapter(InnerAdapter.TypedClone(), _holder);
}
