namespace StockSharp.LMAX
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
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с биржей LMAX.
	/// </summary>
	public class LmaxTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedDictionary<long, CandleSeries> _series = new SynchronizedDictionary<long, CandleSeries>();
		private readonly LmaxMessageAdapter _adapter;

		/// <summary>
		/// Создать <see cref="LmaxTrader"/>.
		/// </summary>
		public LmaxTrader()
		{
			_adapter = new LmaxMessageAdapter(TransactionIdGenerator);

			TransactionAdapter = MarketDataAdapter = _adapter;

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}

		/// <summary>
		/// Поддерживается ли перерегистрация заявок через метод <see cref="IConnector.ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/>
		/// в виде одной транзакции. По-умолчанию включено.
		/// </summary>
		public override bool IsSupportAtomicReRegister
		{
			get { return false; }
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
		/// Подключаться ли к демо торгам вместо сервера с реальной торговлей.
		/// </summary>
		public bool IsDemo
		{
			get { return _adapter.IsDemo; }
			set { _adapter.IsDemo = value; }
		}

		/// <summary>
		/// Загружать ли инструменты из архива с сайта LMAX. По-умолчанию выключено.
		/// </summary>
		public bool IsDownloadSecurityFromSite
		{
			get { return _adapter.IsDownloadSecurityFromSite; }
			set { _adapter.IsDownloadSecurityFromSite = value; }
		}

		IEnumerable<Range<DateTimeOffset>> IExternalCandleSource.GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType != typeof(TimeFrameCandle) || !(series.Arg is TimeSpan))
				yield break;

			var tf = (TimeSpan)series.Arg;

			if (LmaxMessageAdapter.TimeFrames.Contains(tf))
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
				throw new ArgumentException(LocalizedStrings.NotSupportCandle.Put("LMAX", series.CandleType), "series");

			if (!(series.Arg is TimeSpan))
				throw new ArgumentException(LocalizedStrings.WrongCandleArg.Put(series.Arg), "series");

			var transactionId = TransactionIdGenerator.GetNextId();

			_series.Add(transactionId, series);

			MarketDataAdapter.SendInMessage(new MarketDataMessage
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
		/// <param name="adapter">Адаптер, от которого пришло сообщение.</param>
		/// <param name="direction">Направление сообщения.</param>
		protected override void OnProcessMessage(Message message, IMessageAdapter adapter, MessageDirections direction)
		{
			var candleMsg = message as CandleMessage;

			if (candleMsg == null)
			{
				base.OnProcessMessage(message, adapter, direction);
				return;
			}

			var series = _series.TryGetValue(candleMsg.OriginalTransactionId);

			if (series == null)
				return;

			var candle = candleMsg.ToCandle(series);

			// тиковый ТФ
			if (candleMsg.TotalVolume == 0)
			{
				candle.TotalVolume = (candle.OpenVolume + candle.CloseVolume) ?? 0;
				candle.HighPrice = candle.OpenPrice.Max(candle.ClosePrice);
				candle.LowPrice = candle.OpenPrice.Min(candle.ClosePrice);
			}

			NewCandles.SafeInvoke(series, new[] { candle });
		}
	}
}