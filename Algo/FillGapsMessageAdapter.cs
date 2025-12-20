namespace StockSharp.Algo;

/// <summary>
/// Fill gaps by <see cref="IFillGapsBehaviour"/> message adapter.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FillGapsMessageAdapter"/>.
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
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
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
							subscrMsg.From is DateTime from &&
							subscrMsg is ISecurityIdMessage secIdMsg &&
							!secIdMsg.IsAllSecurity())
						{
							var (gapsStart, gapsEnd) = await _behaviour.TryGetNextGapAsync(secIdMsg.SecurityId, subscrMsg.DataType, from, subscrMsg.To ?? CurrentTimeUtc, days, cancellationToken);

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

		await base.OnSendInMessageAsync(message, cancellationToken);
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

				_ = ProcessSubscriptionFinishedAsync(finished, info);
				return;
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

	private async Task ProcessSubscriptionFinishedAsync(SubscriptionFinishedMessage finished, FillGapInfo info)
	{
		try
		{
			var current = info.Current;

			var (gapsStart, gapsEnd) = await _behaviour.TryGetNextGapAsync(info.SecId, info.Original.DataType, current.To.Value.AddDays(1), info.Original.To ?? CurrentTimeUtc, info.Days, default);

			if (gapsStart is null)
			{
				_gapsRequests.Remove(finished.OriginalTransactionId);

				if (info.Original.To is null)
				{
					var original = info.Original.TypedClone();

					original.From = null;
					original.FillGaps = null;

					original.LoopBack(this);
					RaiseNewOutMessage((Message)original);
				}
				else
				{
					base.OnInnerAdapterNewOutMessage(finished);
				}

				return;
			}

			current = current.TypedClone();

			current.From = gapsStart.Value;
			current.To = gapsEnd.Value;

			info.Current = current;

			current.LoopBack(this);
			RaiseNewOutMessage((Message)current);
		}
		catch (Exception ex)
		{
			this.AddErrorLog(ex);
			base.OnInnerAdapterNewOutMessage(finished);
		}
	}

	/// <summary>
	/// Create a copy of <see cref="FillGapsMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
		=> new FillGapsMessageAdapter(InnerAdapter.TypedClone(), _behaviour);
}