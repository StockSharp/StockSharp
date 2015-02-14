namespace StockSharp.Quik
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Interop.Dde;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Маркет-дата адаптер сообщений для Quik, работающий через протокол DDE.
	/// </summary>
	public class QuikDdeAdapter : QuikMessageAdapter
	{
		private readonly SynchronizedDictionary<Tuple<string, string>, SynchronizedDictionary<DdeTableColumn, object>> _prevSecurityChanges = new SynchronizedDictionary<Tuple<string, string>, SynchronizedDictionary<DdeTableColumn, object>>();

		private readonly List<string> _eveningClasses = new List<string>();
		private readonly Dictionary<string, TPlusLimits>  _portfolioLimitTypes = new Dictionary<string, TPlusLimits>();

		private readonly DdeCustomTableDeserializer _customTableDeserializer;
		private static int _counter;

		private readonly object _quikDdeServerLock = new object();
		private XlsDdeServer _quikDdeServer;

		private XlsDdeServer QuikDdeServer
		{
			get
			{
				lock (_quikDdeServerLock)
				{
					if (_quikDdeServer == null)
						_quikDdeServer = new XlsDdeServer(SessionHolder.DdeServer, OnPoke, SendOutError);
				}

				return _quikDdeServer;
			}
		}

		/// <summary>
		/// Проверить, установлено ли еще соединение.
		/// </summary>
		public bool IsConnectionAlive
		{
			get
			{
				var terminal = SessionHolder.Terminal;
				return terminal != null && terminal.IsExportStarted;
			}
		}

		/// <summary>
		/// Список произвольных таблиц.
		/// </summary>
		public IList<DdeCustomTable> CustomTables
		{
			get { return _customTableDeserializer.CustomTables; }
		}

		/// <summary>
		/// Обработать поступающие DDE данные (событие вызывается до всех остальных событий <see cref="QuikTrader"/>).
		/// </summary>
		public event Action<string, IList<IList<object>>> PreProcessDdeData;

		/// <summary>
		/// Обработать неизвестные DDE данные.
		/// </summary>
		public event Action<string, IList<IList<object>>> ProcessUnknownDdeData;

		/// <summary>
		/// Обработать известные DDE данные.
		/// </summary>
		public event Action<string, IDictionary<object, IList<object>>> ProcessWellKnownDdeData;

		/// <summary>
		/// Обработать новые строчки таблицы, зарегистрированной через <see cref="CustomTables"/>.
		/// </summary>
		public event Action<Type, IEnumerable<object>> NewCustomTables
		{
			add { _customTableDeserializer.NewCustomTables += value; }
			remove { _customTableDeserializer.NewCustomTables -= value; }
		}

		/// <summary>
		/// Обработать изменения строчек таблицы, зарегистрированной через <see cref="CustomTables"/>.
		/// </summary>
		public event Action<Type, IEnumerable<object>> CustomTablesChanged
		{
			add { _customTableDeserializer.CustomTablesChanged += value; }
			remove { _customTableDeserializer.CustomTablesChanged -= value; }
		}

		/// <summary>
		/// Соответствия кодов инструмента РТС-Стандарт из таблицы "Позиции по деривативам" кодам из таблицы "Инструменты".
		/// </summary>
		/// <remarks>
		/// Код из таблицы "Позиции по деривативам" указывается без последних двух цифр.
		/// </remarks>
		public IDictionary<string, string> RtsStandardDerivativesToSecurities = new Dictionary<string, string>
		{
			{"AFLT", "AFLT"},
			{"CHMF", "CHMF"},
			{"FEES", "FEES"},
			{"GAZR", "GAZP"},
			{"GMKR", "GMKN"},
			{"HYDR", "HYDR"},
			{"IRAO", "IRAO"},
			{"LKOH", "LKOH"},
			{"MAGN", "MAGN"},
			{"MRKH", "MRKH"},
			{"MSNG", "MSNG"},
			{"MTSI", "MTSS"},
			{"NOTK", "NVTK"},
			{"NLMK", "NLMK"},
			{"NMTP", "NMTP"},
			{"OGKA", "OGKA"},
			{"OGKB", "OGKB"},
			{"OGKC", "OGKC"},
			{"PMTL", "PMTL"},
			{"RASP", "RASP"},
			{"ROSN", "ROSN"},
			{"RSTR", "RSTR"},
			{"RTKM", "RTKM"},
			{"SBRF", "SBER"},
			{"SBPR", "SBERP"},
			{"SIBN", "SIBN"},
			{"SNGR", "SNGS"},
			{"SNGP", "SNGSP"},
			{"TATN", "TATN"},
			{"TRNF", "TRNFP"},
			{"TGKA", "TGKA"},
			{"URKA", "URKA"},
			{"VTBR", "VTBR"}
		};

		/// <summary>
		/// Создать <see cref="QuikDdeAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public QuikDdeAdapter(QuikSessionHolder sessionHolder)
			: base(MessageAdapterTypes.Transaction, sessionHolder)
		{
			// http://stocksharp.com/forum/yaf_postst1166_Probliema-s-pierienosom-zaiavok-s-viechierniei-siessii.aspx
			_eveningClasses.Add("FUTEVN");
			_eveningClasses.Add("OPTEVN");

			_customTableDeserializer = new DdeCustomTableDeserializer();

			var ddeServer = "STOCKSHARP";

			if (Interlocked.Increment(ref _counter) > 1)
				ddeServer += _counter;

			SessionHolder.DdeServer = ddeServer;
			SessionHolder.DdeServerChanged += SessionHolderOnDdeServerChanged;
		}

		private void SessionHolderOnDdeServerChanged()
		{
			DisposeDdeServer();
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					_prevSecurityChanges.Clear();

					StartDdeServer();

					var terminal = GetTerminal();

					var customTablesMessage = message as CustomExportMessage;
					if (customTablesMessage != null)
					{
						switch (customTablesMessage.ExportType)
						{
							case CustomExportType.Table:
								terminal.StartDde(customTablesMessage.Table);
								break;

							case CustomExportType.Tables:
								terminal.StartDde(customTablesMessage.Tables);
								break;

							case CustomExportType.Caption:
								terminal.StartDde(customTablesMessage.Caption);
								break;

							default:
								throw new ArgumentOutOfRangeException();
						}
					}
					else
						terminal.StartDde();

					SendOutMessage(new ConnectMessage());
					break;
				}

				case MessageTypes.Disconnect:
				{
					var terminal = GetTerminal();

					var customTablesMessage = message as CustomExportMessage;
					if (customTablesMessage != null)
					{
						switch (customTablesMessage.ExportType)
						{
							case CustomExportType.Table:
								terminal.StopDde(customTablesMessage.Table);
								break;

							case CustomExportType.Tables:
								terminal.StopDde(customTablesMessage.Tables);
								break;

							case CustomExportType.Caption:
								terminal.StopDde(customTablesMessage.Caption);
								break;

							default:
								throw new ArgumentOutOfRangeException();
						}
					}
					else
						terminal.StopDde();

					SendOutMessage(new DisconnectMessage());
					break;
				}

				case MessageTypes.MarketData:
				{
					ProcessMarketDataMessage((MarketDataMessage)message);
					break;
				}
			}
		}

		#region Market data subscription

		private void ProcessMarketDataMessage(MarketDataMessage message)
		{
			switch (message.DataType)
			{
				case MarketDataTypes.Level1:
					SubscribeSecurity(message);
					break;

				case MarketDataTypes.MarketDepth:
					SubscribeMarketDepth(message);
					break;

				case MarketDataTypes.Trades:
					SubscribeTrades(message);
					break;

				default:
					throw new ArgumentOutOfRangeException("message", message.DataType, LocalizedStrings.Str1618);
			}

			var result = (MarketDataMessage)message.Clone();
			result.OriginalTransactionId = message.TransactionId;
			SendOutMessage(result);
		}

		private void SubscribeMarketDepth(MarketDataMessage message)
		{
			var terminal = GetTerminal();

			if (message.IsSubscribe)
			{
				if (!terminal.IsQuotesOpened(message.SecurityId))
					terminal.OpenQuotes(message.SecurityId);

				terminal.StartDde(message.SecurityId);
			}
			else
			{
				terminal.StopDde(message.SecurityId);
			}
		}

		private void SubscribeSecurity(MarketDataMessage message)
		{
			if (message.IsSubscribe)
				GetTerminal().RegisterSecurity(message.Name);
			else
				GetTerminal().UnRegisterSecurity(message.Name);
		}

		private void SubscribeTrades(MarketDataMessage message)
		{
			if (message.IsSubscribe)
				GetTerminal().RegisterTrades(message.Name);
			else
				GetTerminal().UnRegisterTrades(message.Name);
		}

		#endregion
		
		#region Dde server

		private void StartDdeServer()
		{
			if (!QuikDdeServer.IsRegistered)
				QuikDdeServer.Start();
		}

		private void DisposeDdeServer()
		{
			lock (_quikDdeServerLock)
			{
				if (_quikDdeServer == null)
					return;

				_quikDdeServer.Dispose();
				_quikDdeServer = null;
			}
		}

		#endregion

		#region Deserialize data

		/// <summary>
		/// Получить соответствие кода инструмента РТС-стандарт из таблицы "Позиции по деривативам" коду из таблицы "Инструменты".
		/// </summary>
		/// <param name="derivativeCode">Имя инструмента из таблицы "Позиции по деривативам".</param>
		/// <param name="isRtsStandard">Является ли инструмент известным инструментом с рынка РТС-Стандарт.</param>
		/// <returns>Код инструмента из таблицы "Инструменты".</returns>
		private string GetSecurityCode(string derivativeCode, out bool isRtsStandard)
		{
			string securityName = null;

			if (derivativeCode.Length >= 3)
			{
				// Все РТС-Стандарт инструменты оканчиваются на <буква><цифра><цифра>.
				// берем последние три символа и проверям (с конца для скорости) на паттерн <буква><цифра><цифра>.
				// этим мы отсекаем фьючерсы, которые могут иметь вид <.><цифра><цифра> или <буква><буква><цифра>
				var suffix = derivativeCode.Substring(derivativeCode.Length - 3);
				if (Char.IsNumber(suffix[2]) && Char.IsNumber(suffix[1]) && Char.IsLetter(suffix[0]))
				{
					// вырезаем последние две цифры и ищем код в таблице
					var derivativeName = derivativeCode.Substring(0, derivativeCode.Length - 2);
					securityName = RtsStandardDerivativesToSecurities.TryGetValue(derivativeName);
				}
			}

			//Если не нашли в словаре - возвращаем исходное имя
			isRtsStandard = securityName != null;
			return isRtsStandard ? securityName : derivativeCode;
		}

		private static void AddWellKnownDdeData<TItem, TId>(Dictionary<object, IList<object>> wellKnownDdeData, TItem item, IList<object> row, TId id)
			where TItem : class
		{
			if (wellKnownDdeData == null)
				throw new ArgumentNullException("wellKnownDdeData");

			if (item == null)
				throw new ArgumentNullException("item");

			if (row == null)
				throw new ArgumentNullException("row");

			if (wellKnownDdeData.ContainsKey(item))
				throw new ArgumentException(LocalizedStrings.Str1724Params.Put(typeof(TItem).Name, id), "item");

			wellKnownDdeData.Add(item, row);
		}

		private void OnPoke(string category, IList<IList<object>> rows)
		{
			var isWellKnown = true;
			var wellKnownDdeData = new Dictionary<object, IList<object>>();
			var categoryLow = category.ToLowerInvariant();

			try
			{
				PreProcessDdeData.SafeInvoke(category, rows);
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}

			if (categoryLow.CompareIgnoreCase(SessionHolder.MyTradesTable.Caption))
			{
				DeserializeMyTradesTable(rows, wellKnownDdeData);
			}
			else if (categoryLow.CompareIgnoreCase(SessionHolder.TradesTable.Caption))
			{
				DeserializeTradesTable(rows, wellKnownDdeData);
			}
			else if (categoryLow.CompareIgnoreCase(SessionHolder.SecuritiesTable.Caption))
			{
				DeserializeSecuritiesTable(rows, wellKnownDdeData);
			}
			else if (categoryLow.CompareIgnoreCase(SessionHolder.OrdersTable.Caption))
			{
				DeserializeOrdersTable(rows, wellKnownDdeData);
			}
			else if (categoryLow.CompareIgnoreCase(SessionHolder.StopOrdersTable.Caption))
			{
				DeserializeStopOrdersTable(rows, wellKnownDdeData);
			}
			else if (categoryLow.CompareIgnoreCase(SessionHolder.EquityPortfoliosTable.Caption))
			{
				DeserializeEquityPortfoliosTable(rows, wellKnownDdeData);
			}
			else if (categoryLow.CompareIgnoreCase(SessionHolder.DerivativePortfoliosTable.Caption))
			{
				DeserializeDerivativePortfoliosTable(rows, wellKnownDdeData);
			}
			else if (categoryLow.CompareIgnoreCase(SessionHolder.EquityPositionsTable.Caption))
			{
				DeserializeEquityPositionsTable(rows, wellKnownDdeData);
			}
			else if (categoryLow.CompareIgnoreCase(SessionHolder.DerivativePositionsTable.Caption))
			{
				DeserializeDerivativePositionsTable(rows, wellKnownDdeData);
			}
			else if (categoryLow.CompareIgnoreCase(SessionHolder.CurrencyPortfoliosTable.Caption))
			{
				DeserializeCurrencyPortfoliosTable(rows);
			}
			else if (categoryLow.ContainsIgnoreCase(SessionHolder.QuotesTable.Caption))
			{
				DeserializeQuotesTable(category, rows, wellKnownDdeData);
			}
			else if (categoryLow.CompareIgnoreCase(SessionHolder.SecuritiesChangeTable.Caption))
			{
				DeserializeSecuritiesChangeTable(rows);
			}
			else
			{
				isWellKnown = false;
			}

			if (isWellKnown)
			{
				if (wellKnownDdeData.Count > 0)
					ProcessWellKnownDdeData.SafeInvoke(category, wellKnownDdeData);
			}
			else
			{
				if (!_customTableDeserializer.TryDeserialize(categoryLow, rows))
				{
					if (ProcessUnknownDdeData != null)
						ProcessUnknownDdeData(category, rows);
					else
						SendOutError(new ArgumentOutOfRangeException("category", category));
				}
			}
		}

		private void DeserializeMyTradesTable(IList<IList<object>> rows, Dictionary<object, IList<object>> wellKnownDdeData)
		{
			SessionHolder.MyTradesTable.Deserialize(rows, (row, func) =>
			{
				if (IsEveningClass(func, DdeMyTradeColumns.SecurityClass))
					return;

				var orderId = func.Get<long>(DdeMyTradeColumns.OrderId);

				var trade = new ExecutionMessage
				{
					SecurityId = new SecurityId
					{
						SecurityCode = func.Get<string>(DdeMyTradeColumns.SecurityCode),
						BoardCode = func.Get<string>(DdeMyTradeColumns.SecurityClass),
					},
					ServerTime = func.GetTime(SessionHolder.MyTradesTable, DdeMyTradeColumns.Date, DdeMyTradeColumns.Time, DdeMyTradeColumns.TimeMcs),
					OrderId = orderId,
					TradeId = func.Get<long>(DdeMyTradeColumns.Id),
					TradePrice = func.Get<decimal>(DdeMyTradeColumns.Price),
					Volume = func.Get<decimal>(DdeMyTradeColumns.Volume),
					ExecutionType = ExecutionTypes.Trade,
				};

				ExportExtendedProperties(SessionHolder.MyTradesTable, trade, row, func);
				AddWellKnownDdeData(wellKnownDdeData, trade, row, trade.TradeId);

				SendOutMessage(trade);
			}, SendOutError, true);
		}

		private void DeserializeTradesTable(IList<IList<object>> rows, Dictionary<object, IList<object>> wellKnownDdeData)
		{
			var hasDirection = SessionHolder.TradesTable.Columns.Contains(DdeTradeColumns.OrderDirection);

			SessionHolder.TradesTable.Deserialize(rows, (row, func) =>
			{
				if (IsEveningClass(func, DdeTradeColumns.SecurityClass))
					return;

				var tradeMessage = new ExecutionMessage
				{
					SecurityId = new SecurityId
					{
						SecurityCode = func.Get<string>(DdeTradeColumns.SecurityCode),
						BoardCode = func.Get<string>(DdeTradeColumns.SecurityClass),
					},
					ServerTime = func.GetTime(SessionHolder.TradesTable, DdeTradeColumns.Date, DdeTradeColumns.Time, DdeTradeColumns.TimeMcs),
					TradeId = func.Get<long>(DdeTradeColumns.Id),
					TradePrice = func.Get<decimal>(DdeTradeColumns.Price),
					Volume = func.Get<decimal>(DdeTradeColumns.Volume),
					ExecutionType = ExecutionTypes.Tick,
				};

				if (hasDirection)
				{
					var value = func(DdeTradeColumns.OrderDirection);

					if (value is string && (string)value != string.Empty)
						tradeMessage.OriginSide = ((string)value).Substring(0, 1).ToSide();
				}

				ExportExtendedProperties(SessionHolder.TradesTable, tradeMessage, row, func);
				AddWellKnownDdeData(wellKnownDdeData, tradeMessage, row, tradeMessage.OrderId);

				SendOutMessage(tradeMessage);

				// esper
				// в BaseTrader при обрабтке TradeMessage и установленном флаге
				// UpdateSecurityOnEachEvent данные по сделке записываются в инструмент автоматически

				// http://stocksharp.com/forum/yaf_postsm6116_Ratspriedlozhieniie-po-tablitsie-Instrumienty.aspx#post6116
				//
				//var columns = SecuritiesTable.Columns;

				//if (!(columns.Contains(DdeSecurityColumns.LastTradeTime) ||
				//	columns.Contains(DdeSecurityColumns.LastChangeTime) ||
				//	columns.Contains(DdeSecurityColumns.LastTradeVolume) ||
				//	columns.Contains(DdeSecurityColumns.LastTradeVolume2) ||
				//	columns.Contains(DdeSecurityColumns.LastTradePrice)))
				//{
				//	EnqueueMessage(new SecurityChangeMessage(time, tradeMessage.Security, new[]
				//	{
				//		new SecurityChange(time, SecurityChangeTypes.LastTrade, null)
				//	}));
				//}
			}, SendOutError, true);
		}

		private void DeserializeSecuritiesTable(IList<IList<object>> rows, Dictionary<object, IList<object>> wellKnownDdeData)
		{
			SessionHolder.SecuritiesTable.Deserialize(rows, (row, func) =>
			{
				if (IsEveningClass(func, DdeSecurityColumns.Class))
					return;

				var columns = SessionHolder.SecuritiesTable.Columns;

				var code = func.Get<string>(DdeSecurityColumns.Code);
				var secClass = func.Get<string>(DdeSecurityColumns.Class);

				var info = SessionHolder.GetSecurityClassInfo(secClass);
				var boardCode = info.Item2;

				var secId = new SecurityId
				{
					SecurityCode = code,
					BoardCode = boardCode,
				};

				var sec = new SecurityMessage
				{
					SecurityId = secId,
					Name = func.Get<string>(DdeSecurityColumns.Name),
					Multiplier = func.GetNullable<decimal>(DdeSecurityColumns.LotVolume),
					PriceStep = func.GetNullable<decimal>(DdeSecurityColumns.PriceStep),
				};

				if (columns.Contains(DdeSecurityColumns.ShortName))
					sec.ShortName = func.Get<string>(DdeSecurityColumns.ShortName);

				sec.SecurityType = sec.Multiplier == 0 ? SecurityTypes.Index : info.Item1;
				//sec.Board = info.Second;

				if (columns.Contains(DdeSecurityColumns.SettlementDate))
				{
					var settlementDate = func.GetNullable2<DateTime>(DdeSecurityColumns.SettlementDate);

					if (settlementDate != null)
						sec.SettlementDate = settlementDate.Value.ApplyTimeZone(TimeHelper.Moscow);
				}

				if (columns.Contains(DdeSecurityColumns.ExpiryDate))
				{
					var expiryDate = func.GetNullable2<DateTime>(DdeSecurityColumns.ExpiryDate);

					if (expiryDate != null)
						sec.ExpiryDate = expiryDate.Value.ApplyTimeZone(TimeHelper.Moscow);
				}

				if (columns.Contains(DdeSecurityColumns.Strike))
					sec.Strike = func.GetNullable<decimal>(DdeSecurityColumns.Strike);

				if (columns.Contains(DdeSecurityColumns.UnderlyingSecurity))
					sec.UnderlyingSecurityCode = func.Get<string>(DdeSecurityColumns.UnderlyingSecurity);

				if (columns.Contains(DdeSecurityColumns.OptionType))
					sec.OptionType = func.GetOptionType();

				if (columns.Contains(DdeSecurityColumns.NominalCurrency))
					sec.Currency = func.Get<string>(DdeSecurityColumns.NominalCurrency).FromMicexCurrencyName();

				if (columns.Contains(DdeSecurityColumns.ISIN))
					secId.Isin = func.Get<string>(DdeSecurityColumns.ISIN);

				var l1Msg = new Level1ChangeMessage { ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow) };

				l1Msg.Add(Level1Fields.State, func.GetSecurityState());

				if (columns.Contains(DdeSecurityColumns.StepPrice))
					l1Msg.TryAdd(Level1Fields.StepPrice, func.GetNullable(DdeSecurityColumns.StepPrice, sec.PriceStep));

				if (columns.Contains(DdeSecurityColumns.MinPrice))
					l1Msg.TryAdd(Level1Fields.MinPrice, func.GetNullable(DdeSecurityColumns.MinPrice, sec.PriceStep));

				if (columns.Contains(DdeSecurityColumns.MaxPrice))
					l1Msg.TryAdd(Level1Fields.MaxPrice, func.GetNullable<decimal>(DdeSecurityColumns.MaxPrice, int.MaxValue));

				if (columns.Contains(DdeSecurityColumns.ImpliedVolatility))
					l1Msg.TryAdd(Level1Fields.ImpliedVolatility, func.GetNullable<decimal>(DdeSecurityColumns.ImpliedVolatility));

				if (columns.Contains(DdeSecurityColumns.TheorPrice))
					l1Msg.TryAdd(Level1Fields.TheorPrice, func.GetNullable<decimal>(DdeSecurityColumns.TheorPrice));

				if (sec.SecurityType == SecurityTypes.Index && columns.Contains(DdeSecurityColumns.IndexCurrentPrice))
					l1Msg.TryAdd(Level1Fields.LastTradePrice, func.GetNullable<decimal>(DdeSecurityColumns.IndexCurrentPrice));

				if (columns.Contains(DdeSecurityColumns.BestBidPrice))
					l1Msg.TryAdd(Level1Fields.BestBidPrice, func.Get<decimal>(DdeSecurityColumns.BestBidPrice));

				if (columns.Contains(DdeSecurityColumns.BestBidVolume))
					l1Msg.TryAdd(Level1Fields.BestBidVolume, func.Get<decimal>(DdeSecurityColumns.BestBidVolume));

				if (columns.Contains(DdeSecurityColumns.BestAskPrice))
					l1Msg.TryAdd(Level1Fields.BestAskPrice, func.Get<decimal>(DdeSecurityColumns.BestAskPrice));

				if (columns.Contains(DdeSecurityColumns.BestAskVolume))
					l1Msg.TryAdd(Level1Fields.BestAskVolume, func.Get<decimal>(DdeSecurityColumns.BestAskVolume));

				DateTime? lastTradeTime = null;

				if (columns.Contains(DdeSecurityColumns.LastTradeTime))
					lastTradeTime = func.GetNullable2<DateTime>(DdeSecurityColumns.LastTradeTime);
				else if (columns.Contains(DdeSecurityColumns.LastChangeTime))
					lastTradeTime = func.GetNullable2<DateTime>(DdeSecurityColumns.LastChangeTime);

				decimal? lastTradeVolume = null;

				if (columns.Contains(DdeSecurityColumns.LastTradeVolume))
					lastTradeVolume = func.GetNullable2<decimal>(DdeSecurityColumns.LastTradeVolume);
				else if (columns.Contains(DdeSecurityColumns.LastTradeVolume2))
					lastTradeVolume = func.GetNullable2<decimal>(DdeSecurityColumns.LastTradeVolume2);

				decimal? lastTradePrice = null;

				if (columns.Contains(DdeSecurityColumns.LastTradePrice))
					lastTradePrice = func.GetNullable<decimal>(DdeSecurityColumns.LastTradePrice);

				if (
					(lastTradeTime != null && lastTradeTime != default(DateTime)) ||
					(lastTradeVolume != null && lastTradeVolume != default(decimal)) ||
					(lastTradePrice != null && lastTradePrice != default(decimal))
					)
				{
					//if (lastTradeTime != null)
					//	changes.Add(Level1Fields.LastTradeTime, (DateTime)lastTradeTime);

					if (lastTradeVolume != null)
						l1Msg.TryAdd(Level1Fields.LastTradeVolume, (decimal)lastTradeVolume);

					if (lastTradePrice != null)
						l1Msg.TryAdd(Level1Fields.LastTradePrice, (decimal)lastTradePrice);
				}

				if (columns.Contains(DdeSecurityColumns.OpenPrice))
					l1Msg.TryAdd(Level1Fields.OpenPrice, func.GetNullable<decimal>(DdeSecurityColumns.TheorPrice));

				if (columns.Contains(DdeSecurityColumns.ClosePrice))
					l1Msg.TryAdd(Level1Fields.ClosePrice, func.GetNullable<decimal>(DdeSecurityColumns.ClosePrice));

				if (columns.Contains(DdeSecurityColumns.LowPrice))
					l1Msg.TryAdd(Level1Fields.LowPrice, func.GetNullable<decimal>(DdeSecurityColumns.LowPrice));

				if (columns.Contains(DdeSecurityColumns.HighPrice))
					l1Msg.TryAdd(Level1Fields.HighPrice, func.GetNullable<decimal>(DdeSecurityColumns.HighPrice));

				if (columns.Contains(DdeSecurityColumns.MarginBuy))
					l1Msg.TryAdd(Level1Fields.MarginBuy, func.GetNullable<decimal>(DdeSecurityColumns.MarginBuy));

				if (columns.Contains(DdeSecurityColumns.MarginSell))
					l1Msg.TryAdd(Level1Fields.MarginSell, func.GetNullable<decimal>(DdeSecurityColumns.MarginSell));

				if (columns.Contains(DdeSecurityColumns.OpenPositions))
					l1Msg.TryAdd(Level1Fields.OpenInterest, func.GetNullable<decimal>(DdeSecurityColumns.OpenPositions));

				if (columns.Contains(DdeSecurityColumns.BidsCount))
					l1Msg.TryAdd(Level1Fields.BidsCount, func.GetNullable<int>(DdeSecurityColumns.BidsCount));

				if (columns.Contains(DdeSecurityColumns.BidsVolume))
					l1Msg.TryAdd(Level1Fields.BidsVolume, func.GetNullable<decimal>(DdeSecurityColumns.BidsVolume));

				if (columns.Contains(DdeSecurityColumns.AsksCount))
					l1Msg.TryAdd(Level1Fields.AsksCount, func.GetNullable<int>(DdeSecurityColumns.AsksCount));

				if (columns.Contains(DdeSecurityColumns.AsksVolume))
					l1Msg.TryAdd(Level1Fields.AsksVolume, func.GetNullable<decimal>(DdeSecurityColumns.AsksVolume));

				ExportExtendedProperties(SessionHolder.SecuritiesTable, sec, row, func);
				AddWellKnownDdeData(wellKnownDdeData, sec, row, sec.SecurityId);

				sec.SecurityId = l1Msg.SecurityId = secId;

				SendOutMessage(sec);
				SendOutMessage(l1Msg);
			}, SendOutError, true);
		}

		private void DeserializeOrdersTable(IList<IList<object>> rows, Dictionary<object, IList<object>> wellKnownDdeData)
		{
			SessionHolder.OrdersTable.Deserialize(rows, (row, func) =>
			{
				if (IsEveningClass(func, DdeOrderColumns.SecurityClass))
					return;

				//var securityId = CreateSecurityId(func, DdeOrderColumns.SecurityCode, DdeOrderColumns.SecurityClass);
				var account = func.Get<string>(DdeOrderColumns.Account);
				var clientCode = func.Get<string>(DdeOrderColumns.ClientCode);

				var transactionId = func.Get<long>(DdeOrderColumns.TransactionId);

				if (transactionId == 0)
				{
					if (SessionHolder.SupportManualOrders)
						transactionId = TransactionIdGenerator.GetNextId();
					else
						return;
				}

				var order = new ExecutionMessage
				{
					SecurityId = new SecurityId
					{
						SecurityCode = func.Get<string>(DdeOrderColumns.SecurityCode),
						BoardCode = func.Get<string>(DdeOrderColumns.SecurityClass),
					},

					OrderId = func.Get<long>(DdeOrderColumns.Id),
					ServerTime = func.GetTime(SessionHolder.OrdersTable, DdeOrderColumns.Date, DdeOrderColumns.Time, DdeOrderColumns.TimeMcs),

					Price = func.Get<decimal>(DdeOrderColumns.Price),
					Volume = func.Get<decimal>(DdeOrderColumns.Volume),
					Balance = func.Get<decimal>(DdeOrderColumns.Balance),
					Side = func(DdeOrderColumns.Direction).ToSide(),
					PortfolioName = clientCode,

					OrderType = func.GetOrderType(),
					TimeInForce = func.GetTimeInForce(),
					Comment = func.Get<string>(DdeOrderColumns.Comment),
					OriginalTransactionId = transactionId,
					OrderState = OrderStates.Active,
					ExpiryDate = func.GetExpiryDate(DdeOrderColumns.ExpiryDate),

					ExecutionType = ExecutionTypes.Order,

					DepoName = account
				};

				UpdateOrder(func, order, row);

				//order.Action = order.OrderState == OrderStates.Done && order.Balance != 0 
				//	? ExecutionActions.Canceled 
				//	: (order.Volume == order.Balance ? ExecutionActions.Registered : ExecutionActions.Matched);

				AddWellKnownDdeData(wellKnownDdeData, order, row, order.OrderId);

				SendOutMessage(order);

			}, SendOutError, true);
		}

		private void DeserializeCurrencyPortfoliosTable(IList<IList<object>> rows)
		{
			SessionHolder.CurrencyPortfoliosTable.Deserialize(rows, (row, func) =>
			{
				var currency = func.Get<string>(DdeCurrencyPortfolioColumns.Currency).FromMicexCurrencyName();

				if (currency == null)
					return;

				SendOutMessage(
					SessionHolder
						.CreatePortfolioChangeMessage(func.Get<string>(DdeCurrencyPortfolioColumns.ClientCode))
							.Add(PositionChangeTypes.Currency, currency.Value));
			}, SendOutError, true);
		}

		private void DeserializeQuotesTable(string category, IList<IList<object>> rows, Dictionary<object, IList<object>> wellKnownDdeData)
		{
			var startIndex = category.IndexOf(']') + 1;
			var marketDepthTitle = category.Substring(startIndex, category.Length - startIndex);

			var parts = marketDepthTitle.Split('@');

			if (parts.Length != 2)
				throw new InvalidOperationException(LocalizedStrings.Str1725Params.Put(category));

			//var securityId = new SecurityId(CreateSecurityId(parts[0], parts[1]));
			var quotes = new List<QuoteChange>();

			SessionHolder.QuotesTable.Deserialize(rows, (row, func) =>
			{
				var askVolume = func.GetNullable2<decimal>(DdeQuoteColumns.AskVolume);
				var bidVolume = func.GetNullable2<decimal>(DdeQuoteColumns.BidVolume);

				// квик посылает для стаканов иногда пустые значения
				// http://www.quik.ru/forum/iwr/53421/53421/
				if (askVolume != null && bidVolume != null)
				{
					var quote = new QuoteChange(
						bidVolume == 0 ? Sides.Sell : Sides.Buy,
						func.Get<decimal>(DdeQuoteColumns.Price),
						askVolume.Value.Max(bidVolume.Value));

					quotes.Add(quote);

					ExportExtendedProperties(SessionHolder.QuotesTable, quote, row, func);
					wellKnownDdeData.Add(quote, row);
				}
			}, ex => { throw new InvalidOperationException(LocalizedStrings.Str1726Params.Put(parts[0]), ex); }, false);

			SendOutMessage(new QuoteChangeMessage
			{
				SecurityId = new SecurityId
				{
					SecurityCode = parts[0],
					BoardCode = parts[1],
				},
				Bids = quotes.Where(q => q.Side == Sides.Buy),
				Asks = quotes.Where(q => q.Side == Sides.Sell),
				ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
			});
		}

		private void DeserializeSecuritiesChangeTable(IList<IList<object>> rows)
		{
			SessionHolder.SecuritiesChangeTable.Deserialize(rows, (row, func) =>
			{
				if (IsEveningClass(func, DdeSecurityColumns.Class) || SessionHolder.SecuritiesChangeTable.Columns.NonMandatoryColumns.Count == 0)
					return;

				var time = func.GetNullable(DdeSecurityColumns.LastChangeTime, DateTime.MinValue);

				//в таблице изменений могут быть строки с пустой датой,
				//значения остальных столбцов так же могут содержать невалидные значения.
				if (time == DateTime.MinValue)
					return;

				var columns = SessionHolder.SecuritiesChangeTable.Columns.NonMandatoryColumns;

				var message = new HistoryLevel1ChangeMessage
				{
					ServerTime = time.ApplyTimeZone(TimeHelper.Moscow),
					SecurityId = new SecurityId
					{
						SecurityCode = func.Get<string>(DdeSecurityColumns.Code),
						BoardCode = func.Get<string>(DdeSecurityColumns.Class),
					},
				};

				var secCode = func.Get<string>(DdeSecurityColumns.Code);
				var secClass = func.Get<string>(DdeSecurityColumns.Class);

				var prevValues = _prevSecurityChanges.SafeAdd(Tuple.Create(secCode.ToLowerInvariant(), secClass.ToLowerInvariant()));

				foreach (var column in columns)
				{
					var converter = QuikDdeFormatter.DdeColumnValueToSecurityChangeConverters.TryGetValue(column);
					if (converter == null)
						continue;

					var value = func(column);
					var prevValue = prevValues.TryGetValue(column);

					if (value.Equals(prevValue))
						continue;

					prevValues[column] = value;
					message.Changes.Add(converter(value));
				}

				if (!message.Changes.IsEmpty())
					SendOutMessage(message);
			}, SendOutError, true);
		}

		private void DeserializeDerivativePositionsTable(IList<IList<object>> rows, Dictionary<object, IList<object>> wellKnownDdeData)
		{
			SessionHolder.DerivativePositionsTable.Deserialize(rows, (row, func) =>
			{
				var secCode = func.Get<string>(DdeDerivativePositionColumns.SecurityCode);

				// проверить не является ли это акцией РТС-Стандарт, если да то получить правильное название Security Code
				// Для примера secCode = GAZP01 => newSecCode = GAZP
				bool isRtsStandard;
				var newSecCode = GetSecurityCode(secCode, out isRtsStandard);
				
				var account = func.Get<string>(DdeDerivativePositionColumns.Account);

				// Если РТС-Стандарт, то в ShortName записываем имя РТС-Стандарт инструмента (GAZR01)
				// Тогда как сам инструмент будет GAZP
				if (isRtsStandard)
				{
					//TODO надо отправить изменение для ShortName = secCode
					//RaiseNewMessage(new Level1ChangeMessage(){Changes=new List<Level1Change>(){new Level1Change()}});
				}

				var msg = new PositionMessage
				{
					PortfolioName = account,
					SecurityId = new SecurityId { SecurityCode = newSecCode, BoardCode = ExchangeBoard.Forts.Code }
				};

				ExportExtendedProperties(SessionHolder.DerivativePositionsTable, msg, row, func);
				AddWellKnownDdeData(wellKnownDdeData, msg, row, msg.SecurityId + " " + msg.PortfolioName);

				var changes = SessionHolder.CreatePositionChangeMessage(account, msg.SecurityId);

				changes.Add(PositionChangeTypes.BeginValue, func.Get<decimal>(DdeDerivativePositionColumns.BeginPosition));
				changes.Add(PositionChangeTypes.CurrentValue, func.Get<decimal>(DdeDerivativePositionColumns.CurrentPosition));
				changes.Add(PositionChangeTypes.BlockedValue, func.Get<decimal>(DdeDerivativePositionColumns.CurrentBidsVolume) + func.Get<decimal>(DdeDerivativePositionColumns.CurrentAsksVolume));

				if (SessionHolder.DerivativePositionsTable.Columns.Contains(DdeDerivativePositionColumns.EffectivePrice))
					changes.Add(PositionChangeTypes.AveragePrice, func.Get<decimal>(DdeDerivativePositionColumns.EffectivePrice));

				SendOutMessage(msg);
				SendOutMessage(changes);
			}, SendOutError, true);
		}

		private void DeserializeEquityPositionsTable(IList<IList<object>> rows, Dictionary<object, IList<object>> wellKnownDdeData)
		{
			SessionHolder.EquityPositionsTable.Deserialize(rows, (row, func) =>
			{
				var secCode = func.Get<string>(DdeEquityPositionColumns.SecurityCode);

				var depoName = func.Get<string>(DdeEquityPositionColumns.Account);
				var clientCode = func.Get<string>(DdeEquityPositionColumns.ClientCode);

				// mika
				// вначале надо получить портфель, чтобы обновить в случае чего счет ДЕПО у него.
				// если получать сначала инструменты, то можно получить сообщение об ошибке, и счет ДЕПО не инициализируется
				SendOutMessage(
					SessionHolder
						.CreatePortfolioChangeMessage(clientCode)
							.Add(PositionChangeTypes.DepoName, depoName));

				var position = new PositionMessage
				{
					PortfolioName = clientCode,
					SecurityId = new SecurityId { SecurityCode = secCode, SecurityType = SecurityTypes.Stock },
					DepoName = depoName,
					LimitType = func.GetTNLimitType(DdeEquityPositionColumns.LimitType),
				};

				ExportExtendedProperties(SessionHolder.EquityPositionsTable, position, row, func);
				AddWellKnownDdeData(wellKnownDdeData, position, row, position.SecurityId + " " + position.PortfolioName + position.LimitType);

				var changes = new PositionChangeMessage
				{
					PortfolioName = position.PortfolioName,
					SecurityId = position.SecurityId,
					DepoName = depoName,
					LimitType = position.LimitType,
					ServerTime = SessionHolder.CurrentTime.Convert(TimeHelper.Moscow),
				};

				changes.Add(PositionChangeTypes.BeginValue, func.Get<decimal>(DdeEquityPositionColumns.BeginPosition));
				changes.Add(PositionChangeTypes.CurrentValue, func.Get<decimal>(DdeEquityPositionColumns.CurrentPosition));
				changes.Add(PositionChangeTypes.BlockedValue, func.Get<decimal>(DdeEquityPositionColumns.BlockedPosition));

				if (SessionHolder.EquityPositionsTable.Columns.Contains(DdeEquityPositionColumns.BuyPrice))
					changes.Add(PositionChangeTypes.AveragePrice, func.Get<decimal>(DdeEquityPositionColumns.BuyPrice));

				SendOutMessage(position);
				SendOutMessage(changes);
			}, SendOutError, true);
		}

		private void DeserializeDerivativePortfoliosTable(IList<IList<object>> rows, Dictionary<object, IList<object>> wellKnownDdeData)
		{
			SessionHolder.DerivativePortfoliosTable.Deserialize(rows, (row, func) =>
			{
				var type = func.GetLimitType();

				if (type != DerivativeLimitTypes.Money)
					return;

				var account = func.Get<string>(DdeDerivativePortfolioColumns.Account);
				var margin = func.Get<decimal>(DdeDerivativePortfolioColumns.Margin);

				var msg = new PortfolioMessage
				{
					PortfolioName = account,
					BoardCode = ExchangeBoard.Forts.Code
				};

				ExportExtendedProperties(SessionHolder.DerivativePortfoliosTable, msg, row, func);
				AddWellKnownDdeData(wellKnownDdeData, msg, row, msg.PortfolioName);

				var portfolioChanges = SessionHolder.CreatePortfolioChangeMessage(account);

				portfolioChanges.Add(PositionChangeTypes.BeginValue, func.Get<decimal>(DdeDerivativePortfolioColumns.CurrentLimitPositionsPrice));
				portfolioChanges.Add(PositionChangeTypes.CurrentValue, func.Get<decimal>(DdeDerivativePortfolioColumns.CurrentLimitPositionsOrdersPrice));
				portfolioChanges.Add(PositionChangeTypes.Leverage, 0m);
				portfolioChanges.Add(PositionChangeTypes.VariationMargin, margin);

				var columns = SessionHolder.DerivativePortfoliosTable.Columns;
				if (columns.Contains(DdeDerivativePortfolioColumns.ACI))
					portfolioChanges.Add(PositionChangeTypes.VariationMargin, margin + func.Get<decimal>(DdeDerivativePortfolioColumns.ACI));

				if (columns.Contains(DdeDerivativePortfolioColumns.MarketCommission))
					portfolioChanges.Add(PositionChangeTypes.Commission, func.Get<decimal>(DdeDerivativePortfolioColumns.MarketCommission));

				SendOutMessage(msg);
				SendOutMessage(portfolioChanges);
			}, SendOutError, true);
		}

		private void DeserializeEquityPortfoliosTable(IList<IList<object>> rows, Dictionary<object, IList<object>> wellKnownDdeData)
		{
			var table = SessionHolder.EquityPortfoliosTable;

			table.Deserialize(rows, (row, func) =>
			{
				var clientCode = func.Get<string>(DdeEquityPortfolioColumns.ClientCode);
				var limitType = func.GetTNLimitType(DdeEquityPortfolioColumns.LimitType);

				var currentLimitType = _portfolioLimitTypes.TryGetValue(clientCode);

				if (limitType < currentLimitType)
					return;

				var portfolio = new PortfolioMessage
				{
					PortfolioName = clientCode,
					BoardCode = ExchangeBoard.Micex.Code,
				};

				ExportExtendedProperties(table, portfolio, row, func);
				AddWellKnownDdeData(wellKnownDdeData, portfolio, row, portfolio.PortfolioName + limitType);

				SendOutMessage(portfolio);

				SendOutMessage(
					SessionHolder.CreatePortfolioChangeMessage(clientCode)
					.Add(PositionChangeTypes.BeginValue, func.Get<decimal>(DdeEquityPortfolioColumns.BeginCurrency))
					.Add(PositionChangeTypes.CurrentValue, func.Get<decimal>(DdeEquityPortfolioColumns.CurrentCurrency))
					.Add(PositionChangeTypes.Leverage, func.Get<decimal>(DdeEquityPortfolioColumns.CurrentLeverage)));

			}, SendOutError, true);
		}

		private void DeserializeStopOrdersTable(IList<IList<object>> rows, Dictionary<object, IList<object>> wellKnownDdeData)
		{
			SessionHolder.StopOrdersTable.Deserialize(rows, (row, func) =>
			{
				if (IsEveningClass(func, DdeStopOrderColumns.SecurityClass))
					return;

				var transactionId = func.Get<long>(DdeStopOrderColumns.TransactionId);

				if (transactionId == 0)
					transactionId = TransactionIdGenerator.GetNextId();

				//var securityId = CreateSecurityId(func, DdeStopOrderColumns.SecurityCode, DdeStopOrderColumns.SecurityClass);
				var account = func.Get<string>(DdeStopOrderColumns.Account);
				var clientCode = func.Get<string>(DdeStopOrderColumns.ClientCode);

				var isActiveTime = func.GetBool(DdeStopOrderColumns.ActiveTime);
				var date = func.Get<DateTime>(DdeStopOrderColumns.Date);

				var condition = new QuikOrderCondition
				{
					StopPrice = func.Get<decimal>(DdeStopOrderColumns.StopPrice),
					StopPriceCondition = func.GetStopPriceCondition(),
					LinkedOrderPrice = func.GetZeroable<decimal>(DdeStopOrderColumns.LinkedOrderPrice),
					Offset = func.GetUnit(DdeStopOrderColumns.OffsetType, DdeStopOrderColumns.OffsetValue),
					Spread = func.GetUnit(DdeStopOrderColumns.SpreadType, DdeStopOrderColumns.SpreadValue),
					ActiveTime = isActiveTime == null ? null : new Range<DateTimeOffset>(date + func(DdeStopOrderColumns.ActiveFrom).To<TimeSpan>(), date + func(DdeStopOrderColumns.ActiveTo).To<TimeSpan>()),
					IsMarketStopLimit = func.GetBool(DdeStopOrderColumns.StopLimitMarket),
					StopLimitPrice = func.GetNullable2<decimal>(DdeStopOrderColumns.StopLimitPrice),
					IsMarketTakeProfit = func.GetBool(DdeStopOrderColumns.TakeProfitMarket),
					Type = func.GetStopOrderType(),
					Result = func.GetStopResult(),
				};

				var execution = new ExecutionMessage
				{
					SecurityId = new SecurityId
					{
						SecurityCode = func.Get<string>(DdeStopOrderColumns.SecurityCode),
						BoardCode = func.Get<string>(DdeStopOrderColumns.SecurityClass),
					},

					OrderId = func.Get<long>(DdeStopOrderColumns.Id),
					ServerTime = (func.Get<DateTime>(DdeStopOrderColumns.Date) + func.Get<TimeSpan>(DdeStopOrderColumns.Time)).ApplyTimeZone(TimeHelper.Moscow),

					Price = func.Get<decimal>(DdeStopOrderColumns.Price),
					Volume = func.Get<decimal>(DdeStopOrderColumns.Volume),
					Balance = func.Get<decimal>(DdeStopOrderColumns.Balance),
					Side = func(DdeStopOrderColumns.Direction).ToSide(),

					OrderType = OrderTypes.Conditional,
					Comment = func.Get<string>(DdeStopOrderColumns.Comment),
					OriginalTransactionId = transactionId,
					OrderState = OrderStates.Active,
					ExpiryDate = func.GetExpiryDate(DdeStopOrderColumns.ExpiryDate),
					PortfolioName = clientCode,

					Condition = condition,
					ExecutionType = ExecutionTypes.Order,
					DepoName = account
				};

				var derivedOrderId = func.GetZeroable<long>(DdeStopOrderColumns.DerivedOrderId);
				if (derivedOrderId != null)
					execution.DerivedOrderId = derivedOrderId;

				// http://www.quik.ru/forum/ideas/51556/
				if (condition.Type == QuikOrderConditionTypes.OtherSecurity)
				{
					var otherSecurityCode = func.Get<string>(DdeStopOrderColumns.OtherSecurityCode);
					var otherSecurityClass = func.Get<string>(DdeStopOrderColumns.OtherSecurityClass);

					condition.OtherSecurityId = otherSecurityCode.IsEmpty()
						? execution.SecurityId
						: new SecurityId
						{
							SecurityCode = otherSecurityCode,
							BoardCode = otherSecurityClass,
						};
				}

				var code = func.Get<string>(DdeStopOrderColumns.TypeCode);

				var conditionOrderId = func.GetZeroable<long>(DdeStopOrderColumns.ConditionOrderId);
				if (conditionOrderId != null)
				{
					condition.ConditionOrderId = conditionOrderId;
					condition.ConditionOrderPartiallyMatched = code.Contains('P');
					condition.ConditionOrderUseMatchedBalance = code.Contains('U');
				}
				else if (condition.LinkedOrderPrice != null)
				{
					condition.LinkedOrderCancel = code.Contains('P');
				}

				UpdateStopOrder(func, execution, row);
				AddWellKnownDdeData(wellKnownDdeData, execution, row, execution.OrderId);

				SendOutMessage(execution);
			}, SendOutError, true);
		}

		private bool IsEveningClass(Func<DdeTableColumn, object> func, DdeTableColumn classColumn)
		{
			var securityClass = func.Get<string>(classColumn);
			return _eveningClasses.Contains(securityClass);
		}

		private void UpdateStopOrder(Func<DdeTableColumn, object> func, ExecutionMessage message, IList<object> row)
		{
			if (func == null)
				throw new ArgumentNullException("func");

			if (message == null)
				throw new ArgumentNullException("message");

			if (row == null)
				throw new ArgumentNullException("row");

			// Когда заявка уже была снята через асинхронный колбэк 
			// http://groups.google.ru/group/stocksharp/browse_thread/thread/c2a0adef8a430726#
			//
			if (message.OrderState != OrderStates.Done)
				message.OrderState = func.GetState(DdeStopOrderColumns.State) ?? OrderStates.Done;

			var cancelTime = func.GetNullable2<TimeSpan>(DdeStopOrderColumns.CancelTime);

			//order.CancelTime = cancelTime == null ? null : order.Time.Date + cancelTime;
			if (cancelTime != null)
				message.ServerTime = message.ServerTime.Date + cancelTime.Value;

			//TODO поддержка стопов через месседжи
			//var derivedOrderId = func.GetZeroable<long>(DdeStopOrderColumns.DerivedOrderId);
			//if (derivedOrderId != null)
			//	AddDerivedOrder(order.Security, (long)derivedOrderId, order, (s, o) => s.DerivedOrder = o);

			//var linkedOrderId = func.GetZeroable<long>(DdeStopOrderColumns.LinkedOrderId);
			//if (linkedOrderId != null)
			//	AddDerivedOrder(order.Security, (long)linkedOrderId, order, (s, o) => ((QuikOrderCondition)s.Condition).LinkedOrder = o);

			CheckStopResult(message, func.GetStopResult());

			ExportExtendedProperties(SessionHolder.StopOrdersTable, message, row, func);
		}

		private static void CheckStopResult(ExecutionMessage orderMessage, QuikOrderConditionResults? result)
		{
			var quikCond = (QuikOrderCondition)orderMessage.Condition;
			quikCond.Result = result;

			if (result == QuikOrderConditionResults.RejectedByTS || result == QuikOrderConditionResults.LimitControlFailed)
			{
				var message = result == QuikOrderConditionResults.LimitControlFailed
					? LocalizedStrings.Str1727Params.Put(orderMessage.OrderId)
					: LocalizedStrings.Str1728Params.Put(orderMessage.OrderId);

				orderMessage.Error = new InvalidOperationException(message);
				orderMessage.OrderState = OrderStates.Failed;
			}
		}

		private void UpdateOrder(Func<DdeTableColumn, object> func, ExecutionMessage message, IList<object> row)
		{
			if (func == null)
				throw new ArgumentNullException("func");

			if (message == null)
				throw new ArgumentNullException("message");

			if (row == null)
				throw new ArgumentNullException("row");

			var cancelTime = func.GetNullableTime(SessionHolder.OrdersTable, DdeOrderColumns.Date, DdeOrderColumns.CancelTime, DdeOrderColumns.CancelTimeMcs);
			if (cancelTime.HasValue)
				message.ServerTime = cancelTime.Value;

			if (message.Balance == 0)
				message.OrderState = OrderStates.Done;
			else
			{
				var state = func.GetState(DdeOrderColumns.State);

				// заявка снята
				if (state == null)
				{
					// http://stocksharp.com/forum/yaf_postsm7221_3-1-6-MarketQuotingStrategy-bagh.aspx
					message.OrderState = OrderStates.Done;
				}
				else
				{
					// Предотвратить исполнение заявки при еще необновленном объеме
					// http://groups.google.ru/group/stocksharp/msg/a7d97814c859c722
					//
					if (state != OrderStates.Done)
						message.OrderState = state;
					else
					{
						throw new InvalidOperationException(LocalizedStrings.Str1729Params
																.Put(message.OriginalTransactionId != 0 ? message.OriginalTransactionId : message.OrderId, message.Balance));
					}
				}
			}

			ExportExtendedProperties(SessionHolder.OrdersTable, message, row, func);
		}

		private static void ExportExtendedProperties(DdeTable table, IExtendableEntity entity, IList<object> row, Func<DdeTableColumn, object> func)
		{
			if (table == null)
				throw new ArgumentNullException("table");

			if (entity == null)
				throw new ArgumentNullException("entity");

			if (row == null)
				throw new ArgumentNullException("row");

			if (func == null)
				throw new ArgumentNullException("func");

			if (table.Columns.ExtendedColumns.Count > 0)
			{
				if (entity.ExtensionInfo == null)
					entity.ExtensionInfo = new Dictionary<object, object>();

				table.Columns.ExtendedColumns.ForEach(c =>
				{
					var value = func(c);
					entity.ExtensionInfo[c.TableType + "-" + c.Name] = QuikDdeFormatter.Get(value, c);
				});

				entity.ExtensionInfo = entity.ExtensionInfo;
			}
		}

		#endregion
		
		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			Interlocked.Decrement(ref _counter);
			SessionHolder.DdeServerChanged -= SessionHolderOnDdeServerChanged;

			try
			{
				GetTerminal().StopActiveDdeExport();
			}
			catch (Exception ex)
			{
				SendOutError(ex);
			}

			DisposeDdeServer();

			base.DisposeManaged();
		}
	}
}