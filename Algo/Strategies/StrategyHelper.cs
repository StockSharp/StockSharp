#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Strategies.Algo
File: StrategyHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
	using StockSharp.Algo.Strategies.Messages;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Logging;

	/// <summary>
	/// Extension class for <see cref="Strategy"/>.
	/// </summary>
	public static class StrategyHelper
	{
		/// <summary>
		/// Allow trading key.
		/// </summary>
		public const string AllowTradingKey = "AllowTrading";

		/// <summary>
		/// To create initialized object of buy order at market price.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
		/// <returns>The initialized order object.</returns>
		/// <remarks>
		/// The order is not registered, only the object is created.
		/// </remarks>
		public static Order BuyAtMarket(this Strategy strategy, decimal? volume = null)
		{
			return strategy.CreateOrder(Sides.Buy, null, volume);
		}

		/// <summary>
		/// To create the initialized order object of sell order at market price.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
		/// <returns>The initialized order object.</returns>
		/// <remarks>
		/// The order is not registered, only the object is created.
		/// </remarks>
		public static Order SellAtMarket(this Strategy strategy, decimal? volume = null)
		{
			return strategy.CreateOrder(Sides.Sell, null, volume);
		}

		/// <summary>
		/// To create the initialized order object for buy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="price">Price.</param>
		/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
		/// <returns>The initialized order object.</returns>
		/// <remarks>
		/// The order is not registered, only the object is created.
		/// </remarks>
		public static Order BuyAtLimit(this Strategy strategy, decimal price, decimal? volume = null)
		{
			return strategy.CreateOrder(Sides.Buy, price, volume);
		}

		/// <summary>
		/// To create the initialized order object for sell.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="price">Price.</param>
		/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
		/// <returns>The initialized order object.</returns>
		/// <remarks>
		/// The order is not registered, only the object is created.
		/// </remarks>
		public static Order SellAtLimit(this Strategy strategy, decimal price, decimal? volume = null)
		{
			return strategy.CreateOrder(Sides.Sell, price, volume);
		}

		/// <summary>
		/// To create the initialized order object.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="direction">Order side.</param>
		/// <param name="price">The price. If <see langword="null" /> value is passed, the order is registered at market price.</param>
		/// <param name="volume">The volume. If <see langword="null" /> value is passed, then <see cref="Strategy.Volume"/> value is used.</param>
		/// <returns>The initialized order object.</returns>
		/// <remarks>
		/// The order is not registered, only the object is created.
		/// </remarks>
		public static Order CreateOrder(this Strategy strategy, Sides direction, decimal? price, decimal? volume = null)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			var security = strategy.Security;

			if (security == null)
				throw new InvalidOperationException(LocalizedStrings.Str1403Params.Put(strategy.Name));

			var order = new Order
			{
				Portfolio = strategy.Portfolio,
				Security = strategy.Security,
				Direction = direction,
				Volume = volume ?? strategy.Volume,
			};

			if (price == null)
			{
				//if (security.Board.IsSupportMarketOrders)
				order.Type = OrderTypes.Market;
				//else
				//	order.Price = strategy.GetMarketPrice(direction) ?? 0;
			}
			else
				order.Price = price.Value;

			return order;
		}

		/// <summary>
		/// To close open position by market (to register the order of the type <see cref="OrderTypes.Market"/>).
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="slippage">The slippage level, admissible at the order registration. It is used, if the order is registered using the limit order.</param>
		/// <remarks>
		/// The market order is not operable on all exchanges.
		/// </remarks>
		public static void ClosePosition(this Strategy strategy, decimal slippage = 0)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

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

		private const string _candleManagerKey = "CandleManager";

		/// <summary>
		/// To get the candle manager, associated with the passed strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <returns>The candles manager.</returns>
		public static ICandleManager GetCandleManager(this Strategy strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			return strategy.Environment.GetValue<ICandleManager>(_candleManagerKey);
		}

		/// <summary>
		/// To set the candle manager for the strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="candleManager">The candles manager.</param>
		public static void SetCandleManager(this Strategy strategy, ICandleManager candleManager)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (candleManager == null)
				throw new ArgumentNullException(nameof(candleManager));

			strategy.Environment.SetValue(_candleManagerKey, candleManager);
		}

		private const string _messageSenderKey = "MessageSender";

		/// <summary>
		/// To get the message sender, associated with the passed strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <returns>Message sender.</returns>
		public static IMessageSender GetMessageSender(this Strategy strategy)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			return strategy.Environment.GetValue<IMessageSender>(_messageSenderKey);
		}

		/// <summary>
		/// To set the message sender for the strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="messageSender">Message sender.</param>
		public static void SetMessageSender(this Strategy strategy, IMessageSender messageSender)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (messageSender == null)
				throw new ArgumentNullException(nameof(messageSender));

			strategy.Environment.SetValue(_messageSenderKey, messageSender);
		}

		private const string _isEmulationModeKey = "IsEmulationMode";

		/// <summary>
		/// To get the strategy start-up mode (paper trading or real).
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <returns>If the paper trading mode is used - <see langword="true" />, otherwise - <see langword="false" />.</returns>
		public static bool GetIsEmulation(this Strategy strategy)
		{
			return strategy.Environment.GetValue(_isEmulationModeKey, false);
		}

		/// <summary>
		/// To get the strategy start-up mode (paper trading or real).
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="isEmulation">If the paper trading mode is used - <see langword="true" />, otherwise - <see langword="false" />.</param>
		public static void SetIsEmulation(this Strategy strategy, bool isEmulation)
		{
			strategy.Environment.SetValue(_isEmulationModeKey, isEmulation);
		}

		/// <summary>
		/// To get the strategy operation mode (initialization or trade).
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <returns>If initialization is performed - <see langword="true" />, otherwise - <see langword="false" />.</returns>
		public static bool GetAllowTrading(this Strategy strategy)
		{
			return strategy.Environment.GetValue(AllowTradingKey, false);
		}

		/// <summary>
		/// To set the strategy operation mode (initialization or trade).
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="isInitialization">If initialization is performed - <see langword="true" />, otherwise - <see langword="false" />.</param>
		public static void SetAllowTrading(this Strategy strategy, bool isInitialization)
		{
			strategy.Environment.SetValue(AllowTradingKey, isInitialization);
			strategy.RaiseParametersChanged(AllowTradingKey);
		}

		/// <summary>
		/// To restore the strategy state.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="storage">Market data storage.</param>
		/// <remarks>
		/// This method is used to load statistics, orders and trades.
		/// The data storage shall include the following parameters:
		/// 1. Settings (SettingsStorage) - statistics settings.
		/// 2. Statistics(SettingsStorage) - saved state of statistics.
		/// 3. Orders (IDictionary[Order, IEnumerable[MyTrade]]) - orders and corresponding trades.
		/// 4. Positions (IEnumerable[Position]) - strategy positions.
		/// If any of the parameters is missing, data will not be restored.
		/// </remarks>
		public static void LoadState(this Strategy strategy, SettingsStorage storage)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			var settings = storage.GetValue<SettingsStorage>("Settings");
			if (settings != null && settings.Count != 0)
			{
				var connector = strategy.Connector ?? ConfigManager.TryGetService<IConnector>();

				if (connector != null && settings.Contains("security"))
					strategy.Security = connector.LookupById(settings.GetValue<string>("security"));

				if (connector != null && settings.Contains("portfolio"))
					strategy.Portfolio = connector.LookupByPortfolioName(settings.GetValue<string>("portfolio"));

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

			var positions = storage.GetValue<IEnumerable<KeyValuePair<Tuple<SecurityId, string>, decimal>>>("Positions");
			if (positions != null)
			{
				strategy.PositionManager.Positions = positions;
			}
		}

		/// <summary>
		/// To get market data value for the strategy instrument.
		/// </summary>
		/// <typeparam name="T">The type of the market data field value.</typeparam>
		/// <param name="strategy">Strategy.</param>
		/// <param name="field">Market-data field.</param>
		/// <returns>The field value. If no data, the <see langword="null" /> will be returned.</returns>
		public static T GetSecurityValue<T>(this Strategy strategy, Level1Fields field)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			return strategy.GetSecurityValue<T>(strategy.Security, field);
		}

		///// <summary>
		///// To get market price for the instrument by maximal and minimal possible prices.
		///// </summary>
		///// <param name="strategy">Strategy.</param>
		///// <param name="side">Order side.</param>
		///// <returns>The market price. If there is no information on maximal and minimal possible prices, then <see langword="null" /> will be returned.</returns>
		//public static decimal? GetMarketPrice(this Strategy strategy, Sides side)
		//{
		//	if (strategy == null)
		//		throw new ArgumentNullException(nameof(strategy));

		//	return strategy.Security.GetMarketPrice(strategy.SafeGetConnector(), side);
		//}

		/// <summary>
		/// To get the tracing-based order identifier.
		/// </summary>
		/// <param name="order">Order.</param>
		/// <returns>The tracing-based order identifier.</returns>
		public static string GetTraceId(this Order order)
		{
			return "{0} (0x{1:X})".Put(order.TransactionId, order.GetHashCode());
		}

		private sealed class EquityStrategy : Strategy
		{
			private readonly Dictionary<DateTimeOffset, Order[]> _orders;
			private readonly Dictionary<Tuple<Security, Portfolio>, Strategy> _childStrategies;

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

				SafeGetConnector()
					.WhenTimeCome(_orders.Keys)
					.Do(time => _orders[time].ForEach(o => _childStrategies[GetKey(o)].RegisterOrder(o)))
					.Apply(this);
			}

			private static Tuple<Security, Portfolio> GetKey(Order order)
			{
				return Tuple.Create(order.Security, order.Portfolio);
			}
		}

		/// <summary>
		/// To emulate orders on history.
		/// </summary>
		/// <param name="orders">Orders to be emulated on history.</param>
		/// <param name="storageRegistry">The external storage for access to history data.</param>
		/// <param name="openedPositions">Trades, describing initial open positions.</param>
		/// <returns>The virtual strategy, containing progress of paper trades.</returns>
		public static Strategy EmulateOrders(this IEnumerable<Order> orders, IStorageRegistry storageRegistry, IDictionary<Security, decimal> openedPositions)
		{
			if (openedPositions == null)
				throw new ArgumentNullException(nameof(openedPositions));

			if (storageRegistry == null)
				throw new ArgumentNullException(nameof(storageRegistry));

			if (orders == null)
				throw new ArgumentNullException(nameof(orders));

			var array = orders.ToArray();

			if (array.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(orders));

			using (var connector = new RealTimeEmulationTrader<HistoryMessageAdapter>(new HistoryMessageAdapter(new IncrementalIdGenerator(), new CollectionSecurityProvider(array.Select(o => o.Security).Distinct()))
			{
				StorageRegistry = storageRegistry
			}))
			{
				var from = array.Min(o => o.Time);
				var to = from.EndOfDay();

				var strategy = new EquityStrategy(array, openedPositions) { Connector = connector };

				var waitHandle = new SyncObject();

				//connector.UnderlyngMarketDataAdapter.StateChanged += () =>
				//{
				//	if (connector.UnderlyngMarketDataAdapter.State == EmulationStates.Started)
				//		strategy.Start();

				//	if (connector.UnderlyngMarketDataAdapter.State == EmulationStates.Stopped)
				//	{
				//		strategy.Stop();

				//		waitHandle.Pulse();
				//	}
				//};

				connector.UnderlyngMarketDataAdapter.StartDate = from;
				connector.UnderlyngMarketDataAdapter.StopDate = to;

				connector.Connect();

				//lock (waitHandle)
				//{
				//	if (connector.UnderlyngMarketDataAdapter.State != EmulationStates.Stopped)
				//		waitHandle.Wait();
				//}

				return strategy;
			}
		}

		#region Strategy rules

		private abstract class StrategyRule<TArg> : MarketRule<Strategy, TArg>
		{
			protected StrategyRule(Strategy strategy)
				: base(strategy)
			{
				Strategy = strategy ?? throw new ArgumentNullException(nameof(strategy));
			}

			protected Strategy Strategy { get; }
		}

		private sealed class PnLManagerStrategyRule : StrategyRule<decimal>
		{
			private readonly Func<decimal, bool> _changed;

			public PnLManagerStrategyRule(Strategy strategy)
				: this(strategy, v => true)
			{
				Name = LocalizedStrings.PnLChange;
			}

			public PnLManagerStrategyRule(Strategy strategy, Func<decimal, bool> changed)
				: base(strategy)
			{
				_changed = changed ?? throw new ArgumentNullException(nameof(changed));

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
				_changed = changed ?? throw new ArgumentNullException(nameof(changed));

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

		private sealed class NewMyTradeStrategyRule : StrategyRule<MyTrade>
		{
			public NewMyTradeStrategyRule(Strategy strategy)
				: base(strategy)
			{
				Name = LocalizedStrings.Str1251 + " " + strategy;
				Strategy.NewMyTrade += OnStrategyNewMyTrade;
			}

			private void OnStrategyNewMyTrade(MyTrade trade)
			{
				Activate(trade);
			}

			protected override void DisposeManaged()
			{
				Strategy.NewMyTrade -= OnStrategyNewMyTrade;
				base.DisposeManaged();
			}
		}

		private sealed class OrderRegisteredStrategyRule : StrategyRule<Order>
		{
			public OrderRegisteredStrategyRule(Strategy strategy)
				: base(strategy)
			{
				Name = LocalizedStrings.Str1252 + " " + strategy;
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
				Name = LocalizedStrings.Str1253 + " " + strategy;
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
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

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
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

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
			private readonly bool _processChildStrategyErrors;

			public ErrorStrategyRule(Strategy strategy, bool processChildStrategyErrors)
				: base(strategy)
			{
				_processChildStrategyErrors = processChildStrategyErrors;

				Name = strategy + LocalizedStrings.Str1254;
				Strategy.Error += OnError;
			}

			private void OnError(Strategy strategy, Exception error)
			{
				if (!_processChildStrategyErrors && !Equals(Strategy, strategy))
					return;

				Activate(error);
			}

			protected override void DisposeManaged()
			{
				Strategy.Error -= OnError;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of occurrence new strategy trade.
		/// </summary>
		/// <param name="strategy">The strategy, based on which trade occurrence will be traced.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, MyTrade> WhenNewMyTrade(this Strategy strategy)
		{
			return new NewMyTradeStrategyRule(strategy);
		}

		/// <summary>
		/// To create a rule for event of occurrence of new strategy order.
		/// </summary>
		/// <param name="strategy">The strategy, based on which order occurrence will be traced.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Order> WhenOrderRegistered(this Strategy strategy)
		{
			return new OrderRegisteredStrategyRule(strategy);
		}

		/// <summary>
		/// To create a rule for event of change of any strategy order.
		/// </summary>
		/// <param name="strategy">The strategy, based on which orders change will be traced.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Order> WhenOrderChanged(this Strategy strategy)
		{
			return new OrderChangedStrategyRule(strategy);
		}

		/// <summary>
		/// To create a rule for the event of strategy position change.
		/// </summary>
		/// <param name="strategy">The strategy, based on which position change will be traced.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPositionChanged(this Strategy strategy)
		{
			return new PositionManagerStrategyRule(strategy);
		}

		/// <summary>
		/// To create a rule for event of position event reduction below the specified level.
		/// </summary>
		/// <param name="strategy">The strategy, based on which position change will be traced.</param>
		/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPositionLess(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.Position - value;

			return new PositionManagerStrategyRule(strategy, pos => pos < finishPosition)
			{
				Name = LocalizedStrings.Str1255Params.Put(finishPosition)
			};
		}

		/// <summary>
		/// To create a rule for event of position event increase above the specified level.
		/// </summary>
		/// <param name="strategy">The strategy, based on which position change will be traced.</param>
		/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPositionMore(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.Position + value;

			return new PositionManagerStrategyRule(strategy, pos => pos > finishPosition)
			{
				Name = LocalizedStrings.Str1256Params.Put(finishPosition)
			};
		}

		/// <summary>
		/// To create a rule for event of profit reduction below the specified level.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the profit change will be traced.</param>
		/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPnLLess(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.PnL - value;

			return new PnLManagerStrategyRule(strategy, pos => pos < finishPosition)
			{
				Name = LocalizedStrings.Str1257Params.Put(finishPosition)
			};
		}

		/// <summary>
		/// To create a rule for event of profit increase above the specified level.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the profit change will be traced.</param>
		/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPnLMore(this Strategy strategy, Unit value)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var finishPosition = value.Type == UnitTypes.Limit ? value : strategy.PnL + value;

			return new PnLManagerStrategyRule(strategy, pos => pos > finishPosition)
			{
				Name = LocalizedStrings.Str1258Params.Put(finishPosition)
			};
		}

		/// <summary>
		/// To create a rule for event of profit change.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the profit change will be traced.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, decimal> WhenPnLChanged(this Strategy strategy)
		{
			return new PnLManagerStrategyRule(strategy);
		}

		/// <summary>
		/// To create a rule for event of start of strategy operation.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the start of strategy operation will be expected.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Strategy> WhenStarted(this Strategy strategy)
		{
			return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Started)
			{
				Name = strategy + LocalizedStrings.Str1259,
			};
		}

		/// <summary>
		/// To create a rule for event of beginning of the strategy operation stop.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the beginning of stop will be determined.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Strategy> WhenStopping(this Strategy strategy)
		{
			return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Stopping)
			{
				Name = strategy + LocalizedStrings.Str1260,
			};
		}

		/// <summary>
		/// To create a rule for event full stop of strategy operation.
		/// </summary>
		/// <param name="strategy">The strategy, based on which the full stop will be expected.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Strategy> WhenStopped(this Strategy strategy)
		{
			return new ProcessStateChangedStrategyRule(strategy, s => s == ProcessStates.Stopped)
			{
				Name = strategy + LocalizedStrings.Str1261,
			};
		}

		/// <summary>
		/// To create a rule for event of strategy error (transition of state <see cref="Strategy.ErrorState"/> into <see cref="LogLevels.Error"/>).
		/// </summary>
		/// <param name="strategy">The strategy, based on which error will be expected.</param>
		/// <param name="processChildStrategyErrors">Process the child strategies errors.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Strategy, Exception> WhenError(this Strategy strategy, bool processChildStrategyErrors = false)
		{
			return new ErrorStrategyRule(strategy, processChildStrategyErrors);
		}

		/// <summary>
		/// To create a rule for event of strategy warning (transition of state <see cref="Strategy.ErrorState"/> into <see cref="LogLevels.Warning"/>).
		/// </summary>
		/// <param name="strategy">The strategy, based on which the warning will be expected.</param>
		/// <returns>Rule.</returns>
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
		/// To create an action, registering the order.
		/// </summary>
		/// <param name="rule">Rule.</param>
		/// <param name="order">The order to be registered.</param>
		/// <returns>Rule.</returns>
		public static IMarketRule Register(this IMarketRule rule, Order order)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			if (order == null)
				throw new ArgumentNullException(nameof(order));

			return rule.Do(() => GetRuleStrategy(rule).RegisterOrder(order));
		}

		/// <summary>
		/// To create an action, re-registering the order.
		/// </summary>
		/// <param name="rule">Rule.</param>
		/// <param name="oldOrder">The order to be re-registered.</param>
		/// <param name="newOrder">Information about new order.</param>
		/// <returns>Rule.</returns>
		public static IMarketRule ReRegister(this IMarketRule rule, Order oldOrder, Order newOrder)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			if (oldOrder == null)
				throw new ArgumentNullException(nameof(oldOrder));

			if (newOrder == null)
				throw new ArgumentNullException(nameof(newOrder));

			return rule.Do(() => GetRuleStrategy(rule).ReRegisterOrder(oldOrder, newOrder));
		}

		/// <summary>
		/// To create an action, cancelling the order.
		/// </summary>
		/// <param name="rule">Rule.</param>
		/// <param name="order">The order to be cancelled.</param>
		/// <returns>Rule.</returns>
		public static IMarketRule Cancel(this IMarketRule rule, Order order)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			if (order == null)
				throw new ArgumentNullException(nameof(order));

			return rule.Do(() => GetRuleStrategy(rule).CancelOrder(order));
		}

		#endregion

		private static Strategy GetRuleStrategy(IMarketRule rule)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			if (!(rule.Container is Strategy strategy))
				throw new ArgumentException(LocalizedStrings.Str1263Params.Put(rule.Name), nameof(rule));

			return strategy;
		}

		/// <summary>
		/// Convert <see cref="Type"/> to <see cref="StrategyTypeMessage"/>.
		/// </summary>
		/// <param name="strategyType">Strategy type.</param>
		/// <param name="transactionId">ID of the original message <see cref="StrategyLookupMessage.TransactionId"/> for which this message is a response.</param>
		/// <returns>The message contains information about strategy type.</returns>
		public static StrategyTypeMessage ToTypeMessage(this Type strategyType, long transactionId = 0)
		{
			if (strategyType == null)
				throw new ArgumentNullException(nameof(strategyType));

			return new StrategyTypeMessage
			{
				StrategyTypeId = strategyType.GUID,
				StrategyName = strategyType.Name,
				OriginalTransactionId = transactionId,
			};
		}
	}
}