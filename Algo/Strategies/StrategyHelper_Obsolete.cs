namespace StockSharp.Algo.Strategies
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Testing;
	using StockSharp.BusinessEntities;
	using StockSharp.Localization;

	static partial class StrategyHelper
	{
		private const string _candleManagerKey = "CandleManager";

		/// <summary>
		/// To get the candle manager, associated with the passed strategy.
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <returns>The candles manager.</returns>
		[Obsolete("Use Strategy direct.")]
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
		[Obsolete("Use Strategy direct.")]
		public static void SetCandleManager(this Strategy strategy, ICandleManager candleManager)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (candleManager == null)
				throw new ArgumentNullException(nameof(candleManager));

			strategy.Environment.SetValue(_candleManagerKey, candleManager);
		}

		/// <summary>
		/// Allow trading key.
		/// </summary>
		[Obsolete("Use Strategy.AllowTrading property.")]
		public const string AllowTradingKey = "AllowTrading";

		/// <summary>
		/// To get the strategy operation mode (initialization or trade).
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <returns>If initialization is performed - <see langword="true" />, otherwise - <see langword="false" />.</returns>
		[Obsolete("Use Strategy.AllowTrading property.")]
		public static bool GetAllowTrading(this Strategy strategy)
		{
			return strategy.Environment.GetValue(AllowTradingKey, false);
		}

		/// <summary>
		/// To set the strategy operation mode (initialization or trade).
		/// </summary>
		/// <param name="strategy">Strategy.</param>
		/// <param name="isInitialization">If initialization is performed - <see langword="true" />, otherwise - <see langword="false" />.</param>
		[Obsolete("Use Strategy.AllowTrading property.")]
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
		[Obsolete]
		public static void LoadState(this Strategy strategy, SettingsStorage storage)
		{
			if (strategy == null)
				throw new ArgumentNullException(nameof(strategy));

			if (storage == null)
				throw new ArgumentNullException(nameof(storage));

			var settings = storage.GetValue<SettingsStorage>("Settings");
			if (settings != null && settings.Count != 0)
			{
				var connector = strategy.Connector ?? ServicesRegistry.Connector;

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

			//var positions = storage.GetValue<IEnumerable<KeyValuePair<Tuple<SecurityId, string>, decimal>>>("Positions");
			//if (positions != null)
			//{
			//	strategy.PositionManager.Positions = positions;
			//}
		}

		[Obsolete]
		private sealed class EquityStrategy : Strategy
		{
			private readonly Dictionary<DateTimeOffset, Order[]> _orders;
			private readonly Dictionary<Tuple<Security, Portfolio>, Strategy> _childStrategies;

			public EquityStrategy(Order[] orders, IDictionary<Security, decimal> openedPositions)
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
		[Obsolete]
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

			var secProvider = new CollectionSecurityProvider(array.Select(o => o.Security).Distinct());
			using (var connector = new RealTimeEmulationTrader<HistoryMessageAdapter>(new HistoryMessageAdapter(new IncrementalIdGenerator(), secProvider)
			{
				StorageRegistry = storageRegistry
			}, secProvider))
			{
				var from = array.Min(o => o.Time);
				var to = from.EndOfDay();

				var strategy = new EquityStrategy(array, openedPositions) { Connector = connector };

				//var waitHandle = new SyncObject();

				//connector.MarketDataAdapter.StateChanged += () =>
				//{
				//	if (connector.MarketDataAdapter.State == EmulationStates.Started)
				//		strategy.Start();

				//	if (connector.MarketDataAdapter.State == EmulationStates.Stopped)
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
				//	if (connector.MarketDataAdapter.State != EmulationStates.Stopped)
				//		waitHandle.Wait();
				//}

				return strategy;
			}
		}
	}
}