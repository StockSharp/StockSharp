namespace StockSharp.Studio.Database
{
	using System;
	using System.Collections;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Data.Common;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Threading;
	using System.Windows;

	using Ecng.Collections;
	using Ecng.Configuration;
	using Ecng.Data;
	using Ecng.Data.Providers;
	using Ecng.Data.Sql;
	using Ecng.Interop;
	using Ecng.Serialization;
	using Ecng.Common;
	using Ecng.Xaml;

	using MoreLinq;

	using StockSharp.Algo;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Strategies;
	using StockSharp.BusinessEntities;
	using StockSharp.Logging;
	using StockSharp.Messages;
	using StockSharp.Studio.Core;
	using StockSharp.Studio.Properties;
	using StockSharp.Localization;

	class StudioEntityRegistry : EntityRegistry, IStudioEntityRegistry
	{
		#region Security lists

		private abstract class BaseSecurityList<T> : BaseStorageEntityList<T>
			where T : Security
		{
			protected BaseSecurityList(IStorage storage)
				: base(storage)
			{
			}

			public override void Add(T item)
			{
				item.CheckExchange();
				base.Add(item);
			}

			public override void Update(T entity)
			{
				entity.CheckExchange();
				base.Update(entity);
			}

			public override T ReadById(object id)
			{
				var sec = base.ReadById(id);

				if (sec != null)
					sec.CheckExchange();

				return sec;
			}

			protected override IEnumerable<T> OnGetGroup(long startIndex, long count, Field orderBy, ListSortDirection direction)
			{
				var securities = base.OnGetGroup(startIndex, count, orderBy, direction);

				var logManager = ConfigManager.TryGetService<LogManager>();

				foreach (var security in securities)
				{
					if (security.Board == null)
					{
						if (logManager != null)
							logManager.Application.AddWarningLog(LocalizedStrings.Str3626Params.Put(security.Id));

						if (!security.Class.IsEmpty())
							security.Board = ExchangeBoard.GetBoard(security.Class);

						if (security.Board == null)
						{
							var parts = security.Id.Split('@');
							if (parts.Length == 2)
								security.Board = ExchangeBoard.GetBoard(parts[1]);
						}

						if (security.Board == null)
							security.Board = ExchangeBoard.Associated;
					}
				}

				return securities;
			}
		}

		private sealed class IndexSecurityList : BaseSecurityList<ExpressionIndexSecurity>
		{
			public IndexSecurityList(IStorage storage)
				: base(storage)
			{
				var idField = Schema.Fields["Id"];
				var codeField = Schema.Fields["Code"];
				var boardField = Schema.Fields["Board"];
				var expField = Schema.Fields["Expression"];

				CreateQuery = Query
					.Insert()
					.Into(Schema, expField, idField, codeField, boardField)
					.Values(expField, idField, codeField, boardField);

				UpdateQuery = Query
					.Update(Schema)
					.Set(expField)
					.Where()
					.Equals(idField);

				ReadAllQuery = Query
					.Select(expField, idField, codeField, boardField)
					.From(Schema);

				Recycle = false;
			}
		}

		private sealed class MultiEnumerator<T> : IEnumerator<T>
		{
			private readonly IEnumerator<IEnumerator<T>> _currentEnumerator;
			private bool _firstInit;

			public MultiEnumerator(params IEnumerator<T>[] enumerators)
			{
				if (enumerators == null)
					throw new ArgumentNullException("enumerators");

				if (enumerators.IsEmpty())
					throw new ArgumentOutOfRangeException("enumerators");

				_currentEnumerator = ((IEnumerable<IEnumerator<T>>)enumerators).GetEnumerator();
			}

			void IDisposable.Dispose()
			{
				_currentEnumerator.Dispose();
			}

			public bool MoveNext()
			{
				if (!_firstInit)
				{
					_firstInit = true;

					if (!_currentEnumerator.MoveNext())
						return false;
				}

				if (_currentEnumerator.Current.MoveNext())
					return true;
				
				return _currentEnumerator.MoveNext() && MoveNext();
			}

			void IEnumerator.Reset()
			{
				_currentEnumerator.Reset();
				_firstInit = false;
			}

			public T Current
			{
				get { return _currentEnumerator.Current.Current; }
			}

			object IEnumerator.Current
			{
				get { return Current; }
			}
		}

		private sealed class ContinuousSecurityList : BaseList<ContinuousSecurity>, IStorageEntityList<ContinuousSecurity>
		{
			private sealed class ContinuousSecurityId
			{
				public string ContinuousSecurity { get; set; }

				[RelationSingle(IdentityType = typeof(string))]
				public Security JumpSecurity { get; set; }

				public override string ToString()
				{
					return ContinuousSecurity + " " + (JumpSecurity != null ? JumpSecurity.ToString() : string.Empty);
				}
			}

			private sealed class ContinuousSecurityJump
			{
				[Identity]
				public ContinuousSecurityId Id { get; set; }

				public DateTimeOffset JumpDate { get; set; }
			}

			private sealed class JumpList : BaseStorageEntityList<ContinuousSecurityJump>
			{
				public JumpList(IStorage storage)
					: base(storage)
				{
					Recycle = false;
				}
			}

			private readonly JumpList _jumps;
			private readonly StudioEntityRegistry _registry;
			private readonly SyncObject _syncRoot = new SyncObject();

			public SyncObject SyncRoot { get { return _syncRoot; } }

			public ContinuousSecurityList(StudioEntityRegistry registry)
			{
				_registry = registry;

				_jumps = new JumpList(_registry.Storage) { BulkLoad = true };

				foreach (var group in _jumps.GroupBy(j => j.Id.ContinuousSecurity))
				{
					group.ForEach(s => s.Id.JumpSecurity.CheckExchange());

					var underlyingSecurity = group.First().Id.JumpSecurity;

					var cs = new ContinuousSecurity
					{
						Id = group.Key,
						Board = underlyingSecurity.Board,
						Type = underlyingSecurity.Type,
						PriceStep = underlyingSecurity.PriceStep,
						ExtensionInfo = new Dictionary<object, object>(),
					};

					cs.ExpirationJumps.AddRange(group.Select(j => new KeyValuePair<Security, DateTimeOffset>(j.Id.JumpSecurity, j.JumpDate)));

					Add(cs);
				}
			}

			public ContinuousSecurity ReadById(object id)
			{
				var res = this.FirstOrDefault(s => s.Id.CompareIgnoreCase((string)id));

				if (res != null)
					res.CheckExchange();

				return res;
			}

			protected override bool OnAdding(ContinuousSecurity item)
			{
				if (_jumps.All(j => j.Id.ContinuousSecurity != item.Id))
					Save(item);

				return base.OnAdding(item);
			}

			protected override bool OnInserting(int index, ContinuousSecurity item)
			{
				Save(item);
				return base.OnInserting(index, item);
			}

			protected override bool OnRemoving(ContinuousSecurity item)
			{
				using (var batch = _registry.Storage.BeginBatch())
				{
					_jumps.RemoveWhere(j => j.Id.ContinuousSecurity == item.Id);
					batch.Commit();
				}

				return base.OnRemoving(item);
			}

			protected override void OnAdd(ContinuousSecurity item)
			{
				if (Contains(item))
					return;

				base.OnAdd(item);
			}

			public void Save(ContinuousSecurity entity)
			{
				entity.CheckExchange();

				if (!Contains(entity))
					InnerCollection.Add(entity);

				using (var batch = _registry.Storage.BeginBatch())
				{
					_jumps.RemoveWhere(j => j.Id.ContinuousSecurity == entity.Id);
					_jumps.AddRange(entity.ExpirationJumps.Select(p => new ContinuousSecurityJump
					{
						Id = new ContinuousSecurityId
						{
							ContinuousSecurity = entity.Id,
							JumpSecurity = p.Key,
						},
						JumpDate = p.Value,
					}));

					batch.Commit();
				}
			}

			public DelayAction DelayAction { get; set; }

			IEnumerable<ContinuousSecurity> IStorageEntityList<ContinuousSecurity>.ReadLasts(int count)
			{
				throw new NotSupportedException();
			}
		}

		private sealed class StudioSecurityList : BaseSecurityList<Security>, IStorageSecurityList
		{
			private readonly StudioEntityRegistry _registry;
			private readonly DatabaseCommand _readSecurityIds;
			private readonly DatabaseCommand _readAllByUnderlyingSecurityId;

			public StudioSecurityList(StudioEntityRegistry registry)
				: base(registry.Storage)
			{
				_registry = registry;

				var database = (Database)registry.Storage;

				var readAllByUnderlyingSecurityId = Query
					.Select(Schema)
					.From(Schema)
					.Where()
						.Equals(Schema.Fields["UnderlyingSecurityId"])
						.And()
						.OpenBracket()
							.IsParamNull(Schema.Fields["ExpiryDate"])
							.Or()
							.Equals(Schema.Fields["ExpiryDate"])
						.CloseBracket();

				_readAllByUnderlyingSecurityId = database.GetCommand(readAllByUnderlyingSecurityId, Schema, new FieldList(new[] { Schema.Fields["UnderlyingSecurityId"], Schema.Fields["ExpiryDate"] }), new FieldList());

				var readSecurityIds = Query
					.Execute("SELECT group_concat(Id, ',') FROM Security");

				_readSecurityIds = database.GetCommand(readSecurityIds, null, new FieldList(), new FieldList());
			}

			IEnumerable<Security> ISecurityProvider.Lookup(Security criteria)
			{
				if (!criteria.Id.IsEmpty())
				{
					var security = ReadById(criteria.Id);
					return security == null ? Enumerable.Empty<Security>() : new[] { security };
				}

				if (criteria.UnderlyingSecurityId.IsEmpty())
					return this.Filter(criteria);

				var fields = new[]
				{
					new SerializationItem(Schema.Fields["UnderlyingSecurityId"], criteria.UnderlyingSecurityId),
					new SerializationItem(Schema.Fields["ExpiryDate"], criteria.ExpiryDate)
				};

				return Database.ReadAll<Security>(_readAllByUnderlyingSecurityId, new SerializationItemCollection(fields));
			}

			object ISecurityProvider.GetNativeId(Security security)
			{
				return null;
			}

			IEnumerable<string> ISecurityStorage.GetSecurityIds()
			{
				var str = _readSecurityIds.ExecuteScalar<string>(new SerializationItemCollection());
				return str.SplitByComma(",", true);
			}

			public override void Add(Security item)
			{
				if (item is ExpressionIndexSecurity)
				{
					_registry.IndexSecurities.Add((ExpressionIndexSecurity)item);
				}
				else if (item is ContinuousSecurity)
				{
					_registry.ContinuousSecurities.Add((ContinuousSecurity)item);
				}
				else
					base.Add(item);

				NewSecurity.SafeInvoke(item);
			}

			public override bool Remove(Security item)
			{
				if (item is ExpressionIndexSecurity)
				{
					return _registry.IndexSecurities.Remove((ExpressionIndexSecurity)item);
				}
				else if (item is ContinuousSecurity)
				{
					return _registry.ContinuousSecurities.Remove((ContinuousSecurity)item);
				}
				else
					return base.Remove(item);
			}

			public override bool Contains(Security item)
			{
				if (item is ExpressionIndexSecurity)
				{
					return _registry.IndexSecurities.Contains((ExpressionIndexSecurity)item);
				}
				else if (item is ContinuousSecurity)
				{
					return _registry.ContinuousSecurities.Contains((ContinuousSecurity)item);
				}
				else
					return base.Contains(item);
			}

			public event Action<Security> NewSecurity;

			public override void Save(Security item)
			{
				if (item is ExpressionIndexSecurity)
				{
					_registry.IndexSecurities.Save((ExpressionIndexSecurity)item);
				}
				else if (item is ContinuousSecurity)
				{
					_registry.ContinuousSecurities.Save((ContinuousSecurity)item);
				}
				else
					base.Save(item);
			}

			public override void Update(Security item)
			{
				if (item is ExpressionIndexSecurity)
				{
					_registry.IndexSecurities.Update((ExpressionIndexSecurity)item);
				}
				else if (item is ContinuousSecurity)
				{
					_registry.ContinuousSecurities.Save((ContinuousSecurity)item);
				}
				else
					base.Update(item);
			}

			public override Security ReadById(object id)
			{
				var security = base.ReadById(id);

				if (security != null)
					return security;

				security = _registry.IndexSecurities.ReadById(id);

				if (security != null)
					return security;

				return _registry.ContinuousSecurities.ReadById(id);
			}

			public override IEnumerator<Security> GetEnumerator()
			{
				return new MultiEnumerator<Security>(base.GetEnumerator(), _registry.IndexSecurities.GetEnumerator(), _registry.ContinuousSecurities.GetEnumerator());
			}

			void ISecurityStorage.Delete(Security security)
			{
				Remove(security);
			}

			void ISecurityStorage.DeleteBy(Security criteria)
			{
				this.Filter(criteria).ForEach(s => Remove(s));
			}

			void IDisposable.Dispose()
			{
			}
		}

		#endregion

		private sealed class ExchangeBoardListEx : ExchangeBoardList
		{
			private readonly DatabaseCommand _readBoardCodes;

			public ExchangeBoardListEx(IStorage storage)
				: base(storage)
			{
				var readBoardCodes = Query
					.Execute("SELECT group_concat(Code, ',') FROM ExchangeBoard");

				_readBoardCodes = ((Database)storage).GetCommand(readBoardCodes, null, new FieldList(), new FieldList());
			}

			public override IEnumerable<string> GetIds()
			{
				var str = _readBoardCodes.ExecuteScalar<string>(new SerializationItemCollection());
				return str.SplitByComma(",", true);
			}
		}

		private sealed class StrategyInfoList : BaseStorageEntityList<StrategyInfo>, IStrategyInfoList
		{
			private sealed class StrategyEvents : Disposable
			{
				//private readonly MarketDataBuffer<Security, ExecutionMessage> _buffer = new MarketDataBuffer<Security, ExecutionMessage>();

				private readonly StrategyContainer _strategy;
				private readonly SessionStrategy _sessionStrategy;
				private readonly Action _startTimer;
				private readonly IStorageRegistry _executionRegistry;

				public StrategyEvents(SessionStrategy sessionStrategy, Action startTimer)
				{
					if (sessionStrategy == null)
						throw new ArgumentNullException("sessionStrategy");

					if (startTimer == null)
						throw new ArgumentNullException("startTimer");

					_strategy = sessionStrategy.Strategy;
					_sessionStrategy = sessionStrategy;
					_startTimer = startTimer;

					_executionRegistry = _sessionStrategy.GetExecutionStorage();

					SubscribeEvents();
				}

				public bool Flush()
				{
					var hasData = false;

					//foreach (var pair in _buffer.Get())
					//{
					//	_executionRegistry.GetExecutionStorage(pair.Key, ExecutionTypes.Order).Save(pair.Value);
					//	hasData = true;
					//}

					return hasData;
				}

				private void SubscribeEvents()
				{
					_strategy.ParametersChanged += StrategyParametersChanged;

					if (_strategy.SessionType != SessionType.Battle)
						return;

					_strategy.OrderRegistering += StrategySaveOrder;
					_strategy.StopOrderRegistering += StrategySaveOrder;
					_strategy.OrderRegistered += StrategySaveOrder;
					_strategy.StopOrderRegistered += StrategySaveOrder;
					_strategy.OrderChanged += StrategySaveOrder;
					_strategy.StopOrderChanged += StrategySaveOrder;
					_strategy.OrderRegisterFailed += StrategyOrderFailed;
					_strategy.OrderCancelFailed += StrategyOrderFailed;
					_strategy.NewMyTrades += StrategyNewMyTrades; 
					_strategy.PositionManager.PositionChanged += StrategyPositionsChanged;
					_strategy.PositionManager.NewPosition += StrategyPositionsChanged;
				}

				private void UnSubscribeEvents()
				{
					_strategy.ParametersChanged -= StrategyParametersChanged;

					if (_strategy.SessionType != SessionType.Battle)
						return;

					_strategy.OrderRegistering -= StrategySaveOrder;
					_strategy.StopOrderRegistering -= StrategySaveOrder;
					_strategy.OrderRegistered -= StrategySaveOrder;
					_strategy.StopOrderRegistered -= StrategySaveOrder;
					_strategy.OrderChanged -= StrategySaveOrder;
					_strategy.StopOrderChanged -= StrategySaveOrder;
					_strategy.NewMyTrades -= StrategyNewMyTrades;
					_strategy.OrderRegisterFailed -= StrategyOrderFailed;
					_strategy.OrderCancelFailed -= StrategyOrderFailed;
					_strategy.PositionManager.PositionChanged -= StrategyPositionsChanged;
					_strategy.PositionManager.NewPosition -= StrategyPositionsChanged;
				}

				private void StrategyParametersChanged()
				{
					if (_strategy.GetIsInitialization())
						return;

					var strategySettings = _strategy.Save();

					if (_strategy.Security != null)
						strategySettings.SetValue("security", _strategy.Security.Id);

					if (_strategy.Portfolio != null)
						strategySettings.SetValue("portfolio", _strategy.Portfolio.Name);

					_sessionStrategy.Settings = strategySettings;
					_sessionStrategy.Session.Strategies.Save(_sessionStrategy);

					SaveStatistics();
				}

				private void StrategyPositionsChanged(KeyValuePair<Tuple<SecurityId, string>, decimal> position)
				{
					if (_strategy.GetIsInitialization())
						return;

					//_sessionStrategy.Positions.Save(position);
				}

				protected override void DisposeManaged()
				{
					UnSubscribeEvents();
					base.DisposeManaged();
				}

				private void StrategyNewMyTrades(IEnumerable<MyTrade> trades)
				{
					if (_strategy.GetIsInitialization())
						return;

					//foreach (var trade in trades)
					//	_buffer.Add(trade.Order.Security, trade.ToMessage());

					_startTimer();
				}

				private void StrategySaveOrder(Order order)
				{
					if (_strategy.GetIsInitialization())
						return;

					// TODO коннектор не обрабатывает сообщения без номера транзакции
					if (order.TransactionId == 0)
						return;

					// TODO заявки из события Registering пишутся с минимальной датой
					if (order.LastChangeTime.IsDefault())
						order.LastChangeTime = _strategy.CurrentTime;

					//_buffer.Add(order.Security, order.ToMessage());
					_startTimer();
				}

				void StrategyOrderFailed(OrderFail fail)
				{
					if (_strategy.GetIsInitialization())
						return;

					//_buffer.Add(fail.Order.Security, fail.ToMessage());
					_startTimer();
				}
				
				private void SaveStatistics()
				{
					if (_strategy.GetIsInitialization())
						return;

					var statistics = new SettingsStorage();

					foreach (var parameter in _strategy.StatisticManager.Parameters)
						statistics.SetValue(parameter.Name, parameter.Save());

					_sessionStrategy.Statistics = statistics;
					_sessionStrategy.Session.Strategies.Save(_sessionStrategy);
				}
			}

			private readonly LogManager _logManager = ConfigManager.GetService<LogManager>();
			//private readonly StudioEntityRegistry _parent;
			private readonly SynchronizedDictionary<Strategy, StrategyEvents> _events = new SynchronizedDictionary<Strategy, StrategyEvents>();
			private readonly HashSet<StrategyInfo> _loadedInfos = new HashSet<StrategyInfo>();
			private readonly Session _session;
			private readonly DatabaseCommand _readByType;

			private readonly SyncObject _sync = new SyncObject();
			private readonly TimeSpan _flushInterval = TimeSpan.FromSeconds(1);
			private Timer _flushTimer;
			private bool _isFlushing;

			public StrategyInfoList(StudioEntityRegistry parent, Session session)
				: base(parent.Storage)
			{
				if (session == null) 
					throw new ArgumentNullException("session");

				Recycle = false;

				//_parent = parent;
				_session = session;

				DelayAction = parent.DelayAction;

				_session.Strategies.DelayAction = parent.DelayAction;
				_session.News.DelayAction = parent.DelayAction;

				_session.Strategies.Added += SessionStrategiesAdded;

				var id = _session.RowId.ToString(CultureInfo.InvariantCulture);

				CountQuery = Query
					.Select("count(*)")
					.From(Schema)
					.Where()
					.Equals("Session", id);

				ReadAllQuery = Query
					.Select(Schema)
					.From(Schema)
					.Where()
					.Equals("Session", id);

				var readByType = Query
					.Select(Schema)
					.From(Schema)
					.Where()
					.Equals(Schema.Fields["Type"]);

				_readByType = ((Database)Storage).GetCommand(readByType, null, new FieldList(new[] { Schema.Fields["Type"] }), new FieldList());
			}

			private void SessionStrategiesAdded(SessionStrategy sessionStrategy)
			{
				sessionStrategy.Positions.DelayAction = DelayAction;

				if (sessionStrategy.Strategy == null)
				{
					sessionStrategy.Strategy = sessionStrategy.StrategyInfo.CreateStrategy(sessionStrategy.SessionType);
					sessionStrategy.Strategy.Strategy.Id = sessionStrategy.StrategyId.To<Guid>();

					sessionStrategy.StrategyInfo.Strategies.Add(sessionStrategy.Strategy);
				}

				if (_session.Type == SessionType.Battle)
				{
					ConfigManager
						.GetService<IStrategyService>()
						.InitStrategy(sessionStrategy.Strategy);

					sessionStrategy.Strategy.LoadState(sessionStrategy);	
				}

				SubscribeStrategyEvents(sessionStrategy);
			}

			public override void Add(StrategyInfo info)
			{
				info.Session = _session;
				_loadedInfos.Add(info);
				Init(info);
				base.Add(info);
			}

			private void Init(StrategyInfo info)
			{
				info.PropertyChanged += StrategyInfoPropertyChanged;

				info.Strategies.Added += s =>
				{
					var sessionStrategy = new SessionStrategy
					{
						Session = _session,
						Strategy = s,
						StrategyId = s.GetStrategyId().ToString(),
						StrategyInfo = info,
						Settings = s.Save(),
						SessionType = s.SessionType
					};

					_session.Strategies.Add(sessionStrategy);
				};

				info.Strategies.Removed += s =>
				{
					UnSubscribeStrategyEvents(s);

					var strategy = _session.Strategies.ReadByStrategyId(s.GetStrategyId());
					_session.Strategies.Remove(strategy);

					//TODO так же надо удалить заявки, сделки, позиции и т.д.
				};
			}

			protected override void OnRemove(StrategyInfo info)
			{
				info.PropertyChanged -= StrategyInfoPropertyChanged;
				info.Strategies.ForEach(UnSubscribeStrategyEvents);

				base.OnRemove(info);
			}

			public override StrategyInfo ReadById(object id)
			{
				var info = base.ReadById(id);

				if (info == null)
					return null;

				InitStrategyType(info);

				return info;
			}

			protected override IEnumerable<StrategyInfo> OnGetGroup(long startIndex, long count, Field orderBy, ListSortDirection direction)
			{
				var items = base.OnGetGroup(startIndex, count, orderBy, direction);
				InitStrategyTypes(items);
				return items;
			}

			public IEnumerable<StrategyInfo> ReadByType(StrategyInfoTypes type)
			{
				var items = Database.ReadAll<StrategyInfo>(_readByType, new SerializationItemCollection(new[] { new SerializationItem(Schema.Fields["Type"], type) }));
				InitStrategyTypes(items);
				return items;
			}

			private void InitStrategyType(StrategyInfo info)
			{
				if (!_loadedInfos.Add(info))
					return;

				try
				{
					info.InitStrategyType();

					_session
						.Strategies
						.ReadAllByStrategyInfo(info)
						.ForEach(SessionStrategiesAdded);

					Init(info);
				}
				catch (Exception ex)
				{
					_logManager.Application.AddErrorLog(LocalizedStrings.Str3627Params, info.Name, ex);
				}
			}

			private void InitStrategyTypes(IEnumerable<StrategyInfo> infos)
			{
				foreach (var info in infos)
				{
					InitStrategyType(info);
				}
			}

			private void StrategyInfoPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				Save((StrategyInfo)sender);
			}

			private void SubscribeStrategyEvents(SessionStrategy sessionStrategy)
			{
				if (!sessionStrategy.Strategy.GetIsEmulation()) 
					_logManager.Sources.Add(sessionStrategy.Strategy);

				_events.Add(sessionStrategy.Strategy, new StrategyEvents(sessionStrategy, TryStartTimer));
			}

			private void UnSubscribeStrategyEvents(Strategy strategy)
			{
				_logManager.Sources.Remove(strategy);

				var strategyEvent = _events.TryGetValue(strategy);
				if (strategyEvent == null)
					return;

				strategyEvent.Dispose();
				_events.Remove(strategy);
			}

			private void TryStartTimer()
			{
				lock (_sync)
				{
					if (!_isFlushing && _flushTimer == null)
					{
						_flushTimer = ThreadingHelper
							.Timer(OnFlush)
							.Interval(_flushInterval);
					}
				}
			}

			private void OnFlush()
			{
				try
				{
					StrategyEvents[] items;

					lock (_sync)
					{
						if (_isFlushing)
							return;

						_isFlushing = true;
						items = _events.Values.ToArray();
					}

					var hasData = items.Aggregate(false, (current, strategyEvents) => current || strategyEvents.Flush());

					if (hasData)
						return;

					if (_flushTimer == null)
						return;

					_flushTimer.Dispose();
					_flushTimer = null;
				}
				catch (Exception excp)
				{
					excp.LogError();
				}
				finally
				{
					lock (_sync)
						_isFlushing = false;
				}
			}
		}

		private readonly IStorageSecurityList _securities;
		private readonly BaseStorageEntityList<Portfolio> _portfolios;
		private readonly ExchangeBoardListEx _boards;

		public StudioEntityRegistry()
			: this(ConfigManager.GetService<IStorage>())
		{
		}

		private StudioEntityRegistry(IStorage storage)
			: base(storage)
		{
			Prepare();

			var delayAction = new DelayAction(storage, exception => exception.LogError());

			Sessions = new SessionList(Storage) { DelayAction = delayAction };

			_boards = new ExchangeBoardListEx(Storage);

			_securities = new StudioSecurityList(this) { DelayAction = delayAction };
			_portfolios = new PortfolioList(Storage) { BulkLoad = true, DelayAction = delayAction };

			//FavoriteSecurities = new FavoriteSecurityList(this) { BulkLoad = true, DelayAction = delayAction };
			IndexSecurities = new IndexSecurityList(storage) { BulkLoad = true, DelayAction = delayAction };
			ContinuousSecurities = new ContinuousSecurityList(this) { DelayAction = delayAction };

			var session = Sessions.Battle;
			if (session == null)
			{
				session = new Session
				{
					Type = SessionType.Battle,
					StartTime = DateTime.Today,
					EndTime = DateTime.MaxValue,
					Settings = new SettingsStorage(),
				};

				Sessions.Add(session);
				Sessions.DelayAction.WaitFlush();
			}

			DelayAction = delayAction;

			CreateCommonData();

			Strategies = new StrategyInfoList(this, session);
		}

		public override IStorageEntityList<ExchangeBoard> ExchangeBoards { get { return _boards; } }
		public override IStorageSecurityList Securities { get { return _securities; } }
		public override IStorageEntityList<Portfolio> Portfolios { get { return _portfolios; } }
		public override IStorageEntityList<News> News { get { return Sessions.Battle.News; } }

		private IndexSecurityList IndexSecurities { get; set; }
		private ContinuousSecurityList ContinuousSecurities { get; set; }

		public IStrategyInfoList Strategies { get; private set; }
		public SessionList Sessions { get; private set; }

		public static readonly Version LatestVersion = new Version(4, 2, 27);

		private void Prepare()
		{
			var database = Storage as Database;

			if (database == null || !(database.Provider is SQLiteDatabaseProvider))
				return;

			var conStr = new DbConnectionStringBuilder { ConnectionString = database.ConnectionString };

			var file = (string)conStr["Data Source"];

			file = file.Replace("%Documents%", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));

			file.CreateDirIfNotExists();

			var isNew = false;

			if (!File.Exists(file))
			{
				Resources.StockSharp.Save(file);
				isNew = true;
			}

			conStr["Data Source"] = file;
			database.ConnectionString = conStr.ToString();

			if (isNew)
			{
				UpdateDatabaseVersion();
				UpdateDatabaseWalMode();
			}
			else
				TryUpdateDatabaseVersion();
		}

		private void TryUpdateDatabaseVersion()
		{
			var versionField = new VoidField<string>("Version");
			var getVersionCmd = ((Database)Storage).GetCommand(Query.Select(versionField).From("Settings"), null, new FieldList(), new FieldList());

			var dbVersion = getVersionCmd.ExecuteScalar<Version>(new SerializationItemCollection());

			if (dbVersion.CompareTo(LatestVersion) == 0)
				return;

			ConfigManager
				.GetService<LogManager>()
				.Application
				.AddInfoLog(LocalizedStrings.Str3628Params.Put(dbVersion, TypeHelper.ApplicationName, LatestVersion));

			var database = (Database)Storage;
			var conStrBuilder = new DbConnectionStringBuilder { ConnectionString = database.ConnectionString };

			try
			{
				var path = (string)conStrBuilder.Cast<KeyValuePair<string, object>>().ToDictionary(StringComparer.InvariantCultureIgnoreCase).TryGetValue("Data Source");

				if (path == null)
					throw new InvalidOperationException(LocalizedStrings.Str2895);

				var targetPath = "{0}.bak.{1:yyyyMMdd}".Put(path, DateTime.Now);

				if (File.Exists(targetPath))
					File.Delete(targetPath);

				File.Move(path, targetPath);
				Resources.StockSharp.Save(path);

				UpdateDatabaseVersion();
			}
			catch (Exception ex)
			{
				ex.LogError(LocalizedStrings.Str3629Params);

				GuiDispatcher.GlobalDispatcher.AddSyncAction(() =>
				{
					new MessageBoxBuilder()
						.Text(LocalizedStrings.Str3630Params.Put(Environment.NewLine, ex.Message))
						.Warning()
						.Show();

					Application.Current.Shutdown(-1);
				});
			}
		}

		private void UpdateDatabaseVersion()
		{
			var versionField = new VoidField<string>("Version");
			var updateVersionCmd = ((Database)Storage).GetCommand(Query.Update("Settings").Set(versionField), null, new FieldList(), new FieldList(versionField));

			updateVersionCmd.ExecuteNonQuery(new SerializationItemCollection(new[] { new SerializationItem(versionField, LatestVersion.ToString()) }));
		}

		private void UpdateDatabaseWalMode()
		{
			var walQuery = Query.Execute("PRAGMA journal_mode=WAL;");
			var walCmd = ((Database)Storage).GetCommand(walQuery, null, new FieldList(), new FieldList());

			((Database)Storage).Execute(walCmd, new SerializationItemCollection(), false);
		}

		private void CreateCommonData()
		{
			var securityIds = Securities.GetSecurityIds().ToHashSet();

			var securities = new List<Security>();

			securities.AddRange(CreateSecurities("RI", securityIds));
			securities.AddRange(CreateSecurities("SR", securityIds));

			if (!securityIds.Contains("SBER@TQBR"))
			{
				securities.Add(new Security
				{
					Id = "SBER@TQBR",
					Code = "SBER",
					Board = ExchangeBoard.MicexTqbr,
					Type = SecurityTypes.Stock,
					ExtensionInfo = new Dictionary<object, object>()
				});
			}

			if (securities.Count > 0)
			{
				securities.ForEach(s => Securities.Add(s));
				Securities.DelayAction.WaitFlush();
			}

			const string pfName = "Simulator";

			if (Portfolios.ReadById(pfName) == null)
			{
				Portfolios.Save(new Portfolio
				{
					Name = pfName,
					BeginValue = 1000000,
					Board = ExchangeBoard.Test,
					ExtensionInfo = new Dictionary<object, object>()
				});
				Portfolios.DelayAction.WaitFlush();
			}
		}

		private static IEnumerable<Security> CreateSecurities(string baseCode, HashSet<string> securityIds)
		{
			return baseCode
				.GetFortsJumps(DateTime.Today.AddMonths(-4), DateTime.Today.AddMonths(6), code => new Security
				{
					Id = code + "@" + ExchangeBoard.Forts.Code,
					Code = code,
					Board = ExchangeBoard.Forts,
					Type = SecurityTypes.Future,
					PriceStep = 10,
					ExtensionInfo = new Dictionary<object, object>()
				})
				.Where(s => !securityIds.Contains(s.Id))
				.ToArray();
		}

		public IStrategyInfoList GetStrategyInfoList(Session session)
		{
			return new StrategyInfoList(this, session);
		}

		public IEnumerable<Security> GetIndexSecurities()
		{
			return IndexSecurities.ToArray();
		}

		public IEnumerable<Security> GetContinuousSecurities()
		{
			return ContinuousSecurities.ToArray();
		}
	}
}