namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Candle builder adapter.
	/// </summary>
	public class CandleBuilderMessageAdapter : MessageAdapterWrapper
	{
		private enum SeriesStates
		{
			None,
			Regular,
			SmallTimeFrame,
			Compress,
		}

		private class SeriesInfo
		{
			public SeriesInfo(MarketDataMessage original, MarketDataMessage current)
			{
				Original = original ?? throw new ArgumentNullException(nameof(original));
				Current = current;
			}

			public long Id => Original.TransactionId;

			public MarketDataMessage Original { get; }

			private MarketDataMessage _current;

			public MarketDataMessage Current
			{
				get => _current;
				set => _current = value ?? throw new ArgumentNullException(nameof(value));
			}

			public SeriesStates State { get; set; } = SeriesStates.None;

			public BiggerTimeFrameCandleCompressor BigTimeFrameCompressor { get; set; }

			public ICandleBuilderValueTransform Transform { get; set; }

			public DateTimeOffset? LastTime { get; set; }

			public CandleMessage CurrentCandleMessage { get; set; }

			public CandleMessage NonFinishedCandle { get; set; }
		}

		private readonly SyncObject _syncObject = new SyncObject();
		private readonly Dictionary<long, SeriesInfo> _series = new Dictionary<long, SeriesInfo>();
		private readonly Dictionary<long, long> _replaceId = new Dictionary<long, long>();
		private readonly CandleBuilderProvider _candleBuilderProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleBuilderMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		/// <param name="candleBuilderProvider">Candle builders provider.</param>
		public CandleBuilderMessageAdapter(IMessageAdapter innerAdapter, CandleBuilderProvider candleBuilderProvider)
			: base(innerAdapter)
		{
			_candleBuilderProvider = candleBuilderProvider ?? throw new ArgumentNullException(nameof(candleBuilderProvider));
		}

		/// <inheritdoc />
		protected override bool OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_syncObject)
					{
						_series.Clear();
						_replaceId.Clear();
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (!_candleBuilderProvider.IsRegistered(mdMsg.ToDataType().MessageType))
						break;

					var transactionId = mdMsg.TransactionId;

					if (mdMsg.IsSubscribe)
					{
						var isLoadOnly = mdMsg.BuildMode == MarketDataBuildModes.Load;

						if (mdMsg.IsCalcVolumeProfile || mdMsg.BuildMode == MarketDataBuildModes.Build)
						{
							if (isLoadOnly || !TrySubscribeBuild(mdMsg))
							{
								RaiseNewOutMessage(transactionId.CreateNotSupported());
							}

							return true;
						}

						if (mdMsg.DataType == MarketDataTypes.CandleTimeFrame)
						{
							var originalTf = mdMsg.GetTimeFrame();
							var timeFrames = InnerAdapter.GetTimeFrames(mdMsg.SecurityId, mdMsg.From, mdMsg.To).ToArray();

							if (timeFrames.Contains(originalTf) || InnerAdapter.CheckTimeFrameByRequest)
							{
								this.AddInfoLog("Origin tf: {0}", originalTf);

								var original = (MarketDataMessage)mdMsg.Clone();

								if (mdMsg.To == null &&
									mdMsg.BuildMode == MarketDataBuildModes.LoadAndBuild &&
									!mdMsg.IsFinished &&
									!InnerAdapter.IsSupportCandlesUpdates &&
									InnerAdapter.TryGetCandlesBuildFrom(original, _candleBuilderProvider) != null)
								{
									mdMsg.To = DateTimeOffset.Now;
								}

								lock (_syncObject)
								{
									_series.Add(transactionId, new SeriesInfo(original, original)
									{
										State = SeriesStates.Regular,
										LastTime = original.From,
									});
								}

								break;
							}

							if (isLoadOnly)
							{
								RaiseNewOutMessage(transactionId.CreateNotSupported());

								return true;
							}

							if (mdMsg.AllowBuildFromSmallerTimeFrame)
							{
								var smaller = timeFrames
								              .FilterSmallerTimeFrames(originalTf)
								              .OrderByDescending()
								              .FirstOr();

								if (smaller != null)
								{
									this.AddInfoLog("Smaller tf: {0}->{1}", originalTf, smaller);

									var original = (MarketDataMessage)mdMsg.Clone();

									var current = (MarketDataMessage)original.Clone();
									current.Arg = smaller;

									lock (_syncObject)
									{
										_series.Add(transactionId, new SeriesInfo(original, current)
										{
											State = SeriesStates.SmallTimeFrame,
											BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original, (TimeFrameCandleBuilder)_candleBuilderProvider.Get(typeof(TimeFrameCandleMessage))),
											LastTime = original.From,
										});
									}

									return base.OnSendInMessage(current);
								}
							}

							if (!TrySubscribeBuild(mdMsg))
							{
								RaiseNewOutMessage(transactionId.CreateNotSupported());
							}
						}
						else
						{
							if (InnerAdapter.IsCandlesSupported(mdMsg))
							{
								this.AddInfoLog("Origin arg: {0}", mdMsg.Arg);

								var original = (MarketDataMessage)mdMsg.Clone();

								lock (_syncObject)
								{
									_series.Add(transactionId, new SeriesInfo(original, original)
									{
										State = SeriesStates.Regular,
										LastTime = original.From,
									});
								}

								break;
							}
							else
							{
								if (isLoadOnly || !TrySubscribeBuild(mdMsg))
								{
									RaiseNewOutMessage(transactionId.CreateNotSupported());
								}
							}
						}
						
						return true;
					}
					else
					{
						var series = TryRemoveSeries(mdMsg.OriginalTransactionId);

						if (series == null)
							break;

						var unsubscribe = (MarketDataMessage)series.Current.Clone();

						unsubscribe.OriginalTransactionId = unsubscribe.TransactionId;
						unsubscribe.TransactionId = transactionId;
						unsubscribe.IsSubscribe = false;

						message = unsubscribe;

						break;
					}
				}
			}

			return base.OnSendInMessage(message);
		}

		private SeriesInfo TryGetSeries(long id, out long originalId)
		{
			lock (_syncObject)
			{
				if (_replaceId.TryGetValue(id, out originalId))
					id = originalId;
				else
					originalId = id;

				return _series.TryGetValue(id);
			}
		}

		private SeriesInfo TryRemoveSeries(long id)
		{
			this.AddInfoLog("Series removing {0}.", id);

			lock (_syncObject)
			{
				if (!_series.TryGetAndRemove(id, out var series))
					return null;

				_replaceId.RemoveWhere(p => p.Value == id);
				return series;
			}
		}

		private MarketDataMessage TryCreateBuildSubscription(MarketDataMessage original, DateTimeOffset? lastTime)
		{
			if (original == null)
				throw new ArgumentNullException(nameof(original));

			var buildFrom = InnerAdapter.TryGetCandlesBuildFrom(original, _candleBuilderProvider);

			if (buildFrom == null)
				return null;

			var current = new MarketDataMessage
			{
				DataType = buildFrom.Value,
				From = lastTime,
				To = original.To,
				Count = original.Count,
				MaxDepth = original.MaxDepth,
				BuildField = original.BuildField,
				IsSubscribe = true,
			};

			original.CopyTo(current, false);

			this.AddInfoLog("Build tf: {0}->{1}", buildFrom, original.Arg);

			return current;
		}

		private bool TrySubscribeBuild(MarketDataMessage original)
		{
			var current = TryCreateBuildSubscription(original, original.From);

			if (current == null)
				return false;

			current.TransactionId = original.TransactionId;

			var series = new SeriesInfo((MarketDataMessage)original.Clone(), current)
			{
				LastTime = current.From,
				Transform = CreateTransform(current.DataType, current.BuildField),
				State = SeriesStates.Compress,
			};

			lock (_syncObject)
				_series.Add(original.TransactionId, series);

			base.OnSendInMessage(current);
			return true;
		}

		private static ICandleBuilderValueTransform CreateTransform(MarketDataTypes dataType, Level1Fields? field)
		{
			switch (dataType)
			{
				case MarketDataTypes.Trades:
					return new TickCandleBuilderValueTransform();

				case MarketDataTypes.MarketDepth:
				{
					var t = new QuoteCandleBuilderValueTransform();

					if (field != null)
						t.Type = field.Value;

					return t;
				}

				case MarketDataTypes.Level1:
				{
					var t = new Level1CandleBuilderValueTransform();

					if (field != null)
						t.Type = field.Value;

					return t;
				}

				case MarketDataTypes.OrderLog:
				{
					var t = new OrderLogCandleBuilderValueTransform();

					if (field != null)
						t.Type = field.Value;

					return t;
				}

				default:
					throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1219);
			}
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.SubscriptionResponse:
				{
					var response = (SubscriptionResponseMessage)message;
					var requestId = response.OriginalTransactionId;

					var series = TryGetSeries(requestId, out _);

					if (series == null)
						break;

					if (response.IsOk())
					{
						if (series.Id == requestId)
							RaiseNewOutMessage(message);
					}
					else
						UpgradeSubscription(series, response);

					return;
				}

				case MessageTypes.SubscriptionFinished:
				{
					var finishMsg = (SubscriptionFinishedMessage)message;
					var subscriptionId = finishMsg.OriginalTransactionId;

					var series = TryGetSeries(subscriptionId, out _);

					if (series == null)
						break;

					UpgradeSubscription(series, null);
					return;
				}

				case MessageTypes.SubscriptionOnline:
				{
					var onlineMsg = (SubscriptionOnlineMessage)message;
					var subscriptionId = onlineMsg.OriginalTransactionId;

					lock (_syncObject)
					{
						if (_series.ContainsKey(subscriptionId))
							this.AddInfoLog("Series online {0}.", subscriptionId);

						if (_replaceId.TryGetValue(subscriptionId, out var originalId))
							onlineMsg.OriginalTransactionId = originalId;
					}

					break;
				}

				case MessageTypes.Execution:
				case MessageTypes.QuoteChange:
				case MessageTypes.Level1Change:
				{
					if (message.Type == MessageTypes.Execution && !((ExecutionMessage)message).IsMarketData())
						break;

					var subscrMsg = (ISubscriptionIdMessage)message;

					if (ProcessValue(subscrMsg))
						return;

					break;
				}

				default:
				{
					if (message is CandleMessage candleMsg)
					{
						if (candleMsg.Type == MessageTypes.CandleTimeFrame)
						{
							var subscriptionIds = candleMsg.GetSubscriptionIds();
							HashSet<long> newSubscriptionIds = null;

							foreach (var subscriptionId in subscriptionIds)
							{
								var series = TryGetSeries(subscriptionId, out _);

								if (series == null)
									continue;

								if (newSubscriptionIds == null)
									newSubscriptionIds = new HashSet<long>(subscriptionIds);

								newSubscriptionIds.Remove(subscriptionId);

								switch (series.State)
								{
									case SeriesStates.Regular:
										ProcessCandle(series, candleMsg);
										break;

									case SeriesStates.SmallTimeFrame:
										var candles = series.BigTimeFrameCompressor.Process(candleMsg).Where(c => c.State == CandleStates.Finished);

										foreach (var bigCandle in candles)
										{
											bigCandle.OriginalTransactionId = series.Id;
											bigCandle.Adapter = candleMsg.Adapter;
											series.LastTime = bigCandle.CloseTime;
											base.OnInnerAdapterNewOutMessage(bigCandle);
										}

										break;

									// TODO default
								}
							}

							if (newSubscriptionIds != null)
							{
								if (newSubscriptionIds.Count == 0)
									return;

								candleMsg.SetSubscriptionIds(newSubscriptionIds.ToArray());
							}
						}
						else
						{
							var subscriptionIds = candleMsg.GetSubscriptionIds();
							HashSet<long> newSubscriptionIds = null;

							foreach (var subscriptionId in subscriptionIds)
							{
								var series = TryGetSeries(subscriptionId, out _);

								if (series == null)
									continue;

								if (newSubscriptionIds == null)
									newSubscriptionIds = new HashSet<long>(subscriptionIds);

								newSubscriptionIds.Remove(subscriptionId);

								ProcessCandle(series, candleMsg);	
							}

							if (newSubscriptionIds != null)
							{
								if (newSubscriptionIds.Count == 0)
									return;

								candleMsg.SetSubscriptionIds(newSubscriptionIds.ToArray());
							}
						}
					}

					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void UpgradeSubscription(SeriesInfo series, SubscriptionResponseMessage response)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var original = series.Original;

			void Finish()
			{
				TryRemoveSeries(series.Id);

				if (response != null && !response.IsOk())
				{
					response = (SubscriptionResponseMessage)response.Clone();
					response.OriginalTransactionId = original.TransactionId;
					RaiseNewOutMessage(response);
				}
				else
					RaiseNewOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = original.TransactionId });
			}

			if (original.To != null && series.LastTime != null && original.To <= series.LastTime)
			{
				Finish();
				return;
			}

			switch (series.State)
			{
				case SeriesStates.Regular:
				{
					var isLoadOnly = series.Original.BuildMode == MarketDataBuildModes.Load;

					// upgrade to smaller tf only in case failed subscription
					if (response != null && original.DataType == MarketDataTypes.CandleTimeFrame && original.AllowBuildFromSmallerTimeFrame)
					{
						if (isLoadOnly)
						{
							Finish();
							return;
						}

						var smaller = InnerAdapter
										.GetTimeFrames(original.SecurityId, series.LastTime, original.To)
						                .FilterSmallerTimeFrames(original.GetTimeFrame())
						                .OrderByDescending()
						                .FirstOr();

						if (smaller != null)
						{
							series.Current = (MarketDataMessage)original.Clone();
							series.Current.Arg = smaller;
							series.Current.TransactionId = TransactionIdGenerator.GetNextId();

							series.BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original, (TimeFrameCandleBuilder)_candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)));
							series.State = SeriesStates.SmallTimeFrame;
							series.NonFinishedCandle = null;

							// loopback
							series.Current.IsBack = true;
							RaiseNewOutMessage(series.Current);

							return;
						}
					}

					if (response == null && isLoadOnly)
					{
						Finish();
						return;
					}

					series.State = SeriesStates.Compress;
					break;
				}
				case SeriesStates.SmallTimeFrame:
				{
					series.BigTimeFrameCompressor = null;
					series.State = SeriesStates.Compress;

					break;
				}
				case SeriesStates.Compress:
				{
					Finish();
					return;
				}
				//case SeriesStates.None:
				default:
					throw new ArgumentOutOfRangeException(nameof(series), series.State, LocalizedStrings.Str1219);
			}

			if (series.State != SeriesStates.Compress)
				throw new InvalidOperationException(series.State.ToString());

			series.NonFinishedCandle = null;

			var current = TryCreateBuildSubscription(original, series.LastTime);

			if (current == null)
			{
				Finish();
				return;
			}

			current.TransactionId = TransactionIdGenerator.GetNextId();

			lock (_syncObject)
				_replaceId.Add(current.TransactionId, series.Id);

			this.AddInfoLog("Series compress: ids {0}->{1}", original.TransactionId, current.TransactionId);

			series.Transform = CreateTransform(current.DataType, current.BuildField);
			series.Current = current;

			// loopback
			current.IsBack = true;
			RaiseNewOutMessage(current);
		}

		private void ProcessCandle(SeriesInfo info, CandleMessage candleMsg)
		{
			if (info.LastTime != null && info.LastTime > candleMsg.OpenTime)
				return;

			info.LastTime = candleMsg.OpenTime;

			var nonFinished = info.NonFinishedCandle;

			if (nonFinished != null && nonFinished.OpenTime < candleMsg.OpenTime)
			{
				nonFinished.State = CandleStates.Finished;
				RaiseNewOutMessage(nonFinished);
				info.NonFinishedCandle = null;
			}

			candleMsg = (CandleMessage)candleMsg.Clone();

			if (candleMsg.Type == MessageTypes.CandleTimeFrame)
			{
				// make all incoming candles as Active until next come
				candleMsg.State = CandleStates.Active;
			}

			SendCandle(info, candleMsg);

			if (candleMsg.State != CandleStates.Finished)
				info.NonFinishedCandle = (CandleMessage)candleMsg.Clone();
		}

		private bool ProcessValue(ISubscriptionIdMessage message)
		{
			var subsciptionIds = message.GetSubscriptionIds();

			HashSet<long> newSubscriptionIds = null;

			foreach (var id in subsciptionIds)
			{
				var series = TryGetSeries(id, out var subscriptionId);

				if (series == null)
					continue;

				if (newSubscriptionIds == null)
					newSubscriptionIds = new HashSet<long>(subsciptionIds);

				newSubscriptionIds.Remove(id);

				var transform = series.Transform;

				if (transform?.Process((Message)message) != true)
					continue;

				var time = transform.Time;
				var origin = series.Original;

				if (origin.To != null && origin.To.Value < time)
				{
					TryRemoveSeries(subscriptionId);
					RaiseNewOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = origin.TransactionId });
					continue;
				}

				if (series.LastTime != null && series.LastTime.Value > time)
					continue;

				series.LastTime = time;

				var builder = _candleBuilderProvider.Get(origin.ToDataType().MessageType);

				var result = builder.Process(origin, series.CurrentCandleMessage, transform);

				foreach (var candleMessage in result)
				{
					series.CurrentCandleMessage = candleMessage;

					if (series.Original.IsFinished && candleMessage.State != CandleStates.Finished)
						continue;

					SendCandle(series, (CandleMessage)candleMessage.Clone());
				}
			}

			if (newSubscriptionIds != null)
			{
				if (newSubscriptionIds.Count == 0)
					return true;

				message.SetSubscriptionIds(newSubscriptionIds.ToArray());
			}

			return false;
		}

		private void SendCandle(SeriesInfo info, CandleMessage candleMsg)
		{
			candleMsg.Adapter = candleMsg.Adapter;
			candleMsg.OriginalTransactionId = info.Id;
			candleMsg.SetSubscriptionIds(subscriptionId: info.Id);

			RaiseNewOutMessage(candleMsg);
		}

		/// <summary>
		/// Create a copy of <see cref="CandleBuilderMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new CandleBuilderMessageAdapter((IMessageAdapter)InnerAdapter.Clone(), _candleBuilderProvider);
		}
	}
}