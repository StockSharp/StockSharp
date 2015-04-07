namespace StockSharp.Studio
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Threading.Tasks;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Core.Commands;
	using StockSharp.Studio.Database;
	using StockSharp.Localization;

	using EntityFactory = StockSharp.Algo.EntityFactory;

	internal class StudioConnector : Connector, IStudioConnector
	{
		private sealed class StudioMarketDataAdapter : BasketMessageAdapter
		{
			private readonly Dictionary<object, SecurityId> _securityIds = new Dictionary<object, SecurityId>();

			protected override bool IsSupportNativeSecurityLookup
			{
				get { return true; }
			}

			public StudioMarketDataAdapter(BasketSessionHolder sessionHolder)
				: base(sessionHolder)
			{
			}

			public override void SendOutMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Security:
					{
						var secMsg = (SecurityMessage)message;

						if (secMsg.SecurityId.Native != null)
							_securityIds[secMsg.SecurityId.Native] = secMsg.SecurityId;

						break;
					}

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;
						ReplaceSecurityId(execMsg.SecurityId, id => execMsg.SecurityId = id);
						break;
					}

					case MessageTypes.QuoteChange:
					{
						var quoteMsg = (QuoteChangeMessage)message;
						ReplaceSecurityId(quoteMsg.SecurityId, id => quoteMsg.SecurityId = id);
						break;
					}

					case MessageTypes.Level1Change:
					{
						var level1Msg = (Level1ChangeMessage)message;
						ReplaceSecurityId(level1Msg.SecurityId, id => level1Msg.SecurityId = id);
						break;
					}
				}

				base.SendOutMessage(message);
			}

			private void ReplaceSecurityId(SecurityId securityId, Action<SecurityId> setSecurityId)
			{
				if (securityId.Native == null)
					return;

				SecurityId id;
				if (!_securityIds.TryGetValue(securityId.Native, out id))
					return;

				setSecurityId(new SecurityId { SecurityCode = id.SecurityCode, BoardCode = id.BoardCode, Native = securityId.Native });
			}
		}

		private sealed class StudioHistorySessionHolder : HistorySessionHolder
		{
			public StudioHistorySessionHolder(IdGenerator transactionIdGenerator)
				: base(transactionIdGenerator)
			{
				IsTransactionEnabled = true;
				IsMarketDataEnabled = false;
			}
		}

		private sealed class StudioEmulationAdapter : EmulationMessageAdapter
		{
			public StudioEmulationAdapter(IMessageSessionHolder sessionHolder)
				: base(sessionHolder)
			{
			}

			public void ProcessMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					case MessageTypes.Disconnect:
						return;

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;

						if (execMsg.ExecutionType == ExecutionTypes.Trade || execMsg.ExecutionType == ExecutionTypes.Order)
							return;

						break;
					}
				}

				if (!IsDisposed)
					SendInMessage(message);
			}

			public override void SendOutMessage(Message message)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					case MessageTypes.Disconnect:
						break;

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;

						if (execMsg.ExecutionType != ExecutionTypes.Order && execMsg.ExecutionType != ExecutionTypes.Trade)
							return;

						break;
					}

					case MessageTypes.PortfolioLookupResult:
					case MessageTypes.Portfolio:
					case MessageTypes.PortfolioChange:
					case MessageTypes.Position:
					case MessageTypes.PositionChange:
						break;

					default:
						return;
				}

				base.SendOutMessage(message);
			}
		}

		private sealed class StudioEntityFactory : EntityFactory, ISecurityProvider
		{
			private readonly StudioConnector _parent;
			private readonly StudioEntityRegistry _entityRegistry = (StudioEntityRegistry)ConfigManager.GetService<IStudioEntityRegistry>();

			private static readonly SynchronizedSet<Security> _notSavedSecurities = new SynchronizedSet<Security>();
			private static readonly SynchronizedSet<Portfolio> _notSavedPortfolios = new SynchronizedSet<Portfolio>();
			private static readonly SynchronizedSet<News> _notSavedNews = new SynchronizedSet<News>();

			public StudioEntityFactory(StudioConnector parent)
			{
				if (parent == null)
					throw new ArgumentNullException("parent");

				_parent = parent;

				_parent.NewSecurities += OnSecurities;
				_parent.SecuritiesChanged += OnSecurities;

				_parent.NewPortfolios += OnPortfolios;
				_parent.PortfoliosChanged += OnPortfolios;

				_parent.NewNews += OnNews;
				_parent.NewsChanged += OnNews;
			}

			private void OnSecurities(IEnumerable<Security> securities)
			{
				lock (_notSavedSecurities.SyncRoot)
				{
					if (_notSavedSecurities.Count == 0)
						return;

					foreach (var s in securities)
					{
						var security = s;

						if (!_notSavedSecurities.Contains(security))
							continue;

						// NOTE Когда из Квика пришел инструмент, созданный по сделке
						if (security.Code.IsEmpty())
							continue;

						_parent.AddInfoLog(LocalizedStrings.Str3618Params, security.Id);

						var securityToSave = security.Clone();
						securityToSave.ExtensionInfo = new Dictionary<object, object>();
						_entityRegistry.Securities.Save(securityToSave);

						_notSavedSecurities.Remove(security);
					}
				}

				new NewSecuritiesCommand().Process(this);
			}

			private void OnPortfolios(IEnumerable<Portfolio> portfolios)
			{
				//foreach (var portfolio in portfolios)
				//{
				//	_parent.AddInfoLog("Изменение портфеля {0}.", portfolio.Name);
				//}

				lock (_notSavedPortfolios.SyncRoot)
				{
					if (_notSavedPortfolios.Count == 0)
						return;

					foreach (var p in portfolios)
					{
						var portfolio = p;

						if (!_notSavedPortfolios.Contains(portfolio))
							continue;

						//если площадка у портфеля пустая, то необходимо дождаться ее заполнения
						//пустой площадка может быть когда в начале придет информация о заявке 
						//с портфелем или о позиции, и только потом придет сам портфель

						// mika: портфели могут быть универсальными и не принадлежать площадке

						//var board = portfolio.Board;
						//if (board == null)
						//	continue;

						_parent.AddInfoLog(LocalizedStrings.Str3619Params, portfolio.Name);

						//if (board != null)
						//	_entityRegistry.SaveExchangeBoard(board);

						_entityRegistry.Portfolios.Save(portfolio);

						_notSavedPortfolios.Remove(portfolio);
					}
				}
			}

			private void OnNews(News news)
			{
				lock (_notSavedNews.SyncRoot)
				{
					if (_notSavedNews.Count == 0)
						return;

					if (!_notSavedNews.Contains(news))
						return;

					_parent.AddInfoLog(LocalizedStrings.Str3620Params, news.Headline);

					_entityRegistry.News.Add(news);
					_notSavedNews.Remove(news);
				}
			}

			public override Portfolio CreatePortfolio(string name)
			{
				_parent.AddInfoLog(LocalizedStrings.Str3621Params, name);

				lock (_notSavedPortfolios.SyncRoot)
				{
					var portfolio = _entityRegistry.Portfolios.ReadById(name);

					if (portfolio == null)
					{
						_parent.AddInfoLog(LocalizedStrings.Str3622Params, name);

						portfolio = base.CreatePortfolio(name);
						_notSavedPortfolios.Add(portfolio);
					}

					return portfolio;
				}
			}

			public override Security CreateSecurity(string id)
			{
				_parent.AddInfoLog(LocalizedStrings.Str3623Params, id);

				lock (_notSavedSecurities.SyncRoot)
				{
					var security = _entityRegistry.Securities.ReadById(id);

					if (security == null)
					{
						_parent.AddInfoLog(LocalizedStrings.Str3624Params, id);

						security = base.CreateSecurity(id);
						_notSavedSecurities.Add(security);
					}

					return security;
				}
			}

			//public override Order CreateOrder(Security security, OrderTypes type, long transactionId)
			//{
			//	if(_innerTrader != null)
			//		return base.CreateOrder(security, type, transactionId);

			//	return ((SessionOrderList)_entityRegistry.Orders).ReadByTransactionId(security, type, transactionId) ?? base.CreateOrder(security, type, transactionId);
			//}

			//public override MyTrade CreateMyTrade(Order order, Trade trade)
			//{
			//	if (_innerTrader != null)
			//		return base.CreateMyTrade(order, trade);

			//	return ((SessionMyTradeList)_entityRegistry.MyTrades).ReadByTradeId(order.Security, order.TransactionId, trade.Id) ?? base.CreateMyTrade(order, trade);
			//}

			public override News CreateNews()
			{
				lock (_notSavedSecurities.SyncRoot)
				{
					//var news = _entityRegistry.News.ReadById(id);

					//if (news == null)
					//{
						var news = base.CreateNews();
						_notSavedNews.Add(news);
					//}

					return news;
				}
			}

			IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
			{
				return _entityRegistry.Securities.Lookup(criteria);
			}

			object ISecurityProvider.GetNativeId(Security security)
			{
				return null;
			}
		}

		private readonly CachedSynchronizedDictionary<Security, SynchronizedDictionary<MarketDataTypes, bool>> _exports = new CachedSynchronizedDictionary<Security, SynchronizedDictionary<MarketDataTypes, bool>>();

		private readonly BasketSessionHolder _sessionHolder;
		private readonly StudioMarketDataAdapter _marketDataAdapter;
		private readonly BasketMessageAdapter _transactionAdapter;

		private bool _newsRegistered;

		public BasketSessionHolder BasketSessionHolder { get { return _sessionHolder; } }

		public StudioConnector()
		{
			EntityFactory = new StudioEntityFactory(this);

			SessionHolder = _sessionHolder = new BasketSessionHolder(TransactionIdGenerator);

			MarketDataAdapter = _marketDataAdapter = new StudioMarketDataAdapter(_sessionHolder);
			TransactionAdapter = _transactionAdapter = new BasketMessageAdapter(_sessionHolder);

			ApplyMessageProcessor(MessageDirections.In, true, false);
			ApplyMessageProcessor(MessageDirections.In, false, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);

			_transactionAdapter.NewOutMessage += TransactionAdapterNewOutMessage;

			CreateEmulationSessionHolder();

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

			cmdSvc.Register<LookupSecuritiesCommand>(this, false, cmd => LookupSecurities(cmd.Criteria));
			cmdSvc.Register<RequestTradesCommand>(this, false, cmd => new NewTradesCommand(Trades).Process(this));
			//cmdSvc.Register<RequestPortfoliosCommand>(this, cmd => Portfolios.ForEach(pf => new PortfolioCommand(pf, true).Process(this)));
			cmdSvc.Register<RequestPositionsCommand>(this, false, cmd => Positions.ForEach(pos => new PositionCommand(CurrentTime, pos, true).Process(this)));
			cmdSvc.Register<RequestMarketDataCommand>(this, false, cmd => AddExport(cmd.Security, cmd.Type));
			cmdSvc.Register<RefuseMarketDataCommand>(this, false, cmd => RemoveExport(cmd.Security, cmd.Type));

			//NewPortfolios += portfolios => portfolios.ForEach(pf => new PortfolioCommand(pf, true).Process(this));
			PortfoliosChanged += portfolios => portfolios.ForEach(pf => new PortfolioCommand(pf, false).Process(this));
			NewPositions += positions => positions.ForEach(pos => new PositionCommand(CurrentTime, pos, true).Process(this));
			PositionsChanged += positions => positions.ForEach(pos => new PositionCommand(CurrentTime, pos, false).Process(this));
			NewTrades += trades => new NewTradesCommand(trades).Process(this);
			NewNews += news => new NewNewsCommand(news).Process(this);
			LookupSecuritiesResult += securities => new LookupSecuritiesResultCommand(securities).Process(this);
			//LookupPortfoliosResult += portfolios => new LookupPortfoliosResultCommand(portfolios).Process(this);

			UpdateSecurityLastQuotes = false;
			UpdateSecurityByLevel1 = false;
		}

		private void CreateEmulationSessionHolder()
		{
			var emulationSessionHolder = _sessionHolder.InnerSessions.OfType<StudioHistorySessionHolder>().FirstOrDefault();

			if (emulationSessionHolder == null)
				_sessionHolder.InnerSessions.Add(emulationSessionHolder = new StudioHistorySessionHolder(TransactionIdGenerator), 1);

			//if (!_transactionAdapter.Portfolios.ContainsKey("Simulator"))
			//	_transactionAdapter.Portfolios.Add("Simulator", emulationSessionHolder);
		}

		private IEnumerable<Portfolio> GetEmulationPortfolios()
		{
			var emu = _transactionAdapter.InnerAdapters.OfType<EmulationMessageAdapter>().FirstOrDefault();

			if (emu == null)
				yield break;

			var portfolios = ConfigManager.GetService<IStudioEntityRegistry>().Portfolios.ToList();

			foreach (var portfolio in portfolios)
			{
				//var sessionHolder = _transactionAdapter.Portfolios.TryGetValue(portfolio.Name);

				//if (sessionHolder != emu.SessionHolder)
				//	continue;

				//yield return portfolio;
			}
		}

		private void SendPortfoliosToEmulator()
		{
			var portfolios = GetEmulationPortfolios();

			var messages = new List<Message>
			{
				new ResetMessage()
			};

			foreach (var tmp in portfolios)
			{
				var portfolio = tmp;

				messages.Add(portfolio.ToChangeMessage());

				messages.AddRange(ConfigManager
					.GetService<IStudioEntityRegistry>()
					.Positions
					.Where(p => p.Portfolio == portfolio)
					.Select(p => p.ToChangeMessage()));
			}

			SendToEmulator(messages);
		}

		public void SendToEmulator(IEnumerable<Message> messages)
		{
			if (messages == null)
				throw new ArgumentNullException("messages");

			var emu = _transactionAdapter.InnerAdapters.OfType<EmulationMessageAdapter>().FirstOrDefault();

			if (emu == null)
			{
				this.AddWarningLog(LocalizedStrings.Str3625);
				return;
			}

			messages.ForEach(emu.SendInMessage);
		}

		private void TransactionAdapterNewOutMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					var emu = _transactionAdapter.InnerAdapters.OfType<StudioEmulationAdapter>().FirstOrDefault();

					if (emu == null)
					{
						this.AddWarningLog(LocalizedStrings.Str3625);
						break;
					}

					emu.Emulator.Settings.ConvertTime = true;
					emu.Emulator.Settings.InitialOrderId = DateTime.Now.Ticks;
					emu.Emulator.Settings.InitialTradeId = DateTime.Now.Ticks;

					_marketDataAdapter.NewOutMessage += emu.ProcessMessage;

					break;
				}

				case MessageTypes.Disconnect:
				{
					var emu = _transactionAdapter.InnerAdapters.OfType<StudioEmulationAdapter>().FirstOrDefault();

					if (emu != null)
						_marketDataAdapter.NewOutMessage -= emu.ProcessMessage;

					break;
				}
			}
		}

		private void TrySubscribeMarketData()
		{
			foreach (var pair in _exports.CachedPairs)
			{
				foreach (var type in pair.Value.Where(type => !type.Value))
					base.SubscribeMarketData(pair.Key, type.Key);
			}

			if (_newsRegistered)
				base.OnRegisterNews();
		}

		private void ResetMarketDataSubscriptions()
		{
			foreach (var pair in _exports.CachedPairs)
			{
				var dict = pair.Value;

				foreach (var type in dict.Keys.ToArray())
					dict[type] = false;
			}
		}

		public override void SubscribeMarketData(Security security, MarketDataTypes type)
		{
			AddExport(security, type);
		}

		public override void UnSubscribeMarketData(Security security, MarketDataTypes type)
		{
			RemoveExport(security, type);
		}

		protected override void OnRegisterNews()
		{
			_newsRegistered = true;

			if (ExportState == ConnectionStates.Connected)
				base.OnRegisterNews();
		}

		protected override void OnUnRegisterNews()
		{
			_newsRegistered = false;

			if (ConnectionState == ConnectionStates.Connected)
				base.OnUnRegisterNews();
		}

		protected override void OnConnect()
		{
			CreateEmulationSessionHolder();
			base.OnConnect();
		}

		private void AddExport(Security security, MarketDataTypes type)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			_exports.SafeAdd(security).SafeAdd(type);

			if (ExportState == ConnectionStates.Connected)
				base.SubscribeMarketData(security, type);
		}

		private void RemoveExport(Security security, MarketDataTypes type)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			_exports.SyncDo(d =>
			{
				var types = d.TryGetValue(security);

				if (types == null)
					return;

				types.Remove(type);
			});

			base.UnSubscribeMarketData(security, type);
		}

		protected override void OnProcessMessage(Message message, IMessageAdapter adapter, MessageDirections direction)
		{
			switch (message.Type)
			{
				case MessageTypes.Connect:
				{
					if (direction == MessageDirections.Out &&
						adapter == MarketDataAdapter &&
						((ConnectMessage)message).Error == null)
					{
						SendPortfoliosToEmulator();
						TrySubscribeMarketData();
					}

					break;					
				}

				case MessageTypes.Disconnect:
				{
					if (direction == MessageDirections.Out && adapter == MarketDataAdapter)
						ResetMarketDataSubscriptions();
					break;
				}

				case MessageTypes.MarketData:
				{
					var mdMsg = (MarketDataMessage)message;
					var securityId = mdMsg.SecurityId;

					if (direction == MessageDirections.Out && mdMsg.Error == null && !securityId.SecurityCode.IsDefault() && !securityId.BoardCode.IsDefault())
					{
						var security = GetSecurity(securityId);
						var types = _exports.TryGetValue(security);

						if (types != null && !types.TryGetValue(mdMsg.DataType))
						{
							types[mdMsg.DataType] = true;
						}
					}

					break;
				}

				case MessageTypes.Security:
				{
					var secMsg = (SecurityMessage)message;
					var board = ExchangeBoard.GetBoard(secMsg.SecurityId.BoardCode);

					if (board != null)
						SendToEmulator(new[] { board.ToMessage() });

					break;
				}
			}

			base.OnProcessMessage(message, adapter, direction);
		}
	}

	internal sealed class StudioRegistryConnector : Connector
	{
		private readonly IStudioEntityRegistry _entityRegistry;
		private readonly PassThroughMessageAdapter _adapter;

		public override IEnumerable<Security> Securities
		{
			get { return _entityRegistry.Securities; }
		}

		public override IEnumerable<Portfolio> Portfolios
		{
			get { return _entityRegistry.Portfolios; }
		}

		public StudioRegistryConnector(IConnector studioConnector)
		{
			EntityFactory = new StudioConnectorEntityFactory();

			MarketDataAdapter = _adapter = new PassThroughMessageAdapter(new PassThroughSessionHolder(TransactionIdGenerator));
			TransactionAdapter = new PassThroughMessageAdapter(new PassThroughSessionHolder(TransactionIdGenerator));

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);

			_entityRegistry = ConfigManager.GetService<IStudioEntityRegistry>();
			_entityRegistry.Securities.Added += s => _adapter.SendOutMessage(s.ToMessage(GetSecurityId(s)));
			_entityRegistry.Portfolios.Added += p => _adapter.SendOutMessage(p.ToMessage());

			var cmdSvc = ConfigManager.GetService<IStudioCommandService>();

			//cmdSvc.Register<LookupSecuritiesCommand>(this, cmd => LookupSecurities(cmd.Criteria));
			cmdSvc.Register<RequestPortfoliosCommand>(this, false, cmd => Portfolios.ForEach(pf => new PortfolioCommand(pf, true).Process(this)));

			NewPortfolios += portfolios => portfolios.ForEach(pf => new PortfolioCommand(pf, true).Process(this));
			//NewPositions += positions => positions.ForEach(pos => new PositionCommand(pos, true).Process(this));

			// для корректной работы правил коннектор всегда должен быть реальным
			//NewSecurities += securities => securities.ForEach(s => s.Connector = studioConnector);
			NewPortfolios += portfolios => portfolios.ForEach(p => p.Connector = studioConnector);
		}

		protected override void OnConnect()
		{
			Task.Factory.StartNew(() =>
			{
				base.OnConnect();
				StartExport();
			});
		}

		protected override void OnStartExport()
		{
			base.OnStartExport();
			Securities.ForEach(s => _adapter.SendOutMessage(s.ToMessage(GetSecurityId(s))));
			Portfolios.ForEach(p => _adapter.SendOutMessage(p.ToMessage()));
		}
	}

	internal sealed class StudioConnectorEntityFactory : EntityFactory, ISecurityProvider
	{
		private readonly StudioEntityRegistry _entityRegistry;

		public StudioConnectorEntityFactory()
		{
			_entityRegistry = (StudioEntityRegistry)ConfigManager.GetService<IStudioEntityRegistry>();
		}

		public override Portfolio CreatePortfolio(string name)
		{
			return _entityRegistry.Portfolios.ReadById(name) ?? base.CreatePortfolio(name);
		}

		public override Security CreateSecurity(string id)
		{
			return _entityRegistry.Securities.ReadById(id) ?? base.CreateSecurity(id);
		}

		IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
		{
			return criteria.Id.IsEmpty()
				? Enumerable.Empty<Security>()
				: new[] { _entityRegistry.Securities.ReadById(criteria.Id) };
		}

		object ISecurityProvider.GetNativeId(Security security)
		{
			return null;
		}
	}
}
