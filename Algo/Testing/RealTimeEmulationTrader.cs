namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	using EntityFactory = StockSharp.Algo.EntityFactory;

	using StockSharp.Localization;

	/// <summary>
	/// Симуляционное подключение, предназначенный для тестирования стратегии c реальном подключения к торговой системе через <see cref="UnderlyingConnector"/>,
	/// но без реального выставления заявок на бирже. Исполнение заявок и их сделки эмулируются подключением, используя информацию по стаканам, приходящих от реального подключения.
	/// </summary>
	/// <typeparam name="TUnderlyingConnector">Тип реального подключения, с которым будет вестить симуляция.</typeparam>
	public class RealTimeEmulationTrader<TUnderlyingConnector> : BaseEmulationConnector
		where TUnderlyingConnector : Connector
	{
		private sealed class EmulationEntityFactory : EntityFactory
		{
			private readonly Portfolio _portfolio;
			private readonly Connector _connector;

			public EmulationEntityFactory(Portfolio portfolio, Connector connector)
			{
				_portfolio = portfolio;
				_connector = connector;
			}

			public override Portfolio CreatePortfolio(string name)
			{
				return _portfolio.Name.CompareIgnoreCase(name) ? _portfolio : base.CreatePortfolio(name);
			}

			public override Security CreateSecurity(string id)
			{
				return _connector.LookupById(id);
			}
		}

		private readonly Portfolio _portfolio;
		private readonly bool _ownTrader;

		/// <summary>
		/// Создать <see cref="RealTimeEmulationTrader{TUnderlyingTrader}"/>.
		/// </summary>
		public RealTimeEmulationTrader()
			: this(Activator.CreateInstance<TUnderlyingConnector>())
		{
		}

		/// <summary>
		/// Создать <see cref="RealTimeEmulationTrader{TUnderlyingTrader}"/>.
		/// </summary>
		/// <param name="underlyingConnector">Реальное подключение к торговой системе.</param>
		public RealTimeEmulationTrader(TUnderlyingConnector underlyingConnector)
			: this(underlyingConnector, new Portfolio
			{
				Name = LocalizedStrings.Str1209,
				BeginValue = 1000000
			})
		{
		}

		/// <summary>
		/// Создать <see cref="RealTimeEmulationTrader{TUnderlyingTrader}"/>.
		/// </summary>
		/// <param name="underlyingConnector">Реальное подключение к торговой системе.</param>
		/// <param name="portfolio">Портфель, который будет использоваться для выставления заявок. Если значение не задано, то будет создан портфель по умолчанию с названием Симулятор.</param>
		/// <param name="ownTrader">Контролировать время жизни подключения <paramref name="underlyingConnector"/>.</param>
		public RealTimeEmulationTrader(TUnderlyingConnector underlyingConnector, Portfolio portfolio, bool ownTrader = true)
		{
			if (underlyingConnector == null)
				throw new ArgumentNullException("underlyingConnector");

			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			UnderlyingConnector = underlyingConnector;

			UpdateSecurityByLevel1 = false;
			UpdateSecurityLastQuotes = false;

			_portfolio = portfolio;
			EntityFactory = new EmulationEntityFactory(_portfolio, underlyingConnector);
			
			_ownTrader = ownTrader;

			//MarketEmulator.Settings.UseMarketDepth = true;

			MarketDataAdapter = UnderlyingConnector.MarketDataAdapter;

			if (_ownTrader)
				UnderlyingConnector.Log += RaiseLog;

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}

		/// <summary>
		/// Реальное подключение к торговой системе.
		/// </summary>
		public TUnderlyingConnector UnderlyingConnector { get; private set; }

		/// <summary>
		/// Подключиться к торговой системе.
		/// </summary>
		protected override void OnConnect()
		{
			base.OnConnect();

			if (_ownTrader)
				UnderlyingConnector.Connect();
		}

		/// <summary>
		/// Отключиться от торговой системы.
		/// </summary>
		protected override void OnDisconnect()
		{
			base.OnDisconnect();

			if (_ownTrader)
				UnderlyingConnector.Disconnect();
		}

		/// <summary>
		/// Запустить экспорт данных из торговой системы в программу (получение портфелей, инструментов, заявок и т.д.).
		/// </summary>
		protected override void OnStartExport()
		{
			if (_ownTrader)
				UnderlyingConnector.StartExport();
			else
				RaiseExportStarted();
		}

		/// <summary>
		/// Остановить экспорт данных из торговой системы в программу.
		/// </summary>
		protected override void OnStopExport()
		{
			if (_ownTrader)
				UnderlyingConnector.StopExport();
			else
				RaiseExportStopped();
		}

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		/// <param name="adapterType">Тип адаптера, от которого пришло сообщение.</param>
		/// <param name="direction">Направление сообщения.</param>
		protected override void OnProcessMessage(Message message, MessageAdapterTypes adapterType, MessageDirections direction)
		{
			if (message.Type == MessageTypes.Connect && adapterType == MessageAdapterTypes.Transaction && direction == MessageDirections.Out)
			{
				// передаем первоначальное значение размера портфеля в эмулятор
				TransactionAdapter.SendInMessage(_portfolio.ToMessage());
				TransactionAdapter.SendInMessage(new PortfolioChangeMessage
				{
					PortfolioName = _portfolio.Name
				}.Add(PositionChangeTypes.BeginValue, _portfolio.BeginValue));
			}

			base.OnProcessMessage(message, adapterType, direction);
		}

		/// <summary>
		/// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные инструменты будут переданы через событие <see cref="IConnector.LookupSecuritiesResult"/>.
		/// </summary>
		/// <param name="criteria">Критерий, поля которого будут использоваться в качестве фильтра.</param>
		public override void LookupSecurities(SecurityLookupMessage criteria)
		{
			if (_ownTrader)
				UnderlyingConnector.LookupSecurities(criteria);
		}

		/// <summary>
		/// Найти портфели, соответствующие фильтру <paramref name="criteria"/>.
		/// Найденные портфели будут переданы через событие <see cref="IConnector.LookupPortfoliosResult"/>.
		/// </summary>
		/// <param name="criteria">Портфель, поля которого будут использоваться в качестве фильтра.</param>
		public override void LookupPortfolios(Portfolio criteria)
		{
			if (_ownTrader)
				UnderlyingConnector.LookupPortfolios(criteria);
		}

		/// <summary>
		/// Подписаться на получение рыночных данных по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		/// <param name="type">Тип рыночных данных.</param>
		public override void SubscribeMarketData(Security security, MarketDataTypes type)
		{
			if (_ownTrader)
				UnderlyingConnector.SubscribeMarketData(security, type);
		}

		/// <summary>
		/// Отписаться от получения рыночных данных по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		/// <param name="type">Тип рыночных данных.</param>
		public override void UnSubscribeMarketData(Security security, MarketDataTypes type)
		{
			if (_ownTrader)
				UnderlyingConnector.UnSubscribeMarketData(security, type);
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			UnderlyingConnector.Load(storage.GetValue<SettingsStorage>("UnderlyingConnector"));
			//LagTimeout = storage.GetValue<TimeSpan>("LagTimeout");

			base.Load(storage);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			storage.SetValue("UnderlyingConnector", UnderlyingConnector.Save());
			//storage.SetValue("LagTimeout", LagTimeout);

			base.Save(storage);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			//UnderlyingTrader.NewMessage -= NewMessageHandler;

			if (_ownTrader)
			{
				UnderlyingConnector.Log -= RaiseLog;
				UnderlyingConnector.Dispose();
			}

			base.DisposeManaged();
		}

		//private void NewMessageHandler(Message message, MessageDirections direction)
		//{
		//	// пропускаем из UnderlyingTrader сообщения о его реальных заявках, сделках, подключении к торговой системе
		//	switch (message.Type)
		//	{
		//		case MessageTypes.OrderRegister:
		//		case MessageTypes.OrderReplace:
		//		case MessageTypes.OrderPairReplace:
		//		case MessageTypes.OrderCancel:
		//		case MessageTypes.OrderGroupCancel:
		//		case MessageTypes.OrderError:
		//		case MessageTypes.Portfolio:
		//		case MessageTypes.PortfolioChange:
		//		case MessageTypes.Position:
		//		case MessageTypes.PositionChange:
		//		case MessageTypes.MarketData:
		//			return;
		//		case MessageTypes.Execution:
		//		{
		//			var execMsg = (ExecutionMessage)message;
		//			if (execMsg.OrderId != 0 || execMsg.OriginalTransactionId != 0)
		//				return;

		//			break;
		//		}
		//		case MessageTypes.Connect:
		//		case MessageTypes.Disconnect:
		//			return;
		//	}

		//	MarketDataAdapter.SendMessage(message);
		//}
	}

	/// <summary>
	/// Симуляционное подключение, предназначенный для тестирования стратегии c реальном подключения к торговой системе,
	/// но без реального выставления заявок на бирже. Исполнение заявок и их сделки эмулируются подключением, используя информацию по стаканам, приходящих от реального подключения.
	/// </summary>
	public class RealTimeEmulationTrader : RealTimeEmulationTrader<Connector>
	{
		/// <summary>
		/// Создать <see cref="RealTimeEmulationTrader"/>.
		/// </summary>
		/// <param name="underlyingConnector">Реальное подключение к торговой системе.</param>
		public RealTimeEmulationTrader(Connector underlyingConnector)
			: base(underlyingConnector)
		{
		}
	}
}