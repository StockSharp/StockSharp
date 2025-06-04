namespace StockSharp.Algo;

/// <summary>
/// Message adapter that splits large market data requests on smaller.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="PartialDownloadMessageAdapter"/>.
/// </remarks>
/// <param name="innerAdapter">Underlying adapter.</param>
public class PartialDownloadMessageAdapter(IMessageAdapter innerAdapter) : MessageAdapterWrapper(innerAdapter)
{
	/// <summary>
	/// Message for iterate action.
	/// </summary>
	[DataContract]
	[Serializable]
	private class PartialDownloadMessage : Message, IOriginalTransactionIdMessage
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="PartialDownloadMessage"/>.
		/// </summary>
		public PartialDownloadMessage()
			: base(ExtendedMessageTypes.PartialDownload)
		{
		}

		[DataMember]
		public long OriginalTransactionId { get; set; }

		/// <inheritdoc />
		public override string ToString()
		{
			return base.ToString() + $",OrigTrId={OriginalTransactionId}";
		}

		/// <summary>
		/// Create a copy of <see cref="PartialDownloadMessage"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override Message Clone()
		{
			return CopyTo(new PartialDownloadMessage());
		}

		/// <summary>
		/// Copy the message into the <paramref name="destination" />.
		/// </summary>
		/// <param name="destination">The object, to which copied information.</param>
		/// <returns>The object, to which copied information.</returns>
		private PartialDownloadMessage CopyTo(PartialDownloadMessage destination)
		{
			destination.OriginalTransactionId = OriginalTransactionId;

			return destination;
		}
	}

	private class DownloadInfo
	{
		public MarketDataMessage Origin { get; }

		public long CurrTransId { get; private set; }
		public bool LastIteration => _nextFrom >= Origin.To || (_count <= 0);

		public bool ReplyReceived { get; set; }
		public long? UnsubscribingId { get; set; }

		public bool IsDownloadingInProgress { get; set; }

		private readonly PartialDownloadMessageAdapter _adapter;
		private readonly TimeSpan _iterationInterval;
		private readonly TimeSpan _step;

		private DateTimeOffset _currFrom;
		private bool _firstIteration;
		private DateTimeOffset _nextFrom;
		private readonly DateTimeOffset _to;
		private long? _count;

		private bool IsStepMax => _step == TimeSpan.MaxValue;

		public bool CanFinish => LastIteration || (IsStepMax && Origin.To is not null) || UnsubscribingId != null;

		public DownloadInfo(PartialDownloadMessageAdapter adapter, MarketDataMessage origin, TimeSpan step, TimeSpan iterationInterval)
		{
			if (step <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(step));

			if (iterationInterval < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(iterationInterval));

			_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
			Origin = origin ?? throw new ArgumentNullException(nameof(origin));
			_step = step;
			_iterationInterval = iterationInterval;

			_to = origin.To ?? _adapter.CurrentTime;
			_currFrom = origin.From ?? _to - (IsStepMax ? TimeSpan.FromDays(1) : step);

			_firstIteration = true;

			_count = origin.Count;

			if (_count <= 0)
				throw new ArgumentOutOfRangeException(nameof(origin), origin.Count, LocalizedStrings.InvalidValue);

			if (_nextFrom >= origin.To)
				throw new ArgumentOutOfRangeException(nameof(origin), origin.To, LocalizedStrings.InvalidValue);
		}

		public void TryUpdateNextFrom(DateTimeOffset last)
		{
			if (_currFrom < last)
				_nextFrom = last;

			if (_count != default)
				_count--;
		}

		private void Init(MarketDataMessage mdMsg)
		{
			if (_nextFrom > _to)
				_nextFrom = _to;

			mdMsg.TransactionId = _adapter.TransactionIdGenerator.GetNextId();
			mdMsg.Count = _adapter.GetMaxCount(mdMsg.DataType2);
			mdMsg.From = _currFrom;
			mdMsg.To = _nextFrom;

			if (mdMsg.Count > _count)
				mdMsg.Count = _count;

			CurrTransId = mdMsg.TransactionId;
		}

		public MarketDataMessage InitNext()
		{
			if (LastIteration)
				throw new InvalidOperationException("LastIteration == true");

			var mdMsg = Origin.TypedClone();

			if (_firstIteration)
			{
				_firstIteration = false;

				_nextFrom = IsStepMax ? _to : _currFrom + _step;

				Init(mdMsg);
			}
			else
			{
				_iterationInterval.Sleep();

				mdMsg.Skip = null;

				if (Origin.To == null && (_nextFrom >= _to || IsStepMax))
				{
					// on-line
					mdMsg.From = null;
					mdMsg.Count = null;
				}
				else
				{
					_currFrom = _nextFrom;
					_nextFrom += _step;

					Init(mdMsg);
				}
			}

			return mdMsg;
		}
	}

	private readonly SyncObject _syncObject = new();
	private readonly Dictionary<long, DownloadInfo> _original = [];
	private readonly Dictionary<long, DownloadInfo> _partialRequests = [];
	private readonly Dictionary<long, bool> _liveRequests = [];
	private readonly HashSet<long> _finished = [];

	/// <inheritdoc />
	protected override bool OnSendInMessage(Message message)
	{
		Message outMsg = null;

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				lock (_syncObject)
				{
					_partialRequests.Clear();
					_original.Clear();
					_liveRequests.Clear();
				}

				break;
			}

			case MessageTypes.OrderStatus:
			case MessageTypes.PortfolioLookup:
			{
				var subscriptionMsg = (ISubscriptionMessage)message;

				if (subscriptionMsg.IsSubscribe)
				{
					if (subscriptionMsg.SpecificItemRequest)
						break;

					var from = subscriptionMsg.From;
					var to = subscriptionMsg.To;

					if (from != null || to != null || subscriptionMsg.Count != null)
					{
						var step = InnerAdapter.GetHistoryStepSize(default, DataType.Transactions, out _);

						// adapter do not provide historical request
						if (step == TimeSpan.Zero)
						{
							if (to != null)
							{
								// finishing current history request

								outMsg = subscriptionMsg.CreateResult();
								message = null;
							}
							else
							{
								// or sending further only live subscription
								subscriptionMsg.From = null;
								subscriptionMsg.To = null;

								lock (_syncObject)
									_liveRequests.Add(subscriptionMsg.TransactionId, false);
							}
						}
					}
					else
					{
						lock (_syncObject)
							_liveRequests.Add(subscriptionMsg.TransactionId, false);
					}
				}

				break;
			}

			case MessageTypes.MarketData:
			{
				var mdMsg = (MarketDataMessage)message;
				var transId = mdMsg.TransactionId;

				if (mdMsg.IsSubscribe)
				{
					var from = mdMsg.From;
					var to = mdMsg.To;

					if ((from != null || to != null) && mdMsg.Count is null or > 0 && (to is null || to > default(DateTimeOffset)))
					{
						var step = InnerAdapter.GetHistoryStepSize(mdMsg.SecurityId, mdMsg.DataType2, out var iterationInterval);

						// adapter do not provide historical request
						if (step == TimeSpan.Zero)
						{
							if (to != null)
							{
								// finishing current history request
								outMsg = new SubscriptionFinishedMessage { OriginalTransactionId = transId };
								message = null;
							}
							else
							{
								// or sending further only live subscription
								mdMsg.From = null;
								mdMsg.To = null;

								lock (_syncObject)
									_liveRequests.Add(transId, false);
							}

							break;
						}

						var info = new DownloadInfo(this, mdMsg.TypedClone(), step, iterationInterval);

						message = info.InitNext();

						lock (_syncObject)
						{
							_original.Add(info.Origin.TransactionId, info);
							_partialRequests.Add(info.CurrTransId, info);
							info.IsDownloadingInProgress = true;
						}

						LogDebug("Downloading {0}/{1}: {2}-{3}", mdMsg.SecurityId, mdMsg.DataType2, mdMsg.From, mdMsg.To);
					}
					else
					{
						lock (_syncObject)
							_liveRequests.Add(transId, false);
					}
				}
				else
				{
					lock (_syncObject)
					{
						if (!_original.TryGetValue(mdMsg.OriginalTransactionId, out var info))
							break;

						info.UnsubscribingId = transId;
						message = null;

						if(!info.IsDownloadingInProgress)
							outMsg = new SubscriptionResponseMessage { OriginalTransactionId = info.UnsubscribingId.Value };
					}
				}

				break;
			}

			case ExtendedMessageTypes.PartialDownload:
			{
				var partialMsg = (PartialDownloadMessage)message;

				MarketDataMessage mdMsg;

				lock (_syncObject)
				{
					if (!_original.TryGetValue(partialMsg.OriginalTransactionId, out var info))
						return false;

					if (info.UnsubscribingId != null)
					{
						outMsg = new SubscriptionResponseMessage { OriginalTransactionId = info.UnsubscribingId.Value };
						message = null;
						break;
					}

					mdMsg = info.InitNext();

					if (mdMsg.To == null)
					{
						_liveRequests.Add(mdMsg.TransactionId, true);

						_original.Remove(partialMsg.OriginalTransactionId);
						_partialRequests.RemoveWhere(p => p.Value == info);
					}
					else
					{
						_partialRequests.Add(info.CurrTransId, info);
						info.IsDownloadingInProgress = true;
					}

					message = mdMsg;
				}

				LogDebug("Downloading {0}/{1}: {2}-{3}", mdMsg.SecurityId, mdMsg.DataType2, mdMsg.From, mdMsg.To);

				break;
			}
		}

		var result = true;

		if (message != null)
			result = base.OnSendInMessage(message);

		if (outMsg != null)
			RaiseNewOutMessage(outMsg);

		return result;
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		switch (message.Type)
		{
			case MessageTypes.Disconnect:
			case MessageTypes.Reset:
			{
				lock (_syncObject)
				{
					_partialRequests.Clear();
					_original.Clear();
					_liveRequests.Clear();
				}
				break;
			}
			case MessageTypes.SubscriptionResponse:
			{
				var responseMsg = (SubscriptionResponseMessage)message;
				var originId = responseMsg.OriginalTransactionId;

				lock (_syncObject)
				{
					if (_liveRequests.TryGetValue(originId, out var isPartial))
					{
						if (responseMsg.IsOk())
						{
							if (isPartial)
							{
								// reply was sent previously for a first partial request,
								return;
							}
						}
						else
						{
							_liveRequests.Remove(originId);
						}

						break;
					}

					if (!_partialRequests.TryGetValue(originId, out var info))
					{
						if (_finished.Contains(originId))
							LogWarning("Subscription {0} already finished.", originId);

						break;
					}

					var requestId = info.Origin.TransactionId;

					if (!responseMsg.IsOk())
					{
						_original.Remove(requestId);
						_partialRequests.RemoveWhere(p => p.Value == info);

						if (info.ReplyReceived)
						{
							// unexpected subscription stop

							message = responseMsg = responseMsg.TypedClone();
							responseMsg.OriginalTransactionId = requestId;
							break;
						}
					}

					if (info.ReplyReceived)
						return;

					info.ReplyReceived = true;

					message = responseMsg = responseMsg.TypedClone();
					responseMsg.OriginalTransactionId = requestId;
				}

				break;
			}

			case MessageTypes.SubscriptionOnline:
			{
				var onlineMsg = (SubscriptionOnlineMessage)message;
				var id = onlineMsg.OriginalTransactionId;

				lock (_syncObject)
				{
					if (_partialRequests.TryGetValue(id, out var info))
					{
						LogWarning("Partial {0} ({1}) became online.", id, info.Origin.TransactionId);
						message = null;
					}
					else if (_liveRequests.TryGetAndRemove(id, out _))
					{
						LogInfo("Downloading {0} is online.", id);
					}
				}

				break;
			}

			case MessageTypes.SubscriptionFinished:
			{
				var finishMsg = (SubscriptionFinishedMessage)message;
				var id = finishMsg.OriginalTransactionId;

				lock (_syncObject)
				{
					if (_partialRequests.TryGetAndRemove(id, out var info))
					{
						_finished.Add(id);
						info.IsDownloadingInProgress = false;

						var origin = info.Origin;

						if (finishMsg.NextFrom != null)
							info.TryUpdateNextFrom(finishMsg.NextFrom.Value);

						if (info.CanFinish)
						{
							_original.Remove(origin.TransactionId);
							_partialRequests.RemoveWhere(p => p.Value == info);

							if (info.UnsubscribingId != null)
								message = new SubscriptionResponseMessage { OriginalTransactionId = info.UnsubscribingId.Value };
							else
								finishMsg.OriginalTransactionId = origin.TransactionId;
						}
						else
						{
							message = new PartialDownloadMessage { OriginalTransactionId = origin.TransactionId }.LoopBack(this);
						}
					}
					else
					{
						if (_finished.Contains(id))
							LogWarning("Subscription {0} already finished.", id);

						break;
					}
				}

				LogInfo("Partial {0} finished.", id);

				break;
			}

			case MessageTypes.Execution:
			{
				TryUpdateSubscriptionResult((ExecutionMessage)message);
				break;
			}

			case MessageTypes.Level1Change:
			{
				TryUpdateSubscriptionResult((Level1ChangeMessage)message);
				break;
			}

			case MessageTypes.QuoteChange:
			{
				TryUpdateSubscriptionResult((QuoteChangeMessage)message);
				break;
			}

			case MessageTypes.PositionChange:
			{
				TryUpdateSubscriptionResult((PositionChangeMessage)message);
				break;
			}

			default:
			{
				if (message is CandleMessage candleMsg)
					TryUpdateSubscriptionResult(candleMsg);

				break;
			}
		}

		base.OnInnerAdapterNewOutMessage(message);
	}

	private void TryUpdateSubscriptionResult<TMessage>(TMessage message)
		where TMessage : ISubscriptionIdMessage, IServerTimeMessage
	{
		var originId = message.OriginalTransactionId;

		if (originId == 0)
			return;

		lock (_syncObject)
		{
			if (!_partialRequests.TryGetValue(originId, out var info))
				return;

			info.TryUpdateNextFrom(message.ServerTime);

			message.OriginalTransactionId = info.Origin.TransactionId;
			message.SetSubscriptionIds(subscriptionId: info.Origin.TransactionId);
		}
	}

	/// <summary>
	/// Create a copy of <see cref="PartialDownloadMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageChannel Clone()
	{
		return new PartialDownloadMessageAdapter(InnerAdapter.TypedClone());
	}
}
