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

		private class SeriesInfo : ICandleBuilderSubscription
		{
			public SeriesInfo(MarketDataMessage original, MarketDataMessage current)
			{
				Original = original ?? throw new ArgumentNullException(nameof(original));
				Current = current;
			}

			public long Id => Original.TransactionId;

			MarketDataMessage ICandleBuilderSubscription.Message => Original;

			public MarketDataMessage Original { get; }

			public Dictionary<SecurityId, SeriesInfo> Child { get; } = new Dictionary<SecurityId, SeriesInfo>();

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

			public long? Count { get; set; }

			public CandleMessage CurrentCandle { get; set; }

			public CandleMessage NonFinishedCandle { get; set; }

			public VolumeProfileBuilder VolumeProfile { get; set; }

			public bool Stopped;
		}

		private readonly SyncObject _syncObject = new SyncObject();

		private readonly Dictionary<long, SeriesInfo> _series = new Dictionary<long, SeriesInfo>();
		private readonly Dictionary<long, long> _replaceId = new Dictionary<long, long>();
		private readonly CandleBuilderProvider _candleBuilderProvider;
		private readonly Dictionary<long, SeriesInfo> _allChilds = new Dictionary<long, SeriesInfo>();
		private readonly Dictionary<long, RefPair<long, SubscriptionStates>> _pendingLoopbacks = new Dictionary<long, RefPair<long, SubscriptionStates>>();
		
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

		/// <summary>
		/// Send out finished candles when they received.
		/// </summary>
		public bool SendFinishedCandlesImmediatelly { get; set; }

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
						_allChilds.Clear();
						_pendingLoopbacks.Clear();
					}

					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (!_candleBuilderProvider.IsRegistered(mdMsg.DataType2.MessageType))
						break;

					var transactionId = mdMsg.TransactionId;

					if (mdMsg.IsSubscribe)
					{
						lock (_syncObject)
						{
							if (_replaceId.ContainsKey(transactionId))
								break;

							if (_pendingLoopbacks.TryGetAndRemove(transactionId, out var tuple))
							{
								if (tuple.Second != SubscriptionStates.Stopped)
								{
									if (tuple.Second == SubscriptionStates.Finished)
									{
										RaiseNewOutMessage(new SubscriptionFinishedMessage
										{
											OriginalTransactionId = transactionId,
										});
									}
									else
									{
										RaiseNewOutMessage(new SubscriptionResponseMessage
										{
											OriginalTransactionId = transactionId,
											Error = new InvalidOperationException(LocalizedStrings.SubscriptionInvalidState.Put(transactionId, tuple.Second)),
										});
									}

									return true;
								}

								tuple.Second = SubscriptionStates.Active;
								this.AddDebugLog("New ALL candle-map (active): {0}/{1} TrId={2}", mdMsg.SecurityId, tuple.Second, mdMsg.TransactionId);
								
								RaiseNewOutMessage(mdMsg.CreateResponse());
								return true;
							}
						}

						var isLoadOnly = mdMsg.BuildMode == MarketDataBuildModes.Load;

						if (mdMsg.IsCalcVolumeProfile)
						{
							if (!IsSupportCandlesPriceLevels)
							{
								if (isLoadOnly)
								{
									RaiseNewOutMessage(transactionId.CreateNotSupported());
								}
								else
								{
									if (!TrySubscribeBuild(mdMsg))
										RaiseNewOutMessage(transactionId.CreateNotSupported());
								}

								return true;
							}
						}
						
						if (mdMsg.BuildMode == MarketDataBuildModes.Build)
						{
							if (!TrySubscribeBuild(mdMsg))
								RaiseNewOutMessage(transactionId.CreateNotSupported());
							
							return true;
						}

						if (mdMsg.DataType2.MessageType == typeof(TimeFrameCandleMessage))
						{
							var originalTf = mdMsg.GetTimeFrame();
							var timeFrames = InnerAdapter.GetTimeFrames(mdMsg.SecurityId, mdMsg.From, mdMsg.To).ToArray();

							if (timeFrames.Contains(originalTf) || InnerAdapter.CheckTimeFrameByRequest)
							{
								this.AddInfoLog("Origin tf: {0}", originalTf);

								var original = mdMsg.TypedClone();

								if (mdMsg.To == null &&
									mdMsg.BuildMode == MarketDataBuildModes.LoadAndBuild &&
									!mdMsg.IsFinishedOnly &&
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
										Count = original.Count,
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

									var original = mdMsg.TypedClone();

									var current = original.TypedClone();
									current.SetArg(smaller);

									lock (_syncObject)
									{
										_series.Add(transactionId, new SeriesInfo(original, current)
										{
											State = SeriesStates.SmallTimeFrame,
											BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original, _candleBuilderProvider.Get(typeof(TimeFrameCandleMessage))),
											LastTime = original.From,
											Count = original.Count,
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
								this.AddInfoLog("Origin arg: {0}", mdMsg.GetArg());

								var original = mdMsg.TypedClone();

								lock (_syncObject)
								{
									_series.Add(transactionId, new SeriesInfo(original, original)
									{
										State = SeriesStates.Regular,
										LastTime = original.From,
										Count = original.Count,
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
						{
							var sentResponse = false;

							lock (_syncObject)
							{
								if (_allChilds.TryGetAndRemove(mdMsg.OriginalTransactionId, out var child))
								{
									child.Stopped = true;
									sentResponse = true;
								}
							}

							if (sentResponse)
							{
								RaiseNewOutMessage(mdMsg.CreateResponse());
								return true;
							}

							break;
						}
						else
						{
							// TODO sub childs
						}

						var unsubscribe = series.Current.TypedClone();

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

		private MarketDataMessage TryCreateBuildSubscription(MarketDataMessage original, DateTimeOffset? lastTime, long? count, bool needCalcCount)
		{
			if (original == null)
				throw new ArgumentNullException(nameof(original));

			var buildFrom = InnerAdapter.TryGetCandlesBuildFrom(original, _candleBuilderProvider);

			if (buildFrom == null)
				return null;

			if (needCalcCount && count is null)
				count = GetMaxCount(buildFrom);

			var current = new MarketDataMessage
			{
				DataType2 = buildFrom,
				From = lastTime,
				To = original.To,
				Count = count,
				MaxDepth = original.MaxDepth,
				BuildField = original.BuildField,
				IsSubscribe = true,
			};

			original.CopyTo(current, false);

			this.AddInfoLog("Build tf: {0}->{1}", buildFrom, original.GetArg());

			return current;
		}

		private bool TrySubscribeBuild(MarketDataMessage original)
		{
			var current = TryCreateBuildSubscription(original, original.From, original.Count, false);

			if (current == null)
				return false;

			current.TransactionId = original.TransactionId;

			var series = new SeriesInfo(original.TypedClone(), current)
			{
				LastTime = current.From,
				Count = current.Count,
				Transform = CreateTransform(current),
				State = SeriesStates.Compress,
			};

			lock (_syncObject)
				_series.Add(original.TransactionId, series);

			base.OnSendInMessage(current);
			return true;
		}

		private static ICandleBuilderValueTransform CreateTransform(MarketDataMessage current)
		{
			return CreateTransform(current.DataType2, current.BuildField);
		}

		private static ICandleBuilderValueTransform CreateTransform(DataType dataType, Level1Fields? field)
		{
			if (dataType == DataType.Ticks)
			{
				return new TickCandleBuilderValueTransform();
			}
			else if (dataType == DataType.MarketDepth)
			{
				var t = new QuoteCandleBuilderValueTransform();

				if (field != null)
					t.Type = field.Value;

				return t;
			}
			else if (dataType == DataType.Level1)
			{
				var t = new Level1CandleBuilderValueTransform();

				if (field != null)
					t.Type = field.Value;

				return t;
			}
			else if (dataType == DataType.OrderLog)
			{
				var t = new OrderLogCandleBuilderValueTransform();

				if (field != null)
					t.Type = field.Value;

				return t;
			}
			else
				throw new ArgumentOutOfRangeException(nameof(dataType), dataType, LocalizedStrings.Str1219);
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
						UpgradeSubscription(series, response, null);

					return;
				}

				case MessageTypes.SubscriptionFinished:
				{
					var finishMsg = (SubscriptionFinishedMessage)message;
					var subscriptionId = finishMsg.OriginalTransactionId;

					var series = TryGetSeries(subscriptionId, out _);

					if (series == null)
						break;

					UpgradeSubscription(series, null, finishMsg);
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
					if (message.Type == MessageTypes.QuoteChange && ((QuoteChangeMessage)message).State != null)
						break;

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
											bigCandle.SetSubscriptionIds(subscriptionId: series.Id);
											bigCandle.Adapter = candleMsg.Adapter;
											series.LastTime = bigCandle.CloseTime;

											if (series.Count != null)
											{
												if (series.Count <= 0)
													break;

												series.Count--;
											}

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

		private void UpgradeSubscription(SeriesInfo series, SubscriptionResponseMessage response, SubscriptionFinishedMessage finish)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var original = series.Original;

			void Finish()
			{
				TryRemoveSeries(series.Id);

				if (response != null && !response.IsOk())
				{
					response = response.TypedClone();
					response.OriginalTransactionId = original.TransactionId;
					RaiseNewOutMessage(response);
				}
				else
					RaiseNewOutMessage(new SubscriptionFinishedMessage { OriginalTransactionId = original.TransactionId });
			}

			if (finish?.NextFrom != null)
				series.LastTime = finish.NextFrom;

			if (original.To != null && series.LastTime != null && original.To <= series.LastTime)
			{
				Finish();
				return;
			}

			if (original.Count != null && series.Count <= 0)
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
					if (response != null && original.DataType2.MessageType == typeof(TimeFrameCandleMessage) && original.AllowBuildFromSmallerTimeFrame)
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
							var newTransId = TransactionIdGenerator.GetNextId();

							series.Current = original.TypedClone();
							series.Current.SetArg(smaller);
							series.Current.TransactionId = newTransId;

							series.BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original, _candleBuilderProvider.Get(typeof(TimeFrameCandleMessage)));
							series.State = SeriesStates.SmallTimeFrame;
							series.NonFinishedCandle = null;

							lock (_syncObject)
								_replaceId.Add(series.Current.TransactionId, series.Id);

							this.AddInfoLog("Series smaller tf: ids {0}->{1}", original.TransactionId, newTransId);

							// loopback
							series.Current.LoopBack(this);
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

			var current = TryCreateBuildSubscription(original, series.LastTime, null, series.Count != null);

			if (current == null)
			{
				Finish();
				return;
			}

			current.TransactionId = TransactionIdGenerator.GetNextId();

			lock (_syncObject)
				_replaceId.Add(current.TransactionId, series.Id);

			this.AddInfoLog("Series compress: ids {0}->{1}", original.TransactionId, current.TransactionId);

			series.Transform = CreateTransform(current);
			series.Current = current;

			// loopback
			current.LoopBack(this);
			RaiseNewOutMessage(current);
		}

		private void ProcessCandle(SeriesInfo info, CandleMessage candleMsg)
		{
			if (info.LastTime != null && info.LastTime > candleMsg.OpenTime)
				return;

			if (info.Count <= 0)
				return;

			info.LastTime = candleMsg.OpenTime;

			var nonFinished = info.NonFinishedCandle;

			if (nonFinished != null && nonFinished.OpenTime < candleMsg.OpenTime)
			{
				nonFinished.State = CandleStates.Finished;
				nonFinished.LocalTime = candleMsg.LocalTime;
				RaiseNewOutCandle(info, nonFinished);
				info.NonFinishedCandle = null;
			}

			candleMsg = candleMsg.TypedClone();

			if (candleMsg.Type == MessageTypes.CandleTimeFrame && !SendFinishedCandlesImmediatelly)
			{
				// make all incoming candles as Active until next come
				candleMsg.State = CandleStates.Active;
			}

			SendCandle(info, candleMsg);

			if (candleMsg.State != CandleStates.Finished)
				info.NonFinishedCandle = candleMsg.TypedClone();
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

				var isAll = series.Original.SecurityId == default;

				if (isAll)
				{
					SubscriptionSecurityAllMessage allMsg = null;

					lock (_syncObject)
					{
						series = series.Child.SafeAdd(((ISecurityIdMessage)message).SecurityId, key =>
						{
							allMsg = new SubscriptionSecurityAllMessage();

							series.Original.CopyTo(allMsg);

							allMsg.ParentTransactionId = series.Original.TransactionId;
							allMsg.TransactionId = TransactionIdGenerator.GetNextId();
							allMsg.SecurityId = key;

							allMsg.LoopBack(this, MessageBackModes.Chain);
							_pendingLoopbacks.Add(allMsg.TransactionId, RefTuple.Create(allMsg.ParentTransactionId, SubscriptionStates.Stopped));

							this.AddDebugLog("New ALL candle-map: {0}/{1} TrId={2}-{3}", key, series.Original.DataType2, allMsg.ParentTransactionId, allMsg.TransactionId);
							
							return new SeriesInfo(allMsg, allMsg)
							{
								LastTime = allMsg.From,
								Count = allMsg.Count,
								Transform = CreateTransform(series.Current),
								State = SeriesStates.Compress,
							};
						});

						if (series.Stopped)
							continue;
					}

					if (allMsg != null)
						RaiseNewOutMessage(allMsg);
				}
				
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

				if (series.Count <= 0)
					continue;

				series.LastTime = time;

				var builder = _candleBuilderProvider.Get(origin.DataType2.MessageType);

				var result = builder.Process(series, transform);

				foreach (var candleMessage in result)
				{
					if (series.Original.IsFinishedOnly && candleMessage.State != CandleStates.Finished)
						continue;

					SendCandle(series, candleMessage.TypedClone());
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

			RaiseNewOutCandle(info, candleMsg);
		}

		private void RaiseNewOutCandle(SeriesInfo info, CandleMessage candleMsg)
		{
			if (candleMsg.State == CandleStates.Finished)
			{
				if (info.Count != null)
					info.Count--;
			}

			RaiseNewOutMessage(candleMsg);
		}

		/// <summary>
		/// Create a copy of <see cref="CandleBuilderMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new CandleBuilderMessageAdapter(InnerAdapter.TypedClone(), _candleBuilderProvider) { SendFinishedCandlesImmediatelly = SendFinishedCandlesImmediatelly };
		}
	}
}