namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Linq;

	using Ecng.Collections;

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
		}

		private readonly SynchronizedDictionary<long, SeriesInfo> _seriesByTransactionId = new SynchronizedDictionary<long, SeriesInfo>();
		private readonly SynchronizedDictionary<SecurityId, CachedSynchronizedList<SeriesInfo>> _seriesBySecurityId = new SynchronizedDictionary<SecurityId, CachedSynchronizedList<SeriesInfo>>();
		private readonly SynchronizedSet<long> _unsubscriptions = new SynchronizedSet<long>();
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
		public override void SendInMessage(Message message)
		{
			if (message.IsBack)
			{
				base.SendInMessage(message);
				return;
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
					_seriesByTransactionId.Clear();
					_seriesBySecurityId.Clear();
					_unsubscriptions.Clear();
					break;

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (!_candleBuilderProvider.IsRegistered(mdMsg.DataType))
						break;

					if (mdMsg.IsSubscribe)
					{
						var transactionId = mdMsg.TransactionId;
						var isLoadOnly = mdMsg.BuildMode == MarketDataBuildModes.Load;

						if (mdMsg.IsCalcVolumeProfile || mdMsg.BuildMode == MarketDataBuildModes.Build)
						{
							if (isLoadOnly || !TrySubscribeBuild(mdMsg, transactionId))
							{
								RaiseNewOutMessage(new MarketDataMessage
								{
									OriginalTransactionId = transactionId,
									IsNotSupported = true,
								});
							}

							return;
						}

						if (mdMsg.DataType == MarketDataTypes.CandleTimeFrame)
						{
							var originalTf = (TimeSpan)mdMsg.Arg;
							var timeFrames = InnerAdapter.GetTimeFrames(mdMsg.SecurityId).ToArray();

							if (timeFrames.Contains(originalTf) || InnerAdapter.CheckTimeFrameByRequest)
							{
								this.AddInfoLog("Origin tf: {0}", originalTf);

								var original = (MarketDataMessage)mdMsg.Clone();
								_seriesByTransactionId.Add(transactionId, new SeriesInfo(original, original)
								{
									State = SeriesStates.Regular,
									LastTime = original.From,
								});

								break;
							}

							if (isLoadOnly)
							{
								RaiseNewOutMessage(new MarketDataMessage
								{
									OriginalTransactionId = transactionId,
									IsNotSupported = true,
								});

								return;
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

									_seriesByTransactionId.Add(transactionId, new SeriesInfo(original, current)
									{
										State = SeriesStates.SmallTimeFrame,
										BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original, (TimeFrameCandleBuilder)_candleBuilderProvider.Get(MarketDataTypes.CandleTimeFrame)),
										LastTime = original.From,
									});

									base.SendInMessage(current);
									return;
								}
							}

							if (!TrySubscribeBuild(mdMsg, transactionId))
							{
								RaiseNewOutMessage(new MarketDataMessage
								{
									OriginalTransactionId = transactionId,
									IsNotSupported = true,
								});
							}
						}
						else
						{
							if (InnerAdapter.IsMarketDataTypeSupported(mdMsg.DataType))
							{
								this.AddInfoLog("Origin arg: {0}", mdMsg.Arg);

								var original = (MarketDataMessage)mdMsg.Clone();
								_seriesByTransactionId.Add(transactionId, new SeriesInfo(original, original)
								{
									State = SeriesStates.Regular,
									LastTime = original.From,
								});

								break;
							}
							else
							{
								if (!TrySubscribeBuild(mdMsg, transactionId))
								{
									RaiseNewOutMessage(new MarketDataMessage
									{
										OriginalTransactionId = transactionId,
										IsNotSupported = true,
									});
								}
							}
						}
						
						return;
					}
					else
					{
						var series = _seriesByTransactionId.TryGetValue(mdMsg.OriginalTransactionId);

						if (series != null)
						{
							RemoveSeries(series);

							RaiseNewOutMessage(new MarketDataMessage
							{
								OriginalTransactionId = mdMsg.TransactionId,
							});

							var transactionId = TransactionIdGenerator.GetNextId();

							_unsubscriptions.Add(transactionId);

							var unsubscribe = (MarketDataMessage)series.Current.Clone();

							unsubscribe.TransactionId = transactionId;
							unsubscribe.OriginalTransactionId = series.Current.TransactionId;
							unsubscribe.IsSubscribe = false;

							base.SendInMessage(unsubscribe);

							return;
						}

						break;
					}
				}
			}

			base.SendInMessage(message);
		}

		private MarketDataMessage TryCreateBuildSubscription(MarketDataMessage original, DateTimeOffset? lastTime, Func<long> getTransactionId)
		{
			if (original == null)
				throw new ArgumentNullException(nameof(original));

			if (getTransactionId == null)
				throw new ArgumentNullException(nameof(getTransactionId));

			var buildFrom = original.BuildFrom ?? InnerAdapter.SupportedMarketDataTypes.Intersect(CandleHelper.CandleDataSources).OrderBy(t =>
			{
				// make priority
				switch (t)
				{
					case MarketDataTypes.Trades:
						return 0;
					case MarketDataTypes.Level1:
						return 1;
					case MarketDataTypes.OrderLog:
						return 2;
					case MarketDataTypes.MarketDepth:
						return 3;
					default:
						return 4;
				}
			}).FirstOr();

			if (buildFrom == null || !InnerAdapter.SupportedMarketDataTypes.Contains(buildFrom.Value))
				return null;

			var current = new MarketDataMessage
			{
				DataType = buildFrom.Value,
				From = lastTime,
				To = original.To,
				Count = original.Count,
				MaxDepth = original.MaxDepth,
				TransactionId = getTransactionId(),
				BuildField = original.BuildField,
				IsSubscribe = true,
			};

			original.CopyTo(current, false);

			this.AddInfoLog("Build tf: {0}->{1}", buildFrom, original.Arg);

			var series = new SeriesInfo((MarketDataMessage)original.Clone(), current)
			{
				LastTime = lastTime,
				Transform = CreateTransform(current.DataType, current.BuildField),
				State = SeriesStates.Compress,
			};

			AddSeries(series);

			return current;
		}

		private bool TrySubscribeBuild(MarketDataMessage original, long transactionId)
		{
			var current = TryCreateBuildSubscription(original, original.From, () => transactionId);

			if (current == null)
				return false;

			base.SendInMessage(current);
			return true;
		}

		private void AddSeries(SeriesInfo series)
		{
			_seriesByTransactionId.Add(series.Current.TransactionId, series);

			_seriesBySecurityId
				.SafeAdd(series.Original.SecurityId)
				.Add(series);
		}

		private void RemoveSeries(SeriesInfo series)
		{
			lock (_seriesBySecurityId.SyncRoot)
			{
				var seriesList = _seriesBySecurityId.TryGetValue(series.Original.SecurityId);

				if (seriesList == null)
					return;

				lock (seriesList.SyncRoot)
					seriesList.RemoveWhere(s => s.Original.TransactionId == series.Original.TransactionId);

				if (seriesList.Count == 0)
					_seriesBySecurityId.Remove(series.Original.SecurityId);
			}
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
				case MessageTypes.MarketData:
				{
					if (ProcessMarketDataResponse((MarketDataMessage)message))
						return;

					break;
				}

				case MessageTypes.MarketDataFinished:
				{
					if (ProcessMarketDataFinished((MarketDataFinishedMessage)message))
						return;

					break;
				}

				case MessageTypes.CandleTimeFrame:
				{
					var candle = (CandleMessage)message;
					var series = _seriesByTransactionId.TryGetValue(candle.OriginalTransactionId);

					if (series == null)
						break;

					switch (series.State)
					{
						case SeriesStates.Regular:
							if (ProcessCandle((CandleMessage)message))
								return;

							break;

						case SeriesStates.SmallTimeFrame:
							var candles = series.BigTimeFrameCompressor.Process(candle).Where(c => c.State == CandleStates.Finished);

							foreach (var bigCandle in candles)
							{
								bigCandle.OriginalTransactionId = series.Original.TransactionId;
								bigCandle.Adapter = candle.Adapter;
								series.LastTime = bigCandle.CloseTime;
								base.OnInnerAdapterNewOutMessage(bigCandle);
							}

							break;

						// TODO default
					}

					return;
				}

				case MessageTypes.CandlePnF:
				case MessageTypes.CandleRange:
				case MessageTypes.CandleRenko:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleVolume:
				{
					if (ProcessCandle((CandleMessage)message))
						return;

					break;
				}

				case MessageTypes.Execution:
				{
					base.OnInnerAdapterNewOutMessage(message);
					
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType == ExecutionTypes.Tick || execMsg.ExecutionType == ExecutionTypes.OrderLog)
						ProcessValue(execMsg.SecurityId, execMsg.OriginalTransactionId, execMsg);

					return;
				}

				case MessageTypes.QuoteChange:
				{
					base.OnInnerAdapterNewOutMessage(message);

					var quoteMsg = (QuoteChangeMessage)message;

					ProcessValue(quoteMsg.SecurityId, 0, quoteMsg);
					return;
				}

				case MessageTypes.Level1Change:
				{
					base.OnInnerAdapterNewOutMessage(message);

					var l1Msg = (Level1ChangeMessage)message;
					
					ProcessValue(l1Msg.SecurityId, 0, l1Msg);
					return;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private bool ProcessMarketDataResponse(MarketDataMessage message)
		{
			if (_unsubscriptions.Remove(message.OriginalTransactionId))
				return true;

			if (!_seriesByTransactionId.TryGetValue(message.OriginalTransactionId, out var series))
				return false;

			var isOk = !message.IsNotSupported && message.Error == null;

			if (isOk)
			{
				if (series.Current.TransactionId == message.OriginalTransactionId)
					RaiseNewOutMessage(message);
			}
			else
				UpgradeSubscription(series, message);

			return true;
		}

		private bool ProcessMarketDataFinished(MarketDataFinishedMessage message)
		{
			if (!_seriesByTransactionId.TryGetValue(message.OriginalTransactionId, out var series))
				return false;

			UpgradeSubscription(series, null);
			return true;
		}

		private void UpgradeSubscription(SeriesInfo series, MarketDataMessage response)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var original = series.Original;

			void Finish()
			{
				RemoveSeries(series);

				if (response != null && (response.Error != null || response.IsNotSupported))
				{
					response = (MarketDataMessage)response.Clone();
					response.OriginalTransactionId = original.TransactionId;
					RaiseNewOutMessage(response);
				}
				else
					RaiseNewOutMessage(new MarketDataFinishedMessage { OriginalTransactionId = original.TransactionId });
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

					// upgrate to smaller tf only in case failed subscription
					if (response != null && original.AllowBuildFromSmallerTimeFrame)
					{
						if (isLoadOnly)
						{
							Finish();
							return;
						}

						var smaller = InnerAdapter
										.TimeFrames
						                .FilterSmallerTimeFrames((TimeSpan)original.Arg)
						                .OrderByDescending()
						                .FirstOr();

						if (smaller != null)
						{
							series.Current = (MarketDataMessage)original.Clone();
							series.Current.Arg = smaller;
							series.Current.TransactionId = TransactionIdGenerator.GetNextId();

							series.BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original, (TimeFrameCandleBuilder)_candleBuilderProvider.Get(MarketDataTypes.CandleTimeFrame));
							series.State = SeriesStates.SmallTimeFrame;

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

			var current = TryCreateBuildSubscription(original, series.LastTime, TransactionIdGenerator.GetNextId);

			if (current == null)
			{
				Finish();
				return;
			}

			// loopback
			current.IsBack = true;
			RaiseNewOutMessage(current);
		}

		private bool ProcessCandle(CandleMessage candleMsg)
		{
			if (!_seriesByTransactionId.TryGetValue(candleMsg.OriginalTransactionId, out var info))
				return false;

			if (info.LastTime != null && info.LastTime > candleMsg.OpenTime)
				return true;

			SendCandle(info, candleMsg);
			return true;
		}

		private void ProcessValue<TMessage>(SecurityId securityId, long transactionId, TMessage message)
			where TMessage : Message
		{
			var seriesList = _seriesBySecurityId.TryGetValue(securityId);

			if (seriesList == null)
				return;

			foreach (var series in seriesList.Cache)
			{
				if (series.Current.TransactionId != transactionId && (transactionId != 0/* || info.IsHistory*/))
					continue;

				var transform = series.Transform;

				if (transform?.Process(message) != true)
					continue;

				var time = transform.Time;
				var origin = series.Original;

				if (origin.To != null && origin.To.Value < time)
				{
					RemoveSeries(series);
					RaiseNewOutMessage(new MarketDataFinishedMessage { OriginalTransactionId = origin.TransactionId });
					continue;
				}

				if (series.LastTime != null && series.LastTime.Value > time)
					continue;

				series.LastTime = time;

				var builder = _candleBuilderProvider.Get(origin.DataType);

				var result = builder.Process(origin, series.CurrentCandleMessage, transform);

				foreach (var candleMessage in result)
				{
					series.CurrentCandleMessage = candleMessage;
					SendCandle(series, candleMessage);
				}
			}
		}

		private void SendCandle(SeriesInfo info, CandleMessage candleMsg)
		{
			//if (info.LastTime > candleMsg.OpenTime)
			//	return;

			info.LastTime = candleMsg.OpenTime;

			var clone = (CandleMessage)candleMsg.Clone();
			clone.Adapter = candleMsg.Adapter;
			clone.OriginalTransactionId = info.Original.TransactionId;

			RaiseNewOutMessage(clone);
		}

		/// <summary>
		/// Create a copy of <see cref="CandleBuilderMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new CandleBuilderMessageAdapter(InnerAdapter, _candleBuilderProvider);
		}
	}
}