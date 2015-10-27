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

	/// <summary>
	/// The interface <see cref="IConnector"/> implementation which provides a connection to the BarChart.
	/// </summary>
	[Icon("BarChart_logo.png")]
	public class BarChartTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedDictionary<long, RefFive<List<CandleMessage>, SyncObject, bool, CandleSeries, bool>> _candleInfo = new SynchronizedDictionary<long, RefFive<List<CandleMessage>, SyncObject, bool, CandleSeries, bool>>();
		private readonly SynchronizedDictionary<long, RefFive<List<ExecutionMessage>, SyncObject, bool, Security, bool>> _ticksInfo = new SynchronizedDictionary<long, RefFive<List<ExecutionMessage>, SyncObject, bool, Security, bool>>();
		private readonly SynchronizedDictionary<long, CandleSeries> _candleSeries = new SynchronizedDictionary<long, CandleSeries>();

		/// <summary>
		/// Initializes a new instance of the <see cref="BarChartTrader"/>.
		/// </summary>
		public BarChartTrader()
		{
			var adapter = new BarChartMessageAdapter(TransactionIdGenerator);
			adapter.AddMarketDataSupport();

			Adapter.InnerAdapters.Add(adapter);
		}

		private BarChartMessageAdapter NativeAdapter
		{
			get { return Adapter.InnerAdapters.OfType<BarChartMessageAdapter>().First(); }
		}

		/// <summary>
		/// Login.
		/// </summary>
		public string Login
		{
			get { return NativeAdapter.Login; }
			set { NativeAdapter.Login = value; }
		}

		/// <summary>
		/// Password.
		/// </summary>
		public string Password
		{
			get { return NativeAdapter.Password.To<string>(); }
			set { NativeAdapter.Password = value.To<SecureString>(); }
		}

		/// <summary>
		/// To get historical ticks.
		/// </summary>
		/// <param name="security">The instrument for which you need to get all trades.</param>
		/// <param name="count">Maximum ticks count.</param>
		/// <param name="isSuccess">Whether all data were obtained successfully or the download process has been interrupted.</param>
		/// <returns>Historical ticks.</returns>
		public IEnumerable<ExecutionMessage> GetHistoricalTicks(Security security, long count, out bool isSuccess)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			this.AddInfoLog(LocalizedStrings.Str2144Params, security, count);

			var transactionId = TransactionIdGenerator.GetNextId();

			var info = RefTuple.Create(new List<ExecutionMessage>(), new SyncObject(), false, security, false);
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
		/// To get historical ticks.
		/// </summary>
		/// <param name="security">The instrument for which you need to get all trades.</param>
		/// <param name="from">Begin period.</param>
		/// <param name="to">End period.</param>
		/// <param name="isSuccess">Whether all data were obtained successfully or the download process has been interrupted.</param>
		/// <returns>Historical ticks.</returns>
		public IEnumerable<ExecutionMessage> GetHistoricalTicks(Security security, DateTime from, DateTime to, out bool isSuccess)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			this.AddInfoLog(LocalizedStrings.Str2145Params, security, from, to);

			var transactionId = TransactionIdGenerator.GetNextId();

			var info = RefTuple.Create(new List<ExecutionMessage>(), new SyncObject(), false, security, false);
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
				throw new ArgumentNullException("security");

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
		public IEnumerable<CandleMessage> GetHistoricalCandles(Security security, Type candleType, object arg, DateTime from, DateTime to, out bool isSuccess)
		{
			if (security == null)
				throw new ArgumentNullException("security");

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
		/// To get time ranges for which this source of passed candles series has data.
		/// </summary>
		/// <param name="series">Candles series.</param>
		/// <returns>Time ranges.</returns>
		public IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType == typeof(TimeFrameCandle) && series.Arg is TimeSpan)
			{
				if (BarChartMessageAdapter.TimeFrames.Contains((TimeSpan)series.Arg))
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
							info.First.Add(candleMsg);
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

							info.First.Add(execMsg);
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