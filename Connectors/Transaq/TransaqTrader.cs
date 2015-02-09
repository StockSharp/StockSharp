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
		private readonly TransaqMessageAdapter _marketDataAdapter;

		/// <summary>
		/// Создать <see cref="TransaqTrader"/>.
		/// </summary>
		public TransaqTrader()
		{
			base.SessionHolder = new TransaqSessionHolder(TransactionIdGenerator);

			_marketDataAdapter = MarketDataAdapter.To<TransaqMessageAdapter>();

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
		/// Сменить пароль для подключения к серверу. Максимальная длинна 19 символов.
		/// </summary>
		/// <param name="currPass">Текущий пароль.</param>
		/// <param name="newPass">Новый пароль.</param>
		/// <param name="handler">Обработчик результата.</param>
		public void ChangePassword(string currPass, string newPass, Action<bool, string> handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			// TODO
			//var command = new ChangePassMessage
			//{
			//	NewPass = newPass,
			//	OldPass = currPass
			//};

			//SendCommand(command, result =>
			//{
			//	var text = result.Text;

			//	if (result.IsSuccess)
			//		this.AddInfoLog(text);
			//	else
			//		RaiseProcessDataError(new InvalidOperationException(text));

			//	handler(result.IsSuccess, text);
			//});
		}

		#region News

		/// <summary>
		/// Начать получать новости.
		/// </summary>
		protected override void OnRegisterNews()
		{
			GetNewsHeader(TransaqMessageAdapter.MaxNewsHeaderCount);
		}

		/// <summary>
		/// Запросить заголовки старых новостей.
		/// </summary>
		/// <param name="count">Количество заголовков новостей.</param>
		private void GetNewsHeader(int count)
		{
			MarketDataAdapter.SendInMessage(new MarketDataMessage
			{
				TransactionId = TransactionIdGenerator.GetNextId(),
				DataType = MarketDataTypes.News,
				Count = count,
				IsSubscribe = true
			});
		}

		#endregion

		/// <summary>
		/// Вызвать событие <see cref="NewCandles"/>
		/// </summary>
		/// <param name="candleSeries">Серия свечек.</param>
		/// <param name="candles">Свечи.</param>
		protected virtual void RaiseNewCandles(CandleSeries candleSeries, IEnumerable<TimeFrameCandle> candles)
		{
			NewCandles.SafeInvoke(candleSeries, candles);
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		IEnumerable<Range<DateTimeOffset>> IExternalCandleSource.GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType == typeof(TimeFrameCandle) && series.Arg is TimeSpan && _marketDataAdapter.CandleTimeFrames.Contains((TimeSpan)series.Arg))
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

			var series = _candleSeries.TryGetValue(candleMsg.OriginalTransactionId);

			if (series == null)
				return;

			var candle = candleMsg.ToCandle(series);
			NewCandles.SafeInvoke(series, new[] { candle });
		}

		/// <summary>
		/// Сменить пароль.
		/// </summary>
		/// <param name="adapterType">Тип адаптера, в который необходимо отправить сообщение о смене пароля.</param>
		/// <param name="newPassword">Новый пароль.</param>
		public void ChangePassword(MessageAdapterTypes adapterType, string newPassword)
		{
			var msg = new ChangePasswordMessage
			{
				NewPassword = newPassword.To<SecureString>(),
				TransactionId = TransactionIdGenerator.GetNextId()
			};

			switch (adapterType)
			{
				case MessageAdapterTypes.Transaction:
					TransactionAdapter.SendInMessage(msg);
					break;
				case MessageAdapterTypes.MarketData:
					MarketDataAdapter.SendInMessage(msg);
					break;
				default:
					throw new ArgumentOutOfRangeException("adapterType");
			}
		}
	}
}