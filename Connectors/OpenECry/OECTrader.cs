namespace StockSharp.OpenECry
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/>, предоставляющая подключение к брокеру OEC.
	/// </summary>
	public sealed class OECTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedPairSet<long, CandleSeries> _series = new SynchronizedPairSet<long, CandleSeries>();
		private readonly OpenECryMessageAdapter _adapter;

		/// <summary>
		/// Создать <see cref="OECTrader"/>.
		/// </summary>
		public OECTrader()
		{
			_adapter = new OpenECryMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(_adapter.ToChannel(this));
		}

		/// <summary>
		/// Уникальный идентификатор программного обеспечения.
		/// </summary>
		public string Uuid
		{
			get { return _adapter.Uuid; }
			set { _adapter.Uuid = value; }
		}

		/// <summary>
		/// Имя пользователя OpenECry.
		/// </summary>
		public string Login
		{
			get { return _adapter.Login; }
			set { _adapter.Login = value; }
		}

		/// <summary>
		/// Пароль пользователя OpenECry.
		/// </summary>
		public string Password
		{
			get { return _adapter.Password.To<string>(); }
			set { _adapter.Password = value.To<SecureString>(); }
		}

		/// <summary>
		/// Требуемый режим подключения к терминалу. По умолчанию <see cref="OpenECryRemoting.None"/>.
		/// </summary>
		public OpenECryRemoting RemotingRequired
		{
			get { return _adapter.Remoting; }
			set { _adapter.Remoting = value; }
		}

		/// <summary>
		/// Использовать "родной" механизм восстановления соединения.
		/// По умолчанию включено.
		/// </summary>
		public bool UseNativeReconnect
		{
			get { return _adapter.UseNativeReconnect; }
			set { _adapter.UseNativeReconnect = value; }
		}

		/// <summary>
		/// Адрес API сервера OpenECry. По-умолчанию равен <see cref="OpenECryAddresses.Api"/>.
		/// </summary>
		public EndPoint Address
		{
			get { return _adapter.Address; }
			set { _adapter.Address = value; }
		}

		/// <summary>
		/// Использовать логирование библиотеки OEC.
		/// </summary>
		public bool EnableOECLogging
		{
			get { return _adapter.EnableOECLogging; }
			set { _adapter.EnableOECLogging = value; }
		}

		///// <summary>
		///// Проверить, установлено ли еще соединение. Проверяется только в том случае, если был вызван метод <see cref="IConnector.Connect"/>.
		///// </summary>
		///// <returns><see langword="true"/>, если соединение еще установлено, false, если торговая система разорвала подключение.</returns>
		//protected override bool IsConnectionAlive()
		//{
		//	return SessionHolder.Session != null && !SessionHolder.Session.CompleteConnected;
		//}

		/// <summary>
		/// Отправить сообщение другому пользователю.
		/// </summary>
		/// <param name="userName">Имя получателя.</param>
		/// <param name="text">Текст сообщения.</param>
		public void SendMessage(string userName, string text)
		{
			SendInMessage(new NewsMessage
			{
				Source = userName,
				Headline = text,
				ServerTime = CurrentTime
			});
		}

		IEnumerable<Range<DateTimeOffset>> IExternalCandleSource.GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType != typeof(TimeFrameCandle) || !(series.Arg is TimeSpan))
				yield break;

			var tf = (TimeSpan)series.Arg;

			if (OpenECryMessageAdapter.TimeFrames.Contains(tf))
				yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
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
			if (series == null)
				throw new ArgumentNullException("series");

			if (series.CandleType != typeof(TimeFrameCandle))
				throw new ArgumentException(LocalizedStrings.NotSupportCandle.Put("OpenECry", series.CandleType), "series");

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
		/// Остановить подписку получения свечек, ранее созданную через <see cref="IExternalCandleSource.SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
			var originalTransactionId = _series.TryGetKey(series);

			if (originalTransactionId == 0)
				return;

			SendInMessage(new MarketDataMessage
			{
				OriginalTransactionId = originalTransactionId,
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.CandleTimeFrame,
				//SecurityId = GetSecurityId(series.Security),
				Arg = series.Arg,
				IsSubscribe = false,
			}.FillSecurityInfo(this, series.Security));
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnProcessMessage(Message message)
		{
			var candleMsg = message as CandleMessage;

			if (candleMsg == null)
			{
				base.OnProcessMessage(message);
				return;
			}

			var series = _series.TryGetValue(candleMsg.OriginalTransactionId);

			if (series == null)
				return;

			var candle = candleMsg.ToCandle(series);
			NewCandles.SafeInvoke(series, new[] { candle });

			if (candleMsg.IsFinished)
				Stopped.SafeInvoke(series);
		}
	}
}