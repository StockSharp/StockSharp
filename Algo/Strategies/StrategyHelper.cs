namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Вспомогательный класс для работы с классом <see cref="Strategy"/>.
	/// </summary>
	public static class StrategyHelper
	{
		/// <summary>
		/// Создать инициализированный объект заявки на покупку по рыночной цене.
		/// </summary>
		/// <remarks>
		/// Заявка не регистрируется, а только создается объект.
		/// </remarks>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="volume">Объем. Если передается значение 0, то используется значение <see cref="Strategy.Volume"/>.</param>
		/// <returns>Инициализированный объект заявки.</returns>
		public static Order BuyAtMarket(this Strategy strategy, decimal volume = 0)
		{
			return strategy.CreateOrder(Sides.Buy, 0, volume);
		}

		/// <summary>
		/// Создать инициализированный объект заявки на продажу по рыночной цене.
		/// </summary>
		/// <remarks>
		/// Заявка не регистрируется, а только создается объект.
		/// </remarks>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="volume">Объем. Если передается значение 0, то используется значение <see cref="Strategy.Volume"/>.</param>
		/// <returns>Инициализированный объект заявки.</returns>
		public static Order SellAtMarket(this Strategy strategy, decimal volume = 0)
		{
			return strategy.CreateOrder(Sides.Sell, 0, volume);
		}

		/// <summary>
		/// Создать инициализированный объект заявки на покупку.
		/// </summary>
		/// <remarks>
		/// Заявка не регистрируется, а только создается объект.
		/// </remarks>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="price">Цена.</param>
		/// <param name="volume">Объем. Если передается значение 0, то используется значение <see cref="Strategy.Volume"/>.</param>
		/// <returns>Инициализированный объект заявки.</returns>
		public static Order BuyAtLimit(this Strategy strategy, decimal price, decimal volume = 0)
		{
			return strategy.CreateOrder(Sides.Buy, price, volume);
		}

		/// <summary>
		/// Создать инициализированный объект заявки на продажу.
		/// </summary>
		/// <remarks>
		/// Заявка не регистрируется, а только создается объект.
		/// </remarks>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="price">Цена.</param>
		/// <param name="volume">Объем. Если передается значение 0, то используется значение <see cref="Strategy.Volume"/>.</param>
		/// <returns>Инициализированный объект заявки.</returns>
		public static Order SellAtLimit(this Strategy strategy, decimal price, decimal volume = 0)
		{
			return strategy.CreateOrder(Sides.Sell, price, volume);
		}

		/// <summary>
		/// Создать инициализированный объект заявки.
		/// </summary>
		/// <remarks>
		/// Заявка не регистрируется, а только создается объект.
		/// </remarks>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="direction">Направление заявки.</param>
		/// <param name="price">Цена. Если передается значение 0, то выставляется заявка по рыночной цене.</param>
		/// <param name="volume">Объем. Если передается значение 0, то используется значение <see cref="Strategy.Volume"/>.</param>
		/// <returns>Инициализированный объект заявки.</returns>
		public static Order CreateOrder(this Strategy strategy, Sides direction, decimal price, decimal volume = 0)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			var security = strategy.Security;

			if (security == null)
				throw new InvalidOperationException(LocalizedStrings.Str1403Params.Put(strategy.Name));

			var order = new Order
			{
				Portfolio = strategy.Portfolio,
				Security = strategy.Security,
				Direction = direction,
				Price = price,
				Volume = volume == 0 ? strategy.Volume : volume,
			};

			if (price != 0)
				return order;

			if (security.Board.IsSupportMarketOrders)
				order.Type = OrderTypes.Market;
			else
				order.Price = strategy.GetMarketPrice(direction) ?? 0;

			return order;
		}

		/// <summary>
		/// Закрыть открытую позицию по рынку (выставить заявку типа <see cref="OrderTypes.Market"/>).
		/// </summary>
		/// <remarks>
		/// Рыночная заявка не работает на всех биржах.
		/// </remarks>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="slippage">Уровень проскальзывания, допустимый при регистрации заявки. Используется, если заявка регистрируется лимиткой.</param>
		public static void ClosePosition(this Strategy strategy, decimal slippage = 0)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			var position = strategy.Position;

			if (position != 0)
			{
				var volume = position.Abs();

				var order = position > 0 ? strategy.SellAtMarket(volume) : strategy.BuyAtMarket(volume);

				if (order.Type != OrderTypes.Market)
				{
					order.Price += (order.Direction == Sides.Buy ? slippage : -slippage);
				}

				strategy.RegisterOrder(order);
			}
		}

		/// <summary>
		/// Получить менеджер свечек, ассоциированный с переданной стратегией.
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		/// <returns>Менеджер свечек.</returns>
		public static ICandleManager GetCandleManager(this Strategy strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			return strategy.Environment.GetValue<ICandleManager>("CandleManager");
		}

		/// <summary>
		/// Установить менеджер свечек для стратегии.
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="candleManager">Менеджер свечек.</param>
		public static void SetCandleManager(this Strategy strategy, ICandleManager candleManager)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			if (candleManager == null)
				throw new ArgumentNullException("candleManager");

			strategy.Environment.SetValue("CandleManager", candleManager);
		}

		/// <summary>
		/// Получить режим запуска стратегии (эмуляция или реал).
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		/// <returns>Если используется режим эмуляции - true, иначе - false.</returns>
		public static bool GetIsEmulation(this Strategy strategy)
		{
			return strategy.Environment.GetValue("IsEmulationMode", false);
		}

		/// <summary>
		/// Установить режим запуска стратегии (эмуляция или реал).
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="isEmulation">Если используется режим эмуляции - true, иначе - false.</param>
		public static void SetIsEmulation(this Strategy strategy, bool isEmulation)
		{
			strategy.Environment.SetValue("IsEmulationMode", isEmulation);
		}

		/// <summary>
		/// Получить режим работы стратегии (инициализация или торговля).
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		/// <returns>Если выполняется инициализация - true, иначе - false.</returns>
		public static bool GetIsInitialization(this Strategy strategy)
		{
			return strategy.Environment.GetValue("IsInitializationMode", false);
		}

		/// <summary>
		/// Установить режим работы стратегии (инициализация или торговля).
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="isInitialization">Если выполняется инициализация - true, иначе - false.</param>
		public static void SetIsInitialization(this Strategy strategy, bool isInitialization)
		{
			strategy.Environment.SetValue("IsInitializationMode", isInitialization);
		}

		/// <summary>
		/// Восстановить состояние стратегии.
		/// </summary>
		/// <remarks>
		/// Данный метод используется для загрузки статистики, заявок и сделок.
		/// 
		/// Хранилище данных должно содержать следующие параметры:
		/// 1. Settings (SettingsStorage) - настройки стратегии.
		/// 2. Statistics(SettingsStorage) - сохраненное состояние статистики.
		/// 3. Orders (IDictionary[Order, IEnumerable[MyTrade]]) - заявки и сделки по ним.
		/// 4. Positions (IEnumerable[Position]) - позиции стратегии.
		/// 
		/// При отсутствии одного из параметров соответствующие данные восстанавливаться не будут.
		/// </remarks>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="storage">Хранилище данных.</param>
		public static void LoadState(this Strategy strategy, SettingsStorage storage)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			if (storage == null)
				throw new ArgumentNullException("storage");

			var settings = storage.GetValue<SettingsStorage>("Settings");
			if (settings != null && settings.Count != 0)
			{
				var connector = strategy.Connector ?? ConfigManager.TryGetService<IConnector>();

				if (connector != null && settings.Contains("security"))
					strategy.Security = connector.LookupById(settings.GetValue<string>("security"));

				if (connector != null && settings.Contains("portfolio"))
					strategy.Portfolio = connector.Portfolios.FirstOrDefault(p => p.Name == settings.GetValue<string>("portfolio"));

				var id = strategy.Id;

				strategy.Load(settings);

				if (strategy.Id != id)
					throw new InvalidOperationException(LocalizedStrings.Str1404);
			}

			var statistics = storage.GetValue<SettingsStorage>("Statistics");
			if (statistics != null)
			{
				foreach (var parameter in strategy.StatisticManager.Parameters.Where(parameter => statistics.ContainsKey(parameter.Name)))
				{
					parameter.Load(statistics.GetValue<SettingsStorage>(parameter.Name));
				}
			}

			var orders = storage.GetValue<IDictionary<Order, IEnumerable<MyTrade>>>("Orders");
			if (orders != null)
			{
				foreach (var pair in orders)
				{
					strategy.AttachOrder(pair.Key, pair.Value);
				}
			}

			var positions = storage.GetValue<IEnumerable<Position>>("Positions");
			if (positions != null)
			{
				strategy.PositionManager.Positions = positions;
			}
		}

		/// <summary>
		/// Получить значение маркет-данных для инструмента стратегии.
		/// </summary>
		/// <typeparam name="T">Тип значения поля маркет-данных.</typeparam>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="field">Поле маркет-данных.</param>
		/// <returns>Значение поля. Если данных нет, то будет возвращено <see langword="null"/>.</returns>
		public static T GetSecurityValue<T>(this Strategy strategy, Level1Fields field)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			return strategy.GetSecurityValue<T>(strategy.Security, field);
		}

		/// <summary>
		/// Получить рыночную цену для инструмента по максимально и минимально возможным ценам.
		/// </summary>
		/// <param name="strategy">Стратегия.</param>
		/// <param name="side">Направление заявки.</param>
		/// <returns>Рыночная цена. Если нет информации о максимально и минимально возможных ценах, то будет возвращено <see langword="null"/>.</returns>
		public static decimal? GetMarketPrice(this Strategy strategy, Sides side)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			return strategy.Security.GetMarketPrice(strategy.SafeGetConnector(), side);
		}

		/// <summary>
		/// Получить трассировочный идентификатор заявки.
		/// </summary>
		/// <param name="order">Заявка.</param>
		/// <returns>Трассировочный идентификатор заявки.</returns>
		public static string GetTraceId(this Order order)
		{
			return "{0} (0x{1:X})".Put(order.TransactionId, order.GetHashCode());
		}

		private sealed class EquityStrategy : Strategy
		{
			private readonly Dictionary<DateTimeOffset, Order[]> _orders;
			private readonly Dictionary<Tuple<Security, Portfolio>, Strategy> _childStrategies = new Dictionary<Tuple<Security, Portfolio>, Strategy>();

			public EquityStrategy(IEnumerable<Order> orders, IDictionary<Security, decimal> openedPositions)
			{
				_orders = orders.GroupBy(o => o.Time).ToDictionary(g => g.Key, g => g.ToArray());

				_childStrategies = orders.ToDictionary(GetKey, o => new Strategy
				{
					Portfolio = o.Portfolio,
					Security = o.Security,
					Position = openedPositions.TryGetValue2(o.Security) ?? 0,
				});

				ChildStrategies.AddRange(_childStrategies.Values);
			}

			protected override void OnStarted()
			{
				base.OnStarted();

				Security
					.WhenTimeCome(SafeGetConnector(), _orders.Keys)
					.Do(time => _orders[time].ForEach(o => _childStrategies[GetKey(o)].RegisterOrder(o)))
					.Apply(this);
			}

			private static Tuple<Security, Portfolio> GetKey(Order order)
			{
				return Tuple.Create(order.Security, order.Portfolio);
			}
		}

		/// <summary>
		/// Сэмулировать заявки на истории.
		/// </summary>
		/// <param name="orders">Заявки, которые необходимо сэмулировать на истории.</param>
		/// <param name="storageRegistry">Внешнеее хранилище для доступа к исторических данным.</param>
		/// <param name="openedPositions">Сделки, описывающие начальные открытые позиции.</param>
		/// <returns>Виртуальная стратегии, содержащая в себе ход эмуляционных торгов.</returns>
		public static Strategy EmulateOrders(this IEnumerable<Order> orders, IStorageRegistry storageRegistry, IDictionary<Security, decimal> openedPositions)
		{
			if (openedPositions == null)
				throw new ArgumentNullException("openedPositions");

			if (storageRegistry == null)
				throw new ArgumentNullException("storageRegistry");

			if (orders == null)
				throw new ArgumentNullException("orders");

			if (orders.IsEmpty())
				throw new ArgumentOutOfRangeException("orders");

			using (var connector = new RealTimeEmulationTrader<HistoryEmulationConnector>(new HistoryEmulationConnector(orders.Select(o => o.Security).Distinct(), orders.Select(o => o.Portfolio).Distinct())
			{
				StorageRegistry = storageRegistry
			}))
			{
				var from = orders.Min(o => o.Time).Date;
				var to = from.EndOfDay();

				var strategy = new EquityStrategy(orders, openedPositions) { Connector = connector };

				var waitHandle = new SyncObject();

				connector.UnderlyingConnector.StateChanged += () =>
				{
					if (connector.UnderlyingConnector.State == EmulationStates.Started)
						strategy.Start();

					if (connector.UnderlyingConnector.State == EmulationStates.Stopped)
					{
						strategy.Stop();

						waitHandle.Pulse();
					}
				};

				connector.Connect();
				connector.StartExport();

				connector.UnderlyingConnector.Start(from, to);

				lock (waitHandle)
				{
					if (connector.UnderlyingConnector.State != EmulationStates.Stopped)
						waitHandle.Wait();
				}

				return strategy;
			}
		}

		#region Strategy rules

		private abstract class StrategyRule<TArg> : MarketRule<Strategy, TArg>
		{
			protected StrategyRule(Strategy strategy)
				: base(strategy)
			{
				if (strategy == null)
					throw new ArgumentNullException("strategy");

				Strategy = strategy;
			}

			protected Strategy Strategy { get; private set; }
		}

		private sealed class PnLManagerStrategyRule : StrategyRule<decimal>
		{
			private readonly Func<decimal, bool> _changed;

			public PnLManagerStrategyRule(Strategy strategy)
				: this(strategy, v => true)
			{
				Name = LocalizedStrings.Str1249;
			}

			public PnLManagerStrategyRule(Strategy strategy, Func<decimal, bool> changed)
				: base(strategy)
			{
				if (changed == null)
					throw new ArgumentNullException("changed");

				_changed = changed;

				Strategy.PnLChanged += OnPnLChanged;
			}

			private void OnPnLChanged()
			{
				if (_changed(Strategy.PnL))
					Activate(Strategy.PnL);
			}

			protected override void DisposeManaged()
			{
				Strategy.PnLChanged -= OnPnLChanged;
				base.DisposeManaged();
			}
		}

		private sealed class PositionManagerStrategyRule : StrategyRule<decimal>
		{
			private readonly Func<decimal, bool> _changed;

			public PositionManagerStrategyRule(Strategy strategy)
				: this(strategy, v => true)
			{
				Name = LocalizedStrings.Str1250;
			}

			public PositionManagerStrategyRule(Strategy strategy, Func<decimal, bool> changed)
				: base(strategy)
			{
				if (changed == null)
					throw new ArgumentNullException("changed");

				_changed = changed;

				Strategy.PositionChanged += OnPositionChanged;
			}

			private void OnPositionChanged()
			{
				if (_changed(Strategy.Position))
					Activate(Strategy.Position);
			}

			protected override void DisposeManaged()
			{
				Strategy.PositionChanged -= OnPositionChanged;
				base.DisposeManaged();
			}
		}

		private sealed class NewMyTradesStrategyRule : StrategyRule<IEnumerable<MyTrade>>
		{
			public NewMyTradesStrategyRule(Strategy strategy)
				: base(strategy)
			{
				Name = LocalizedStrings.Str1251 + strategy;
				Strategy.NewMyTrades += OnStrategyNewMyTrades;
			}

			private void OnStrategyNewMyTrades(IEnumerable<MyTrade> myTrades)
			{
				Activate(myTrades);
			}

			protected override void DisposeManaged()
			{
				Strategy.NewMyTrades -= OnStrategyNewMyTrades;
				base.DisposeManaged();
			}
		}

		private sealed class OrderRegisteredStrategyRule : StrategyRule<Order>
		{
			public OrderRegisteredStrategyRule(Strategy strategy)
				: base(strategy)
			{
				Name = LocalizedStrings.Str1252 + strategy;
				Strategy.OrderRegistered += Activate;
				Strategy.StopOrderRegistered += Activate;
			}

			protected override void DisposeManaged()
			{
				Strategy.OrderRegistered -= Activate;
				Strategy.StopOrderRegistered -= Activate;
				base.DisposeManaged();
			}
		}

		private sealed class OrderChangedStrategyRule : StrategyRule<Order>
		{
			public OrderChangedStrategyRule(Strategy strategy)
				: base(strategy)
			{
				Name = LocalizedStrings.Str1253 + strategy;
				Strategy.OrderChanged += Activate;
				Strategy.StopOrderChanged += Activate;
			}

			protected override void DisposeManaged()
			{
				Strategy.OrderChanged -= Activate;
				Strategy.StopOrderChanged -= Activate;
				base.DisposeManaged();
			}
		}

		private sealed class ProcessStateChangedStrategyRule : StrategyRule<Strategy>
		{
			private readonly Func<ProcessStates, bool> _condition;

			public ProcessStateChangedStrategyRule(Strategy strategy, Func<ProcessStates, bool> condition)
				: base(strategy)
			{
				if (condition == null)
					throw new ArgumentNullException("condition");

				_condition = condition;

				Strategy.ProcessStateChanged += OnProcessStateChanged;
			}

			private void OnProcessStateChanged(Strategy strategy)
			{
				if (_condition(Strategy.ProcessState))
					Activate(Strategy);
			}

			protected override void DisposeManaged()
			{
				Strategy.ProcessStateChanged -= OnProcessStateChanged;
				base.DisposeManaged();
			}
		}

		private sealed class PropertyChangedStrategyRule : StrategyRule<Strategy>
		{
			private readonly Func<Strategy, bool> _condition;

			public PropertyChangedStrategyRule(Strategy strategy, Func<Strategy, bool> condition)
				: base(strategy)
			{
				if (condition == null)
					throw new ArgumentNullException("condition");

				_condition = condition;

				Strategy.PropertyChanged += OnPropertyChanged;
			}

			private void OnPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				if (_condition(Strategy))
					Activate(Strategy);
			}

			protected override void DisposeManaged()
			{
				Strategy.PropertyChanged -= OnPropertyChanged;
				base.DisposeManaged();
			}
		}

		private sealed class ErrorStrategyRule : StrategyRule<Exception>
		{
			public ErrorStrategyRule(Strategy strategy)
				: base(strategy)
			{
				Name = strategy + LocalizedStrings.Str1254;
				Strategy.Error += OnError;
			}

			private void OnError(Exception error)
			{
				Activate(error);
			}

			protected override void DisposeManaged()
			{
				Strategy.Error -= OnError;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// Создать правило на событие появление новых сделок стратегии.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет отслеживаться появление сделок.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, IEnumerable<MyTrade>> WhenNewMyTrades(this Strategy strategy)
		{
			return new NewMyTradesStrategyRule(strategy);
		}

		/// <summary>
		/// Создать правило на событие появление новой заявки стратегии.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет отслеживаться появление заявки.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, Order> WhenOrderRegistered(this Strategy strategy)
		{
			return new OrderRegisteredStrategyRule(strategy);
		}

		/// <summary>
		/// Создать правило на событие изменения любой заявки стратегии.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет отслеживаться изменение заявок.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, Order> WhenOrderChanged(this Strategy strategy)
		{
			return new OrderChangedStrategyRule(strategy);
		}

		/// <summary>
		/// Создать правило на событие изменения позиции у стратегии.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет отслеживаться изменение позиции.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, decimal> WhenPositionChanged(this Strategy strategy)
		{
			return new PositionManagerStrategyRule(strategy);
		}

		/// <summary>
		/// Создать правило на событие уменьшения позиции у стратегии ниже определённого уровня.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет отслеживаться изменение позиции.</param>
		/// <param name="value">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, decimal> WhenPositionLess(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			if (value == null)
				throw new ArgumentNullException("value");

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.Position - value;

			return new PositionManagerStrategyRule(strategy, pos => pos < finishPosition)
			{
				Name = LocalizedStrings.Str1255Params.Put(finishPosition)
			};
		}

		/// <summary>
		/// Создать правило на событие увеличения позиции у стратегии выше определенного уровня.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет отслеживаться изменение позиции.</param>
		/// <param name="value">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, decimal> WhenPositionMore(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			if (value == null)
				throw new ArgumentNullException("value");

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.Position + value;

			return new PositionManagerStrategyRule(strategy, pos => pos > finishPosition)
			{
				Name = LocalizedStrings.Str1256Params.Put(finishPosition)
			};
		}

		/// <summary>
		/// Создать правило на событие уменьшения прибыли ниже определённого уровня.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет отслеживаться изменение прибыли.</param>
		/// <param name="value">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, decimal> WhenPnLLess(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			if (value == null)
				throw new ArgumentNullException("value");

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.PnL - value;

			return new PnLManagerStrategyRule(strategy, pos => pos < finishPosition)
			{
				Name = LocalizedStrings.Str1257Params.Put(finishPosition)
			};
		}

		/// <summary>
		/// Создать правило на событие увеличения прибыли выше определенного уровня.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет отслеживаться изменение прибыли.</param>
		/// <param name="value">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, decimal> WhenPnLMore(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException("strategy");

			if (value == null)
				throw new ArgumentNullException("value");

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.PnL + value;

			return new PnLManagerStrategyRule(strategy, pos => pos > finishPosition)
			{
				Name = LocalizedStrings.Str1258Params.Put(finishPosition)
			};
		}

		/// <summary>
		/// Создать правило на событие изменения прибыли.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет отслеживаться изменение прибыли.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, decimal> WhenPnLChanged(this Strategy strategy)
		{
			return new PnLManagerStrategyRule(strategy);
		}

		/// <summary>
		/// Создать правило на событие начала работы стратегии.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет ожидаться начало работы стратегии.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, Strategy> WhenStarted(this Strategy strategy)
		{
			return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Started)
			{
				Name = strategy + LocalizedStrings.Str1259,
			};
		}

		/// <summary>
		/// Создать правило на событие начала остановки работы стратегии.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет ожидаться начало остановки.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, Strategy> WhenStopping(this Strategy strategy)
		{
			return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Stopping)
			{
				Name = strategy + LocalizedStrings.Str1260,
			};
		}

		/// <summary>
		/// Создать правило на событие полной остановки работы стратегии.
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет ожидаться полная остановка.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, Strategy> WhenStopped(this Strategy strategy)
		{
			return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Stopped)
			{
				Name = strategy + LocalizedStrings.Str1261,
			};
		}

		/// <summary>
		/// Создать правило на событие ошибки стратегии (переход состояния <see cref="Strategy.ErrorState"/> в <see cref="LogLevels.Error"/>).
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет ожидаться ошибка.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, Exception> WhenError(this Strategy strategy)
		{
			return new ErrorStrategyRule(strategy);
		}

		/// <summary>
		/// Создать правило на событие предупреждения стратегии (переход состояния <see cref="Strategy.ErrorState"/> в <see cref="LogLevels.Warning"/>).
		/// </summary>
		/// <param name="strategy">Стратегия, по которой будет ожидаться предупреждение.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Strategy, Strategy> WhenWarning(this Strategy strategy)
		{
			return new PropertyChangedStrategyRule(strategy, s => s.ErrorState == LogLevels.Warning)
			{
				Name = strategy + LocalizedStrings.Str1262,
			};
		}

		#endregion

		#region Order actions

		/// <summary>
		/// Создать действие, регистрирующее заявку.
		/// </summary>
		/// <param name="rule">Правило.</param>
		/// <param name="order">Заявка, которую необходимо зарегистрировать.</param>
		/// <returns>Правило.</returns>
		public static IMarketRule Register(this IMarketRule rule, Order order)
		{
			if (rule == null)
				throw new ArgumentNullException("rule");

			if (order == null)
				throw new ArgumentNullException("order");

			return rule.Do(() => GetRuleStrategy(rule).RegisterOrder(order));
		}

		/// <summary>
		/// Создать действие, перерегистрирующее заявку.
		/// </summary>
		/// <param name="rule">Правило.</param>
		/// <param name="oldOrder">Заявка, которую необходимо перезарегистрировать.</param>
		/// <param name="newOrder">Информация о новой заявке.</param>
		/// <returns>Правило.</returns>
		public static IMarketRule ReRegister(this IMarketRule rule, Order oldOrder, Order newOrder)
		{
			if (rule == null)
				throw new ArgumentNullException("rule");

			if (oldOrder == null)
				throw new ArgumentNullException("oldOrder");

			if (newOrder == null)
				throw new ArgumentNullException("newOrder");

			return rule.Do(() => GetRuleStrategy(rule).ReRegisterOrder(oldOrder, newOrder));
		}

		/// <summary>
		/// Создать действие, отменяющее заявку.
		/// </summary>
		/// <param name="rule">Правило.</param>
		/// <param name="order">Заявка, которую необходимо отменить.</param>
		/// <returns>Правило.</returns>
		public static IMarketRule Cancel(this IMarketRule rule, Order order)
		{
			if (rule == null)
				throw new ArgumentNullException("rule");

			if (order == null)
				throw new ArgumentNullException("order");

			return rule.Do(() => GetRuleStrategy(rule).CancelOrder(order));
		}

		#endregion

		private static Strategy GetRuleStrategy(IMarketRule rule)
		{
			if (rule == null)
				throw new ArgumentNullException("rule");

			var strategy = rule.Container as Strategy;

			if (strategy == null)
				throw new ArgumentException(LocalizedStrings.Str1263Params.Put(rule.Name), "rule");

			return strategy;
		}
	}
}