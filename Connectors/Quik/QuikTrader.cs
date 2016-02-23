#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Quik.QuikPublic
File: QuikTrader.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Quik
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Messages;
	using StockSharp.Quik.Lua;
	using StockSharp.Localization;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/>, предоставляющая подключение к терминалу Quik.
	/// </summary>
	[Icon("Quik_logo.png")]
	public class QuikTrader : Connector
	{
		/// <summary>
		/// Адрес по-умолчанию к LUA FIX серверу. Равен localhost:5001
		/// </summary>
		public static readonly EndPoint DefaultLuaAddress = "localhost:5001".To<EndPoint>();

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
			Path = path;
			IsDde = false;
		}

		private LuaFixTransactionMessageAdapter FixTransactionAdapter => Adapter.InnerAdapters.OfType<LuaFixTransactionMessageAdapter>().First();

		private LuaFixMarketDataMessageAdapter FixMarketDataAdapter => Adapter.InnerAdapters.OfType<LuaFixMarketDataMessageAdapter>().First();

		private QuikTrans2QuikAdapter Trans2QuikAdapter => Adapter.InnerAdapters.OfType<QuikTrans2QuikAdapter>().First();

		private QuikDdeAdapter DdeAdapter => Adapter.InnerAdapters.OfType<QuikDdeAdapter>().First();

		private bool _isDde;

		/// <summary>
		/// Использовать для старое подключение DDE + Trans2Quik. По-умолчанию выключено.
		/// </summary>
		public bool IsDde
		{
			get { return _isDde; }
			set
			{
				_isDde = value;

				Adapter.InnerAdapters.Clear();

				if (value)
				{
					var trans2QuikAdapter = new QuikTrans2QuikAdapter(TransactionIdGenerator);
					var ddeAdapter = new QuikDdeAdapter(TransactionIdGenerator);

					trans2QuikAdapter.GetTerminal = ddeAdapter.GetTerminal = () => Terminal;

					Adapter.InnerAdapters.Add(trans2QuikAdapter);
					Adapter.InnerAdapters.Add(ddeAdapter);
				}
				else
				{
					Adapter.InnerAdapters.Add(new LuaFixTransactionMessageAdapter(TransactionIdGenerator));
					Adapter.InnerAdapters.Add(new LuaFixMarketDataMessageAdapter(TransactionIdGenerator));
				}
			}
		}

		/// <summary>
		/// Запрашивать все инструменты при подключении.
		/// </summary>
		public bool RequestAllSecurities
		{
			get { return FixMarketDataAdapter.RequestAllSecurities; }
			set { FixMarketDataAdapter.RequestAllSecurities = value; }
		}

		/// <summary>
		/// Поддерживает ли Quik Единую Денежную Позицию.
		/// </summary>
		/// <remarks>
		/// <see langword="false"/> по умолчанию.
		/// </remarks>
		public bool IsCommonMonetaryPosition { get; set; }

		/// <summary>
		/// Имя dll-файла, содержащее Quik API. По-умолчанию равно TRANS2QUIK.DLL.
		/// </summary>
		public string DllName
		{
			get { return Trans2QuikAdapter.DllName; }
			set { Trans2QuikAdapter.DllName = value; }
		}

		/// <summary>
		/// Название DDE сервера. По-умолчанию равно STOCKSHARP.
		/// </summary>
		public string DdeServer
		{
			get { return DdeAdapter.DdeServer; }
			set { DdeAdapter.DdeServer = value; }
		}

		private string _path;

		/// <summary>
		/// Путь к директории, где установлен Quik (или путь к файлу info.exe).
		/// По-умолчанию равно <see cref="QuikTerminal.GetDefaultPath"/>.
		/// </summary>
		public string Path
		{
			get { return _path; }
			set
			{
				if (Path == value)
					return;

				Terminal = null;
				_path = value;
			}
		}

		/// <summary>
		/// Адрес сервера для Lua подключения.
		/// </summary>
		public EndPoint LuaFixServerAddress
		{
			get { return FixTransactionAdapter.Address; }
			set
			{
				FixTransactionAdapter.Address = value;
				FixMarketDataAdapter.Address = value;
			}
		}

		/// <summary>
		/// Идентификатор пользователя для Lua подключения.
		/// </summary>
		public string LuaLogin
		{
			get { return FixTransactionAdapter.SenderCompId; }
			set
			{
				FixTransactionAdapter.SenderCompId = value;
				FixTransactionAdapter.Login = value;

				FixMarketDataAdapter.SenderCompId = value;
				FixMarketDataAdapter.Login = value;
			}
		}

		/// <summary>
		/// Пароль пользователя для Lua подключения.
		/// </summary>
		public SecureString LuaPassword
		{
			get { return FixTransactionAdapter.Password; }
			set
			{
				FixTransactionAdapter.Password = value;
				FixMarketDataAdapter.Password = value;
			}
		}

		private static readonly SynchronizedSet<string> _terminalPaths = new SynchronizedSet<string>();

		private QuikTerminal _terminal;

		/// <summary>
		/// Вспомогательный класс для управления терминалом Quik.
		/// </summary>
		public QuikTerminal Terminal
		{
			get
			{
				if (Path.IsEmpty())
					return null;

				if (_terminal == null)
				{
					_terminal = QuikTerminal.Get(Path);
					_terminal.Adapter = DdeAdapter;
				}

				return _terminal;
			}
			private set
			{
				if (_terminal == value)
					return;

				if (_terminal != null)
				{
					_terminalPaths.Remove(_terminal.FileName);

					_terminal.Adapter = DdeAdapter;
					_terminal = null;
				}

				if (value != null)
				{
					if (!_terminalPaths.TryAdd(value.FileName))
						throw new InvalidOperationException(LocalizedStrings.Str1807Params.Put(value.FileName));

					_terminal = value;
					_terminal.Adapter = DdeAdapter;
				}

				//TerminalChanged.SafeInvoke();
			}
		}

		/// <summary>
		/// Асинхронный режим. Если <see langword="true"/>, то все транзакции, такие как <see cref="IConnector.RegisterOrder"/>
		/// или <see cref="IConnector.CancelOrder"/> будут отправляться в асинхронном режиме.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию <see langword="true"/>.
		/// </remarks>
		public bool IsAsyncMode
		{
			get { return Trans2QuikAdapter.IsAsyncMode; }
			set { Trans2QuikAdapter.IsAsyncMode = value; }
		}

		/// <summary>
		/// Обработать поступающие DDE данные (событие вызывается до всех остальных событий <see cref="QuikTrader"/>).
		/// </summary>
		public event Action<string, IList<IList<object>>> PreProcessDdeData
		{
			add { DdeAdapter.PreProcessDdeData += value; }
			remove { DdeAdapter.PreProcessDdeData -= value; }
		}

		/// <summary>
		/// Обработать неизвестные DDE данные.
		/// </summary>
		public event Action<string, IList<IList<object>>> ProcessUnknownDdeData
		{
			add { DdeAdapter.ProcessUnknownDdeData += value; }
			remove { DdeAdapter.ProcessUnknownDdeData -= value; }
		}

		/// <summary>
		/// Обработать известные DDE данные.
		/// </summary>
		public event Action<string, IDictionary<object, IList<object>>> ProcessWellKnownDdeData
		{
			add { DdeAdapter.ProcessWellKnownDdeData += value; }
			remove { DdeAdapter.ProcessWellKnownDdeData -= value; }
		}

		/// <summary>
		/// Обработать новые строчки таблицы, зарегистрированной через <see cref="CustomTables"/>.
		/// </summary>
		public event Action<Type, IEnumerable<object>> NewCustomTables
		{
			add { DdeAdapter.NewCustomTables += value; }
			remove { DdeAdapter.NewCustomTables -= value; }
		}

		/// <summary>
		/// Обработать изменения строчек таблицы, зарегистрированной через <see cref="CustomTables"/>.
		/// </summary>
		public event Action<Type, IEnumerable<object>> CustomTablesChanged
		{
			add { DdeAdapter.CustomTablesChanged += value; }
			remove { DdeAdapter.CustomTablesChanged -= value; }
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
			add { Trans2QuikAdapter.FormatTransaction += value; }
			remove { Trans2QuikAdapter.FormatTransaction -= value; }
		}

		/// <summary>
		/// Настройки DDE таблицы Инструменты.
		/// </summary>
		public DdeTable SecuritiesTable => DdeAdapter.SecuritiesTable;

		/// <summary>
		/// Настройки DDE таблицы Инструменты (изменения).
		/// </summary>
		public DdeTable SecuritiesChangeTable => DdeAdapter.SecuritiesChangeTable;

		/// <summary>
		/// Настройки DDE таблицы Сделки.
		/// </summary>
		public DdeTable TradesTable => DdeAdapter.TradesTable;

		/// <summary>
		/// Настройки DDE таблицы Мои Сделки.
		/// </summary>
		public DdeTable MyTradesTable => DdeAdapter.MyTradesTable;

		/// <summary>
		/// Настройки DDE таблицы Заявки.
		/// </summary>
		public DdeTable OrdersTable => DdeAdapter.OrdersTable;

		/// <summary>
		/// Настройки DDE таблицы Стоп-Заявки.
		/// </summary>
		public DdeTable StopOrdersTable => DdeAdapter.StopOrdersTable;

		/// <summary>
		/// Настройки DDE таблицы со стаканом.
		/// </summary>
		public DdeTable QuotesTable => DdeAdapter.QuotesTable;

		/// <summary>
		/// Настройки DDE таблицы Портфель по бумагам.
		/// </summary>
		public DdeTable EquityPortfoliosTable => DdeAdapter.EquityPortfoliosTable;

		/// <summary>
		/// Настройки DDE таблицы Портфель по деривативам.
		/// </summary>
		public DdeTable DerivativePortfoliosTable => DdeAdapter.DerivativePortfoliosTable;

		/// <summary>
		/// Настройки DDE таблицы Позиции по бумагам.
		/// </summary>
		public DdeTable EquityPositionsTable => DdeAdapter.EquityPositionsTable;

		/// <summary>
		/// Настройки DDE таблицы Позиции по деривативам.
		/// </summary>
		public DdeTable DerivativePositionsTable => DdeAdapter.DerivativePositionsTable;

		/// <summary>
		/// Настройки DDE таблицы Валюты портфелей.
		/// </summary>
		public DdeTable CurrencyPortfoliosTable => DdeAdapter.CurrencyPortfoliosTable;

		/// <summary>
		/// Список произвольных таблиц.
		/// </summary>
		public IList<DdeCustomTable> CustomTables => DdeAdapter.CustomTables;

		/// <summary>
		/// Загружать заявки, поданные вручную через Quik.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию <see langword="false"/>.
		/// </remarks>
		public bool SupportManualOrders
		{
			get { return DdeAdapter.SupportManualOrders; }
			set { DdeAdapter.SupportManualOrders = value; }
		}

		/// <summary>
		/// Перезаписать файл библиотеки из ресурсов. По-умолчанию файл будет перезаписан.
		/// </summary>
		public bool OverrideDll
		{
			get { return Trans2QuikAdapter.OverrideDll; }
			set { Trans2QuikAdapter.OverrideDll = value; }
		}

		/// <summary>
		/// Обработать сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnProcessMessage(Message message)
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

			base.OnProcessMessage(message);
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
				SendInMessage(oldOrder.CreateReplaceMessage(newOrder, GetSecurityId(newOrder.Security)));
			else
				base.OnReRegisterOrder(oldOrder, newOrder);
		}

		/// <summary>
		/// Таблицы, для которых будет запущен экспорт данных по DDE.
		/// </summary>
		public IEnumerable<DdeTable> DdeTables
		{
			get { return DdeAdapter.Tables; }
			set { DdeAdapter.Tables = value; }
		}

		/// <summary>
		/// Отменить группу заявок на бирже по фильтру.
		/// </summary>
		/// <param name="transactionId">Идентификатор транзакции отмены.</param>
		/// <param name="isStopOrder"><see langword="true"/>, если нужно отменить только стоп-заявки, <see langword="false"/> - если только обычный и <see langword="null"/> - если оба типа.</param>
		/// <param name="portfolio">Портфель. Если значение равно <see langword="null"/>, то портфель не попадает в фильтр снятия заявок.</param>
		/// <param name="direction">Направление заявки. Если значение равно <see langword="null"/>, то направление не попадает в фильтр снятия заявок.</param>
		/// <param name="board">Торговая площадка. Если значение равно <see langword="null"/>, то площадка не попадает в фильтр снятия заявок.</param>
		/// <param name="security">Инструмент. Если значение равно <see langword="null"/>, то инструмент не попадает в фильтр снятия заявок.</param>
		protected override void OnCancelOrders(long transactionId, bool? isStopOrder = null, Portfolio portfolio = null, Sides? direction = null, ExchangeBoard board = null, Security security = null)
		{
			if (security != null && portfolio != null && security.Type == SecurityTypes.Future && !security.UnderlyingSecurityId.IsEmpty())
				base.OnCancelOrders(transactionId, isStopOrder, portfolio, direction, board, security);
			else
				this.CancelOrders(Orders, isStopOrder, portfolio, direction, board);
		}

		///// <summary>
		///// Запустить экспорт данных из торговой системы в программу по таблицам, указанных параметром ddeTables.
		///// </summary>
		///// <example><code>// запускаем экспорт по таблице инструментов и заявкам.
		///// _trader.StartExport(_trader.SecuritiesTable, _trader.OrdersTable);</code></example>
		///// <param name="ddeTables">Таблицы, для которых необходимо запустить экспорт через DDE.</param>
		//public void StartExport(IEnumerable<DdeTable> ddeTables)
		//{
		//	CheckIsDde();
		//	ExportState = ConnectionStates.Connecting;
		//	SendInMessage(new CustomExportMessage(ddeTables) { IsSubscribe = true });
		//}

		///// <summary>
		///// Остановить экспорт данных из торговой системы в программу по таблицам, указанных параметром ddeTables.
		///// </summary>
		///// <param name="ddeTables">Таблицы, для которых необходимо остановить экспорт через DDE.</param>
		//public void StopExport(IEnumerable<DdeTable> ddeTables)
		//{
		//	CheckIsDde();
		//	SendInMessage(new CustomExportMessage(ddeTables) { IsSubscribe = false });
		//}

		/// <summary>
		/// Запустить экспорт данных из торговой системы в программу для произвольной таблицы, зарегистрированной в <see cref="QuikTrader.CustomTables"/>.
		/// </summary>
		/// <param name="customTable">Описание DDE экспорта произвольной таблицы.</param>
		public void StartExport(DdeCustomTable customTable)
		{
			CheckIsDde();
			//ExportState = ConnectionStates.Connecting;
			SendInMessage(new CustomExportMessage(customTable) { IsSubscribe = true });
		}

		/// <summary>
		/// Остановить экспорт данных из торговой системы в программу для произвольной таблицы, зарегистрированной в <see cref="QuikTrader.CustomTables"/>.
		/// </summary>
		/// <param name="customTable">Описание DDE экспорта произвольной таблицы.</param>
		public void StopExport(DdeCustomTable customTable)
		{
			CheckIsDde();
			SendInMessage(new CustomExportMessage(customTable) { IsSubscribe = false });
		}

		/// <summary>
		/// Запустить экспорт данных таблицы из торговой системы в программу.
		/// </summary>
		/// <param name="caption">Название таблицы, для которой нужно запустить экспорт.</param>
		public void StartExport(string caption)
		{
			CheckIsDde();
			//ExportState = ConnectionStates.Connecting;
			SendInMessage(new CustomExportMessage(caption) { IsSubscribe = true });
		}

		/// <summary>
		/// Остановить экспорт данных таблицы из торговой системы в программу.
		/// </summary>
		/// <param name="caption">Название таблицы, для которой нужно запустить экспорт.</param>
		public void StopExport(string caption)
		{
			CheckIsDde();
			SendInMessage(new CustomExportMessage(caption) { IsSubscribe = false });
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
			return Trans2QuikAdapter.GetTransaction(id);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(IsDde), IsDde);
			storage.SetValue(nameof(Path), Path);

			base.Save(storage);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			IsDde = storage.GetValue<bool>(nameof(IsDde));
			Path = storage.GetValue<string>(nameof(Path));

			base.Load(storage);
		}
	}
}