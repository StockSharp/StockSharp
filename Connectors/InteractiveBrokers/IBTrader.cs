namespace StockSharp.InteractiveBrokers
{
	using System;
	using System.Collections.Generic;
	using System.Net;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.Algo.Candles;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	static class ExtendedMarketDataTypes
	{
		public const MarketDataTypes Scanner = (MarketDataTypes)(-1);
		public const MarketDataTypes FundamentalReport = (MarketDataTypes)(-2);
		public const MarketDataTypes OptionCalc = (MarketDataTypes)(-3);
	}

	static class ExtendedMessageTypes
	{
		public const MessageTypes Scanner = (MessageTypes)(-1);
		public const MessageTypes FundamentalReport = (MessageTypes)(-2);
		public const MessageTypes ScannerParameters = (MessageTypes)(-3);
		public const MessageTypes FinancialAdvise = (MessageTypes)(-4);
	}

	/// <summary>
	/// Сообщение запуска сканера инструментов на основе заданных параметров.
	/// Результаты будут приходить через событие <see cref="IBTrader.NewScannerResults"/>.
	/// </summary>
	public class ScannerMarketDataMessage : MarketDataMessage
	{
		/// <summary>
		/// Фильтр.
		/// </summary>
		public ScannerFilter Filter { get; private set; }

		/// <summary>
		/// Создать <see cref="ScannerMarketDataMessage"/>.
		/// </summary>
		/// <param name="filter">Фильтр.</param>
		public ScannerMarketDataMessage(ScannerFilter filter)
		{
			if (filter == null)
				throw new ArgumentNullException("filter");

			Filter = filter;
			DataType = ExtendedMarketDataTypes.Scanner;
		}
	}

	/// <summary>
	/// Сообщение с результатами сканера, запущенного сообщением <see cref="ScannerMarketDataMessage"/>.
	/// </summary>
	public class ScannerResultMessage : Message
	{
		/// <summary>
		/// Создать <see cref="ScannerResultMessage"/>.
		/// </summary>
		public ScannerResultMessage()
			: base(ExtendedMessageTypes.Scanner)
		{
		}

		/// <summary>
		/// Результаты.
		/// </summary>
		public IEnumerable<ScannerResult> Results { get; set; }

		/// <summary>
		/// Идентификатор запроса <see cref="ScannerMarketDataMessage"/>.
		/// </summary>
		public long OriginalTransactionId { get; set; }
	}

	/// <summary>
	/// Сообщение с параметрами сканера.
	/// </summary>
	public class ScannerParametersMessage : Message
	{
		/// <summary>
		/// Создать <see cref="ScannerParametersMessage"/>.
		/// </summary>
		public ScannerParametersMessage()
			: base(ExtendedMessageTypes.ScannerParameters)
		{
		}

		/// <summary>
		/// Параметры в формате xml.
		/// </summary>
		public string Parameters { get; set; }
	}

	/// <summary>
	/// Сообщение с финансовой консультацией.
	/// </summary>
	public class FinancialAdviseMessage : Message
	{
		/// <summary>
		/// Создать <see cref="FinancialAdviseMessage"/>.
		/// </summary>
		public FinancialAdviseMessage()
			: base(ExtendedMessageTypes.FinancialAdvise)
		{
		}

		/// <summary>
		/// Тип.
		/// </summary>
		public int AdviseType { get; set; }

		/// <summary>
		/// Данные в формате xml.
		/// </summary>
		public string Data { get; set; }
	}

	/// <summary>
	/// Сообщение на получение отчетов по рынку для заданного инструмента.
	/// Результаты будут приходить через событие <see cref="IBTrader.NewFundamentalReport"/>.
	/// </summary>
	public class FundamentalReportMarketDataMessage : MarketDataMessage
	{
		/// <summary>
		/// Тип отчета.
		/// </summary>
		public FundamentalReports Report { get; private set; }

		/// <summary>
		/// Создать <see cref="FundamentalReportMarketDataMessage"/>.
		/// </summary>
		/// <param name="report">Тип отчета.</param>
		public FundamentalReportMarketDataMessage(FundamentalReports report)
		{
			Report = report;
			DataType = ExtendedMarketDataTypes.FundamentalReport;
		}
	}

	/// <summary>
	/// Сообщение с отчетом по рынку, инициированного сообщением <see cref="FundamentalReportMarketDataMessage"/>.
	/// </summary>
	public class FundamentalReportMessage : Message
	{
		/// <summary>
		/// Создать <see cref="FundamentalReportMessage"/>.
		/// </summary>
		public FundamentalReportMessage()
			: base(ExtendedMessageTypes.FundamentalReport)
		{
		}

		/// <summary>
		/// Текст отчета.
		/// </summary>
		public string Data { get; set; }

		/// <summary>
		/// Идентификатор запроса <see cref="FundamentalReportMarketDataMessage"/>.
		/// </summary>
		public long OriginalTransactionId { get; set; }
	}

	/// <summary>
	/// Сообщение подписки на получение расчетных значений опциона.
	/// </summary>
	public class OptionCalcMarketDataMessage : MarketDataMessage
	{
		/// <summary>
		/// Создать <see cref="OptionCalcMarketDataMessage"/>.
		/// </summary>
		/// <param name="impliedVolatility">Подразумеваемая волатильность.</param>
		/// <param name="optionPrice">Цена опциона.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		public OptionCalcMarketDataMessage(decimal impliedVolatility, decimal optionPrice, decimal assetPrice)
		{
			AssetPrice = assetPrice;
			OptionPrice = optionPrice;
			ImpliedVolatility = impliedVolatility;

			DataType = ExtendedMarketDataTypes.OptionCalc;
		}

		/// <summary>
		/// Подразумеваемая волатильность.
		/// </summary>
		public decimal ImpliedVolatility { get; private set; }

		/// <summary>
		/// Цена опциона.
		/// </summary>
		public decimal OptionPrice { get; private set; }

		/// <summary>
		/// Цена базового актива.
		/// </summary>
		public decimal AssetPrice { get; private set; }
	}

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/>, предоставляющая подключение к Interactive Brokers через IB Gateway.
	/// </summary>
	public class IBTrader : Connector, IExternalCandleSource
	{
		private readonly SynchronizedDictionary<long, CandleSeries> _candleSeries = new SynchronizedDictionary<long, CandleSeries>();
		private readonly SynchronizedDictionary<long, object> _states = new SynchronizedDictionary<long, object>(); 

		/// <summary>
		/// Создать <see cref="IBTrader"/>.
		/// </summary>
		public IBTrader()
		{
			base.SessionHolder = new InteractiveBrokersSessionHolder(TransactionIdGenerator);

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}

		private new InteractiveBrokersSessionHolder SessionHolder
		{
			get { return (InteractiveBrokersSessionHolder)base.SessionHolder; }
		}

		/// <summary>
		/// Адрес.
		/// </summary>
		public EndPoint Address
		{
			get { return SessionHolder.Address; }
			set { SessionHolder.Address = value; }
		}

		///// <summary>
		///// Проверить, установлено ли еще соединение. Проверяется только в том случае, если был вызван метод <see cref="IConnector.Connect"/>.
		///// </summary>
		///// <returns><see langword="true"/>, если соединение еще установлено, false, если торговая система разорвала подключение.</returns>
		//protected override bool IsConnectionAlive()
		//{
		//	return _socket.IsConnected;
		//}

		///// <summary>
		///// Время подключения.
		///// </summary>
		//public DateTimeOffset ConnectedTime
		//{
		//	get { return SessionHolder.CurrentTime; }
		//}

		/// <summary>
		/// Уникальный идентификатор. Используется в случае подключения нескольких клиентов к одному терминалу или gateway.
		/// </summary>
		public int ClientId
		{
			get { return SessionHolder.ClientId; }
			set { SessionHolder.ClientId = value; }
		}

		/// <summary>
		/// Уровень логирования сообщений сервера. По-умолчанию равен <see cref="ServerLogLevels.Detail"/>.
		/// </summary>
		public ServerLogLevels ServerLogLevel
		{
			get { return SessionHolder.ServerLogLevel; }
			set { SessionHolder.ServerLogLevel = value; }
		}

		/// <summary>
		/// Использовать ли данные реального времени или "замороженные" на сервере брокера. По-умолчанию используются "замороженные" данные.
		/// </summary>
		public bool IsRealTimeMarketData
		{
			get { return SessionHolder.IsRealTimeMarketData; }
			set { SessionHolder.IsRealTimeMarketData = value; }
		}

		/// <summary>
		/// Событие появление новых результатов сканера, запущенного ранее через <see cref="SubscribeScanner"/>.
		/// </summary>
		public event Action<ScannerFilter, IEnumerable<ScannerResult>> NewScannerResults;
		
		///// <summary>
		///// Событие появления новой свечи реального времени, полученной по подписке через <see cref="SubscribeCandles"/>.
		///// </summary>
		//public event Action<TimeFrameCandle> NewRealTimeCandle;

		/// <summary>
		/// Событие появления новых свечек, полученных после подписки через <see cref="SubscribeCandles"/>.
		/// </summary>
		public event Action<CandleSeries, IEnumerable<Candle>> NewCandles;

		/// <summary>
		/// Событие появление нового отчета, полученного по подписке <see cref="SubscribeFundamentalReport"/>.
		/// </summary>
		public event Action<Security, FundamentalReports, string> NewFundamentalReport;

		/// <summary>
		/// Событие о появлении новых параметров сканера, которые применяются через <see cref="ScannerFilter"/>. Параметры передаются ввиде xml.
		/// </summary>
		public event Action<string> NewScannerParameters;

		/// <summary>
		/// Событие о появлении новых финансовых консультаций. Параметры передаются ввиде xml.
		/// </summary>
		public event Action<int, string> NewFinancialAdvise;

		///// <summary>
		///// Событие о появлении отчета о комиссии по сделке.
		///// </summary>
		//public event Action<IBCommission> NewCommission;

		/// <summary>
		/// Отменить группу заявок на бирже по фильтру.
		/// </summary>
		/// <param name="transactionId">Идентификатор транзакции отмены.</param>
		/// <param name="isStopOrder"><see langword="true"/>, если нужно отменить только стоп-заявки, false - если только обычный и null - если оба типа.</param>
		/// <param name="portfolio">Портфель. Если значение равно null, то портфель не попадает в фильтр снятия заявок.</param>
		/// <param name="direction">Направление заявки. Если значение равно null, то направление не попадает в фильтр снятия заявок.</param>
		/// <param name="board">Торговая площадка. Если значение равно null, то площадка не попадает в фильтр снятия заявок.</param>
		/// <param name="security">Инструмент. Если значение равно null, то инструмент не попадает в фильтр снятия заявок.</param>
		protected override void OnCancelOrders(long transactionId, bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			if (isStopOrder == null && portfolio == null && direction == null && board == null && security == null)
				base.OnCancelOrders(transactionId);
			else
				this.CancelOrders(Orders, isStopOrder, portfolio, direction, board, security);
		}

		/// <summary>
		/// Запустить или остановить сканер инструментов на основе заданных параметров.
		/// Результаты будут приходить через событие <see cref="IBTrader.NewScannerResults"/>.
		/// </summary>
		/// <param name="filter">Фильтр.</param>
		/// <param name="isSubscribe"><see langword="true"/> если необходимо подписаться, иначе <see langword="false"/>.</param>
		public void SubscribeScanner(ScannerFilter filter, bool isSubscribe)
		{
			var transactionId = TransactionIdGenerator.GetNextId();

			_states.Add(transactionId, filter);

			MarketDataAdapter.SendInMessage(new ScannerMarketDataMessage(filter)
			{
				TransactionId = transactionId,
				IsSubscribe = isSubscribe
			});
		}

		/// <summary>
		/// Подписаться или отписаться на получение отчетов по рынку для заданного инструмента.
		/// Результаты будут приходить через событие <see cref="NewFundamentalReport"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="report">Тип отчета.</param>
		/// <param name="isSubscribe"><see langword="true"/> если необходимо подписаться, иначе <see langword="false"/>.</param>
		public void SubscribeFundamentalReport(Security security, FundamentalReports report, bool isSubscribe)
		{
			var transactionId = TransactionIdGenerator.GetNextId();

			_states.Add(transactionId, Tuple.Create(security, report));

			MarketDataAdapter.SendInMessage(new FundamentalReportMarketDataMessage(report)
			{
				//SecurityId = GetSecurityId(security),
				TransactionId = transactionId,
				IsSubscribe = isSubscribe
			}.FillSecurityInfo(this, security));
		}

		/// <summary>
		/// Подписаться или отписаться на получение расчетных значений опциона.
		/// Результаты будут приходить через событие <see cref="IConnector.SecuritiesChanged"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="impliedVolatility">Подразумеваемая волатильность.</param>
		/// <param name="optionPrice">Цена опциона.</param>
		/// <param name="assetPrice">Цена базового актива.</param>
		/// <param name="isSubscribe"><see langword="true"/> если необходимо подписаться, иначе <see langword="false"/>.</param>
		public void SubscribeOptionCalc(Security security, decimal impliedVolatility, decimal optionPrice, decimal assetPrice, bool isSubscribe)
		{
			var transactionId = TransactionIdGenerator.GetNextId();

			MarketDataAdapter.SendInMessage(new OptionCalcMarketDataMessage(impliedVolatility, optionPrice, assetPrice)
			{
				//SecurityId = GetSecurityId(security),
				TransactionId = transactionId,
				IsSubscribe = isSubscribe
			}.FillSecurityInfo(this, security));
		}

		/// <summary>
		/// Получить временные диапазоны, для которых у данного источниках для передаваемой серии свечек есть данные.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <returns>Временные диапазоны.</returns>
		public IEnumerable<Range<DateTimeOffset>> GetSupportedRanges(CandleSeries series)
		{
			if (series.CandleType == typeof(TimeFrameCandle) &&
				series.Arg is TimeSpan &&
				IBTimeFrames.CanConvert((TimeSpan)series.Arg))
			{
				yield return new Range<DateTimeOffset>(DateTimeOffset.MinValue, CurrentTime);
			}
		}

		/// <summary>
		/// Подписаться на получение свечек.
		/// </summary>
		/// <param name="series">Серия свечек.</param>
		/// <param name="from">Начальная дата, с которой необходимо получать данные.</param>
		/// <param name="to">Конечная дата, до которой необходимо получать данные.</param>
		public void SubscribeCandles(CandleSeries series, DateTimeOffset from, DateTimeOffset to)
		{
			var transactionId = TransactionIdGenerator.GetNextId();

			_candleSeries.Add(transactionId, series);

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
			MarketDataAdapter.SendInMessage(new MarketDataMessage
			{
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
			if (direction == MessageDirections.Out)
			{
				switch (message.Type)
				{
					case ExtendedMessageTypes.Scanner:
					{
						var scannerMsg = (ScannerResultMessage)message;
						var state = (ScannerFilter)_states[scannerMsg.OriginalTransactionId];
						NewScannerResults.SafeInvoke(state, scannerMsg.Results);

						break;
					}
					case ExtendedMessageTypes.ScannerParameters:
					{
						var scannerMsg = (ScannerParametersMessage)message;
						NewScannerParameters.SafeInvoke(scannerMsg.Parameters);

						break;
					}
					case ExtendedMessageTypes.FinancialAdvise:
					{
						var adviseMsg = (FinancialAdviseMessage)message;
						NewFinancialAdvise.SafeInvoke(adviseMsg.AdviseType, adviseMsg.Data);

						break;
					}
					case ExtendedMessageTypes.FundamentalReport:
					{
						var reportMsg = (FundamentalReportMessage)message;
						var state = (Tuple<Security, FundamentalReports>)_states[reportMsg.OriginalTransactionId];
						NewFundamentalReport.SafeInvoke(state.Item1, state.Item2, reportMsg.Data);

						break;
					}
					default:
					{
						var candleMsg = message as CandleMessage;

						if (candleMsg == null)
							break;

						var series = _candleSeries.TryGetValue(candleMsg.OriginalTransactionId);

						if (series != null)
						{
							var candle = candleMsg.ToCandle(series);
							NewCandles.SafeInvoke(series, new[] { candle });
						}

						return;
					}
				}
			}

			base.OnProcessMessage(message, adapterType, direction);
		}
	}
}