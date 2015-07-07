namespace StockSharp.BarChart
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;
	using StockSharp.Logging;
	using StockSharp.Messages;

	public class BarChartTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedDictionary<long, RefFive<List<Candle>, SyncObject, bool, CandleSeries, bool>> _candleInfo = new SynchronizedDictionary<long, RefFive<List<Candle>, SyncObject, bool, CandleSeries, bool>>();
		private readonly SynchronizedDictionary<long, RefFive<List<Trade>, SyncObject, bool, Security, bool>> _ticksInfo = new SynchronizedDictionary<long, RefFive<List<Trade>, SyncObject, bool, Security, bool>>();
		private readonly SynchronizedDictionary<long, CandleSeries> _candleSeries = new SynchronizedDictionary<long, CandleSeries>();

		private readonly BarChartMessageAdapter _adapter;

		/// <summary>
		/// Создать <see cref="BarChartTrader"/>.
		/// </summary>
		public BarChartTrader()
		{
			_adapter = new BarChartMessageAdapter(TransactionIdGenerator);
			_adapter.AddMarketDataSupport();

			Adapter.InnerAdapters.Add(_adapter.ToChannel(this));
		}

		/// <summary>
		/// Логин.
		/// </summary>
		public string Login
		{
			get { return _adapter.Login; }
			set { _adapter.Login = value; }
		}

		/// <summary>
		/// Пароль.
		/// </summary>
		public string Password
		{
			get { return _adapter.Password.To<string>(); }
			set { _adapter.Password = value.To<SecureString>(); }
		}

		/// <summary>
		/// Получить исторических тиков.
		/// </summary>
		/// <param name="security">Инструмент, для которого необходимо получить все сделки.</param>
		/// <param name="count">Максимальное количество тиков.</param>
		/// <param name="isSuccess">Успешно ли получены все данные или процесс загрузки был прерван.</param>
		/// <returns>Исторические сделки.</returns>
		public IEnumerable<Trade> GetHistoricalTicks(Security security, long count, out bool isSuccess)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			this.AddInfoLog(LocalizedStrings.Str2144Params, security, count);

			var transactionId = TransactionIdGenerator.GetNextId();

			var info = new RefFive<List<Trade>, SyncObject, bool, Security, bool>(new List<Trade>(), new SyncObject(), false, security, false);
			_ticksInfo.Add(transactionId, info);

			SendInMessage(new MarketDataMessage
			{
				SecurityId = security.ToSecurityId(),
				DataType = MarketDataTypes.Trades,
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
		/// <param name="security">Инструмент, для которого необходимо получить все сделки.</param>
		/// <param name="from">Дата начала периода.</param>
		/// <param name="to">Дата окончания периода.</param>
		/// <param name="isSuccess">Успешно ли получены все данные или процесс загрузки был прерван.</param>
		/// <returns>Исторические сделки.</returns>
		public IEnumerable<Trade> GetHistoricalTicks(Security security, DateTime from, DateTime to, out bool isSuccess)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			this.AddInfoLog(LocalizedStrings.Str2145Params, security, from, to);

			var transactionId = TransactionIdGenerator.GetNextId();

			var info = new RefFive<List<Trade>, SyncObject, bool, Security, bool>(new List<Trade>(), new SyncObject(), false, security, false);
			_ticksInfo.Add(transactionId, info);

			SendInMessage(new MarketDataMessage
			{
				SecurityId = security.ToSecurityId(),
				DataType = MarketDataTypes.Trades,
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
				DataType = GetCandleType(series.CandleType),
				TransactionId = transactionId,
				From = from,
				To = to,
				Arg = series.Arg,
				IsSubscribe = true,
			}.FillSecurityInfo(this, series.Security);

			_candleSeries.Add(transactionId, series);

			SendInMessage(mdMsg);
		}

		private static MarketDataTypes GetCandleType(Type candleType)
		{
			if (candleType == typeof(TimeFrameCandle))
				return MarketDataTypes.CandleTimeFrame;

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
				if (BarChartMessageAdapter.TimeFrames.Contains((TimeSpan)series.Arg))
					yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
			}
		}

		/// <summary>
		/// Событие появления новых свечек, полученных после подписки через <see cref="IExternalCandleSource.SubscribeCandles"/>.
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
		/// Остановить подписку получения свечек, ранее созданную через <see cref="IExternalCandleSource.SubscribeCandles"/>.
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

						var l1Info = _ticksInfo.TryGetValue(mdMsg.OriginalTransactionId);

						if (l1Info != null)
						{
							lock (l1Info.Second)
							{
								l1Info.Third = true;
								l1Info.Second.Pulse();
							}

							_ticksInfo.Remove(mdMsg.OriginalTransactionId);
						}
					}

					break;
				}

				case MessageTypes.CandleTimeFrame:
				{
					var candleMsg = (CandleMessage)message;

					var series = _candleSeries.TryGetValue(candleMsg.OriginalTransactionId);

					if (series == null)
						return;

					var candle = candleMsg.ToCandle(series);

					NewCandles.SafeInvoke(series, new[] { candle });

					if (candleMsg.IsFinished)
						Stopped.SafeInvoke(series);

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

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType == ExecutionTypes.Tick)
					{
						var info = _ticksInfo.TryGetValue(execMsg.OriginalTransactionId);

						// IsFinished = true message do not contains any data,
						// just mark historical data gathering as finished
						if (execMsg.GetValue<bool>("IsFinished"))
						{
							if (info == null)
							{
								this.AddWarningLog(LocalizedStrings.Str2149Params.Put(execMsg.OriginalTransactionId));
								return;
							}

							lock (info.Second)
							{
								info.Third = true;
								info.Fifth = true;
								info.Second.Pulse();
							}

							_ticksInfo.Remove(execMsg.OriginalTransactionId);
						}
						else
						{
							// streaming l1 message (non historical)
							if (info == null)
								break;

							info.First.Add(execMsg.ToTrade(info.Fourth));
						}

						// DO NOT send historical data to Connector
						return;
					}

					break;
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

			foreach (var pair in _ticksInfo.SyncGet(c => c.CopyAndClear()))
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