namespace StockSharp.Oanda
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
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с брокером OANDA.
	/// </summary>
	public class OandaTrader : Connector, IExternalCandleSource
    {
		private readonly SynchronizedDictionary<long, CandleSeries> _series = new SynchronizedDictionary<long, CandleSeries>();
		private readonly OandaMessageAdapter _adapter;

		/// <summary>
		/// Создать <see cref="OandaTrader"/>.
		/// </summary>
		public OandaTrader()
		{
			_adapter = new OandaMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(_adapter.ToChannel(this));
		}

		/// <summary>
		/// Сервер.
		/// </summary>
		public OandaServers Server
		{
			get { return _adapter.Server; }
			set { _adapter.Server = value; }
		}

		/// <summary>
		/// Токен.
		/// </summary>
		public SecureString Token
		{
			get { return _adapter.Token; }
			set { _adapter.Token = value; }
		}

		IEnumerable<Range<DateTimeOffset>> IExternalCandleSource.GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType != typeof(TimeFrameCandle) || !(series.Arg is TimeSpan))
				yield break;

			var tf = (TimeSpan)series.Arg;

			if (OandaMessageAdapter.TimeFrames.Contains(tf))
				yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
		}

		/// <summary>
		/// Событие появления новых свечек, полученных после подписки через <see cref="SubscribeCandles"/>.
		/// </summary>
		public event Action<CandleSeries, IEnumerable<Candle>> NewCandles;

		/// <summary>
		/// Подписаться на получение свечек.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		public void SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			if (series.CandleType != typeof(TimeFrameCandle))
				throw new ArgumentException(LocalizedStrings.NotSupportCandle.Put("OANDA", series.CandleType), "series");

			if (!(series.Arg is TimeSpan))
				throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), "series");

			var transactionId = TransactionIdGenerator.GetNextId();

			_series.Add(transactionId, series);

			SendInMessage(new MarketDataMessage
			{
				TransactionId = transactionId,
				DataType = MarketDataTypes.CandleTimeFrame,
				//SecurityId = GetSecurityId(series.Security),
				Arg = series.Arg,
				IsSubscribe = true,
				From = from,
				To = to,
			}.FillSecurityInfo(this, series.Security));
		}

		/// <summary>
		/// Остановить подписку получения свечек, ранее созданную через <see cref="SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
		}

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		/// <param name="direction">Направление сообщения.</param>
		protected override void OnProcessMessage(Message message, MessageDirections direction)
		{
			var candleMsg = message as CandleMessage;

			if (candleMsg == null)
			{
				base.OnProcessMessage(message, direction);
				return;
			}

			var series = _series.TryGetValue(candleMsg.OriginalTransactionId);

			if (series == null)
				return;

			var candle = candleMsg.ToCandle(series);
			NewCandles.SafeInvoke(series, new[] { candle });
		}
    }
}