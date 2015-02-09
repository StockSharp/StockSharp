namespace StockSharp.Alor
{
	using System;
	using System.Collections.Generic;
	using System.Security;
	
	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Alor.Metadata;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo;
	using StockSharp.Messages;

	using TEClientLib;

	using StockSharp.Localization;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/>, предоставляющая подключение брокером Alor.
	/// </summary>
	public sealed partial class AlorTrader : Connector
	{
		/// <summary>
		/// Slot
		/// </summary>
		private readonly Slot _slot;

		/// <summary>
		/// включение и выключение столбцов. Настройте до вызова функции StartExport()
		/// </summary>
		public readonly AlorManagerColumns ManagerColumns;

		private readonly Dictionary<int, MarketDepth> _orderBookData = new Dictionary<int, MarketDepth>();
		private readonly SynchronizedPairSet<int, Security> _orderBooks = new SynchronizedPairSet<int, Security>();

		/// <summary>
		/// Адрес сервера.
		/// </summary>
		public string Address { get; private set; }

		/// <summary>
		/// Логин.
		/// </summary>
		public string Login { get; private set; }

		/// <summary>
		/// Пароль.
		/// </summary>
		private readonly SecureString _password;

		/// <summary>
		/// Настройки таблицы SECURITIES.
		/// </summary>
		public AlorTable SecuritiesTable { get; private set; }

		/// <summary>
		/// Настройки таблицы ALL_TRADES.
		/// </summary>
		public AlorTable TradesTable { get; private set; }

		/// <summary>
		/// Настройки таблицы ORDERS.
		/// </summary>
		public AlorTable OrdersTable { get; private set; }

		/// <summary>
		/// Настройки таблицы STOPORDERS.
		/// </summary>
		public AlorTable StopOrdersTable { get; private set; }

		/// <summary>
		/// Настройки таблицы TRADES.
		/// </summary>
		public AlorTable MyTradesTable { get; private set; }

		/// <summary>
		/// Настройки таблицы ORDERBOOK.
		/// </summary>
		public Dictionary<int, AlorTable> QuotesTable { get; private set; }

		/// <summary>
		/// Настройки таблицы POSITIONS.
		/// </summary>
		public AlorTable MoneyPositionTable { get; private set; }

		/// <summary>
		/// Настройки таблицы HOLDING.
		/// </summary>
		public AlorTable HoldingTable { get; private set; }

		/// <summary>
		/// Настройки таблицы TRDACC.
		/// </summary>
		public AlorTable PortfoliosTable { get; private set; }

		/// <summary>
		/// Настройки таблицы TESYSTIME.
		/// </summary>
		public AlorTable TimeTable { get; private set; }

		/// <summary>
		/// Создать подключение.
		/// </summary>
		/// <param name="slotId">Номер слота.</param>
		/// <param name="address">Адрес сервера.</param>
		/// <param name="login">Логин.</param>
		/// <param name="password">Пароль.</param>
		public AlorTrader(int slotId, string address, string login, string password)
		{
			if (address == null)
				throw new ArgumentNullException("address");

			if (login.IsEmpty())
				throw new ArgumentNullException("login");

			if (password.IsEmpty())
				throw new ArgumentNullException("password");

			Address = address;
			Login = login;
			_password = password.To<SecureString>();
			_slot = new Slot();
			_slot.Open(slotId);
			_slot.Connected += SlotConnected;
			_slot.Disconnected += SlotDisconnected;
			_slot.Error += SlotError;

			SecuritiesTable = new AlorTable(AlorTableTypes.Security, "SECURITIES", RaiseProcessDataError);
			TradesTable = new AlorTable(AlorTableTypes.Trade, "ALL_TRADES", RaiseProcessDataError);
			OrdersTable = new AlorTable(AlorTableTypes.Order, "ORDERS", RaiseProcessDataError);
			StopOrdersTable = new AlorTable(AlorTableTypes.StopOrder, "STOPORDERS", RaiseProcessDataError);
			MyTradesTable = new AlorTable(AlorTableTypes.MyTrade, "TRADES", RaiseProcessDataError);
			QuotesTable = new Dictionary<int, AlorTable>();
			PortfoliosTable = new AlorTable(AlorTableTypes.Portfolio, "TRDACC", RaiseProcessDataError);
			HoldingTable = new AlorTable(AlorTableTypes.Position, "HOLDING", RaiseProcessDataError);
			MoneyPositionTable = new AlorTable(AlorTableTypes.Money, "POSITIONS", RaiseProcessDataError);
			TimeTable = new AlorTable(AlorTableTypes.Time, "TESYSTIME", RaiseProcessDataError);
			AlorManagerColumns.InitMetadata();
			ManagerColumns = new AlorManagerColumns();
			Synchronized();
			_slot.Synchronized += SlotSynchronized;

		}

		private void SlotConnected(int slotId, int connectId)
		{
			if (connectId == 0)
				RaiseConnectionError(new InvalidOperationException(LocalizedStrings.Str3705));
			else
			{
				RaiseConnected();
			}
		}

		private void SlotError(int slotId, int code, string description)
		{
			RaiseConnectionError(AlorExceptionHelper.GetException(code, description));
		}

		private void SlotDisconnected(int slotId, int connectId)
		{
			RaiseDisconnected();
		}

		private void SlotSynchronized(int slotId)
		{
			Synchronized();
		}

		/// <summary>
		/// Текстовое описание подключения.
		/// </summary>
		public override string DisplayName
		{
			get { return "Alor"; }
		}

		/// <summary>
		/// Проверить, установлено ли еще соединение. Проверяется только в том случае, если был вызван метод <see cref="IConnector.Connect"/>.
		/// </summary>
		/// <returns><see langword="true"/>, если соединение еще установлено, false, если торговая система разорвала подключение.</returns>
		protected override bool IsConnectionAlive()
		{
			var state = (SS)_slot.GetCurrentState();
			return state == SS.SS_READY || state == SS.SS_TRANSACTING;
		}

		/// <summary>
		/// Подключиться к торговой системе.
		/// </summary>
		protected override void OnConnect()
		{
			string res;
			_slot.Connect(Address, Login, _password.To<string>(), out res).ThrowIfNeed(res);
		}

		/// <summary>
		/// Отключиться от торговой системы.
		/// </summary>
		protected override void OnDisconnect()
		{
			string res;
			_slot.Disconnect(out res).ThrowIfNeed(res);
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new AlorOrderCondition();
		}

		private static void CloseTable(AlorTable table)
		{
			if (table == null)
				throw new ArgumentNullException("table");
			if (table.MetaTable != null && table.MetaTable.ID != 0)
				table.MetaTable.Close(table.MetaTable.ID);
		}
	}
}