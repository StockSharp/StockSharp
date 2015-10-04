namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using EntityFactory = StockSharp.Algo.EntityFactory;

	/// <summary>
	/// Симуляционное подключение, предназначенный для тестирования стратегии c реальном подключения к торговой системе через <see cref="UnderlyngMarketDataAdapter"/>,
	/// но без реального выставления заявок на бирже. Исполнение заявок и их сделки эмулируются подключением, используя информацию по стаканам, приходящих от реального подключения.
	/// </summary>
	/// <typeparam name="TUnderlyingMarketDataAdapter">Тип <see cref="IMessageAdapter"/>, через который будут получаться маркет-данные.</typeparam>
	public class RealTimeEmulationTrader<TUnderlyingMarketDataAdapter> : BaseEmulationConnector
		where TUnderlyingMarketDataAdapter : IMessageAdapter
	{
		private sealed class EmulationEntityFactory : EntityFactory
		{
			private readonly Portfolio _portfolio;
			//private readonly Connector _connector;

			public EmulationEntityFactory(Portfolio portfolio/*, Connector connector*/)
			{
				_portfolio = portfolio;
				//_connector = connector;
			}

			public override Portfolio CreatePortfolio(string name)
			{
				return _portfolio.Name.CompareIgnoreCase(name) ? _portfolio : base.CreatePortfolio(name);
			}

			//public override Security CreateSecurity(string id)
			//{
			//	return _connector.LookupById(id);
			//}
		}

		private readonly Portfolio _portfolio;
		private readonly bool _ownAdapter;

		///// <summary>
		///// Создать <see cref="RealTimeEmulationTrader{TUnderlyingMarketDataAdapter}"/>.
		///// </summary>
		//public RealTimeEmulationTrader()
		//	: this(Activator.CreateInstance<TUnderlyingMarketDataAdapter>())
		//{
		//}

		/// <summary>
		/// Создать <see cref="RealTimeEmulationTrader{TUnderlyingMarketDataAdapter}"/>.
		/// </summary>
		/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, через который будут получаться маркет-данные.</param>
		public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter)
			: this(underlyngMarketDataAdapter, new Portfolio
			{
				Name = LocalizedStrings.Str1209,
				BeginValue = 1000000
			})
		{
		}

		/// <summary>
		/// Создать <see cref="RealTimeEmulationTrader{TUnderlyingMarketDataAdapter}"/>.
		/// </summary>
		/// <param name="underlyngMarketDataAdapter"><see cref="IMessageAdapter"/>, через который будут получаться маркет-данные.</param>
		/// <param name="portfolio">Портфель, который будет использоваться для выставления заявок. Если значение не задано, то будет создан портфель по умолчанию с названием Симулятор.</param>
		/// <param name="ownAdapter">Контролировать время жизни подключения <paramref name="underlyngMarketDataAdapter"/>.</param>
		public RealTimeEmulationTrader(TUnderlyingMarketDataAdapter underlyngMarketDataAdapter, Portfolio portfolio, bool ownAdapter = true)
		{
			if (underlyngMarketDataAdapter == null)
				throw new ArgumentNullException("underlyngMarketDataAdapter");

			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			UnderlyngMarketDataAdapter = underlyngMarketDataAdapter;
			UnderlyngMarketDataAdapter.RemoveTransactionalSupport();

			UpdateSecurityByLevel1 = false;
			UpdateSecurityLastQuotes = false;

			_portfolio = portfolio;
			EntityFactory = new EmulationEntityFactory(_portfolio);

			_ownAdapter = ownAdapter;

			//MarketEmulator.Settings.UseMarketDepth = true;

			Adapter.InnerAdapters.Add(underlyngMarketDataAdapter);

			if (_ownAdapter)
				UnderlyngMarketDataAdapter.Log += RaiseLog;

			Adapter.InnerAdapters.Add(EmulationAdapter);
		}

		/// <summary>
		/// <see cref="IMessageAdapter"/>, через который будут получаться маркет-данные.
		/// </summary>
		public TUnderlyingMarketDataAdapter UnderlyngMarketDataAdapter { get; private set; }

		///// <summary>
		///// Подключиться к торговой системе.
		///// </summary>
		//protected override void OnConnect()
		//{
		//	base.OnConnect();

		//	if (_ownAdapter)
		//		UnderlyingConnector.Connect();
		//}

		///// <summary>
		///// Отключиться от торговой системы.
		///// </summary>
		//protected override void OnDisconnect()
		//{
		//	base.OnDisconnect();

		//	if (_ownAdapter)
		//		UnderlyingConnector.Disconnect();
		//}

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		protected override void OnProcessMessage(Message message)
		{
			if (message.Type == MessageTypes.Connect && message.Adapter == TransactionAdapter)
			{
				// передаем первоначальное значение размера портфеля в эмулятор
				TransactionAdapter.SendInMessage(_portfolio.ToMessage());
				TransactionAdapter.SendInMessage(new PortfolioChangeMessage
				{
					PortfolioName = _portfolio.Name
				}.Add(PositionChangeTypes.BeginValue, _portfolio.BeginValue));
			}
			else if (message.Adapter == MarketDataAdapter)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					case MessageTypes.Disconnect:
					case MessageTypes.MarketData:
					case MessageTypes.SecurityLookupResult:
						break;
					default:
						TransactionAdapter.SendInMessage(message);
						break;
				}
			}

			base.OnProcessMessage(message);
		}

		///// <summary>
		///// Найти инструменты, соответствующие фильтру <paramref name="criteria"/>.
		///// Найденные инструменты будут переданы через событие <see cref="IConnector.LookupSecuritiesResult"/>.
		///// </summary>
		///// <param name="criteria">Критерий, поля которого будут использоваться в качестве фильтра.</param>
		//public override void LookupSecurities(SecurityLookupMessage criteria)
		//{
		//	MarketDataAdapter.LookupSecurities(criteria);
		//}

		///// <summary>
		///// Найти портфели, соответствующие фильтру <paramref name="criteria"/>.
		///// Найденные портфели будут переданы через событие <see cref="IConnector.LookupPortfoliosResult"/>.
		///// </summary>
		///// <param name="criteria">Портфель, поля которого будут использоваться в качестве фильтра.</param>
		//public override void LookupPortfolios(Portfolio criteria)
		//{
		//	if (_ownTrader)
		//		UnderlyingConnector.LookupPortfolios(criteria);
		//}

		///// <summary>
		///// Подписаться на получение рыночных данных по инструменту.
		///// </summary>
		///// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		///// <param name="type">Тип рыночных данных.</param>
		//public override void SubscribeMarketData(Security security, MarketDataTypes type)
		//{
		//	if (_ownTrader)
		//		UnderlyingConnector.SubscribeMarketData(security, type);
		//}

		///// <summary>
		///// Отписаться от получения рыночных данных по инструменту.
		///// </summary>
		///// <param name="security">Инструмент, по которому необходимо начать получать новую информацию.</param>
		///// <param name="type">Тип рыночных данных.</param>
		//public override void UnSubscribeMarketData(Security security, MarketDataTypes type)
		//{
		//	if (_ownTrader)
		//		UnderlyingConnector.UnSubscribeMarketData(security, type);
		//}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Load(SettingsStorage storage)
		{
			if (_ownAdapter)
				UnderlyngMarketDataAdapter.Load(storage.GetValue<SettingsStorage>("UnderlyngMarketDataAdapter"));

			//LagTimeout = storage.GetValue<TimeSpan>("LagTimeout");

			base.Load(storage);
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public override void Save(SettingsStorage storage)
		{
			if (_ownAdapter)
				storage.SetValue("UnderlyngMarketDataAdapter", UnderlyngMarketDataAdapter.Save());

			//storage.SetValue("LagTimeout", LagTimeout);

			base.Save(storage);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			//UnderlyingTrader.NewMessage -= NewMessageHandler;

			if (_ownAdapter)
			{
				MarketDataAdapter.Log -= RaiseLog;
				MarketDataAdapter.Dispose();
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

	///// <summary>
	///// Симуляционное подключение, предназначенный для тестирования стратегии c реальном подключения к торговой системе,
	///// но без реального выставления заявок на бирже. Исполнение заявок и их сделки эмулируются подключением, используя информацию по стаканам, приходящих от реального подключения.
	///// </summary>
	//public class RealTimeEmulationTrader : RealTimeEmulationTrader<Connector>
	//{
	//	/// <summary>
	//	/// Создать <see cref="RealTimeEmulationTrader"/>.
	//	/// </summary>
	//	/// <param name="underlyingConnector">Реальное подключение к торговой системе.</param>
	//	public RealTimeEmulationTrader(Connector underlyingConnector)
	//		: base(underlyingConnector)
	//	{
	//	}
	//}
}