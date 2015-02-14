namespace StockSharp.Quik
{
	using System;
	using System.ComponentModel;
	using System.Net;
	using System.Security;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Localization;
	using Ecng.Serialization;
	using Ecng.Xaml;

	using StockSharp.BusinessEntities;
	using StockSharp.Fix;
	using StockSharp.Messages;
	using StockSharp.Quik.Lua;
	using StockSharp.Quik.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Контейнер для сессии.
	/// </summary>
	[DisplayName("Quik")]
	[CategoryLoc(LocalizedStrings.Str1769Key)]
	[DescriptionLoc(LocalizedStrings.Str1770Key)]
	[CategoryOrderLoc(LocalizedStrings.Str1771Key, 0)]
	[CategoryOrder(_luaCategory, 1)]
	[CategoryOrder(_ddeCategory, 2)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 3)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 4)]
	[TargetPlatform(Languages.Russian)]
	public class QuikSessionHolder : FixSessionHolder
	{
		private const string _ddeCategory = "DDE";
		private const string _luaCategory = "LUA";

		internal event Action IsLuaChanged;
		private bool _isDde;

		/// <summary>
		/// Использовать для старое подключение DDE + Trans2Quik. По-умолчанию выключено.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str1771Key)]
		[DisplayName("DDE")]
		[DescriptionLoc(LocalizedStrings.Str1772Key)]
		[PropertyOrder(0)]
		public bool IsDde
		{
			get { return _isDde; }
			set
			{
				if (_isDde == value)
					return;

				_isDde = value;
				IsLuaChanged.SafeInvoke();
			}
		}

		private string _path;

		/// <summary>
		/// Путь к директории, где установлен Quik (или путь к файлу info.exe).
		/// По-умолчанию равно <see cref="QuikTerminal.GetDefaultPath"/>.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1773Key)]
		[DescriptionLoc(LocalizedStrings.Str1774Key)]
		[PropertyOrder(0)]
		[Editor(typeof(FolderBrowserEditor), typeof(FolderBrowserEditor))]
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
		/// Загружать заявки, поданные вручную через Quik.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию false.
		/// </remarks>
		[CategoryLoc(LocalizedStrings.Str1771Key)]
		[DisplayNameLoc(LocalizedStrings.Str1775Key)]
		[DescriptionLoc(LocalizedStrings.Str1776Key)]
		[PropertyOrder(1)]
		public bool SupportManualOrders { get; set; }

		private FixSession _transactionSession = new FixSession
		{
			Login = "quik",
			Password = "quik".To<SecureString>(),
			Address = "localhost:5001".To<EndPoint>(),
			TargetCompId = "StockSharpTS",
			SenderCompId = "quik",
			ExchangeBoard = ExchangeBoard.Forts,
			Version = FixVersions.Fix44,
			RequestAllPortfolios = true,
			MarketData = FixMarketData.None
		};

		/// <summary>
		/// Транзакционная сессия.
		/// </summary>
		[Category(_luaCategory)]
		[DisplayNameLoc(LocalizedStrings.TransactionsKey)]
		[DescriptionLoc(LocalizedStrings.TransactionalSessionKey)]
		[PropertyOrder(0)]
		public override FixSession TransactionSession
		{
			get { return _transactionSession; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_transactionSession = value;
			}
		}

		private FixSession _marketDataSession = new FixSession
		{
			Login = "quik",
			Password = "quik".To<SecureString>(),
			Address = "localhost:5001".To<EndPoint>(),
			TargetCompId = "StockSharpMD",
			SenderCompId = "quik",
			ExchangeBoard = ExchangeBoard.Forts,
			Version = FixVersions.Fix44,
			RequestAllSecurities = true,
			MarketData = FixMarketData.MarketData,
		};

		/// <summary>
		/// Маркет-дата сессия.
		/// </summary>
		[Category(_luaCategory)]
		[DisplayNameLoc(LocalizedStrings.MarketDataKey)]
		[DescriptionLoc(LocalizedStrings.MarketDataSessionKey)]
		[PropertyOrder(1)]
		public override FixSession MarketDataSession
		{
			get { return _marketDataSession; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				_marketDataSession = value;
			}
		}

		/// <summary>
		/// Транзакционная сессия (копия, используется в случае ММВБ для получение заявок и сделок, зарегистрированных под подключения).
		/// </summary>
		[Browsable(false)]
		public override FixSession TransactionCopySession
		{
			get { return base.TransactionCopySession; }
			set { base.TransactionCopySession = value; }
		}

		private string _dllName = "TRANS2QUIK.DLL";

		/// <summary>
		/// Имя dll-файла, содержащее Quik API. По-умолчанию равно TRANS2QUIK.DLL.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1777Key)]
		[DescriptionLoc(LocalizedStrings.Str1778Key)]
		[PropertyOrder(0)]
		[Editor(typeof(FileBrowserEditor), typeof(FileBrowserEditor))]
		public string DllName
		{
			get { return _dllName; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				if (value == _dllName)
					return;

				_dllName = value;
				DllNameChanged.SafeInvoke();
			}
		}

		private string _ddeServer = "STOCKSHARP";

		/// <summary>
		/// Название DDE сервера. По-умолчанию равно STOCKSHARP.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1779Key)]
		[DescriptionLoc(LocalizedStrings.Str1780Key)]
		[PropertyOrder(1)]
		public string DdeServer
		{
			get { return _ddeServer; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				if (DdeServer == value)
					return;

				_ddeServer = value;
				DdeServerChanged.SafeInvoke();
			}
		}

		/// <summary>
		/// Асинхронный режим. Если true, то все транзакции, такие как <see cref="OrderRegisterMessage"/>
		/// или <see cref="OrderCancelMessage"/> будут отправляться в асинхронном режиме.
		/// </summary>
		/// <remarks>
		/// Значение по умолчанию true.
		/// </remarks>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1781Key)]
		[DescriptionLoc(LocalizedStrings.Str1782Key)]
		[PropertyOrder(2)]
		public bool IsAsyncMode { get; set; }

		/// <summary>
		/// Использовать в экспорте таблицу <see cref="CurrencyPortfoliosTable"/>. По-умолчанию не используется.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1783Key)]
		[DescriptionLoc(LocalizedStrings.Str1784Key)]
		[PropertyOrder(3)]
		public bool UseCurrencyPortfolios { get; set; }

		/// <summary>
		/// Использовать в экспорте таблицу <see cref="SecuritiesChangeTable"/>. По-умолчанию не используется.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1785Key)]
		[DescriptionLoc(LocalizedStrings.Str1786Key)]
		[PropertyOrder(4)]
		public bool UseSecuritiesChange { get; set; }

		/// <summary>
		/// Настройки DDE таблицы Инструменты.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.SecuritiesKey)]
		[DescriptionLoc(LocalizedStrings.Str1787Key)]
		[PropertyOrder(5)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable SecuritiesTable { get; private set; }

		/// <summary>
		/// Настройки DDE таблицы Инструменты (изменения).
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1788Key)]
		[DescriptionLoc(LocalizedStrings.Str1789Key)]
		[PropertyOrder(6)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable SecuritiesChangeTable { get; private set; }

		/// <summary>
		/// Настройки DDE таблицы Сделки.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str985Key)]
		[DescriptionLoc(LocalizedStrings.Str1790Key)]
		[PropertyOrder(7)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable TradesTable { get; private set; }

		/// <summary>
		/// Настройки DDE таблицы Мои Сделки.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1791Key)]
		[DescriptionLoc(LocalizedStrings.Str1792Key)]
		[PropertyOrder(8)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable MyTradesTable { get; private set; }

		/// <summary>
		/// Настройки DDE таблицы Заявки.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.OrdersKey)]
		[DescriptionLoc(LocalizedStrings.Str1793Key)]
		[PropertyOrder(9)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable OrdersTable { get; private set; }

		/// <summary>
		/// Настройки DDE таблицы Стоп-Заявки.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1351Key)]
		[DescriptionLoc(LocalizedStrings.Str1794Key)]
		[PropertyOrder(10)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable StopOrdersTable { get; private set; }

		/// <summary>
		/// Настройки DDE таблицы со стаканом.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.MarketDepthKey)]
		[DescriptionLoc(LocalizedStrings.Str1796Key)]
		[PropertyOrder(11)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable QuotesTable { get; private set; }

		/// <summary>
		/// Настройки DDE таблицы Портфель по бумагам.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1797Key)]
		[DescriptionLoc(LocalizedStrings.Str1798Key)]
		[PropertyOrder(12)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable EquityPortfoliosTable { get; private set; }

		/// <summary>
		/// Настройки DDE таблицы Портфель по деривативам.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1799Key)]
		[DescriptionLoc(LocalizedStrings.Str1800Key)]
		[PropertyOrder(13)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable DerivativePortfoliosTable { get; private set; }

		/// <summary>
		/// Настройки DDE таблицы Позиции по бумагам.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1801Key)]
		[DescriptionLoc(LocalizedStrings.Str1802Key)]
		[PropertyOrder(14)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable EquityPositionsTable { get; private set; }

		/// <summary>
		/// Настройки DDE таблицы Позиции по деривативам.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1803Key)]
		[DescriptionLoc(LocalizedStrings.Str1804Key)]
		[PropertyOrder(15)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable DerivativePositionsTable { get; private set; }

		/// <summary>
		/// Настройки DDE таблицы Валюты портфелей.
		/// </summary>
		[Category(_ddeCategory)]
		[DisplayNameLoc(LocalizedStrings.Str1805Key)]
		[DescriptionLoc(LocalizedStrings.Str1806Key)]
		[PropertyOrder(16)]
		[Editor(typeof(DdeTableColumnsEditor), typeof(DdeTableColumnsEditor))]
		public DdeTable CurrencyPortfoliosTable { get; private set; }

		/// <summary>
		/// Создать <see cref="QuikSessionHolder"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public QuikSessionHolder(IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			CreateTables();

			IsAsyncMode = true;
			Path = QuikTerminal.GetDefaultPath();

			SecurityClassInfo.Add("SPBOPT", new RefPair<SecurityTypes, string>(SecurityTypes.Option, ExchangeBoard.Forts.Code));
			SecurityClassInfo.Add("SPBFUT", new RefPair<SecurityTypes, string>(SecurityTypes.Future, ExchangeBoard.Forts.Code));

			// http://stocksharp.com/forum/yaf_postsm11628_Pozitsii-po-dierivativam.aspx#post11628
			SecurityClassInfo.Add("OPTUX", new RefPair<SecurityTypes, string>(SecurityTypes.Option, ExchangeBoard.Ux.Code));
			SecurityClassInfo.Add("FUTUX", new RefPair<SecurityTypes, string>(SecurityTypes.Future, ExchangeBoard.Ux.Code));
			//SecurityClassInfo.Add("GTS", new RefPair<SecurityTypes, string>(SecurityTypes.Stock, ExchangeBoard.UxStock.Code));

			// http://groups.google.ru/group/stocksharp/msg/28518b814c925521
			SecurityClassInfo.Add("RTSST", new RefPair<SecurityTypes, string>(SecurityTypes.Stock, ExchangeBoard.Forts.Code));

			SecurityClassInfo.Add("QJSIM", new RefPair<SecurityTypes, string>(SecurityTypes.Stock, ExchangeBoard.MicexJunior.Code));

			UtcOffset = TimeHelper.Moscow.BaseUtcOffset;

			IsTransactionEnabled = true;
			IsMarketDataEnabled = true;
		}

		/// <summary>
		/// Создать транзакционный адаптер.
		/// </summary>
		/// <returns>Транзакционный адаптер.</returns>
		public override IMessageAdapter CreateTransactionAdapter()
		{
			return IsDde ? (IMessageAdapter)new QuikTrans2QuikAdapter(this) : new LuaFixTransactionMessageAdapter(this);
		}

		/// <summary>
		/// Создать адаптер маркет-данных.
		/// </summary>
		/// <returns>Адаптер маркет-данных.</returns>
		public override IMessageAdapter CreateMarketDataAdapter()
		{
			return IsDde ? new QuikDdeAdapter(this) : base.CreateMarketDataAdapter();
		}

		/// <summary>
		/// Объединять обработчики входящих сообщений для адаптеров.
		/// </summary>
		[Browsable(false)]
		public override bool JoinInProcessors
		{
			get { return false; }
		}

		/// <summary>
		/// Проверить введенные параметры на валидность.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid
		{
			get
			{
				return IsDde
					? !DdeServer.IsEmpty() && !Path.IsEmpty() && !DllName.IsEmpty()
					: base.IsValid;
			}
		}

		internal event Action TerminalChanged;
		internal event Action DllNameChanged;
		internal event Action DdeServerChanged;

		private static readonly SynchronizedSet<string> _terminalPaths = new SynchronizedSet<string>();
		private QuikTerminal _terminal;

		/// <summary>
		/// Вспомогательный класс для управления терминалом Quik.
		/// </summary>
		[Browsable(false)]
		public QuikTerminal Terminal
		{
			get
			{
				if (Path.IsEmpty())
					return null;

				if (_terminal == null)
				{
					_terminal = QuikTerminal.Get(Path);
					_terminal.SessionHolder = this;
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

					_terminal.SessionHolder = null;
					_terminal = null;
				}

				if (value != null)
				{
					if (!_terminalPaths.TryAdd(value.FileName))
						throw new InvalidOperationException(LocalizedStrings.Str1807Params.Put(value.FileName));

					_terminal = value;
					_terminal.SessionHolder = this;
				}

				TerminalChanged.SafeInvoke();
			}
		}

		/// <summary>
		/// Являются ли подключения адаптеров независимыми друг от друга.
		/// </summary>
		[Browsable(false)]
		public override bool IsAdaptersIndependent
		{
			get { return true; }
		}

		/// <summary>
		/// Создать для заявки типа <see cref="OrderTypes.Conditional"/> условие, которое поддерживается подключением.
		/// </summary>
		/// <returns>Условие для заявки. Если подключение не поддерживает заявки типа <see cref="OrderTypes.Conditional"/>, то будет возвращено null.</returns>
		public override OrderCondition CreateOrderCondition()
		{
			return new QuikOrderCondition();
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("IsDde", IsDde);

			storage.SetValue("DdeServer", DdeServer);

			storage.SetValue("SupportManualOrders", SupportManualOrders);

			storage.SetValue("SecuritiesTable", SecuritiesTable);
			storage.SetValue("TradesTable", TradesTable);
			storage.SetValue("OrdersTable", OrdersTable);
			storage.SetValue("StopOrdersTable", StopOrdersTable);
			storage.SetValue("MyTradesTable", MyTradesTable);
			storage.SetValue("QuotesTable", QuotesTable);
			storage.SetValue("EquityPortfoliosTable", EquityPortfoliosTable);
			storage.SetValue("EquityPositionsTable", EquityPositionsTable);
			storage.SetValue("DerivativePortfoliosTable", DerivativePortfoliosTable);
			storage.SetValue("DerivativePositionsTable", DerivativePositionsTable);
			storage.SetValue("CurrencyPortfoliosTable", CurrencyPortfoliosTable);

			storage.SetValue("UseCurrencyPortfolios", UseCurrencyPortfolios);
			storage.SetValue("UseSecuritiesChange", UseSecuritiesChange);

			storage.SetValue("DllName", DllName);
			storage.SetValue("IsAsyncMode", IsAsyncMode);

			storage.SetValue("Path", Path);

			base.Save(storage);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			IsDde = storage.GetValue<bool>("IsDde");

			DdeServer = storage.GetValue<string>("DdeServer");

			SupportManualOrders = storage.GetValue<bool>("SupportManualOrders");

			SecuritiesTable = storage.GetValue<DdeTable>("SecuritiesTable");
			TradesTable = storage.GetValue<DdeTable>("TradesTable");
			OrdersTable = storage.GetValue<DdeTable>("OrdersTable");
			StopOrdersTable = storage.GetValue<DdeTable>("StopOrdersTable");
			MyTradesTable = storage.GetValue<DdeTable>("MyTradesTable");
			QuotesTable = storage.GetValue<DdeTable>("QuotesTable");
			EquityPortfoliosTable = storage.GetValue<DdeTable>("EquityPortfoliosTable");
			EquityPositionsTable = storage.GetValue<DdeTable>("EquityPositionsTable");
			DerivativePortfoliosTable = storage.GetValue<DdeTable>("DerivativePortfoliosTable");
			DerivativePositionsTable = storage.GetValue<DdeTable>("DerivativePositionsTable");
			CurrencyPortfoliosTable = storage.GetValue<DdeTable>("CurrencyPortfoliosTable");

			UseCurrencyPortfolios = storage.GetValue<bool>("UseCurrencyPortfolios");
			UseSecuritiesChange = storage.GetValue<bool>("UseSecuritiesChange");

			DllName = storage.GetValue<string>("DllName");
			IsAsyncMode = storage.GetValue<bool>("IsAsyncMode");

			Path = storage.GetValue<string>("Path");

			base.Load(storage);
		}

		/// <summary>
		/// Получить строковое представление контейнера.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return IsDde
				? LocalizedStrings.Str1808Params.Put(Path)
				: base.ToString();
		}

		private void CreateTables()
		{
			SecuritiesTable = new DdeTable(DdeTableTypes.Security, "инструменты", "InfoMDITable", new[]
			{
				DdeSecurityColumns.Name,
				DdeSecurityColumns.Code,
				DdeSecurityColumns.Class,
				DdeSecurityColumns.Status,
				DdeSecurityColumns.LotVolume,
				DdeSecurityColumns.PriceStep
			});

			SecuritiesChangeTable = new DdeTable(DdeTableTypes.Security, "инструменты (изменения)", "InfoMDIChanges", new[]
			{
				DdeSecurityColumns.LastChangeTime,
				DdeSecurityColumns.Code,
				DdeSecurityColumns.Class
			});

			TradesTable = new DdeTable(DdeTableTypes.Trade, "все сделки", "InfoMDIAllTrades", new[]
			{
				DdeTradeColumns.Id,
				DdeTradeColumns.Time,
				DdeTradeColumns.SecurityCode,
				DdeTradeColumns.SecurityClass,
				DdeTradeColumns.Price,
				DdeTradeColumns.Volume,
				DdeTradeColumns.OrderDirection,
				DdeTradeColumns.Date
			});

			OrdersTable = new DdeTable(DdeTableTypes.Order, "заявки", "InfoMDIOrders", new[]
			{
				DdeOrderColumns.Id,
				DdeOrderColumns.SecurityCode,
				DdeOrderColumns.SecurityClass,
				DdeOrderColumns.Price,
				DdeOrderColumns.Volume,
				DdeOrderColumns.Balance,
				DdeOrderColumns.Direction,
				DdeOrderColumns.State,
				DdeOrderColumns.Time,
				DdeOrderColumns.CancelTime,
				DdeOrderColumns.Account,
				DdeOrderColumns.Type,
				DdeOrderColumns.ExpiryDate,
				DdeOrderColumns.Comment,
				DdeOrderColumns.TransactionId,
				DdeOrderColumns.Date,
				DdeOrderColumns.ClientCode
			});

			StopOrdersTable = new DdeTable(DdeTableTypes.StopOrder, "стоп-заявки", "InfoMDIStopOrders", new[]
			{
				DdeStopOrderColumns.Id,
				DdeStopOrderColumns.TypeCode,
				DdeStopOrderColumns.SecurityCode,
				DdeStopOrderColumns.SecurityClass,
				DdeStopOrderColumns.Price,
				DdeStopOrderColumns.Volume,
				DdeStopOrderColumns.Balance,
				DdeStopOrderColumns.Direction,
				DdeStopOrderColumns.Time,
				DdeStopOrderColumns.State,
				DdeStopOrderColumns.Account,
				DdeStopOrderColumns.DerivedOrderId,
				DdeStopOrderColumns.StopPrice,
				DdeStopOrderColumns.OtherSecurityCode,
				DdeStopOrderColumns.OtherSecurityClass,
				DdeStopOrderColumns.StopPriceCondition,
				DdeStopOrderColumns.ExpiryDate,
				DdeStopOrderColumns.LinkedOrderId,
				DdeStopOrderColumns.LinkedOrderPrice,
				DdeStopOrderColumns.OffsetValue,
				DdeStopOrderColumns.OffsetType,
				DdeStopOrderColumns.SpreadValue,
				DdeStopOrderColumns.SpreadType,
				DdeStopOrderColumns.ActiveTime,
				DdeStopOrderColumns.ActiveFrom,
				DdeStopOrderColumns.ActiveTo,
				DdeStopOrderColumns.CancelTime,
				DdeStopOrderColumns.Comment,
				DdeStopOrderColumns.StopLimitMarket,
				DdeStopOrderColumns.StopLimitPrice,
				DdeStopOrderColumns.StopLimitCondition,
				DdeStopOrderColumns.TakeProfitMarket,
				DdeStopOrderColumns.ConditionOrderId,
				DdeStopOrderColumns.Type,
				DdeStopOrderColumns.TransactionId,
				DdeStopOrderColumns.Date,
				DdeStopOrderColumns.ClientCode,
				DdeStopOrderColumns.Result
			});

			MyTradesTable = new DdeTable(DdeTableTypes.MyTrade, "мои сделки", "InfoMDITrades", new[]
			{
				DdeMyTradeColumns.Id,
				DdeMyTradeColumns.Time,
				DdeMyTradeColumns.SecurityCode,
				DdeMyTradeColumns.SecurityClass,
				DdeMyTradeColumns.Price,
				DdeMyTradeColumns.Volume,
				DdeMyTradeColumns.OrderId,
				DdeMyTradeColumns.Date
			});

			QuotesTable = new DdeTable(DdeTableTypes.Quote, "стакан", "InfoPriceTable", new[]
			{
				DdeQuoteColumns.AskVolume,
				DdeQuoteColumns.Price,
				DdeQuoteColumns.BidVolume
			});

			EquityPortfoliosTable = new DdeTable(DdeTableTypes.EquityPortfolio, "портфель по бумагам", "InfoMDIMoneyLimits", new[]
			{
			    DdeEquityPortfolioColumns.ClientCode,
				DdeEquityPortfolioColumns.BeginCurrency,
				DdeEquityPortfolioColumns.CurrentCurrency,
				DdeEquityPortfolioColumns.CurrentLeverage,
				DdeEquityPortfolioColumns.LimitType
			});

			DerivativePortfoliosTable = new DdeTable(DdeTableTypes.DerivativePortfolio, "портфель по деривативам", "InfoMDIFuturesClientLimits", new[]
			{
			    DdeDerivativePortfolioColumns.Account,
				DdeDerivativePortfolioColumns.CurrentLimitPositionsPrice,
				DdeDerivativePortfolioColumns.CurrentLimitPositionsOrdersPrice,
				DdeDerivativePortfolioColumns.Margin,
				DdeDerivativePortfolioColumns.LimitType
			});

			EquityPositionsTable = new DdeTable(DdeTableTypes.EquityPosition, "позиции по бумагам", "InfoMDIDepoLimits", new[]
			{
				DdeEquityPositionColumns.ClientCode,
				DdeEquityPositionColumns.Account,
				DdeEquityPositionColumns.SecurityCode,
				DdeEquityPositionColumns.BeginPosition,
				DdeEquityPositionColumns.CurrentPosition,
				DdeEquityPositionColumns.BlockedPosition,
				DdeEquityPositionColumns.LimitType
			});

			DerivativePositionsTable = new DdeTable(DdeTableTypes.DerivativePosition, "позиции по деривативам", "InfoMDIFuturesClientHoldings", new[]
			{
				DdeDerivativePositionColumns.Account,
				DdeDerivativePositionColumns.SecurityCode,
				DdeDerivativePositionColumns.BeginPosition,
				DdeDerivativePositionColumns.CurrentPosition,
				DdeDerivativePositionColumns.CurrentBidsVolume,
				DdeDerivativePositionColumns.CurrentAsksVolume
			});

			CurrencyPortfoliosTable = new DdeTable(DdeTableTypes.CurrencyPortfolio, "валюты портфелей", "", new[]
			{
			    DdeCurrencyPortfolioColumns.ClientCode,
				DdeCurrencyPortfolioColumns.FirmId,
				DdeCurrencyPortfolioColumns.Currency
			});
		}
	}
}