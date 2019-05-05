namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Runtime.Serialization;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Message adapter that splits large market data requests on smaller.
	/// </summary>
	public class PartialDownloadMessageAdapter : MessageAdapterWrapper
	{
		/// <summary>
		/// Message for iterate action.
		/// </summary>
		[DataContract]
		[Serializable]
		private class PartialDownloadMessage : Message
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="PartialDownloadMessage"/>.
			/// </summary>
			public PartialDownloadMessage()
				: base(ExtendedMessageTypes.PartialDownload)
			{
			}

			/// <summary>
			/// Transaction ID.
			/// </summary>
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

				this.CopyExtensionInfo(destination);

				return destination;
			}
		}

		private class DownloadInfo
		{
			public MarketDataMessage Origin { get; }

			public long CurrTransId { get; private set; }
			public bool LastIteration { get; private set; }

			public bool ReplyReceived { get; set; }

			private readonly PartialDownloadMessageAdapter _adapter;
			private readonly TimeSpan _iterationInterval;
			private readonly TimeSpan _step;

			private DateTimeOffset _currFrom;
			private bool _firstIteration;
			private DateTimeOffset _nextFrom;

			public DownloadInfo(PartialDownloadMessageAdapter adapter, MarketDataMessage origin, TimeSpan step, TimeSpan iterationInterval)
			{
				_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
				Origin = origin ?? throw new ArgumentNullException(nameof(origin));
				_step = step;
				_iterationInterval = iterationInterval;

				var from = origin.From;

				if (from == null)
					throw new ArgumentException(nameof(origin));

				_currFrom = from.Value;
				_firstIteration = true;
			}

			public MarketDataMessage InitNext()
			{
				if (LastIteration)
					throw new InvalidOperationException("LastIteration == true");

				if (_firstIteration)
				{
					_firstIteration = false;

					var mdMsg = (MarketDataMessage)Origin.Clone();
					mdMsg.TransactionId = _adapter.TransactionIdGenerator.GetNextId();
					mdMsg.From = _currFrom;
					mdMsg.To = _currFrom + _step;

					CurrTransId = mdMsg.TransactionId;
					_nextFrom = mdMsg.To.Value;

					if (Origin.To == null)
						LastIteration = _nextFrom > DateTimeOffset.Now;
					else
						LastIteration = _nextFrom >= Origin.To.Value;

					return mdMsg;
				}
				else
				{
					_iterationInterval.Sleep();

					_currFrom = _nextFrom;
					_nextFrom += _step;

					if (Origin.To == null)
						LastIteration = _nextFrom > DateTimeOffset.Now;
					else
						LastIteration = _nextFrom >= Origin.To.Value;

					if (LastIteration)
					{
						var mdMsg = (MarketDataMessage)Origin.Clone();
						mdMsg.From = _currFrom;
						return mdMsg;
					}
					else
					{
						var mdMsg = (MarketDataMessage)Origin.Clone();
						mdMsg.TransactionId = _adapter.TransactionIdGenerator.GetNextId();
						mdMsg.From = _currFrom;
						mdMsg.To = _nextFrom;

						CurrTransId = mdMsg.TransactionId;

						return mdMsg;
					}
				}
			}
		}

		private readonly SyncObject _syncObject = new SyncObject();
		private readonly Dictionary<long, DownloadInfo> _original = new Dictionary<long, DownloadInfo>();
		private readonly Dictionary<long, long> _partialRequests = new Dictionary<long, long>();
		private readonly Dictionary<long, Tuple<long, DownloadInfo>> _unsubscribeRequests = new Dictionary<long, Tuple<long, DownloadInfo>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PartialDownloadMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public PartialDownloadMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		public override void SendInMessage(Message message)
		{
			if (message.IsBack)
			{
				if (message.Adapter == this)
					message.IsBack = false;
				else
				{
					base.SendInMessage(message);
					return;
				}
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_syncObject)
					{
						_partialRequests.Clear();
						_original.Clear();
						_unsubscribeRequests.Clear();
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.IsSubscribe)
					{
						var from = mdMsg.From;

						if (from != null)
						{
							var length = (mdMsg.To ?? DateTimeOffset.Now) - from.Value;
							var step = InnerAdapter.GetHistoryStepSize(mdMsg, out var iterationInterval);

							if (length > step)
							{
								var info = new DownloadInfo(this, (MarketDataMessage)mdMsg.Clone(), step, iterationInterval);

								message = info.InitNext();

								lock (_syncObject)
								{
									_original.Add(info.Origin.TransactionId, info);
									_partialRequests.Add(info.CurrTransId, info.Origin.TransactionId);
								}
							}
						}
					}
					else
					{
						lock (_syncObject)
						{
							if (!_original.TryGetValue(mdMsg.OriginalTransactionId, out var info))
								break;

							var transId = TransactionIdGenerator.GetNextId();
							_unsubscribeRequests.Add(transId, Tuple.Create(mdMsg.TransactionId, info));

							mdMsg.OriginalTransactionId = info.CurrTransId;
							mdMsg.TransactionId = transId;
						}
					}

					break;
				}

				case ExtendedMessageTypes.PartialDownload:
				{
					var partialMsg = (PartialDownloadMessage)message;

					lock (_syncObject)
					{
						lock (_syncObject)
						{
							if (!_original.TryGetValue(partialMsg.OriginalTransactionId, out var info))
								break;

							message = info.InitNext();

							if (info.LastIteration)
								_original.Remove(partialMsg.OriginalTransactionId);
							else
								_partialRequests.Add(info.CurrTransId, info.Origin.TransactionId);
						}
					}

					break;
				}
			}
			
			base.SendInMessage(message);
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.MarketData:
				{
					var responseMsg = (MarketDataMessage)message;

					lock (_syncObject)
					{
						if (!_partialRequests.TryGetValue(responseMsg.OriginalTransactionId, out var requestId))
						{
							if (!_unsubscribeRequests.TryGetValue(responseMsg.OriginalTransactionId, out var tuple))
								break;
							
							requestId = tuple.Item1;

							var originId = tuple.Item2.Origin.TransactionId;
							_original.Remove(originId);
							_partialRequests.RemoveWhere(p => p.Value == originId);
							_unsubscribeRequests.Remove(responseMsg.OriginalTransactionId);
						}
						else
						{
							var info = _original[requestId];

							if (info.ReplyReceived)
								return;

							info.ReplyReceived = true;

							if (responseMsg.Error != null || responseMsg.IsNotSupported)
							{
								_original.Remove(requestId);
								_partialRequests.RemoveWhere(p => p.Value == requestId);
							}
						}
						
						responseMsg.OriginalTransactionId = requestId;
					}

					break;
				}

				case MessageTypes.MarketDataFinished:
				{
					var finishMsg = (MarketDataFinishedMessage)message;

					lock (_syncObject)
					{
						if (_partialRequests.TryGetValue(finishMsg.OriginalTransactionId, out var originId))
						{
							var info = _original[originId];
							var origin = info.Origin;

							if (info.LastIteration)
							{
								_original.Remove(originId);
								_partialRequests.RemoveWhere(p => p.Value == originId);

								finishMsg.OriginalTransactionId = originId;
								break;
							}
							
							_partialRequests.Remove(finishMsg.OriginalTransactionId);

							message = new PartialDownloadMessage
							{
								Adapter = this,
								IsBack = true,
								OriginalTransactionId = origin.TransactionId,
							};
						}
					}

					break;
				}

				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRange:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
				{
					var candleMsg = (CandleMessage)message;

					long originId;

					lock (_syncObject)
					{
						if (!_partialRequests.TryGetValue(candleMsg.OriginalTransactionId, out originId))
							break;
					}

					candleMsg.OriginalTransactionId = originId;

					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;
					var execType = execMsg.ExecutionType;

					if (execMsg.OriginalTransactionId == 0 || (execType != ExecutionTypes.OrderLog && execType != ExecutionTypes.Tick))
						break;

					long originId;

					lock (_syncObject)
					{
						if (!_partialRequests.TryGetValue(execMsg.OriginalTransactionId, out originId))
							break;
					}

					execMsg.OriginalTransactionId = originId;

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		/// <summary>
		/// Create a copy of <see cref="PartialDownloadMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new PartialDownloadMessageAdapter(InnerAdapter);
		}
	}
}