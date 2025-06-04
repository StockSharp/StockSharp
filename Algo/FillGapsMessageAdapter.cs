namespace StockSharp.Algo;

/// <summary>
/// Fill gaps by <see cref="IFillGapsBehaviour"/> message adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SecurityMappingMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">The adapter, to which messages will be directed.</param>
/// <param name="behaviour"><see cref="IFillGapsBehaviour"/>.</param>
public class FillGapsMessageAdapter(IMessageAdapter innerAdapter, IFillGapsBehaviour behaviour) : MessageAdapterWrapper(innerAdapter)
{
	private class FillGapInfo(ISubscriptionMessage original, SecurityId secId, FillGapsDays days)
	{
		public ISubscriptionMessage Original { get; } = original;
		public SecurityId SecId { get; } = secId;
		public FillGapsDays Days { get; } = days;
		public ISubscriptionMessage Current { get; set; }
        public bool ResponseSent { get; set; }
    }

	private readonly SynchronizedDictionary<long, FillGapInfo> _gapsRequests = [];
	private readonly IFillGapsBehaviour _behaviour = behaviour ?? throw new ArgumentNullException(nameof(behaviour));

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_gapsRequests.Clear();
				break;
			}

			default:
			{
				if (message is ISubscriptionMessage subscrMsg)
				{
					if (subscrMsg.IsSubscribe)
					{
						if (subscrMsg.FillGaps is FillGapsDays days &&
							subscrMsg.From is DateTimeOffset from &&
							subscrMsg is ISecurityIdMessage secIdMsg &&
							!secIdMsg.IsAllSecurity())
						{
							var (gapsStart, gapsEnd) = _behaviour.TryGetNextGap(secIdMsg.SecurityId, subscrMsg.DataType, from.UtcDateTime, (subscrMsg.To ?? CurrentTime).UtcDateTime, days);

							if (gapsStart is null)
								break;

							var current = subscrMsg.TypedClone();

							current.From = gapsStart.Value;
							current.To = gapsEnd.Value;
							current.FillGaps = null;

							_gapsRequests.Add(subscrMsg.TransactionId, new(subscrMsg.TypedClone(), secIdMsg.SecurityId, days) { Current = current });

							message = (Message)current;
						}
					}
					else
					{
						_gapsRequests.Remove(subscrMsg.OriginalTransactionId);
					}
				}

				break;
			}
		}

		return base.OnSendInMessage(message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.SubscriptionFinished:
			{
				var finished = (SubscriptionFinishedMessage)message;
				
				if (!_gapsRequests.TryGetValue(finished.OriginalTransactionId, out var info))
					break;

				var current = info.Current;

				var (gapsStart, gapsEnd) = _behaviour.TryGetNextGap(info.SecId, info.Original.DataType, current.To.Value.AddDays(1).UtcDateTime, (info.Original.To ?? CurrentTime).UtcDateTime, info.Days);

				if (gapsStart is null)
				{
					_gapsRequests.Remove(finished.OriginalTransactionId);

					if (info.Original.To is null)
					{
						var original = info.Original.TypedClone();
						
						original.From = null;
						original.FillGaps = null;

						original.LoopBack(this);
						message = (Message)original;
					}

					break;
				}

				current = current.TypedClone();

				current.From = gapsStart.Value;
				current.To = gapsEnd.Value;

				info.Current = current;

				current.LoopBack(this);
				message = (Message)current;

				break;
			}
			case MessageTypes.SubscriptionOnline:
			{
				var online = (SubscriptionOnlineMessage)message;
				_gapsRequests.Remove(online.OriginalTransactionId);
				break;
			}
			case MessageTypes.SubscriptionResponse:
			{
				var response = (SubscriptionResponseMessage)message;

				if (!_gapsRequests.TryGetValue(response.OriginalTransactionId, out var info))
					break;

				if (!response.IsOk())
					_gapsRequests.Remove(response.OriginalTransactionId);
				else
				{
					if (info.ResponseSent)
						return;

					info.ResponseSent = true;
				}

				break;
			}
		}

		base.OnInnerAdapterNewOutMessage(message);
	}

	/// <summary>
	/// Create a copy of <see cref="FillGapsMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
		=> new FillGapsMessageAdapter(InnerAdapter.TypedClone(), _behaviour);
}