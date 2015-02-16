namespace StockSharp.Quik
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.Security;

	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Fix;
	using StockSharp.Messages;
	using StockSharp.Quik.Lua;
	using StockSharp.Localization;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/>, предоставляющая подключение к терминалу Quik.
	/// </summary>
	public class QuikTrader : Connector
	{
		private readonly QuikTrans2QuikAdapter _trans2QuikAdapter;
		private readonly QuikDdeAdapter _ddeAdapter;
		private readonly QuikSessionHolder _sessionHolder;

		private readonly FixMessageAdapter _luaTransactionAdapter;
		private readonly FixMessageAdapter _luaMarketDataAdapter;

		/// <summary>
		/// Создать <see cref="QuikTrader"/>.
		/// </summary>
		public QuikTrader()
			: this(QuikTerminal.GetDefaultPath())
		{
		}

		/// <summary>
		/// Создать <see cref="QuikTrader"/>.
		/// </summary>
		/// <param name="path">Путь к директории, где установлен Quik (или путь к файлу info.exe).</param>
		public QuikTrader(string path)
		{
			_sessionHolder = new QuikSessionHolder(TransactionIdGenerator) { Path = path };
			_trans2QuikAdapter = new QuikTrans2QuikAdapter(_sessionHolder);
			_ddeAdapter = new QuikDdeAdapter(_sessionHolder);

			_luaTransactionAdapter = new LuaFixTransactionMessageAdapter(_sessionHolder);
			_luaMarketDataAdapter = new FixMessageAdapter(MessageAdapterTypes.MarketData, _sessionHolder, _sessionHolder.MarketDataSession);

			SessionHolderOnIsLuaChanged();
			_sessionHolder.IsLuaChanged += SessionHolderOnIsLuaChanged;

			ApplyMessageProcessor(MessageDirections.In, true, false);
			ApplyMessageProcessor(MessageDirections.In, false, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}

		private void SessionHolderOnIsLuaChanged()
		{
			if (IsDde)
			{
				TransactionAdapter = _trans2QuikAdapter;
				MarketDataAdapter = _ddeAdapter;
			}
			else
			{
				TransactionAdapter = _luaTransactionAdapter;
				MarketDataAdapter = _luaMarketDataAdapter;
			}
		}

		/// <summary>
		/// Использовать для старое подключение DDE + Trans2Quik. По-умолчанию выключено.
		/// </summary>
		public bool IsDde
		{
			get { return _sessionHolder.IsDde; }
			set { _sessionHolder.IsDde = value; }
		}

		/// <summary>
		/// Запрашивать все инструменты при подключении.
		/// </summary>
		public bool RequestAllSecurities
		{
			get { return _sessionHolder.MarketDataSession.RequestAllSecurities; }
			set { _sessionHolder.MarketDataSession.RequestAllSecurities = value; }
		}

		/// <summary>
		/// Поддерживает ли Quik Единую Денежную Позицию.
		/// </summary>
		/// <remarks>
		/// False по умолчанию.
		/// </remarks>
		public bool IsCommonMonetaryPosition { get; set; }

		/// <summary>
		/// Имя dll-файла, содержащее Quik API. По-умолчанию равно TRANS2QUIK.DLL.
		/// </summary>
		public string DllName
		{
			get { return _sessionHolder.DllName; }
			set { _sessionHolder.DllName = value; }
		}

		/// <summary>
		/// Название DDE сервера. По-умолчанию равно STOCKSHARP.
		/// </summary>
		public string DdeServer
		{
			get { return _sessionHolder.DdeServer; }
			set { _sessionHolder.DdeServer = value; }
		}

		/// <summary>
		/// Путь к директории, где установлен Quik (или путь к файлу info.exe). По-умолчанию равно <see cref="QuikTerminal.GetDefaultPath"/>.
		/// </summary>
		public string Path
		{
			get { return _sessionHolder.Path; }
			set { _sessionHolder.Path = value; }
		}

		/// <summary>
		/// Адрес сервера для Lua подключения.
		/// </summary>
		public EndPoint LuaFixServerAddress
		{
			get { return _sessionHolder.TransactionSession.Address; }
			set
			{
				_sessionHolder.TransactionSession.Address = value;
				_sessionHolder.MarketDataSession.Address = value;
			}
		}

		/// <summary>
		/// Идентификатор пользователя для Lua подключения.
		/// </summary>
		public string LuaLogin
		{
			get { return _sessionHolder.TransactionSession.SenderCompId; }
			set
			{
				_sessionHolder.TransactionSession.SenderCompId = value;
				_sessionHolder.TransactionSession.Login = value;

				_sessionHolder.MarketDataSession.SenderCompId = value;
				_sessionHolder.MarketDataSession.Login = value;
			}
		}

		/// <summary>
		/// Пароль пользователя для Lua подключения.
		/// </summary>
		public SecureString LuaPassword
		{
			get { return _sessionHolder.TransactionSession.Password; }
			set
			{
				_sessionHolder.TransactionSession.Password = value;
				_sessionHolder.MarketDataSession.Password = value;
			}
		}

		/// <summary>
		/// Вспомогательный класс для управления терминалом Quik.
		/// </summary>
		public QuikTerminal Terminal
		{
			get { return _sessionHolder.Terminal; }
		}

		/// <summary>
		/// Асинхронный режим. Если true, то все транзакции, такие как <see cref="IConnector.RegisterOrder"/>
		/// или <see cref="IConnector.CancelOrder"/> будут отправляться в асинхронном режиме.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию true.
		/// </remarks>
		public bool IsAsyncMode
		{
			get { return _sessionHolder.IsAsyncMode; }
			set { _sessionHolder.IsAsyncMode = value; }
		}

		/// <summary>
		/// Обработать поступающие DDE данные (событие вызывается до всех остальных событий <see cref="QuikTrader"/>).
		/// </summary>
		public event Action<string, IList<IList<object>>> PreProcessDdeData
		{
			add { _ddeAdapter.PreProcessDdeData += value; }
			remove { _ddeAdapter.PreProcessDdeData -= value; }
		}

		/// <summary>
		/// Обработать неизвестные DDE данные.
		/// </summary>
		public event Action<string, IList<IList<object>>> ProcessUnknownDdeData
		{
			add { _ddeAdapter.ProcessUnknownDdeData += value; }
			remove { _ddeAdapter.ProcessUnknownDdeData -= value; }
		}

		/// <summary>
		/// Обработать известные DDE данные.
		/// </summary>
		public event Action<string, IDictionary<object, IList<object>>> ProcessWellKnownDdeData
		{
			add { _ddeAdapter.ProcessWellKnownDdeData += value; }
			remove { _ddeAdapter.ProcessWellKnownDdeData -= value; }
		}

		/// <summary>
		/// Обработать новые строчки таблицы, зарегистрированной через <see cref="CustomTables"/>.
		/// </summary>
		public event Action<Type, IEnumerable<object>> NewCustomTables
		{
			add { _ddeAdapter.NewCustomTables += value; }
			remove { _ddeAdapter.NewCustomTables -= value; }
		}

		/// <summary>
		/// Обработать изменения строчек таблицы, зарегистрированной через <see cref="CustomTables"/>.
		/// </summary>
		public event Action<Type, IEnumerable<object>> CustomTablesChanged
		{
			add { _ddeAdapter.CustomTablesChanged += value; }
			remove { _ddeAdapter.CustomTablesChanged -= value; }
		}

		/// <summary>
		/// Обработать новые строчки таблицы Инструменты (изменения).
		/// </summary>
		public event Action<Security, Level1ChangeMessage> NewSecurityChanges;

		/// <summary>
		/// Отформатировать транзакцию (добавить, удалить или заменить параметры) перед тем, как она будет отправлена в Quik.
		/// </summary>
		public event Action<Transaction> FormatTransaction
		{
			add { _trans2QuikAdapter.FormatTransaction += value; }
			remove { _trans2QuikAdapter.FormatTransaction -= value; }
		}

		/// <summary>
		/// Проверить, установлено ли еще соединение. Проверяется только в том случае, если был вызван метод <see cref="IConnector.Connect"/>.
		/// </summary>
		/// <returns><see langword="true"/>, если соединение еще установлено, false, если торговая система разорвала подключение.</returns>
		protected override bool IsConnectionAlive()
		{
			return IsDde ? _trans2QuikAdapter.IsConnectionAlive : base.IsConnectionAlive();
		}

		/// <summary>
		/// Проверить, установлено ли еще соединение для экспорта. Проверяется только в том случае, если <see cref="Connector.ExportState"/> равен <see cref="ConnectionStates.Connected"/>.
		/// </summary>
		/// <returns><see langword="true"/>, если соединение еще установлено, false, если торговая система разорвала подключение и экспорт не активен.</returns>
		protected override bool IsExportAlive()
		{
			return IsDde ? _trans2QuikAdapter.IsConnectionAlive : base.IsExportAlive();
		}

		/// <summary>
		/// Настройки DDE таблицы Инструменты.
		/// </summary>
		public DdeTable SecuritiesTable { get { return _sessionHolder.SecuritiesTable; } }

		/// <summary>
		/// Настройки DDE таблицы Инструменты (изменения).
		/// </summary>
		public DdeTable SecuritiesChangeTable { get { return _sessionHolder.SecuritiesChangeTable; } }

		/// <summary>
		/// Настройки DDE таблицы Сделки.
		/// </summary>
		public DdeTable TradesTable { get { return _sessionHolder.TradesTable; } }

		/// <summary>
		/// Настройки DDE таблицы Мои Сделки.
		/// </summary>
		public DdeTable MyTradesTable { get { return _sessionHolder.MyTradesTable; } }

		/// <summary>
		/// Настройки DDE таблицы Заявки.
		/// </summary>
		public DdeTable OrdersTable { get { return _sessionHolder.OrdersTable; } }

		/// <summary>
		/// Настройки DDE таблицы Стоп-Заявки.
		/// </summary>
		public DdeTable StopOrdersTable { get { return _sessionHolder.StopOrdersTable; } }

		/// <summary>
		/// Настройки DDE таблицы со стаканом.
		/// </summary>
		public DdeTable QuotesTable { get { return _sessionHolder.QuotesTable; } }

		/// <summary>
		/// Настройки DDE таблицы Портфель по бумагам.
		/// </summary>
		public DdeTable EquityPortfoliosTable { get { return _sessionHolder.EquityPortfoliosTable; } }

		/// <summary>
		/// Настройки DDE таблицы Портфель по деривативам.
		/// </summary>
		public DdeTable DerivativePortfoliosTable { get { return _sessionHolder.DerivativePortfoliosTable; } }

		/// <summary>
		/// Настройки DDE таблицы Позиции по бумагам.
		/// </summary>
		public DdeTable EquityPositionsTable { get { return _sessionHolder.EquityPositionsTable; } }

		/// <summary>
		/// Настройки DDE таблицы Позиции по деривативам.
		/// </summary>
		public DdeTable DerivativePositionsTable { get { return _sessionHolder.DerivativePositionsTable; } }

		/// <summary>
		/// Настройки DDE таблицы Валюты портфелей.
		/// </summary>
		public DdeTable CurrencyPortfoliosTable { get { return _sessionHolder.CurrencyPortfoliosTable; } }

		/// <summary>
		/// Список произвольных таблиц.
		/// </summary>
		public IList<DdeCustomTable> CustomTables
		{
			get { return _ddeAdapter.CustomTables; }
		}
		
		/// <summary>
		/// Загружать заявки, поданные вручную через Quik.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию false.
		/// </remarks>
		public bool SupportManualOrders
		{
			get { return _sessionHolder.SupportManualOrders; }
			set { _sessionHolder.SupportManualOrders = value; }
		}

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		/// <param name="adapterType">Тип адаптера, от которого пришло сообщение.</param>
		/// <param name="direction">Направление сообщения.</param>
		protected override void OnProcessMessage(Message message, MessageAdapterTypes adapterType, MessageDirections direction)
		{
			switch (message.Type)
			{
				case MessageTypes.Level1Change:
				{
					var hist = message as HistoryLevel1ChangeMessage;
					if (hist == null)
						break;

					// TODO
					NewSecurityChanges.SafeInvoke(null, hist);
					return;
				}
			}

			base.OnProcessMessage(message, adapterType, direction);
		}

		/// <summary>
		/// Перерегистрировать заявку на бирже.
		/// </summary>
		/// <param name="oldOrder">Заявка, которую нужно снять.</param>
		/// <param name="newOrder">Новая заявка, которую нужно зарегистрировать.</param>
		protected override void OnReRegisterOrder(Order oldOrder, Order newOrder)
		{
			//Quik не поддерживает Move с MODE=1 для Единой Денежной Позиции.
			//http://quik.ru/forum/import/57855/57855/
			//Поэтому делаем Cancel, потом Register
			if (IsSupportAtomicReRegister && oldOrder.Security.Board.IsSupportAtomicReRegister && !IsCommonMonetaryPosition)
				TransactionAdapter.SendInMessage(oldOrder.CreateReplaceMessage(newOrder, GetSecurityId(newOrder.Security)));
			else
				base.OnReRegisterOrder(oldOrder, newOrder);
		}

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
			if (security != null && portfolio != null && security.Type == SecurityTypes.Future && !security.UnderlyingSecurityId.IsEmpty())
				base.OnCancelOrders(transactionId, isStopOrder, portfolio, direction, board, security);
			else
				this.CancelOrders(Orders, isStopOrder, portfolio, direction, board);
		}

		/// <summary>
		/// Запустить экспорт данных из торговой системы в программу по таблицам, указанных параметром ddeTables.
		/// </summary>
		/// <example><code>// запускаем экспорт по таблице инструментов и заявкам.
		/// _trader.StartExport(_trader.SecuritiesTable, _trader.OrdersTable);</code></example>
		/// <param name="ddeTables">Таблицы, для которых необходимо запустить экспорт через DDE.</param>
		public void StartExport(IEnumerable<DdeTable> ddeTables)
		{
			CheckIsDde();
			ExportState = ConnectionStates.Connecting;
			MarketDataAdapter.SendInMessage(new CustomExportMessage(true, ddeTables));
		}

		/// <summary>
		/// Остановить экспорт данных из торговой системы в программу по таблицам, указанных параметром ddeTables.
		/// </summary>
		/// <param name="ddeTables">Таблицы, для которых необходимо остановить экспорт через DDE.</param>
		public void StopExport(IEnumerable<DdeTable> ddeTables)
		{
			CheckIsDde();
			MarketDataAdapter.SendInMessage(new CustomExportMessage(false, ddeTables));
		}

		/// <summary>
		/// Запустить экспорт данных из торговой системы в программу для произвольной таблицы, зарегистрированной в <see cref="QuikTrader.CustomTables"/>.
		/// </summary>
		/// <param name="customTable">Описание DDE экспорта произвольной таблицы.</param>
		public void StartExport(DdeCustomTable customTable)
		{
			CheckIsDde();
			ExportState = ConnectionStates.Connecting;
			MarketDataAdapter.SendInMessage(new CustomExportMessage(true, customTable));
		}

		/// <summary>
		/// Остановить экспорт данных из торговой системы в программу для произвольной таблицы, зарегистрированной в <see cref="QuikTrader.CustomTables"/>.
		/// </summary>
		/// <param name="customTable">Описание DDE экспорта произвольной таблицы.</param>
		public void StopExport(DdeCustomTable customTable)
		{
			CheckIsDde();
			MarketDataAdapter.SendInMessage(new CustomExportMessage(false, customTable));
		}

		/// <summary>
		/// Запустить экспорт данных таблицы из торговой системы в программу.
		/// </summary>
		/// <param name="caption">Название таблицы, для которой нужно запустить экспорт.</param>
		public void StartExport(string caption)
		{
			CheckIsDde();
			ExportState = ConnectionStates.Connecting;
			MarketDataAdapter.SendInMessage(new CustomExportMessage(true, caption));
		}

		/// <summary>
		/// Остановить экспорт данных таблицы из торговой системы в программу.
		/// </summary>
		/// <param name="caption">Название таблицы, для которой нужно запустить экспорт.</param>
		public void StopExport(string caption)
		{
			CheckIsDde();
			MarketDataAdapter.SendInMessage(new CustomExportMessage(false, caption));
		}

		private void CheckIsDde()
		{
			if (!IsDde)
				throw new NotSupportedException(LocalizedStrings.Str1809);
		}

		/// <summary>
		/// Получить транзакцию по идентификатору.
		/// </summary>
		/// <param name="id">Идентификатор транзакции.</param>
		/// <returns>Транзакция.</returns>
		public Transaction GetTransaction(long id)
		{
			return _trans2QuikAdapter.GetTransaction(id);
		}

		/// <summary>
		/// Освободить занятые ресурсы. В частности, отключиться от торговой системы через <see cref="Connector.Disconnect"/>.
		/// </summary>
		protected override void DisposeManaged()
		{
			_sessionHolder.IsLuaChanged -= SessionHolderOnIsLuaChanged;
			base.DisposeManaged();
		}
	}
}