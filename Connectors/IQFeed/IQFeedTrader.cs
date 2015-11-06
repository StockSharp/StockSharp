namespace StockSharp.IQFeed
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The interface <see cref="IConnector"/> implementation for the interaction with DTN IQFeed for download of historical and real-time market data (level 1, level 2).
	/// </summary>
	[Icon("IQFeed_logo.png")]
	public class IQFeedTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedDictionary<long, RefFive<List<CandleMessage>, SyncObject, bool, CandleSeries, bool>> _candleInfo = new SynchronizedDictionary<long, RefFive<List<CandleMessage>, SyncObject, bool, CandleSeries, bool>>();
		private readonly SynchronizedDictionary<long, RefFive<List<Level1ChangeMessage>, SyncObject, bool, SecurityId, bool>> _level1Info = new SynchronizedDictionary<long, RefFive<List<Level1ChangeMessage>, SyncObject, bool, SecurityId, bool>>();
		private readonly SynchronizedDictionary<long, CandleSeries> _candleSeries = new SynchronizedDictionary<long, CandleSeries>();

		/// <summary>
		/// Initializes a new instance of the <see cref="IQFeedTrader"/>.
		/// </summary>
		public IQFeedTrader()
		{
			CreateAssociatedSecurity = true;

			Adapter.InnerAdapters.Add(new IQFeedMarketDataMessageAdapter(TransactionIdGenerator));
		}

		private IQFeedMarketDataMessageAdapter NativeAdapter
		{
			get { return Adapter.InnerAdapters.OfType<IQFeedMarketDataMessageAdapter>().First(); }
		}

		/// <summary>
		/// The list of all available <see cref="IQFeedLevel1Column"/>.
		/// </summary>
		public IQFeedLevel1ColumnRegistry Level1ColumnRegistry
		{
			get { return NativeAdapter.Level1ColumnRegistry; }
		}

		/// <summary>
		/// Address for obtaining data on Level1.
		/// </summary>
		public EndPoint Level1Address
		{
			get { return NativeAdapter.Level1Address; }
			set { NativeAdapter.Level1Address = value; }
		}

		/// <summary>
		/// Address for obtaining data on Level2.
		/// </summary>
		public EndPoint Level2Address
		{
			get { return NativeAdapter.Level2Address; }
			set { NativeAdapter.Level2Address = value; }
		}

		/// <summary>
		/// Address for obtaining history data.
		/// </summary>
		public EndPoint LookupAddress
		{
			get { return NativeAdapter.LookupAddress; }
			set { NativeAdapter.LookupAddress = value; }
		}

		/// <summary>
		/// Address for obtaining service data.
		/// </summary>
		public EndPoint AdminAddress
		{
			get { return NativeAdapter.AdminAddress; }
			set { NativeAdapter.AdminAddress = value; }
		}

		/// <summary>
		/// Whether to load instruments from the archive of the IQFeed site. The default is off.
		/// </summary>
		public bool IsDownloadSecurityFromSite
		{
			get { return NativeAdapter.IsDownloadSecurityFromSite; }
			set { NativeAdapter.IsDownloadSecurityFromSite = value; }
		}

		/// <summary>
		/// Instruments types that will be downloaded from the site when the option <see cref="IQFeedTrader.IsDownloadSecurityFromSite"/> enabled.
		/// </summary>
		public IEnumerable<SecurityTypes> SecurityTypesFilter
		{
			get { return NativeAdapter.SecurityTypesFilter; }
			set { NativeAdapter.SecurityTypesFilter = value; }
		}

		/// <summary>
		/// All <see cref="IQFeedLevel1Column"/> to be transmit.
		/// </summary>
		public IEnumerable<IQFeedLevel1Column> Level1Columns
		{
			get { return NativeAdapter.Level1Columns; }
			set { NativeAdapter.Level1Columns = value.ToArray(); }
		}

		/// <summary>
		/// To query news for a specified date.
		/// </summary>
		/// <param name="date">Date.</param>
		public virtual void RequestNews(DateTimeOffset date)
		{
			SendInMessage(new MarketDataMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.News,
				IsSubscribe = true,
				From = date,
			});
		}

		/// <summary>
		/// Load historical ticks.
		/// </summary>
		/// <param name="security">The instrument for which you need to get all trades.</param>
		/// <param name="from">Begin period.</param>
		/// <param name="to">End period.</param>
		/// <param name="isSuccess">Whether all data were obtained successfully or the download process has been interrupted.</param>
		/// <returns>Historical ticks.</returns>
		[Obsolete("Метод устарел. Необходимо использовать метод GetHistoricalLevel1.")]
		public IEnumerable<Trade> GetTrades(Security security, DateTimeOffset from, DateTimeOffset to, out bool isSuccess)
		{
			return GetHistoricalLevel1(GetSecurityId(security), from, to, out isSuccess).Select(l1 =>
			{
				var id = (long?)l1.Changes.TryGetValue(Level1Fields.LastTradeId);

				if (id == null)
					return null;

				var t = EntityFactory.CreateTrade(security, id.Value, null);
				
				t.Time = (DateTimeOffset)l1.Changes[Level1Fields.LastTradeTime];
				t.Price = (decimal)l1.Changes[Level1Fields.LastTradePrice];
				t.Volume = (decimal?)l1.Changes.TryGetValue(Level1Fields.LastTradeVolume) ?? 0;
				t.OrderDirection = (Sides?)l1.Changes.TryGetValue(Level1Fields.LastTradeOrigin);
				t.IsUpTick = (bool?)l1.Changes.TryGetValue(Level1Fields.LastTradeUpDown);
				t.IsSystem = (bool?)l1.Changes.TryGetValue(Level1Fields.IsSystem);
				
				return t;
			}).Where(t => t != null);
		}

		/// <summary>
		/// To get historical ticks.
		/// </summary>
		/// <param name="securityId">The instrument identifier for which you need to get all trades.</param>
		/// <param name="count">Maximum ticks count.</param>
		/// <param name="isSuccess">Whether all data were obtained successfully or the download process has been interrupted.</param>
		/// <returns>Historical ticks.</returns>
		public IEnumerable<Level1ChangeMessage> GetHistoricalLevel1(SecurityId securityId, long count, out bool isSuccess)
		{
			this.AddInfoLog(LocalizedStrings.Str2144Params, securityId, count);

			var transactionId = TransactionIdGenerator.GetNextId();

			var info = RefTuple.Create(new List<Level1ChangeMessage>(), new SyncObject(), false, securityId, false);
			_level1Info.Add(transactionId, info);

			SendInMessage(new MarketDataMessage
			{
				SecurityId = securityId,
				DataType = MarketDataTypes.Level1,
				Count = count,
				IsSubscribe = true,
				TransactionId = transactionId,
			});

			lock (info.Second)
			{
				if (!info.Third)
					info.Second.Wait();
			}

			isSuccess = info.Fifth;

			return info.First;
		}

		/// <summary>
		/// To get historical ticks.
		/// </summary>
		/// <param name="securityId">The instrument identifier for which you need to get all trades.</param>
		/// <param name="from">Begin period.</param>
		/// <param name="to">End period.</param>
		/// <param name="isSuccess">Whether all data were obtained successfully or the download process has been interrupted.</param>
		/// <returns>Historical ticks.</returns>
		public IEnumerable<Level1ChangeMessage> GetHistoricalLevel1(SecurityId securityId, DateTimeOffset from, DateTimeOffset to, out bool isSuccess)
		{
			this.AddInfoLog(LocalizedStrings.Str2145Params, securityId, from, to);

			var transactionId = TransactionIdGenerator.GetNextId();

			var info = RefTuple.Create(new List<Level1ChangeMessage>(), new SyncObject(), false, securityId, false);
			_level1Info.Add(transactionId, info);

			SendInMessage(new MarketDataMessage
			{
				SecurityId = securityId,
				DataType = MarketDataTypes.Level1,
				From = from,
				To = to,
				IsSubscribe = true,
				TransactionId = transactionId,
			});

			lock (info.Second)
			{
				if (!info.Third)
					info.Second.Wait();
			}

			isSuccess = info.Fifth;

			return info.First;
		}

		/// <summary>
		/// To get historical candles.
		/// </summary>
		/// <param name="security">The instrument for which you need to get candles.</param>
		/// <param name="candleType">The candle type.</param>
		/// <param name="arg">The candle parameter (for example, time-frame).</param>
		/// <param name="count">Maximum ticks count.</param>
		/// <param name="isSuccess">Whether all data were obtained successfully or the download process has been interrupted.</param>
		/// <returns>Historical candles.</returns>
		public IEnumerable<CandleMessage> GetHistoricalCandles(Security security, Type candleType, object arg, long count, out bool isSuccess)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			//if (timeFrame <= TimeSpan.Zero)
			//	throw new ArgumentException("Тайм-фрейм должен быть больше 0.");

			var transactionId = TransactionIdGenerator.GetNextId();

			var series = new CandleSeries(candleType, security, arg);

			this.AddInfoLog(LocalizedStrings.Str2146Params, series, count);

			var info = RefTuple.Create(new List<CandleMessage>(), new SyncObject(), false, series, false);
			_candleInfo.Add(transactionId, info);

			var mdMsg = new MarketDataMessage
			{
				//SecurityId = GetSecurityId(series.Security),
				DataType = GetCandleType(series.CandleType),
				TransactionId = transactionId,
				Count = count,
				Arg = arg,
				IsSubscribe = true,
			}.FillSecurityInfo(this, series.Security);

			_candleSeries.Add(transactionId, series);

			SendInMessage(mdMsg);

			lock (info.Second)
			{
				if (!info.Third)
					info.Second.Wait();
			}

			isSuccess = info.Fifth;

			return info.First;
		}

		/// <summary>
		/// To get historical candles.
		/// </summary>
		/// <param name="security">The instrument for which you need to get candles.</param>
		/// <param name="candleType">The candle type.</param>
		/// <param name="arg">The candle parameter (for example, time-frame).</param>
		/// <param name="from">Begin period.</param>
		/// <param name="to">End period.</param>
		/// <param name="isSuccess">Whether all data were obtained successfully or the download process has been interrupted.</param>
		/// <returns>Historical candles.</returns>
		public IEnumerable<CandleMessage> GetHistoricalCandles(Security security, Type candleType, object arg, DateTimeOffset from, DateTimeOffset to, out bool isSuccess)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			//if (timeFrame <= TimeSpan.Zero)
			//	throw new ArgumentException("Тайм-фрейм должен быть больше 0.");

			if (from > to)
				throw new ArgumentException(LocalizedStrings.Str2147);

			var id = TransactionIdGenerator.GetNextId();

			var series = new CandleSeries(candleType, security, arg);

			this.AddInfoLog(LocalizedStrings.Str2148Params, series, from, to);

			var info = RefTuple.Create(new List<CandleMessage>(), new SyncObject(), false, series, false);
			_candleInfo.Add(id, info);
			
			SubscribeCandles(series, from, to, id);

			lock (info.Second)
			{
				if (!info.Third)
					info.Second.Wait();
			}

			isSuccess = info.Fifth;

			return info.First;
		}

		private void SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to, long transactionId)
		{
			var mdMsg = new MarketDataMessage
			{
				//SecurityId = GetSecurityId(series.Security),
				DataType = GetCandleType(series.CandleType),
				TransactionId = transactionId,
				From = from,
				To = to,
				Arg = series.Arg,
				IsSubscribe = true,
			}.FillSecurityInfo(this, series.Security);

			//var timeFrame = (TimeSpan)series.Arg;

			//if (timeFrame == TimeSpan.FromDays(7))
			//	mdMsg.Count = (to - from).TotalWeeks().To<int>();
			//else if (timeFrame.Ticks == TimeHelper.TicksPerMonth)
			//	mdMsg.Count = (to - from).TotalMonths().To<int>();

			_candleSeries.Add(transactionId, series);

			SendInMessage(mdMsg);
		}

		private static MarketDataTypes GetCandleType(Type candleType)
		{
			if (candleType == typeof(TimeFrameCandle))
				return MarketDataTypes.CandleTimeFrame;
			else if (candleType == typeof(TickCandle))
				return MarketDataTypes.CandleTick;
			else if (candleType == typeof(VolumeCandle))
				return MarketDataTypes.CandleVolume;

			throw new ArgumentOutOfRangeException(nameof(candleType), candleType, LocalizedStrings.WrongCandleType);
		}

		/// <summary>
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType == typeof(TimeFrameCandle) && series.Arg is TimeSpan)
			{
				if (IQFeedMarketDataMessageAdapter.TimeFrames.Contains((TimeSpan)series.Arg))
					yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
			}
		}

		/// <summary>
		/// Event of new candles occurring, that are received after the subscription by <see cref="SubscribeCandles(StockSharp.Algo.Candles.CandleSeries,System.DateTimeOffset,System.DateTimeOffset)"/>.
		/// </summary>
		public event Action<CandleSeries, IEnumerable<Candle>> NewCandles;

		/// <summary>
		/// The series processing end event.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// Subscribe to receive new candles.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <param name="from">The initial date from which you need to get data.</param>
		/// <param name="to">The final date by which you need to get data.</param>
		public void SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			SubscribeCandles(series, from, to, TransactionIdGenerator.GetNextId());
		}

		/// <summary>
		/// To stop the candles receiving subscription, previously created by <see cref="SubscribeCandles(StockSharp.Algo.Candles.CandleSeries,System.DateTimeOffset,System.DateTimeOffset)"/>.
		/// </summary>
		/// <param name="series">Candles series.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
		}

		/// <summary>
		/// Process message.
		/// </summary>
		/// <param name="message">Message.</param>
		protected override void OnProcessMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				case MessageTypes.Disconnect:
				{
					PulseWaiting();
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;

					if (mdMsg.Error != null)
					{
						var candleInfo = _candleInfo.TryGetValue(mdMsg.OriginalTransactionId);

						if (candleInfo != null)
						{
							lock (candleInfo.Second)
							{
								candleInfo.Third = true;
								candleInfo.Second.Pulse();
							}

							_candleInfo.Remove(mdMsg.OriginalTransactionId);
						}

						var l1Info = _level1Info.TryGetValue(mdMsg.OriginalTransactionId);

						if (l1Info != null)
						{
							lock (l1Info.Second)
							{
								l1Info.Third = true;
								l1Info.Second.Pulse();
							}

							_level1Info.Remove(mdMsg.OriginalTransactionId);
						}
					}

					break;
				}

				case MessageTypes.CandleRange:
				case MessageTypes.CandleTick:
				case MessageTypes.CandleTimeFrame:
				case MessageTypes.CandleVolume:
				{
					var candleMsg = (CandleMessage)message;

					var series = _candleSeries.TryGetValue(candleMsg.OriginalTransactionId);

					if (series == null)
						return;

					// сообщение с IsFinished = true не содержит данные по свече,
					// только флаг, что получение исторических данных завершено
					if (!candleMsg.IsFinished)
						NewCandles.SafeInvoke(series, new[] { candleMsg.ToCandle(series) });
					else
					{
						if (candleMsg.IsFinished)
							Stopped.SafeInvoke(series);
					}

					var info = _candleInfo.TryGetValue(candleMsg.OriginalTransactionId);

					if (info != null)
					{
						if (candleMsg.IsFinished)
						{
							lock (info.Second)
							{
								info.Third = true;
								info.Fifth = true;
								info.Second.Pulse();
							}

							_candleInfo.Remove(candleMsg.OriginalTransactionId);
						}
						else
							info.First.Add(candleMsg);
					}

					// DO NOT send historical data to Connector
					return;
				}

				case MessageTypes.Level1Change:
				{
					var l1Msg = (Level1ChangeMessage)message;

					var transactionId = l1Msg.GetRequestId();
					var info = _level1Info.TryGetValue(transactionId);

					// IsFinished = true message do not contains any data,
					// just mark historical data gathering as finished
					if (l1Msg.GetValue<bool>("IsFinished"))
					{
						if (info == null)
						{
							this.AddWarningLog(LocalizedStrings.Str2149Params.Put(transactionId));
							return;
						}

						lock (info.Second)
						{
							info.Third = true;
							info.Fifth = true;
							info.Second.Pulse();
						}

						_level1Info.Remove(transactionId);
					}
					else
					{
						// streaming l1 message (non historical)
						if (info == null)
							break;

						info.First.Add((Level1ChangeMessage)l1Msg.Clone());
					}

					// DO NOT send historical data to Connector
					return;
				}

				case ExtendedMessageTypes.System:
				{
					var msg = (IQFeedSystemMessage)message;

					if (msg.Feed.Address == NativeAdapter.LookupAddress)
						PulseWaiting();

					return;
				}
			}

			base.OnProcessMessage(message);
		}

		private void PulseWaiting()
		{
			foreach (var pair in _candleInfo.SyncGet(c => c.CopyAndClear()))
			{
				var candleInfo = pair.Value;

				this.AddWarningLog(LocalizedStrings.Str2150Params, candleInfo.Fourth);

				lock (candleInfo.Second)
				{
					candleInfo.Third = true;
					candleInfo.Second.Pulse();
				}
			}

			foreach (var pair in _level1Info.SyncGet(c => c.CopyAndClear()))
			{
				var l1Info = pair.Value;

				this.AddWarningLog(LocalizedStrings.Str2151Params, l1Info.Fourth);

				lock (l1Info.Second)
				{
					l1Info.Third = true;
					l1Info.Second.Pulse();
				}
			}
		}
	}
}
