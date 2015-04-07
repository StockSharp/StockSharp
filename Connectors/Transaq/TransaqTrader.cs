namespace StockSharp.Transaq
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.ComponentModel;

	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/>, предоставляющая подключение к Transaq через TXmlConnector.
	/// </summary>
	public class TransaqTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedDictionary<long, CandleSeries> _candleSeries = new SynchronizedDictionary<long, CandleSeries>();
		private readonly TransaqMessageAdapter _adapter;

		/// <summary>
		/// Создать <see cref="TransaqTrader"/>.
		/// </summary>
		public TransaqTrader()
		{
			var sessionHolder = new TransaqSessionHolder(TransactionIdGenerator);

			base.SessionHolder = sessionHolder;

			_adapter = new TransaqMessageAdapter(sessionHolder);

			TransactionAdapter = _adapter;
			MarketDataAdapter = _adapter;

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}

		private new TransaqSessionHolder SessionHolder
		{
			get { return (TransaqSessionHolder)base.SessionHolder; }
		}

		/// <summary>
		/// Пароль.
		/// </summary>
		public string Password
		{
			get { return SessionHolder.Password.To<string>(); }
			set { SessionHolder.Password = value.To<SecureString>(); }
		}

		/// <summary>
		/// Логин.
		/// </summary>
		public string Login
		{
			get { return SessionHolder.Login; }
			set { SessionHolder.Login = value; }
		}

		/// <summary>
		/// Адрес сервера.
		/// </summary>
		public EndPoint Address
		{
			get { return SessionHolder.Address; }
			set { SessionHolder.Address = value; }
		}

		/// <summary>
		/// Прокси.
		/// </summary>
		public Proxy Proxy
		{
			get { return SessionHolder.Proxy; }
			set { SessionHolder.Proxy = value; }
		}

		/// <summary>
		/// Уровень логирования коннектора. По умолчанию <see cref="ApiLogLevels.Standard"/>.
		/// </summary>
		public ApiLogLevels ApiLogLevel
		{
			get { return SessionHolder.ApiLogLevel; }
			set { SessionHolder.ApiLogLevel = value; }
		}

		/// <summary>
		/// Полный путь к dll файлу, содержащее Transaq API. По-умолчанию равно txmlconnector.dll.
		/// </summary>
		public string DllPath
		{
			get { return SessionHolder.DllPath; }
			set { SessionHolder.DllPath = value; }
		}

		/// <summary>
		/// Передавать ли данные для фондового рынка.
		/// </summary>
		public bool MicexRegisters
		{
			get { return SessionHolder.MicexRegisters; }
			set { SessionHolder.MicexRegisters = value; }
		}

		/// <summary>
		/// Подключаться ли к HFT серверу Финам.
		/// </summary>
		public bool IsHFT
		{
			get { return SessionHolder.IsHFT; }
			set { SessionHolder.IsHFT = value; }
		}

		/// <summary>
		/// Период агрегирования данных на сервере Transaq.
		/// </summary>
		public TimeSpan? MarketDataInterval
		{
			get { return SessionHolder.MarketDataInterval; }
			set { SessionHolder.MarketDataInterval = value; }
		}

		/// <summary>
		/// Версия коннектора.
		/// </summary>
		public string ConnectorVersion
		{
			get { return SessionHolder.ConnectorVersion; }
		}

		/// <summary>
		/// Текущий сервер.
		/// </summary>
		public int CurrentServer
		{
			get { return SessionHolder.CurrentServer; }
		}

		/// <summary>
		/// Разница между локальным и серверным временем.
		/// </summary>
		public TimeSpan? ServerTimeDiff
		{
			get { return SessionHolder.ServerTimeDiff; }
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		IEnumerable<Range<DateTimeOffset>> IExternalCandleSource.GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType == typeof(TimeFrameCandle) && series.Arg is TimeSpan && _adapter.CandleTimeFrames.Contains((TimeSpan)series.Arg))
			{
				yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
			}
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

			if (_candleSeries.Values.Contains(series))
			{
				MarketDataAdapter.SendOutMessage(new ErrorMessage
				{
					Error = new InvalidOperationException(LocalizedStrings.Str3568Params.Put(series))
				});
				return;
			}

			var transactionId = TransactionIdGenerator.GetNextId();

			_candleSeries[transactionId] = series;

			MarketDataAdapter.SendInMessage(new MarketDataMessage
			{
				From = from,
				To = to,
				//SecurityId = GetSecurityId(series.Security),
				DataType = MarketDataTypes.CandleTimeFrame,
				Arg = (TimeSpan)series.Arg,
				TransactionId = transactionId,
				IsSubscribe = true,
				Count = (int)series.Security.GetTimeFrameCount(new Range<DateTimeOffset>(from, to == DateTimeOffset.MaxValue ? DateTimeOffset.Now : to), (TimeSpan)series.Arg)
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

			var series = _candleSeries.TryGetValue(candleMsg.OriginalTransactionId);

			if (series == null)
				return;

			var candle = candleMsg.ToCandle(series);
			NewCandles.SafeInvoke(series, new[] { candle });
		}

		/// <summary>
		/// Сменить пароль.
		/// </summary>
		/// <param name="newPassword">Новый пароль.</param>
		public void ChangePassword(string newPassword)
		{
			var msg = new ChangePasswordMessage
			{
				NewPassword = newPassword.To<SecureString>(),
				TransactionId = TransactionIdGenerator.GetNextId()
			};

			_adapter.SendInMessage(msg);
		}
	}
}