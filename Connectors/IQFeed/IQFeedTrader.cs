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
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с DTN IQFeed для скачивания исторических и реал-тайм маркет-данных (level 1, level 2).
	/// </summary>
	public class IQFeedTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedDictionary<long, RefFive<List<Candle>, SyncObject, bool, CandleSeries, bool>> _candleInfo = new SynchronizedDictionary<long, RefFive<List<Candle>, SyncObject, bool, CandleSeries, bool>>();
		private readonly SynchronizedDictionary<long, RefFive<List<Level1ChangeMessage>, SyncObject, bool, SecurityId, bool>> _level1Info = new SynchronizedDictionary<long, RefFive<List<Level1ChangeMessage>, SyncObject, bool, SecurityId, bool>>();
		private readonly SynchronizedDictionary<long, CandleSeries> _candleSeries = new SynchronizedDictionary<long, CandleSeries>();

		private readonly IQFeedMarketDataMessageAdapter _adapter;

		/// <summary>
		/// Создать <see cref="IQFeedTrader"/>.
		/// </summary>
		public IQFeedTrader()
		{
			CreateAssociatedSecurity = true;

			_adapter = new IQFeedMarketDataMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(_adapter.ToChannel(this));
		}

		/// <summary>
		/// Список всех доступных <see cref="IQFeedLevel1Column"/>.
		/// </summary>
		public IQFeedLevel1ColumnRegistry Level1ColumnRegistry
		{
			get { return _adapter.Level1ColumnRegistry; }
		}

		/// <summary>
		/// Адрес для получения данных по Level1.
		/// </summary>
		public EndPoint Level1Address
		{
			get { return _adapter.Level1Address; }
			set { _adapter.Level1Address = value; }
		}

		/// <summary>
		/// Адрес для получения данных по Level2.
		/// </summary>
		public EndPoint Level2Address
		{
			get { return _adapter.Level2Address; }
			set { _adapter.Level2Address = value; }
		}

		/// <summary>
		/// Адрес для получения исторических данных.
		/// </summary>
		public EndPoint LookupAddress
		{
			get { return _adapter.LookupAddress; }
			set { _adapter.LookupAddress = value; }
		}

		/// <summary>
		/// Адрес для получения служебных данных.
		/// </summary>
		public EndPoint AdminAddress
		{
			get { return _adapter.AdminAddress; }
			set { _adapter.AdminAddress = value; }
		}

		/// <summary>
		/// Загружать ли инструменты из архива с сайта IQFeed. По-умолчанию выключено.
		/// </summary>
		public bool IsDownloadSecurityFromSite
		{
			get { return _adapter.IsDownloadSecurityFromSite; }
			set { _adapter.IsDownloadSecurityFromSite = value; }
		}

		/// <summary>
		/// Типы инструментов, которые будут скачены с сайта при включенной опции <see cref="IsDownloadSecurityFromSite"/>.
		/// </summary>
		public IEnumerable<SecurityTypes> SecurityTypesFilter
		{
			get { return _adapter.SecurityTypesFilter; }
			set { _adapter.SecurityTypesFilter = value; }
		}

		/// <summary>
		/// Все <see cref="IQFeedLevel1Column"/>, которые необходимо транслировать.
		/// </summary>
		public IEnumerable<IQFeedLevel1Column> Level1Columns
		{
			get { return _adapter.Level1Columns; }
			set { _adapter.Level1Columns = value.ToArray(); }
		}

		/// <summary>
		/// Запросить новости для заданной даты.
		/// </summary>
		/// <param name="date">Дата.</param>
		public virtual void RequestNews(DateTime date)
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
		/// Загрузить исторические сделки.
		/// </summary>
		/// <param name="security">Инструмент, для которого необходимо получить все сделки.</param>
		/// <param name="from">Дата начала периода.</param>
		/// <param name="to">Дата окончания периода.</param>
		/// <param name="isSuccess">Успешно ли получены все данные или процесс загрузки был прерван.</param>
		/// <returns>Исторические сделки.</returns>
		[Obsolete("Метод устарел. Необходимо использовать метод GetHistoricalLevel1.")]
		public IEnumerable<Trade> GetTrades(Security security, DateTime from, DateTime to, out bool isSuccess)
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
		/// Получить исторических тиков.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо получить все сделки.</param>
		/// <param name="count">Максимальное количество тиков.</param>
		/// <param name="isSuccess">Успешно ли получены все данные или процесс загрузки был прерван.</param>
		/// <returns>Исторические сделки.</returns>
		public IEnumerable<Level1ChangeMessage> GetHistoricalLevel1(SecurityId securityId, long count, out bool isSuccess)
		{
			this.AddInfoLog(LocalizedStrings.Str2144Params, securityId, count);

			var transactionId = TransactionIdGenerator.GetNextId();

			var info = new RefFive<List<Level1ChangeMessage>, SyncObject, bool, SecurityId, bool>(new List<Level1ChangeMessage>(), new SyncObject(), false, securityId, false);
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
		/// Получить исторических тиков.
		/// </summary>
		/// <param name="securityId">Идентификатор инструмента, для которого необходимо получить все сделки.</param>
		/// <param name="from">Дата начала периода.</param>
		/// <param name="to">Дата окончания периода.</param>
		/// <param name="isSuccess">Успешно ли получены все данные или процесс загрузки был прерван.</param>
		/// <returns>Исторические сделки.</returns>
		public IEnumerable<Level1ChangeMessage> GetHistoricalLevel1(SecurityId securityId, DateTime from, DateTime to, out bool isSuccess)
		{
			this.AddInfoLog(LocalizedStrings.Str2145Params, securityId, from, to);

			var transactionId = TransactionIdGenerator.GetNextId();

			var info = new RefFive<List<Level1ChangeMessage>, SyncObject, bool, SecurityId, bool>(new List<Level1ChangeMessage>(), new SyncObject(), false, securityId, false);
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
		/// Получить исторические свечи.
		/// </summary>
		/// <param name="security">Инструмент, для которого необходимо получить свечи.</param>
		/// <param name="candleType">Тип свечи.</param>
		/// <param name="arg">Параметр свечки (например, тайм-фрейм).</param>
		/// <param name="count">Максимальное количество тиков.</param>
		/// <param name="isSuccess">Успешно ли получены все данные или процесс загрузки был прерван.</param>
		/// <returns>Исторические свечи.</returns>
		public IEnumerable<Candle> GetHistoricalCandles(Security security, Type candleType, object arg, long count, out bool isSuccess)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			//if (timeFrame <= TimeSpan.Zero)
			//	throw new ArgumentException("Тайм-фрейм должен быть больше 0.");

			var transactionId = TransactionIdGenerator.GetNextId();

			var series = new CandleSeries(candleType, security, arg);

			this.AddInfoLog(LocalizedStrings.Str2146Params, series, count);

			var info = new RefFive<List<Candle>, SyncObject, bool, CandleSeries, bool>(new List<Candle>(), new SyncObject(), false, series, false);
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
		/// Получить исторические свечи.
		/// </summary>
		/// <param name="security">Инструмент, для которого необходимо получить свечи.</param>
		/// <param name="candleType">Тип свечи.</param>
		/// <param name="arg">Параметр свечки (например, тайм-фрейм).</param>
		/// <param name="from">Дата начала периода.</param>
		/// <param name="to">Дата окончания периода.</param>
		/// <param name="isSuccess">Успешно ли получены все данные или процесс загрузки был прерван.</param>
		/// <returns>Исторические свечи.</returns>
		public IEnumerable<Candle> GetHistoricalCandles(Security security, Type candleType, object arg, DateTime from, DateTime to, out bool isSuccess)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			//if (timeFrame <= TimeSpan.Zero)
			//	throw new ArgumentException("Тайм-фрейм должен быть больше 0.");

			if (from > to)
				throw new ArgumentException(LocalizedStrings.Str2147);

			var id = TransactionIdGenerator.GetNextId();

			var series = new CandleSeries(candleType, security, arg);

			this.AddInfoLog(LocalizedStrings.Str2148Params, series, from, to);

			var info = new RefFive<List<Candle>, SyncObject, bool, CandleSeries, bool>(new List<Candle>(), new SyncObject(), false, series, false);
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

			throw new ArgumentOutOfRangeException("candleType", candleType, LocalizedStrings.WrongCandleType);
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType == typeof(TimeFrameCandle) && series.Arg is TimeSpan)
			{
				if (IQFeedMarketDataMessageAdapter.TimeFrames.Contains((TimeSpan)series.Arg))
					yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
			}
		}

		/// <summary>
		/// Событие появления новых свечек, полученных после подписки через <see cref="SubscribeCandles(StockSharp.Algo.Candles.CandleSeries,System.DateTimeOffset,System.DateTimeOffset)"/>.
		/// </summary>
		public event Action<CandleSeries, IEnumerable<Candle>> NewCandles;

		/// <summary>
		/// Событие окончания обработки серии.
		/// </summary>
		public event Action<CandleSeries> Stopped;

		/// <summary>
		/// Подписаться на получение свечек.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		public void SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			SubscribeCandles(series, from, to, TransactionIdGenerator.GetNextId());
		}

		/// <summary>
		/// Остановить подписку получения свечек, ранее созданную через <see cref="SubscribeCandles(StockSharp.Algo.Candles.CandleSeries,System.DateTimeOffset,System.DateTimeOffset)"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
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

					var candle = candleMsg.ToCandle(series);

					// сообщение с IsFinished = true не содержит данные по свече,
					// только флаг, что получение исторических данных завершено
					if (!candleMsg.IsFinished)
						NewCandles.SafeInvoke(series, new[] { candle });
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
							info.First.Add(candle);
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

					if (msg.Feed.Address == _adapter.LookupAddress)
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
