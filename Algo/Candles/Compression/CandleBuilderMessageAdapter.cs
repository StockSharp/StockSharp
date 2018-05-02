namespace StockSharp.Algo.Candles.Compression
{
	using System;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.Algo.Storages;
	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Candle builder adapter.
	/// </summary>
	public class CandleBuilderMessageAdapter : MessageAdapterWrapper
	{
		private sealed class CandleBuildersList : SynchronizedList<ICandleBuilder>
		{
			private readonly SynchronizedDictionary<MarketDataTypes, ICandleBuilder> _builders = new SynchronizedDictionary<MarketDataTypes, ICandleBuilder>();

			public ICandleBuilder Get(MarketDataTypes type)
			{
				var builder = _builders.TryGetValue(type);

				if (builder == null)
					throw new ArgumentOutOfRangeException(nameof(type), type, LocalizedStrings.Str1219);

				return builder;
			}

			protected override void OnAdded(ICandleBuilder item)
			{
				_builders.Add(item.CandleType, item);
				base.OnAdded(item);
			}

			protected override bool OnRemoving(ICandleBuilder item)
			{
				lock (_builders.SyncRoot)
					_builders.RemoveWhere(p => p.Value == item);

				return base.OnRemoving(item);
			}

			protected override void OnInserted(int index, ICandleBuilder item)
			{
				_builders.Add(item.CandleType, item);
				base.OnInserted(index, item);
			}

			protected override bool OnClearing()
			{
				_builders.Clear();
				return base.OnClearing();
			}
		}

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

			public SeriesStates State { get; set; }

			public BiggerTimeFrameCandleCompressor BigTimeFrameCompressor { get; set; }

			public ICandleBuilderValueTransform Transform { get; set; }

			public DateTimeOffset? LastTime { get; set; }

			public CandleMessage CurrentCandleMessage { get; set; }
		}

		private readonly SynchronizedDictionary<long, SeriesInfo> _seriesByTransactionId = new SynchronizedDictionary<long, SeriesInfo>();
		private readonly SynchronizedDictionary<SecurityId, SynchronizedList<SeriesInfo>> _seriesBySecurityId = new SynchronizedDictionary<SecurityId, SynchronizedList<SeriesInfo>>();
		private readonly CandleBuildersList _candleBuilders;
		private readonly IExchangeInfoProvider _exchangeInfoProvider;

		/// <summary>
		/// Initializes a new instance of the <see cref="CandleBuilderMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Inner message adapter.</param>
		/// <param name="exchangeInfoProvider">The exchange boards provider.</param>
		public CandleBuilderMessageAdapter(IMessageAdapter innerAdapter, IExchangeInfoProvider exchangeInfoProvider)
			: base(innerAdapter)
		{
			if (exchangeInfoProvider == null)
				throw new ArgumentNullException(nameof(exchangeInfoProvider));

			_exchangeInfoProvider = exchangeInfoProvider;

			_candleBuilders = new CandleBuildersList
			{
				new TimeFrameCandleBuilder(exchangeInfoProvider),
				new TickCandleBuilder(),
				new VolumeCandleBuilder(),
				new RangeCandleBuilder(),
				new RenkoCandleBuilder(),
				new PnFCandleBuilder(),
			};
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
					break;

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.DataType != MarketDataTypes.CandleTimeFrame)
						break;

					if (mdMsg.IsSubscribe)
					{
						var transactionId = mdMsg.TransactionId;

						if (mdMsg.IsCalcVolumeProfile)
						{
							if (!TrySubscribeBuild(mdMsg, transactionId))
							{
								RaiseNewOutMessage(new MarketDataMessage
								{
									OriginalTransactionId = transactionId,
									IsNotSupported = true,
								});
							}

							return;
						}

						var originalTf = (TimeSpan)mdMsg.Arg;
						var timeFrames = InnerAdapter.GetTimeFrames(mdMsg.SecurityId).ToArray();

						if (timeFrames.Contains(originalTf))
						{
							var original = (MarketDataMessage)mdMsg.Clone();
							_seriesByTransactionId.Add(transactionId, new SeriesInfo(original, original)
							{
								State = SeriesStates.Regular,
								LastTime = original.From,
							});

							break;
						}

						if (mdMsg.AllowBuildFromSmallerTimeFrame)
						{
							var smaller = timeFrames
										  .FilterSmallerTimeFrames(originalTf)
										  .OrderByDescending()
										  .FirstOr();

							if (smaller != null)
							{
								var original = (MarketDataMessage)mdMsg.Clone();

								var current = (MarketDataMessage)original.Clone();
								current.Arg = smaller;

								_seriesByTransactionId.Add(transactionId, new SeriesInfo(original, current)
								{
									State = SeriesStates.SmallTimeFrame,
									BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original),
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
						
						return;
					}
					else
					{
						var series = _seriesByTransactionId.TryGetValue(mdMsg.OriginalTransactionId);

						if (series != null)
						{
							RemoveSeries(series);

							base.SendInMessage(new MarketDataMessage
							{
								TransactionId = TransactionIdGenerator.GetNextId(),
								OriginalTransactionId = series.Current.TransactionId,
								IsSubscribe = false,
							});

							RaiseNewOutMessage(new MarketDataMessage
							{
								OriginalTransactionId = mdMsg.OriginalTransactionId,
							});
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

			var buildFrom = original.BuildCandlesFrom ?? InnerAdapter.SupportedMarketDataTypes.Intersect(CandleHelper.CandleDataSources).FirstOr();

			if (buildFrom == null || !InnerAdapter.SupportedMarketDataTypes.Contains(buildFrom.Value))
				return null;

			var current = new MarketDataMessage
			{
				DataType = buildFrom.Value,
				From = original.From,
				To = original.To,
				Count = original.Count,
				MaxDepth = original.MaxDepth,
				TransactionId = getTransactionId(),
				BuildCandlesField = original.BuildCandlesField,
				IsSubscribe = true,
			};

			original.CopyTo(current, false);

			var series = new SeriesInfo((MarketDataMessage)original.Clone(), current)
			{
				LastTime = lastTime,
				Transform = CreateTransform(current.DataType, current.BuildCandlesField)
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
			var seriesList = _seriesBySecurityId.TryGetValue(series.Original.SecurityId);

			if (seriesList == null)
				return;

			lock (seriesList.SyncRoot)
				seriesList.RemoveWhere(s => s.Original.TransactionId == series.Original.TransactionId);
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
					ProcessMarketDataResponse((MarketDataMessage)message);
					break;
				}

				case MessageTypes.MarketDataFinished:
				{
					ProcessMarketDataFinished((MarketDataFinishedMessage)message);
					break;
				}

				case MessageTypes.CandleTimeFrame:
				{
					var smallCandle = (CandleMessage)message;
					var series = _seriesByTransactionId.TryGetValue(smallCandle.OriginalTransactionId);

					if (series == null)
						break;

					switch (series.State)
					{
						case SeriesStates.Regular:
							if (ProcessCandle((CandleMessage)message))
								return;

							break;

						case SeriesStates.SmallTimeFrame:
							var candles = series.BigTimeFrameCompressor.Process(smallCandle).Where(c => c.State == CandleStates.Finished);

							foreach (var bigCandle in candles)
							{
								bigCandle.Adapter = smallCandle.Adapter;
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

					break;
				}

				case MessageTypes.QuoteChange:
				{
					base.OnInnerAdapterNewOutMessage(message);

					var quoteMsg = (QuoteChangeMessage)message;

					ProcessValue(quoteMsg.SecurityId, 0, quoteMsg);
					break;
				}

				case MessageTypes.Level1Change:
				{
					base.OnInnerAdapterNewOutMessage(message);

					var l1Msg = (Level1ChangeMessage)message;
					
					ProcessValue(l1Msg.SecurityId, 0, l1Msg);
					break;
				}
			}

			base.OnInnerAdapterNewOutMessage(message);
		}

		private void ProcessMarketDataResponse(MarketDataMessage message)
		{
			if (!_seriesByTransactionId.TryGetValue(message.OriginalTransactionId, out var series))
			{
				RaiseNewOutMessage(message);
				return;
			}

			var isOk = !message.IsNotSupported && message.Error == null;

			if (isOk)
			{
				if (series.Current.TransactionId == message.OriginalTransactionId)
					RaiseNewOutMessage(message);

				return;
			}

			UpgradeSubscription(series);
		}

		private void ProcessMarketDataFinished(MarketDataFinishedMessage message)
		{
			if (!_seriesByTransactionId.TryGetValue(message.OriginalTransactionId, out var series))
			{
				RaiseNewOutMessage(message);
				return;
			}

			UpgradeSubscription(series);
		}

		private void UpgradeSubscription(SeriesInfo series)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var original = series.Original;

			void Finish()
			{
				RemoveSeries(series);
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
					if (original.AllowBuildFromSmallerTimeFrame)
					{
						var smaller = InnerAdapter.TimeFrames
						                          .FilterSmallerTimeFrames((TimeSpan)original.Arg)
						                          .OrderByDescending()
						                          .FirstOr();

						if (smaller != null)
						{
							series.Current = (MarketDataMessage)original.Clone();
							series.Current.Arg = smaller;
							series.Current.TransactionId = TransactionIdGenerator.GetNextId();

							series.BigTimeFrameCompressor = new BiggerTimeFrameCandleCompressor(original);
							series.State = SeriesStates.SmallTimeFrame;

							// loopback
							series.Current.IsBack = true;
							RaiseNewOutMessage(series.Current);

							return;
						}
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
			var infos = _seriesBySecurityId.TryGetValue(securityId);

			if (infos == null)
				return;

			foreach (var info in infos)
			{
				var transform = info.Transform;

				if (transform?.Process(message) != true)
					continue;

				if (info.Current.TransactionId != transactionId && (transactionId != 0/* || info.IsHistory*/))
					continue;

				if (!CheckTime(info, transform.Time))
					continue;

				var builder = _candleBuilders.Get(info.Original.DataType);

				info.LastTime = transform.Time;

				var result = builder.Process(info.Original, info.CurrentCandleMessage, transform);

				foreach (var candleMessage in result)
				{
					info.CurrentCandleMessage = candleMessage;
					SendCandle(info, candleMessage);
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

		private static bool CheckTime(SeriesInfo info, DateTimeOffset time)
		{
			if (info.LastTime != null && info.LastTime.Value > time)
				return false;

			//var res = info.Board.IsTradeTime(time, out var period);
			//info.CurrentPeriod = Tuple.Create(time, period);

			return true;
		}

		/// <summary>
		/// Create a copy of <see cref="CandleBuilderMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new CandleBuilderMessageAdapter(InnerAdapter, _exchangeInfoProvider);
		}
	}
}