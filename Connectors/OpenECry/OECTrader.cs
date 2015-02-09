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
		
		/// <summary>
		/// Создать <see cref="OECTrader"/>.
		/// </summary>
		public OECTrader()
		{
			base.SessionHolder = new OpenECrySessionHolder(TransactionIdGenerator);

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}

		private new OpenECrySessionHolder SessionHolder
		{
			get { return (OpenECrySessionHolder)base.SessionHolder; }
		}

		/// <summary>
		/// Уникальный идентификатор программного обеспечения.
		/// </summary>
		public string Uuid
		{
			get { return SessionHolder.Uuid; }
			set { SessionHolder.Uuid = value; }
		}

		/// <summary>
		/// Имя пользователя OpenECry.
		/// </summary>
		public string Login
		{
			get { return SessionHolder.Login; }
			set { SessionHolder.Login = value; }
		}

		/// <summary>
		/// Пароль пользователя OpenECry.
		/// </summary>
		public string Password
		{
			get { return SessionHolder.Password.To<string>(); }
			set { SessionHolder.Password = value.To<SecureString>(); }
		}

		/// <summary>
		/// Требуемый режим подключения к терминалу. По умолчанию <see cref="OpenECryRemoting.None"/>.
		/// </summary>
		public OpenECryRemoting RemotingRequired
		{
			get { return SessionHolder.Remoting; }
			set { SessionHolder.Remoting = value; }
		}

		/// <summary>
		/// Использовать "родной" механизм восстановления соединения.
		/// По умолчанию включено.
		/// </summary>
		public bool UseNativeReconnect
		{
			get { return SessionHolder.UseNativeReconnect; }
			set { SessionHolder.UseNativeReconnect = value; }
		}

		/// <summary>
		/// Адрес API сервера OpenECry. По-умолчанию равен <see cref="OpenECryAddresses.Api"/>.
		/// </summary>
		public EndPoint Address
		{
			get { return SessionHolder.Address; }
			set { SessionHolder.Address = value; }
		}

		/// <summary>
		/// Использовать логирование библиотеки OEC.
		/// </summary>
		public bool EnableOECLogging
		{
			get { return SessionHolder.EnableOECLogging; }
			set { SessionHolder.EnableOECLogging = value; }
		}

		/// <summary>
		/// Проверить, установлено ли еще соединение. Проверяется только в том случае, если был вызван метод <see cref="IConnector.Connect"/>.
		/// </summary>
		/// <returns><see langword="true"/>, если соединение еще установлено, false, если торговая система разорвала подключение.</returns>
		protected override bool IsConnectionAlive()
		{
			return SessionHolder.Session != null && !SessionHolder.Session.CompleteConnected;
		}

		/// <summary>
		/// Отправить сообщение другому пользователю.
		/// </summary>
		/// <param name="userName">Имя получателя.</param>
		/// <param name="text">Текст сообщения.</param>
		public void SendMessage(string userName, string text)
		{
			MarketDataAdapter.SendInMessage(new NewsMessage
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

			if (OpenECrySessionHolder.TimeFrames.Contains(tf))
				yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
		}

		/// <summary>
		/// Событие появления новых свечек, полученных после подписки через <see cref="IExternalCandleSource.SubscribeCandles"/>.
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
				throw new ArgumentException(LocalizedStrings.NotSupportCandle.Put("OpenECry", series.CandleType), "series");

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
		/// Остановить подписку получения свечек, ранее созданную через <see cref="IExternalCandleSource.SubscribeCandles"/>.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		public void UnSubscribeCandles(CandleSeries series)
		{
			var originalTransactionId = _series.TryGetKey(series);

			if (originalTransactionId == 0)
				return;

			MarketDataAdapter.SendInMessage(new MarketDataMessage
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
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		/// <param name="adapterType">Тип адаптера, от которого пришло сообщение.</param>
		/// <param name="direction">Направление сообщения.</param>
		protected override void OnProcessMessage(Message message, MessageAdapterTypes adapterType, MessageDirections direction)
		{
			var candleMsg = message as CandleMessage;

			if (candleMsg == null)
			{
				base.OnProcessMessage(message, adapterType, direction);
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