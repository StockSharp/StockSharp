namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo.Candles;
	using StockSharp.Algo.Testing;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Вспомогательный класс для работы с торговыми правилами <see cref="IMarketRule"/>.
	/// </summary>
	public static class MarketRuleHelper
	{
		#region Order rules

		private abstract class OrderRule<TArg> : MarketRule<Order, TArg>
		{
			protected OrderRule(Order order)
				: base(order)
			{
				if (order == null)
					throw new ArgumentNullException("order");

				Order = order;

				if (order.Connector == null)
				{
					((INotifyPropertyChanged)order).PropertyChanged += OnOrderPropertyChanged;
					//throw new ArgumentException("Заявка не имеет информации о подключении.");
				}
			}

			protected override bool CanFinish()
			{
				return base.CanFinish() || CheckOrderState();
			}

			protected virtual bool CheckOrderState()
			{
				return Order.State == OrderStates.Done || Order.State == OrderStates.Failed;
			}

			private void OnOrderPropertyChanged(object sender, PropertyChangedEventArgs e)
			{
				if (TrySubscribe())
				{
					((INotifyPropertyChanged)Order).PropertyChanged -= OnOrderPropertyChanged;
				}
			}

			protected Order Order { get; private set; }

			protected bool TrySubscribe()
			{
				if (Order.Connector != null)
				{
					Subscribe();
					Container.AddRuleLog(LogLevels.Debug, this, LocalizedStrings.Str1028);
					return true;
				}

				return false;
			}

			protected abstract void Subscribe();
			protected abstract void UnSubscribe();

			protected override void DisposeManaged()
			{
				if (Order.Connector != null)
					UnSubscribe();

				base.DisposeManaged();
			}

			/// <summary>
			/// Получить строковое представление.
			/// </summary>
			/// <returns>Строковое представление.</returns>
			public override string ToString()
			{
				return "{0} {2}/{3} (0x{1:X})".Put(Name, GetHashCode(), Order.TransactionId, (Order.Id == 0 ? Order.StringId : Order.Id.To<string>()));
			}
		}

		//private sealed class RegisteredOrderRule : OrderRule<Order>
		//{
		//	public RegisteredOrderRule(Order order)
		//		: base(order)
		//	{
		//		Name = "Регистрация заявки ";
		//		TrySubscribe();
		//	}

		//	protected override void Subscribe()
		//	{
		//		if (Order.Type == OrderTypes.Conditional)
		//			Order.Trader.StopOrdersChanged += OnNewOrder;
		//		else
		//			Order.Trader.Orders += OnNewOrder;
		//	}

		//	protected override void UnSubscribe()
		//	{
		//		if (Order.Type == OrderTypes.Conditional)
		//			Order.Trader.NewStopOrders -= OnNewOrder;
		//		else
		//			Order.Trader.NewOrders -= OnNewOrder;
		//	}

		//	private void OnNewOrder(IEnumerable<Order> orders)
		//	{
		//		if (orders.Contains(Order))
		//			Activate(Order);
		//	}
		//}

		private sealed class RegisterFailedOrderRule : OrderRule<OrderFail>
		{
			public RegisterFailedOrderRule(Order order)
				: base(order)
			{
				Name = LocalizedStrings.Str2960 + " ";
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
					Order.Connector.StopOrdersRegisterFailed += OnOrdersRegisterFailed;
				else
					Order.Connector.OrdersRegisterFailed += OnOrdersRegisterFailed;
			}

			protected override void UnSubscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
					Order.Connector.StopOrdersRegisterFailed -= OnOrdersRegisterFailed;
				else
					Order.Connector.OrdersRegisterFailed -= OnOrdersRegisterFailed;
			}

			private void OnOrdersRegisterFailed(IEnumerable<OrderFail> fails)
			{
				var fail = fails.FirstOrDefault(f => f.Order == Order);
				if (fail != null)
					Activate(fail);
			}
		}

		private sealed class CancelFailedOrderRule : OrderRule<OrderFail>
		{
			public CancelFailedOrderRule(Order order)
				: base(order)
			{
				Name = LocalizedStrings.Str1030;
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
					Order.Connector.StopOrdersCancelFailed += OnOrdersCancelFailed;
				else
					Order.Connector.OrdersCancelFailed += OnOrdersCancelFailed;
			}

			protected override void UnSubscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
					Order.Connector.StopOrdersCancelFailed -= OnOrdersCancelFailed;
				else
					Order.Connector.OrdersCancelFailed -= OnOrdersCancelFailed;
			}

			private void OnOrdersCancelFailed(IEnumerable<OrderFail> fails)
			{
				var fail = fails.FirstOrDefault(f => f.Order == Order);
				if (fail != null)
					Activate(fail);
			}
		}

		private sealed class ChangedOrNewOrderRule : OrderRule<Order>
		{
			private readonly Func<Order, bool> _condition;

			public ChangedOrNewOrderRule(Order order)
				: this(order, o => true)
			{
			}

			public ChangedOrNewOrderRule(Order order, Func<Order, bool> condition)
				: base(order)
			{
				if (condition == null)
					throw new ArgumentNullException("condition");

				_condition = condition;

				Name = LocalizedStrings.Str1031;

				TrySubscribe();
			}

			protected override void Subscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
				{
					Order.Connector.StopOrdersChanged += OnOrdersChanged;
					Order.Connector.NewStopOrders += OnOrdersChanged;
				}
				else
				{
					Order.Connector.OrdersChanged += OnOrdersChanged;
					Order.Connector.NewOrders += OnOrdersChanged;
				}
			}

			protected override void UnSubscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
				{
					Order.Connector.StopOrdersChanged -= OnOrdersChanged;
					Order.Connector.NewStopOrders -= OnOrdersChanged;
				}
				else
				{
					Order.Connector.OrdersChanged -= OnOrdersChanged;
					Order.Connector.NewOrders -= OnOrdersChanged;
				}
			}

			private void OnOrdersChanged(IEnumerable<Order> orders)
			{
				var array = orders as Order[];

				if (array != null && array.Length == 1)
				{
					var order = array[0];

					if (order == Order && _condition(order))
						Activate(order);
				}
				else if (orders.Contains(Order) && _condition(Order))
				{
					Activate(Order);
				}
			}
		}

		private class NewTradesOrderRule : OrderRule<IEnumerable<MyTrade>>
		{
			private decimal _receivedVolume;

			protected bool AllTradesReceived
			{
				get
				{
					return Order.State == OrderStates.Done && (Order.Volume - Order.Balance == _receivedVolume);
				}
			}

			public NewTradesOrderRule(Order order)
				: base(order)
			{
				Name = LocalizedStrings.Str1032;
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				Order.Connector.NewMyTrades += OnNewMyTrades;
			}

			protected override void UnSubscribe()
			{
				Order.Connector.NewMyTrades -= OnNewMyTrades;
			}

			protected override bool CheckOrderState()
			{
				return Order.State == OrderStates.Failed || AllTradesReceived;
			}

			private void OnNewMyTrades(IEnumerable<MyTrade> trades)
			{
				var filteredTrades = trades
					.Where(t => t.Order == Order || Order.Type == OrderTypes.Conditional && t.Order == Order.DerivedOrder)
					.ToArray();

				if (filteredTrades.Length <= 0)
					return;
				
				_receivedVolume += filteredTrades.Sum(t => t.Trade.Volume);
				
				Activate(filteredTrades);
			}
		}

		private sealed class AllTradesOrderRule : NewTradesOrderRule
		{
			private readonly SynchronizedList<MyTrade> _trades = new SynchronizedList<MyTrade>(); 

			public AllTradesOrderRule(Order order)
				: base(order)
			{
				Name = LocalizedStrings.Str1033;
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
				{
					Order.Connector.StopOrdersChanged += OnOrdersChanged;
					Order.Connector.NewStopOrders += OnOrdersChanged;
				}
				else
				{
					Order.Connector.OrdersChanged += OnOrdersChanged;
					Order.Connector.NewOrders += OnOrdersChanged;
				}

				base.Subscribe();
			}

			protected override void UnSubscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
				{
					Order.Connector.StopOrdersChanged -= OnOrdersChanged;
					Order.Connector.NewStopOrders -= OnOrdersChanged;
				}
				else
				{
					Order.Connector.OrdersChanged -= OnOrdersChanged;
					Order.Connector.NewOrders -= OnOrdersChanged;
				}

				base.UnSubscribe();
			}

			private void OnOrdersChanged(IEnumerable<Order> orders)
			{
				if (orders.Contains(Order))
				{
					TryActivate();
				}
			}

			protected override void Activate(IEnumerable<MyTrade> trades)
			{
				_trades.AddRange(trades);
				TryActivate();
			}

			private void TryActivate()
			{
				if (AllTradesReceived)
				{
					base.Activate(_trades.ToArray());
				}
			}
		}

		/// <summary>
		/// Создать правило на событие успешной регистрации заявки на бирже.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо отслеживать на событие успешной регистрации.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Order, Order> WhenRegistered(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			return new ChangedOrNewOrderRule(order, o => o.State == OrderStates.Active) { Name = LocalizedStrings.Str1034 }.Once();
		}

		/// <summary>
		/// Создать правило на событие активации стоп-заявки.
		/// </summary>
		/// <param name="stopOrder">Стоп-заявка, которую необходимо отслеживать на событие активации.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Order, Order> WhenActivated(this Order stopOrder)
		{
			if (stopOrder == null)
				throw new ArgumentNullException("stopOrder");

			return new ChangedOrNewOrderRule(stopOrder, o => o.DerivedOrder != null) { Name = LocalizedStrings.Str1035 }.Once();
		}

		/// <summary>
		/// Создать правило на событие частичного исполнения заявки.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо отслеживать на событие частичного исполнения.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Order, Order> WhenPartiallyMatched(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			var balance = order.Volume;
			var hasVolume = balance != 0;

			return new ChangedOrNewOrderRule(order, o =>
			{
				if (!hasVolume)
				{
					balance = order.Volume;
					hasVolume = balance != 0;
				}

				var result = hasVolume && order.Balance != balance;
				balance = order.Balance;

				return result;
			})
			{
				Name = LocalizedStrings.Str1036,
			};
		}

		/// <summary>
		/// Создать правило на событие неудачной регистрации заявки на бирже.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо отслеживать на событие неудачной регистрации.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Order, OrderFail> WhenRegisterFailed(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			return new RegisterFailedOrderRule(order).Once();
		}

		/// <summary>
		/// Создать правило на событие неудачного снятия заявки на бирже.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо отслеживать на событие неудачного снятия.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Order, OrderFail> WhenCancelFailed(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			return new CancelFailedOrderRule(order);
		}

		/// <summary>
		/// Создать правило на событие отмены заявки.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо отслеживать на событие отмены.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Order, Order> WhenCanceled(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			return new ChangedOrNewOrderRule(order, o => o.IsCanceled()) { Name = LocalizedStrings.Str1037 }.Once();
		}

		/// <summary>
		/// Создать правило на событие полного исполнения заявки.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо отслеживать на событие полного исполнения.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Order, Order> WhenMatched(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			return new ChangedOrNewOrderRule(order, o => o.IsMatched()) { Name = LocalizedStrings.Str1038 }.Once();
		}

		/// <summary>
		/// Создать правило на событие изменения заявки.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо отслеживать на событие изменения.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Order, Order> WhenChanged(this Order order)
		{
			return new ChangedOrNewOrderRule(order);
		}

		/// <summary>
		/// Создать правило на событие появления сделок по заявке.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо отслеживать на событие появления сделок.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Order, IEnumerable<MyTrade>> WhenNewTrades(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			return new NewTradesOrderRule(order);
		}

		/// <summary>
		/// Создать правило на событие появления всех сделок по заявке.
		/// </summary>
		/// <param name="order">Заявка, которую необходимо отслеживать на событие появления всех сделок.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Order, IEnumerable<MyTrade>> WhenAllTrades(this Order order)
		{
			if (order == null)
				throw new ArgumentNullException("order");

			return new AllTradesOrderRule(order);
		}

		#endregion

		#region Portfolio rules

		private sealed class PortfolioRule : MarketRule<Portfolio, Portfolio>
		{
			private readonly Func<Portfolio, bool> _changed;
			private readonly Portfolio _portfolio;

			public PortfolioRule(Portfolio portfolio, Func<Portfolio, bool> changed)
				: base(portfolio)
			{
				if (portfolio == null)
					throw new ArgumentNullException("portfolio");

				if (changed == null)
					throw new ArgumentNullException("changed");

				if (portfolio.Connector == null)
					throw new ArgumentException(LocalizedStrings.Str1039);

				_changed = changed;

				_portfolio = portfolio;
				_portfolio.Connector.PortfoliosChanged += OnPortfoliosChanged;
			}

			private void OnPortfoliosChanged(IEnumerable<Portfolio> portfolios)
			{
				if (portfolios.Contains(_portfolio) && _changed(_portfolio))
					Activate(_portfolio);
			}

			protected override void DisposeManaged()
			{
				_portfolio.Connector.PortfoliosChanged -= OnPortfoliosChanged;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// Создать правило на событие уменьшения денег в портфеле ниже определённого уровня.
		/// </summary>
		/// <param name="portfolio">Портфель, который необходимо отслеживать на событие уменьшении денег ниже определённого уровня.</param>
		/// <param name="money">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Portfolio, Portfolio> WhenMoneyLess(this Portfolio portfolio, Unit money)
		{
			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			if (money == null)
				throw new ArgumentNullException("money");

			var finishMoney = money.Type == UnitTypes.Limit ? money : portfolio.CurrentValue - money;

			return new PortfolioRule(portfolio, pf => pf.CurrentValue < finishMoney)
			{
				Name = LocalizedStrings.Str1040Params.Put(portfolio, finishMoney)
			};
		}

		/// <summary>
		/// Создать правило на событие увеличения денег в портфеле выше определённого уровня.
		/// </summary>
		/// <param name="portfolio">Портфель, который необходимо отслеживать на событие увеличения денег выше определённого уровня.</param>
		/// <param name="money">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Portfolio, Portfolio> WhenMoneyMore(this Portfolio portfolio, Unit money)
		{
			if (portfolio == null)
				throw new ArgumentNullException("portfolio");

			if (money == null)
				throw new ArgumentNullException("money");

			var finishMoney = money.Type == UnitTypes.Limit ? money : portfolio.CurrentValue + money;

			return new PortfolioRule(portfolio, pf => pf.CurrentValue > finishMoney)
			{
				Name = LocalizedStrings.Str1041Params.Put(portfolio, finishMoney)
			};
		}

		#endregion

		#region Position rules

		private sealed class PositionRule : MarketRule<Position, Position>
		{
			private readonly Func<Position, bool> _changed;
			private readonly Position _position;

			public PositionRule(Position position)
				: this(position, p => true)
			{
				Name = LocalizedStrings.Str1042 + position.Portfolio.Name;
			}

			public PositionRule(Position position, Func<Position, bool> changed)
				: base(position)
			{
				if (position == null)
					throw new ArgumentNullException("position");

				if (changed == null)
					throw new ArgumentNullException("changed");

				if (position.Portfolio.Connector == null)
					throw new ArgumentException(LocalizedStrings.Str1043);

				_changed = changed;

				_position = position;
				_position.Portfolio.Connector.PositionsChanged += OnPositionsChanged;
			}

			private void OnPositionsChanged(IEnumerable<Position> positions)
			{
				if (positions.Contains(_position) && _changed(_position))
					Activate(_position);
			}

			protected override void DisposeManaged()
			{
				_position.Portfolio.Connector.PositionsChanged -= OnPositionsChanged;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// Создать правило на событие уменьшения позиции ниже определённого уровня.
		/// </summary>
		/// <param name="position">Позиция, которую необходимо отслеживать на событие уменьшения ниже определенного уровня.</param>
		/// <param name="value">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Position, Position> WhenLess(this Position position, Unit value)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			if (value == null)
				throw new ArgumentNullException("value");

			var finishPosition = value.Type == UnitTypes.Limit ? value : position.CurrentValue - value;

			return new PositionRule(position, pf => pf.CurrentValue < finishPosition)
			{
				Name = LocalizedStrings.Str1044Params.Put(position, finishPosition)
			};
		}

		/// <summary>
		/// Создать правило на событие увеличения позиции выше определенного уровня.
		/// </summary>
		/// <param name="position">Позиция, которую необходимо отслеживать на событие увеличения выше определенного уровня.</param>
		/// <param name="value">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Position, Position> WhenMore(this Position position, Unit value)
		{
			if (position == null)
				throw new ArgumentNullException("position");

			if (value == null)
				throw new ArgumentNullException("value");

			var finishPosition = value.Type == UnitTypes.Limit ? value : position.CurrentValue + value;

			return new PositionRule(position, pf => pf.CurrentValue > finishPosition)
			{
				Name = LocalizedStrings.Str1045Params.Put(position, finishPosition)
			};
		}

		/// <summary>
		/// Создать правило на событие изменения позиции.
		/// </summary>
		/// <param name="position">Позиция, которую необходимо отслеживать на событие изменения.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Position, Position> Changed(this Position position)
		{
			return new PositionRule(position);
		}

		#endregion

		#region Security rules

		private abstract class SecurityRule<TArg> : MarketRule<Security, TArg>
		{
			protected SecurityRule(Security security, IConnector connector)
				: base(security)
			{
				if (security == null)
					throw new ArgumentNullException("security");

				if (connector == null)
					throw new ArgumentNullException("connector");

				Security = security;
				Connector = connector;
			}

			protected Security Security { get; private set; }
			protected IConnector Connector { get; private set; }
		}

		private sealed class SecurityChangedRule : SecurityRule<Security>
		{
			private readonly Func<Security, bool> _condition;

			public SecurityChangedRule(Security security, IConnector connector)
				: this(security, connector, s => true)
			{
			}

			public SecurityChangedRule(Security security, IConnector connector, Func<Security, bool> condition)
				: base(security, connector)
			{
				if (condition == null)
					throw new ArgumentNullException("condition");

				_condition = condition;

				Name = LocalizedStrings.Str1046 + security;
				Connector.SecuritiesChanged += OnSecuritiesChanged;
			}

			private void OnSecuritiesChanged(IEnumerable<Security> securities)
			{
				var basket = Security as BasketSecurity;

				if (basket != null)
				{
					var basketSecurities = securities
						.Where(s => basket.Contains(s) && _condition(s))
						.ToArray();

					if (basketSecurities.Length > 0)
						Activate(basketSecurities[0]);
				}
				else
				{
					if (securities.Contains(Security) && _condition(Security))
						Activate(Security);
				}
			}

			protected override void DisposeManaged()
			{
				Connector.SecuritiesChanged -= OnSecuritiesChanged;
				base.DisposeManaged();
			}
		}

		private sealed class SecurityNewTradesRule : SecurityRule<IEnumerable<Trade>>
		{
			public SecurityNewTradesRule(Security security, IConnector connector)
				: base(security, connector)
			{
				Name = LocalizedStrings.Str1047 + security;
				Connector.NewTrades += OnNewTrades;
			}

			private void OnNewTrades(IEnumerable<Trade> trades)
			{
				var sec = Security;

				var basket = sec as BasketSecurity;

				if (Connector is IEmulationConnector)
				{
					// в рилтайме сделки приходят гарантированно по одной. см. BaseTrader.GetTrade
					// в эмуляции сделки приходят кучками, но все для одного и того же интсрумента. см. EmuTrader.Message

					// mika: для Квика утверждение не справедливо

					var t = trades.FirstOrDefault();

					if (!ReferenceEquals(t, null) &&
					    (!ReferenceEquals(basket, null)
					     	? basket.Contains(t.Security)
					     	: ReferenceEquals(sec, t.Security)))
						Activate(trades);
				}
				else
				{
					var securityTrades = (basket != null
					                      	? trades.Where(t => basket.Contains(t.Security))
					                      	: trades.Where(t => t.Security == sec)).ToArray();

					if (securityTrades.Length > 0)
						Activate(securityTrades);
				}
			}

			protected override void DisposeManaged()
			{
				Connector.NewTrades -= OnNewTrades;
				base.DisposeManaged();
			}
		}

		private sealed class SecurityNewOrderLogItems : SecurityRule<IEnumerable<OrderLogItem>>
		{
			public SecurityNewOrderLogItems(Security security, IConnector connector)
				: base(security, connector)
			{
				Name = LocalizedStrings.Str1048 + security;
				Connector.NewOrderLogItems += OnNewOrderLogItems;
			}

			private void OnNewOrderLogItems(IEnumerable<OrderLogItem> items)
			{
				var sec = Security;

				var basket = sec as BasketSecurity;

				var securityLogItems = (basket != null
										? items.Where(i => basket.Contains(i.Order.Security))
										: items.Where(i => i.Order.Security == sec)).ToArray();

				if (securityLogItems.Length > 0)
					Activate(securityLogItems);
			}

			protected override void DisposeManaged()
			{
				Connector.NewOrderLogItems -= OnNewOrderLogItems;
				base.DisposeManaged();
			}
		}

		private sealed class SecurityLastTradeRule : SecurityRule<Security>
		{
			private readonly Func<Security, bool> _condition;

			public SecurityLastTradeRule(Security security, IConnector connector, Func<Security, bool> condition)
				: base(security, connector)
			{
				if (condition == null)
					throw new ArgumentNullException("condition");

				_condition = condition;

				Name = LocalizedStrings.Str1049 + security;

				Connector.SecuritiesChanged += OnSecuritiesChanged;
				Connector.NewTrades += OnNewTrades;
			}

			private void OnSecuritiesChanged(IEnumerable<Security> securities)
			{
				var security = CheckLastTrade(securities);

				if (security != null)
					Activate(security);
			}

			private Security CheckLastTrade(IEnumerable<Security> securities)
			{
				var basket = Security as BasketSecurity;
				if (basket != null)
				{
					return securities.FirstOrDefault(sec => basket.Contains(sec) && _condition(sec));
				}
				else
				{
					if (securities.Contains(Security) && _condition(Security))
						return Security;
				}

				return null;
			}

			private void OnNewTrades(IEnumerable<Trade> trades)
			{
				var trade = CheckTrades(Security, trades);

				if (trade != null)
					Activate(trade.Security);
			}

			private Trade CheckTrades(Security security, IEnumerable<Trade> trades)
			{
				var basket = security as BasketSecurity;

				return basket != null
					? trades.FirstOrDefault(t => basket.Contains(t.Security) && _condition(t.Security))
					: trades.FirstOrDefault(t => t.Security == security && _condition(t.Security));
			}

			protected override void DisposeManaged()
			{
				Connector.NewTrades -= OnNewTrades;
				Connector.SecuritiesChanged -= OnSecuritiesChanged;

				base.DisposeManaged();
			}
		}

		private sealed class SecurityMarketDepthChangedRule : SecurityRule<MarketDepth>
		{
			public SecurityMarketDepthChangedRule(Security security, IConnector connector)
				: base(security, connector)
			{
				Name = LocalizedStrings.Str1050 + security;
				Connector.MarketDepthsChanged += OnMarketDepthsChanged;
			}

			private void OnMarketDepthsChanged(IEnumerable<MarketDepth> depths)
			{
				var depth = depths.FirstOrDefault(d => d.Security == Security);

				if (depth != null)
					Activate(depth);
			}

			protected override void DisposeManaged()
			{
				Connector.MarketDepthsChanged -= OnMarketDepthsChanged;
				base.DisposeManaged();
			}
		}

		private sealed class BasketSecurityMarketDepthChangedRule : SecurityRule<IEnumerable<MarketDepth>>
		{
			public BasketSecurityMarketDepthChangedRule(BasketSecurity security, IConnector connector)
				: base(security, connector)
			{
				Name = LocalizedStrings.Str1050 + security;
				Connector.MarketDepthsChanged += OnMarketDepthsChanged;
			}

			private void OnMarketDepthsChanged(IEnumerable<MarketDepth> depths)
			{
				var basketDepths = CheckDepths(Security, depths).ToArray();

				if (!basketDepths.IsEmpty())
					Activate(basketDepths);
			}

			private static IEnumerable<MarketDepth> CheckDepths(Security security, IEnumerable<MarketDepth> depths)
			{
				var basket = security as BasketSecurity;

				return basket != null 
					? depths.Where(d => basket.Contains(d.Security)) 
					: depths.Where(d => d.Security == security);
			}

			protected override void DisposeManaged()
			{
				Connector.MarketDepthsChanged -= OnMarketDepthsChanged;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// Создать правило на событие изменения инструмента.
		/// </summary>
		/// <param name="security">Инструмент, изменения которого будут отслеживаться.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, Security> WhenChanged(this Security security, IConnector connector)
		{
			return new SecurityChangedRule(security, connector);
		}

		/// <summary>
		/// Создать правило на событие появления у инструмента новой сделки.
		/// </summary>
		/// <param name="security">Инструмент, который необходимо отслеживать на событие появления новой сделки.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, IEnumerable<Trade>> WhenNewTrades(this Security security, IConnector connector)
		{
			return new SecurityNewTradesRule(security, connector);
		}

		/// <summary>
		/// Создать правило на событие появления у инструмента новых записей в логе заявок.
		/// </summary>
		/// <param name="security">Инструмент, который необходимо отслеживать на событие появления новых записей в логе заявок.</param>		
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, IEnumerable<OrderLogItem>> WhenNewOrderLogItems(this Security security, IConnector connector)
		{
			return new SecurityNewOrderLogItems(security, connector);
		}

		/// <summary>
		/// Создать правило на событие изменения стакана по инструменту.
		/// </summary>
		/// <param name="security">Инструмент, который необходимо отслеживать на событие изменения стакана по инструменту.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, MarketDepth> WhenMarketDepthChanged(this Security security, IConnector connector)
		{
			return new SecurityMarketDepthChangedRule(security, connector);
		}

		/// <summary>
		/// Создать правило на событие изменения стаканов по корзине инструментов.
		/// </summary>
		/// <param name="security">Корзина инструментов, которую необходимо отслеживать на событие изменения стаканов по внутренним инструментам.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, IEnumerable<MarketDepth>> WhenMarketDepthChanged(this BasketSecurity security, IConnector connector)
		{
			return new BasketSecurityMarketDepthChangedRule(security, connector);
		}

		/// <summary>
		/// Создать правило на событие превышения лучшего бида определенного уровня.
		/// </summary>
		/// <param name="security">Инструмент, который необходимо отслеживать на событие превышения лучшего бида определенного уровня.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="price">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, Security> WhenBestBidPriceMore(this Security security, IConnector connector, Unit price)
		{
			return CreateSecurityCondition(security, connector, Level1Fields.BestBidPrice, price, false);
		}

		/// <summary>
		/// Создать правило на событие понижения лучшего бида ниже определенного уровня.
		/// </summary>
		/// <param name="security">Инструмент, который необходимо отслеживать на событие понижения лучшего бида ниже определенного уровня.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="price">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, Security> WhenBestBidPriceLess(this Security security, IConnector connector, Unit price)
		{
			return CreateSecurityCondition(security, connector, Level1Fields.BestBidPrice, price, true);
		}

		/// <summary>
		/// Создать правило на событие превышения лучшего оффера определенного уровня.
		/// </summary>
		/// <param name="security">Инструмент, который необходимо отслеживать на событие превышения лучшего оффера определенного уровня.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="price">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, Security> WhenBestAskPriceMore(this Security security, IConnector connector, Unit price)
		{
			return CreateSecurityCondition(security, connector, Level1Fields.BestAskPrice, price, false);
		}

		/// <summary>
		/// Создать правило на событие понижения лучшего оффера ниже определенного уровня.
		/// </summary>
		/// <param name="security">Инструмент, который необходимо отслеживать на событие понижения лучшего оффера ниже определенного уровня.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="price">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, Security> WhenBestAskPriceLess(this Security security, IConnector connector, Unit price)
		{
			return CreateSecurityCondition(security, connector, Level1Fields.BestAskPrice, price, true);
		}

		/// <summary>
		/// Создать правило на событие повышения цены последней сделки выше определённого уровня.
		/// </summary>
		/// <param name="security">Инструмент, который необходимо отслеживать на событие повышения цены последней сделки выше определённого уровня.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="provider">Поставщик маркет-данных.</param>
		/// <param name="price">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, Security> WhenLastTradePriceMore(this Security security, IConnector connector, IMarketDataProvider provider, Unit price)
		{
			return CreateLastTradeCondition(security, connector, provider, price, false);
		}

		/// <summary>
		/// Создать правило на событие понижения цены последней сделки ниже определённого уровня.
		/// </summary>
		/// <param name="security">Инструмент, который необходимо отслеживать на событие понижения цены последней сделки ниже определённого уровня.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="provider">Поставщик маркет-данных.</param>
		/// <param name="price">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, Security> WhenLastTradePriceLess(this Security security, IConnector connector, IMarketDataProvider provider, Unit price)
		{
			return CreateLastTradeCondition(security, connector, provider, price, true);
		}

		private static SecurityChangedRule CreateSecurityCondition(Security security, IConnector connector, Level1Fields field, Unit offset, bool isLess)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (offset == null)
				throw new ArgumentNullException("offset");

			if (offset.Value == 0)
				throw new ArgumentException(LocalizedStrings.Str1051, "offset");

			if (offset.Value < 0)
				throw new ArgumentException(LocalizedStrings.Str1052, "offset");

			var price = (decimal?)connector.GetSecurityValue(security, field);

			if (price == null && offset.Type != UnitTypes.Limit)
				throw new InvalidOperationException(LocalizedStrings.Str1053);

			if (isLess)
			{
				var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price - offset);
				return new SecurityChangedRule(security, connector, s =>
				{
					var quote = (decimal?)connector.GetSecurityValue(s, field);
					return quote != null && quote < finishPrice;
				});
			}
			else
			{
				var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price + offset);
				return new SecurityChangedRule(security, connector, s =>
				{
					var quote = (decimal?)connector.GetSecurityValue(s, field);
					return quote != null && quote > finishPrice;
				});
			}
		}

		private static SecurityLastTradeRule CreateLastTradeCondition(Security security, IConnector connector, IMarketDataProvider provider, Unit offset, bool isLess)
		{
			if (security == null)
				throw new ArgumentNullException("security");

			if (provider == null)
				throw new ArgumentNullException("provider");

			if (offset == null)
				throw new ArgumentNullException("offset");

			if (offset.Value == 0)
				throw new ArgumentException(LocalizedStrings.Str1051, "offset");

			if (offset.Value < 0)
				throw new ArgumentException(LocalizedStrings.Str1052, "offset");

			var price = (decimal?)provider.GetSecurityValue(security, Level1Fields.LastTradePrice);

			if (price == null && offset.Type != UnitTypes.Limit)
				throw new ArgumentException(LocalizedStrings.Str1054, "security");

			if (isLess)
			{
				var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price - offset);
				return new SecurityLastTradeRule(security, connector, s => (decimal?)provider.GetSecurityValue(s, Level1Fields.LastTradePrice) < finishPrice);
			}
			else
			{
				var finishPrice = (decimal)(offset.Type == UnitTypes.Limit ? offset : price + offset);
				return new SecurityLastTradeRule(security, connector, s => (decimal?)provider.GetSecurityValue(s, Level1Fields.LastTradePrice) > finishPrice);
			}
		}

		private sealed class SecurityMarketTimeRule : SecurityRule<DateTimeOffset>
		{
			private readonly MarketTimer _timer;

			public SecurityMarketTimeRule(Security security, IConnector connector, IEnumerable<DateTimeOffset> times)
				: base(security, connector)
			{
				if (times == null)
					throw new ArgumentNullException("times");

				var currentTime = connector.CurrentTime;

				var intervals = new SynchronizedQueue<TimeSpan>();
				var timesList = new SynchronizedList<DateTimeOffset>();

				foreach (var time in times)
				{
					var interval = time - currentTime;

					if (interval <= TimeSpan.Zero)
						continue;

					intervals.Enqueue(interval);
					currentTime = time;
					timesList.Add(time);
				}

				// все даты устарели
				if (timesList.IsEmpty())
					return;

				Name = LocalizedStrings.Str1055;

				var index = 0;

				_timer = new MarketTimer(connector, () =>
				{
					var activateTime = timesList[index++];

					Activate(activateTime);

					if (index == timesList.Count)
					{
						_timer.Stop();
					}
					else
					{
						_timer.Interval(intervals.Dequeue());
					}
				})
				.Interval(intervals.Dequeue())
				.Start();
			}

			protected override bool CanFinish()
			{
				return _timer == null || base.CanFinish();
			}

			protected override void DisposeManaged()
			{
				if (_timer != null)
					_timer.Dispose();

				base.DisposeManaged();
			}
		}

		/// <summary>
		/// Создать правило, которое активизируется при наступлении точного времени, указанного через <paramref name="times"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="times">Точное время. Может быть передано несколько значений.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, DateTimeOffset> WhenTimeCome(this Security security, IConnector connector, params DateTimeOffset[] times)
		{
			return security.WhenTimeCome(connector, (IEnumerable<DateTimeOffset>)times);
		}

		/// <summary>
		/// Создать правило, которое активизируется при наступлении точного времени, указанного через <paramref name="times"/>.
		/// </summary>
		/// <param name="security">Инструмент.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="times">Точное время. Может быть передано несколько значений.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<Security, DateTimeOffset> WhenTimeCome(this Security security, IConnector connector, IEnumerable<DateTimeOffset> times)
		{
			return new SecurityMarketTimeRule(security, connector, times);
		}

		#endregion

		#region MarketDepth rules

		private abstract class MarketDepthRule : MarketRule<MarketDepth, MarketDepth>
		{
			protected MarketDepthRule(MarketDepth depth)
				: base(depth)
			{
				if (depth == null)
					throw new ArgumentNullException("depth");

				Depth = depth;
			}

			protected MarketDepth Depth { get; private set; }
		}

		private sealed class MarketDepthChangedRule : MarketDepthRule
		{
			private readonly Func<MarketDepth, bool> _condition;

			public MarketDepthChangedRule(MarketDepth depth)
				: this(depth, d => true)
			{
			}

			public MarketDepthChangedRule(MarketDepth depth, Func<MarketDepth, bool> condition)
				: base(depth)
			{
				if (condition == null)
					throw new ArgumentNullException("condition");

				_condition = condition;

				Name = LocalizedStrings.Str1056 + depth.Security;
				Depth.QuotesChanged += OnQuotesChanged;
			}

			private void OnQuotesChanged()
			{
				if (_condition(Depth))
					Activate(Depth);
			}

			protected override void DisposeManaged()
			{
				Depth.QuotesChanged -= OnQuotesChanged;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// Создать правило на событие изменения стакана.
		/// </summary>
		/// <param name="depth">Стакан, который необходимо отслеживать на событие изменения.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenChanged(this MarketDepth depth)
		{
			return new MarketDepthChangedRule(depth);
		}

		/// <summary>
		/// Создать правило на событие повышения размера спреда стакана на определенную величину.
		/// </summary>
		/// <param name="depth">Стакан, который необходимо отслеживать на событие изменения спреда.</param>
		/// <param name="price">Величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenSpreadMore(this MarketDepth depth, Unit price)
		{
			var pair = depth.BestPair;
			var firstPrice = (pair == null ? null : pair.SpreadPrice) ?? 0;
			return new MarketDepthChangedRule(depth, d => d.BestPair != null && d.BestPair.SpreadPrice > (firstPrice + price))
			{
				Name = LocalizedStrings.Str1057Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// Создать правило на событие понижения размера спреда стакана на определенную величину.
		/// </summary>
		/// <param name="depth">Стакан, который необходимо отслеживать на событие изменения спреда.</param>
		/// <param name="price">Величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenSpreadLess(this MarketDepth depth, Unit price)
		{
			var pair = depth.BestPair;
			var firstPrice = (pair == null ? null : pair.SpreadPrice) ?? 0;
			return new MarketDepthChangedRule(depth, d => d.BestPair != null && d.BestPair.SpreadPrice < (firstPrice - price))
			{
				Name = LocalizedStrings.Str1058Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// Создать правило на событие повышения лучшего бида на определенную величину.
		/// </summary>
		/// <param name="depth">Стакан, который необходимо отслеживать на событие повышения лучшего бида на определенную величину.</param>
		/// <param name="price">Величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenBestBidPriceMore(this MarketDepth depth, Unit price)
		{
			return new MarketDepthChangedRule(depth, CreateDepthCondition(price, () => depth.BestBid, false))
			{
				Name = LocalizedStrings.Str1059Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// Создать правило на событие понижения лучшего бида на определенную величину.
		/// </summary>
		/// <param name="depth">Стакан, который необходимо отслеживать на событие понижения лучшего бида на определенную величину.</param>
		/// <param name="price">Величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenBestBidPriceLess(this MarketDepth depth, Unit price)
		{
			return new MarketDepthChangedRule(depth, CreateDepthCondition(price, () => depth.BestBid, true))
			{
				Name = LocalizedStrings.Str1060Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// Создать правило на событие повышения лучшего оффера на определенную величину.
		/// </summary>
		/// <param name="depth">Стакан, который необходимо отслеживать на событие повышения лучшего оффера на определенную величину.</param>
		/// <param name="price">Величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenBestAskPriceMore(this MarketDepth depth, Unit price)
		{
			return new MarketDepthChangedRule(depth, CreateDepthCondition(price, () => depth.BestAsk, false))
			{
				Name = LocalizedStrings.Str1061Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// Создать правило на событие понижения лучшего оффера на определенную величину.
		/// </summary>
		/// <param name="depth">Стакан, который необходимо отслеживать на событие понижения лучшего оффера на определенную величину.</param>
		/// <param name="price">Величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenBestAskPriceLess(this MarketDepth depth, Unit price)
		{
			return new MarketDepthChangedRule(depth, CreateDepthCondition(price, () => depth.BestAsk, true))
			{
				Name = LocalizedStrings.Str1062Params.Put(depth.Security, price)
			};
		}

		private static Func<MarketDepth, bool> CreateDepthCondition(Unit price, Func<Quote> currentQuote, bool isLess)
		{
			if (price == null)
				throw new ArgumentNullException("price");

			if (currentQuote == null)
				throw new ArgumentNullException("currentQuote");

			if (price.Value == 0)
				throw new ArgumentException(LocalizedStrings.Str1051, "price");

			if (price.Value < 0)
				throw new ArgumentException(LocalizedStrings.Str1052, "price");

			var curQuote = currentQuote();
			if (curQuote == null)
				throw new ArgumentException(LocalizedStrings.Str1063, "currentQuote");

			if (isLess)
			{
				var finishPrice = (decimal)(curQuote.Price - price);
				return depth =>
				{
					var quote = currentQuote();
					return quote != null && quote.Price < finishPrice;
				};
			}
			else
			{
				var finishPrice = (decimal)(curQuote.Price + price);
				return depth =>
				{
					var quote = currentQuote();
					return quote != null && quote.Price > finishPrice;
				};
			}
		}

		#endregion

		#region Candle rules

		private abstract class BaseCandleSeriesRule<TArg> : MarketRule<CandleSeries, TArg>
		{
			protected BaseCandleSeriesRule(CandleSeries series)
				: base(series)
			{
				if (series == null)
					throw new ArgumentNullException("series");

				Series = series;
			}

			protected CandleSeries Series { get; private set; }
		}

		private abstract class CandleSeriesRule<TArg> : BaseCandleSeriesRule<TArg>
		{
			protected CandleSeriesRule(CandleSeries series)
				: base(series)
			{
				Series.ProcessCandle += OnProcessCandle;
			}

			protected abstract void OnProcessCandle(Candle candle);

			protected override void DisposeManaged()
			{
				Series.ProcessCandle -= OnProcessCandle;
				base.DisposeManaged();
			}
		}

		private sealed class CandleStateSeriesRule : CandleSeriesRule<Candle>
		{
			private readonly CandleStates _state;
			private readonly CandleStates[] _states;

			public CandleStateSeriesRule(CandleSeries series, params CandleStates[] states)
				: base(series)
			{
				if (states == null)
					throw new ArgumentNullException("states");

				if (states.IsEmpty())
					throw new ArgumentOutOfRangeException("states");

				_state = states[0];

				if (states.Length > 1)
					_states = states;
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if ((_states == null && candle.State == _state) || (_states != null && _states.Contains(candle.State)))
					Activate(candle);
			}
		}

		private sealed class CandleChangedSeriesRule : CandleSeriesRule<Candle>
		{
			private readonly Func<Candle, bool> _condition;

			public CandleChangedSeriesRule(CandleSeries series)
				: this(series, c => true)
			{
			}

			public CandleChangedSeriesRule(CandleSeries series, Func<Candle, bool> condition)
				: base(series)
			{
				if (condition == null)
					throw new ArgumentNullException("condition");

				_condition = condition;
				Name = LocalizedStrings.Str1064 + series;
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (candle.State == CandleStates.Changed && _condition(candle))
					Activate(candle);
			}
		}

		private sealed class CurrentCandleSeriesRule : CandleSeriesRule<Candle>
		{
			private readonly Func<Candle, bool> _condition;

			public CurrentCandleSeriesRule(CandleSeries series, Func<Candle, bool> condition)
				: base(series)
			{
				if (condition == null)
					throw new ArgumentNullException("condition");

				_condition = condition;
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if ((candle.State == CandleStates.Started || candle.State == CandleStates.Changed) && _condition(candle))
					Activate(candle);
			}
		}

		private abstract class CandleRule : CandleSeriesRule<Candle>
		{
			protected CandleRule(Candle candle)
				: base(candle.CheckSeries())
			{
				Candle = candle;
			}

			protected Candle Candle { get; private set; }
		}

		private sealed class ChangedCandleRule : CandleRule
		{
			private readonly Func<Candle, bool> _condition;

			public ChangedCandleRule(Candle candle)
				: this(candle, c => true)
			{
			}

			public ChangedCandleRule(Candle candle, Func<Candle, bool> condition)
				: base(candle)
			{
				if (condition == null)
					throw new ArgumentNullException("condition");

				_condition = condition;
				Name = LocalizedStrings.Str1065 + candle;
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (candle.State == CandleStates.Changed && Candle == candle && _condition(Candle))
					Activate(Candle);
			}
		}

		private sealed class FinishedCandleRule : CandleRule
		{
			public FinishedCandleRule(Candle candle)
				: base(candle)
			{
				Name = LocalizedStrings.Str1066 + candle;
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (candle.State == CandleStates.Finished && candle == Candle)
					Activate(Candle);
			}
		}

		/// <summary>
		/// Создать правило на событие превышения цены закрытия свечи выше определенного уровня.
		/// </summary>
		/// <param name="candle">Свеча, которую необходимо отслеживать на событие превышения цены закрытия свечи выше определенного уровня.</param>
		/// <param name="price">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenClosePriceMore(this Candle candle, Unit price)
		{
			return new ChangedCandleRule(candle, candle.CreateCandleCondition(price, c => c.ClosePrice, false))
			{
				Name = LocalizedStrings.Str1067Params.Put(candle, price)
			};
		}

		/// <summary>
		/// Создать правило на событие понижения цены закрытия свечи ниже определенного уровня.
		/// </summary>
		/// <param name="candle">Свеча, которую необходимо отслеживать на событие понижения цены закрытия свечи ниже определенного уровня.</param>
		/// <param name="price">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenClosePriceLess(this Candle candle, Unit price)
		{
			return new ChangedCandleRule(candle, candle.CreateCandleCondition(price, c => c.ClosePrice, true))
			{
				Name = LocalizedStrings.Str1068Params.Put(candle, price)
			};
		}

		private static Func<Candle, bool> CreateCandleCondition(this Candle candle, Unit price, Func<Candle, decimal> currentPrice, bool isLess)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			if (price == null)
				throw new ArgumentNullException("price");

			if (currentPrice == null)
				throw new ArgumentNullException("currentPrice");

			if (price.Value == 0)
				throw new ArgumentException(LocalizedStrings.Str1051, "price");

			if (price.Value < 0)
				throw new ArgumentException(LocalizedStrings.Str1052, "price");

			if (isLess)
			{
				var finishPrice = (decimal)(price.Type == UnitTypes.Limit ? price : currentPrice(candle) - price);
				return c => currentPrice(c) < finishPrice;
			}
			else
			{
				var finishPrice = (decimal)(price.Type == UnitTypes.Limit ? price : currentPrice(candle) + price);
				return c => currentPrice(c) > finishPrice;
			}
		}

		/// <summary>
		/// Создать правило на событие превышения общего объема свечи выше определённого уровня.
		/// </summary>
		/// <param name="candle">Свеча, которую необходимо отслеживать на событие превышения общего объема выше определённого уровня.</param>
		/// <param name="volume">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenTotalVolumeMore(this Candle candle, Unit volume)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			var finishVolume = volume.Type == UnitTypes.Limit ? volume : candle.TotalVolume + volume;

			return new ChangedCandleRule(candle, c => c.TotalVolume > finishVolume)
			{
				Name = candle + LocalizedStrings.Str1069Params.Put(volume)
			};
		}

		/// <summary>
		/// Создать правило на событие превышения общего объема свечи выше определенного уровня.
		/// </summary>
		/// <param name="series">Серия свечек, из которой будет браться свеча.</param>
		/// <param name="volume">Уровень. Если тип <see cref="Unit.Type"/> равен <see cref="UnitTypes.Limit"/>, то задается конкретная цена. Иначе, указывается величина сдвига.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCurrentCandleTotalVolumeMore(this CandleSeries series, Unit volume)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			var finishVolume = volume;

			if (volume.Type != UnitTypes.Limit)
			{
				var curCandle = series.GetCurrentCandle<Candle>();

				if (curCandle == null)
					throw new ArgumentException(LocalizedStrings.Str1070, "series");

				finishVolume = curCandle.TotalVolume + volume;	
			}

			return new CurrentCandleSeriesRule(series, candle => candle.TotalVolume > finishVolume)
			{
				Name = series + LocalizedStrings.Str1071Params.Put(volume)
			};
		}

		/// <summary>
		/// Создать правило на событие появления новых свечек.
		/// </summary>
		/// <param name="series">Серия свечек, для которой будут отслеживаться новые свечи.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandlesStarted(this CandleSeries series)
		{
			return new CandleStateSeriesRule(series, CandleStates.Started) { Name = LocalizedStrings.Str1072 + series };
		}

		/// <summary>
		/// Создать правило на событие изменения свечек.
		/// </summary>
		/// <param name="series">Серия свечек, для которой будут отслеживаться измененные свечи.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandlesChanged(this CandleSeries series)
		{
			return new CandleChangedSeriesRule(series);
		}

		/// <summary>
		/// Создать правило на событие окончания свечек.
		/// </summary>
		/// <param name="series">Серия свечек, для которой будут отслеживаться законченные свечи.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandlesFinished(this CandleSeries series)
		{
			return new CandleStateSeriesRule(series, CandleStates.Finished) { Name = LocalizedStrings.Str1073 + series };
		}

		/// <summary>
		/// Создать правило на событие появления, изменения и икончания свечек.
		/// </summary>
		/// <param name="series">Серия свечек, для которой будут отслеживаться свечи.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandles(this CandleSeries series)
		{
			return new CandleStateSeriesRule(series, CandleStates.Started, CandleStates.Changed, CandleStates.Finished)
			{
				Name = LocalizedStrings.Candles + " " + series
			};
		}

		/// <summary>
		/// Создать правило на событие изменения свечи.
		/// </summary>
		/// <param name="candle">Свеча, для которой будет отслеживаться изменение.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenChanged(this Candle candle)
		{
			return new ChangedCandleRule(candle);
		}

		/// <summary>
		/// Создать правило на событие окончания свечи.
		/// </summary>
		/// <param name="candle">Свеча, для которой будет отслеживаться окончание.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenFinished(this Candle candle)
		{
			return new FinishedCandleRule(candle).Once();
		}

		/// <summary>
		/// Создать правило на событие частичного окончания свечи.
		/// </summary>
		/// <param name="candle">Свеча, для которой будет отслеживаться частичное окончание.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="percent">Процент завершения свечи.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenPartiallyFinished(this Candle candle, IConnector connector, decimal percent)
		{
			var rule = (candle is TimeFrameCandle)
						? (MarketRule<CandleSeries, Candle>)new TimeFrameCandleChangedRule(candle, connector, percent)
			           	: new ChangedCandleRule(candle, candle.IsCandlePartiallyFinished(percent));

			rule.Name = LocalizedStrings.Str1075Params.Put(percent);
			return rule;
		}

		/// <summary>
		/// Создать правило на событие частичного окончания свечек.
		/// </summary>
		/// <param name="series">Серия свечек, для которой будут отслеживаться частичное окончание свечи.</param>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="percent">Процент завершения свечи.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<CandleSeries, Candle> WhenPartiallyFinishedCandles(this CandleSeries series, IConnector connector, decimal percent)
		{
			var rule = (series.CandleType == typeof(TimeFrameCandle))
				? (MarketRule<CandleSeries, Candle>)new TimeFrameCandlesChangedSeriesRule(series, connector, percent)
				: new CandleChangedSeriesRule(series, series.IsCandlePartiallyFinished(percent));

			rule.Name = LocalizedStrings.Str1076Params.Put(percent);
			return rule;
		}

		private sealed class TimeFrameCandleChangedRule : BaseCandleSeriesRule<Candle>
		{
			private readonly MarketTimer _timer;

			public TimeFrameCandleChangedRule(Candle candle, IConnector connector, decimal percent)
				: base(candle.CheckSeries())
			{
				_timer = CreateAndActivateTimeFrameTimer(candle.Series, connector, () => Activate(candle), percent, false);
			}

			protected override void DisposeManaged()
			{
				_timer.Dispose();
				base.DisposeManaged();
			}
		}

		private sealed class TimeFrameCandlesChangedSeriesRule : BaseCandleSeriesRule<Candle>
		{
			private readonly MarketTimer _timer;

			public TimeFrameCandlesChangedSeriesRule(CandleSeries series, IConnector connector, decimal percent)
				: base(series)
			{
				_timer = CreateAndActivateTimeFrameTimer(series, connector, () => Activate(Series.GetCurrentCandle<Candle>()), percent, true);
			}

			protected override void DisposeManaged()
			{
				_timer.Dispose();
				base.DisposeManaged();
			}
		}

		private static MarketTimer CreateAndActivateTimeFrameTimer(CandleSeries series, IConnector connector, Action callback, decimal percent, bool periodical)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			if (connector == null)
				throw new ArgumentNullException("connector");

			if (callback == null)
				throw new ArgumentNullException("callback");

			if (percent <= 0)
				throw new ArgumentOutOfRangeException("percent", LocalizedStrings.Str1077);

			var timeFrame = (TimeSpan)series.Arg;

			MarketTimer timer = null;

			timer = new MarketTimer(connector, () =>
			{
				if (periodical)
					timer.Interval(timeFrame);
				else
					timer.Stop();

				callback();
			});

			var marketTime = series.Security.ToExchangeTime(connector.CurrentTime);
			var candleBounds = timeFrame.GetCandleBounds(marketTime, series.Security.Board);

			percent = percent / 100;

			var startTime = candleBounds.Min + TimeSpan.FromMilliseconds(timeFrame.TotalMilliseconds * (double)percent);

			var diff = startTime - marketTime;

			if (diff == TimeSpan.Zero)
				timer.Interval(timeFrame);
			else if (diff > TimeSpan.Zero)
				timer.Interval(diff);
			else
				timer.Interval(timeFrame + diff);

			return timer.Start();
		}

		private static Func<Candle, bool> IsCandlePartiallyFinished(this Candle candle, decimal percent)
		{
			if (candle == null)
				throw new ArgumentNullException("candle");

			return candle.Series.IsCandlePartiallyFinished(percent);
		}

		private static Func<Candle, bool> IsCandlePartiallyFinished(this CandleSeries series, decimal percent)
		{
			if (series == null)
				throw new ArgumentNullException("series");

			if (percent <= 0)
				throw new ArgumentOutOfRangeException("percent", LocalizedStrings.Str1077);

			var realPercent = percent / 100;

			if (series.CandleType == typeof(TickCandle))
			{
				var count = realPercent * (int)series.Arg;
				return newCandle => ((TickCandle)newCandle).CurrentTradeCount >= count;
			}
			else if (series.CandleType == typeof(RangeCandle))
			{
				return newCandle => (decimal)(newCandle.LowPrice + (Unit)series.Arg) >= realPercent * newCandle.HighPrice;
			}
			else if (series.CandleType == typeof(VolumeCandle))
			{
				var volume = realPercent * (decimal)series.Arg;
				return newCandle => newCandle.TotalVolume >= volume;
			}
			else
				throw new ArgumentOutOfRangeException("series", series.CandleType, LocalizedStrings.WrongCandleType);
		}

		#endregion

		#region ITrader rules

		private abstract class TraderRule<TArg> : MarketRule<IConnector, TArg>
		{
			protected TraderRule(IConnector connector)
				: base(connector)
			{
				if (connector == null)
					throw new ArgumentNullException("connector");

				Connector = connector;
			}

			protected IConnector Connector { get; private set; }
		}

		private sealed class MarketTimeRule : TraderRule<IConnector>
		{
			private readonly MarketTimer _timer;

			public MarketTimeRule(IConnector connector, TimeSpan interval/*, bool firstTimeRun*/)
				: base(connector)
			{
				Name = LocalizedStrings.Str175 + " " + interval;

				_timer = new MarketTimer(connector, () => Activate(connector))
					.Interval(interval)
					.Start();
			}

			protected override void DisposeManaged()
			{
				_timer.Dispose();
				base.DisposeManaged();
			}
		}

		private sealed class NewMyTradesTraderRule : TraderRule<IEnumerable<MyTrade>>
		{
			public NewMyTradesTraderRule(IConnector connector)
				: base(connector)
			{
				Name = LocalizedStrings.Str1080;
				Connector.NewMyTrades += OnNewMyTrades;
			}

			private void OnNewMyTrades(IEnumerable<MyTrade> trades)
			{
				Activate(trades);
			}

			protected override void DisposeManaged()
			{
				Connector.NewMyTrades -= OnNewMyTrades;
				base.DisposeManaged();
			}
		}

		private sealed class NewOrdersTraderRule : TraderRule<IEnumerable<Order>>
		{
			public NewOrdersTraderRule(IConnector connector)
				: base(connector)
			{
				Name = LocalizedStrings.Str1081;
				Connector.NewOrders += OnNewOrders;
			}

			private void OnNewOrders(IEnumerable<Order> orders)
			{
				Activate(orders);
			}

			protected override void DisposeManaged()
			{
				Connector.NewOrders -= OnNewOrders;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// Создать правило на событие <see cref="IConnector.MarketTimeChanged"/>, активизирующееся по истечению <paramref name="interval"/>.
		/// </summary>
		/// <param name="connector">Подключение к торговой системе.</param>
		/// <param name="interval">Интервал.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<IConnector, IConnector> WhenIntervalElapsed(this IConnector connector, TimeSpan interval/*, bool firstTimeRun = false*/)
		{
			/*/// <param name="firstTimeRun">Сработает ли правило в момент создания (нулевое время). False по умолчанию.</param>*/

			if (connector == null)
				throw new ArgumentNullException("connector");

			return new MarketTimeRule(connector, interval/*, firstTimeRun*/);
		}

		/// <summary>
		/// Создать правило на событие появление новых сделок.
		/// </summary>
		/// <param name="connector">Подключение, по которому будет отслеживаться появление сделок.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<IConnector, IEnumerable<MyTrade>> WhenNewMyTrades(this IConnector connector)
		{
			return new NewMyTradesTraderRule(connector);
		}

		/// <summary>
		/// Создать правило на событие появление новых заявок.
		/// </summary>
		/// <param name="connector">Подключение, по которому будет отслеживаться появление заявок.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<IConnector, IEnumerable<Order>> WhenNewOrder(this IConnector connector)
		{
			return new NewOrdersTraderRule(connector);
		}

		#endregion

		#region Apply

		/// <summary>
		/// Сформировать правило (включить <see cref="IMarketRule.IsReady"/>).
		/// </summary>
		/// <param name="rule">Правило.</param>
		/// <returns>Правило.</returns>
		public static IMarketRule Apply(this IMarketRule rule)
		{
			if (rule == null)
				throw new ArgumentNullException("rule");

			return rule.Apply(DefaultRuleContainer);
		}

		/// <summary>
		/// Сформировать правило (включить <see cref="IMarketRule.IsReady"/>).
		/// </summary>
		/// <param name="rule">Правило.</param>
		/// <param name="container">Контейнер правил.</param>
		/// <returns>Правило.</returns>
		public static IMarketRule Apply(this IMarketRule rule, IMarketRuleContainer container)
		{
			if (rule == null)
				throw new ArgumentNullException("rule");

			if (container == null)
				throw new ArgumentNullException("container");

			container.Rules.Add(rule);
			return rule;
		}

		/// <summary>
		/// Сформировать правило (включить <see cref="IMarketRule.IsReady"/>).
		/// </summary>
		/// <typeparam name="TToken">Тип токена.</typeparam>
		/// <typeparam name="TArg">Тип принимаемого правилом аргумента.</typeparam>
		/// <param name="rule">Правило.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<TToken, TArg> Apply<TToken, TArg>(this MarketRule<TToken, TArg> rule)
		{
			return rule.Apply(DefaultRuleContainer);
		}

		/// <summary>
		/// Сформировать правило (включить <see cref="IMarketRule.IsReady"/>).
		/// </summary>
		/// <typeparam name="TToken">Тип токена.</typeparam>
		/// <typeparam name="TArg">Тип принимаемого правилом аргумента.</typeparam>
		/// <param name="rule">Правило.</param>
		/// <param name="container">Контейнер правил.</param>
		/// <returns>Правило.</returns>
		public static MarketRule<TToken, TArg> Apply<TToken, TArg>(this MarketRule<TToken, TArg> rule, IMarketRuleContainer container)
		{
			return (MarketRule<TToken, TArg>)((IMarketRule)rule).Apply(container);
		}

		/// <summary>
		/// Активировать правило.
		/// </summary>
		/// <param name="container">Контейнер правил.</param>
		/// <param name="rule">Правило.</param>
		/// <param name="process">Обработчик.</param>
		public static void ActiveRule(this IMarketRuleContainer container, IMarketRule rule, Func<bool> process)
		{
			container.AddRuleLog(LogLevels.Debug, rule, LocalizedStrings.Str1082);

			List<IMarketRule> removedRules = null;

			// mika
			// проверяем правило, так как оно могло быть удалено параллельным потоком
			if (!rule.IsReady)
				return;

			rule.IsActive = true;

			try
			{
				if (process())
				{
					container.Rules.Remove(rule);
					removedRules = new List<IMarketRule> { rule };
				}
			}
			finally
			{
				rule.IsActive = false;
			}

			if (removedRules == null)
				return;

			if (rule.ExclusiveRules.Count > 0)
			{
				foreach (var exclusiveRule in rule.ExclusiveRules.SyncGet(c => c.CopyAndClear()))
				{
					container.TryRemoveRule(exclusiveRule, false);
					removedRules.Add(exclusiveRule);
				}
			}

			foreach (var removedRule in removedRules)
			{
				container.AddRuleLog(LogLevels.Debug, removedRule, LocalizedStrings.Str1083);
			}
		}

		private sealed class MarketRuleContainer : BaseLogReceiver, IMarketRuleContainer
		{
			private readonly object _rulesSuspendLock = new object();
			private int _rulesSuspendCount;

			public MarketRuleContainer()
			{
				_rules = new MarketRuleList(this);
			}

			ProcessStates IMarketRuleContainer.ProcessState
			{
				get { return ProcessStates.Started; }
			}

			void IMarketRuleContainer.ActivateRule(IMarketRule rule, Func<bool> process)
			{
				this.ActiveRule(rule, process);
			}

			bool IMarketRuleContainer.IsRulesSuspended
			{
				get { return _rulesSuspendCount > 0; }
			}

			void IMarketRuleContainer.SuspendRules()
			{
				lock (_rulesSuspendLock)
					_rulesSuspendCount++;
			}

			void IMarketRuleContainer.ResumeRules()
			{
				lock (_rulesSuspendLock)
				{
					if (_rulesSuspendCount > 0)
						_rulesSuspendCount--;
				}
			}

			private readonly MarketRuleList _rules;

			IMarketRuleList IMarketRuleContainer.Rules
			{
				get { return _rules; }
			}
		}

		/// <summary>
		/// Контейнер правил, который будет применяться по умолчанию ко всем правилам, не входящим в стратегию.
		/// </summary>
		public static readonly IMarketRuleContainer DefaultRuleContainer = new MarketRuleContainer();

		/// <summary>
		/// Обработать правила в приостановленном режиме (например, создать несколько правил и запустить их одновременно).
		/// После окончания работы метода все правила, присоединенные к контейнеру, возобновляют свою активность.
		/// </summary>
		/// <param name="action">Действие, которое необходимо обработать при остановленных правилах. Например, добавить одновременно несколько правил.</param>
		public static void SuspendRules(Action action)
		{
			DefaultRuleContainer.SuspendRules(action);
		}

		/// <summary>
		/// Обработать правила в приостановленном режиме (например, создать несколько правил и запустить их одновременно).
		/// После окончания работы метода все правила, присоединенные к контейнеру, возобновляют свою активность.
		/// </summary>
		/// <param name="container">Контейнер правил.</param>
		/// <param name="action">Действие, которое необходимо обработать при остановленных правилах. Например, добавить одновременно несколько правил.</param>
		public static void SuspendRules(this IMarketRuleContainer container, Action action)
		{
			if (container == null)
				throw new ArgumentNullException("container");

			if (action == null)
				throw new ArgumentNullException("action");

			container.SuspendRules();

			try
			{
				action();
			}
			finally
			{
				container.ResumeRules();
			}
		}

		#endregion

		/// <summary>
		/// Удалить правило. Если правило выполняется в момент вызова данного метода, то оно не будет удалено.
		/// </summary>
		/// <param name="container">Контейнер правил.</param>
		/// <param name="rule">Правило.</param>
		/// <param name="checkCanFinish">Проверять возможность остановки правила.</param>
		/// <returns><see langword="true"/>, если правило было успешно удалено, false - если правило нельзя удалить в текущий момент.</returns>
		public static bool TryRemoveRule(this IMarketRuleContainer container, IMarketRule rule, bool checkCanFinish = true)
		{
			if (container == null)
				throw new ArgumentNullException("container");

			if (rule == null)
				throw new ArgumentNullException("rule");

			// не блокируем выполнение, если правило активно в данный момент
			// оно в последствии само удалится, так как стратегия уже перешла в состояние Stopping
			//if (!rule.SyncRoot.TryEnter())
			//	return false;

			var isRemoved = false;

			//try
			//{
			if ((!checkCanFinish && !rule.IsActive && rule.IsReady) || rule.CanFinish())
			{
				container.Rules.Remove(rule);
				isRemoved = true;
			}
			//}
			//finally
			//{
			//	rule.SyncRoot.Exit();
			//}

			if (isRemoved)
			{
				container.AddRuleLog(LogLevels.Debug, rule, LocalizedStrings.Str1084, rule);
			}

			return isRemoved;
		}

		/// <summary>
		/// Удалить правило и все противоположные правила. Если правило выполняется в момент вызова данного метода, то оно не будет удалено.
		/// </summary>
		/// <param name="container">Контейнер правил.</param>
		/// <param name="rule">Правило.</param>
		public static bool TryRemoveWithExclusive(this IMarketRuleContainer container, IMarketRule rule)
		{
			if (container == null)
				throw new ArgumentNullException("container");

			if (rule == null)
				throw new ArgumentNullException("rule");

			if (container.TryRemoveRule(rule))
			{
				if (rule.ExclusiveRules.Count > 0)
				{
					foreach (var exclusiveRule in rule.ExclusiveRules.SyncGet(c => c.CopyAndClear()))
					{
						container.TryRemoveRule(exclusiveRule, false);
					}
				}

				return true;
			}

			return false;
		}

		/// <summary>
		/// Сделать правила взаимо исключающими.
		/// </summary>
		/// <param name="rule1">Первое правило.</param>
		/// <param name="rule2">Второе правило.</param>
		public static void Exclusive(this IMarketRule rule1, IMarketRule rule2)
		{
			if (rule1 == null)
				throw new ArgumentNullException("rule1");

			if (rule2 == null)
				throw new ArgumentNullException("rule2");

			if (rule1 == rule2)
				throw new ArgumentException(LocalizedStrings.Str1085Params.Put(rule1), "rule2");

			rule1.ExclusiveRules.Add(rule2);
			rule2.ExclusiveRules.Add(rule1);
		}

		#region Or

		private abstract class BaseComplexRule<TToken, TArg> : MarketRule<TToken, TArg>, IMarketRuleContainer
		{
			private readonly List<IMarketRule> _innerRules = new List<IMarketRule>();

			protected BaseComplexRule(IEnumerable<IMarketRule> innerRules)
				: base(default(TToken))
			{
				if (innerRules == null)
					throw new ArgumentNullException("innerRules");

				_innerRules.AddRange(innerRules.Select(Init));

				if (_innerRules.IsEmpty())
					throw new InvalidOperationException(LocalizedStrings.Str1086);

				Name = _innerRules.Select(r => r.Name).Join(" OR ");

				_innerRules.ForEach(r => r.Container = this);
			}

			protected abstract IMarketRule Init(IMarketRule rule);

			public override LogLevels LogLevel
			{
				set
				{
					base.LogLevel = value;

					foreach (var rule in _innerRules)
						rule.LogLevel = value;
				}
			}

			public override bool IsSuspended
			{
				set
				{
					base.IsSuspended = value;
					_innerRules.ForEach(r => r.Suspend(value));
				}
			}

			protected override void DisposeManaged()
			{
				_innerRules.ForEach(r => r.Dispose());
				base.DisposeManaged();
			}

			#region Implementation of ILogSource

			private readonly Guid _id = Guid.NewGuid();

			Guid ILogSource.Id
			{
				get { return _id; }
			}

			ILogSource ILogSource.Parent
			{
				get { return Container; }
				set
				{
					throw new NotSupportedException();
				}
			}

			LogLevels ILogSource.LogLevel
			{
				get { return Container.LogLevel; }
				set { throw new NotSupportedException(); }
			}

			event Action<LogMessage> ILogSource.Log
			{
				add { Container.Log += value; }
				remove { Container.Log -= value; }
			}

			DateTimeOffset ILogSource.CurrentTime
			{
				get { return Container.CurrentTime; }
			}

			#endregion

			void ILogReceiver.AddLog(LogMessage message)
			{
				Container.AddLog(new LogMessage(Container, message.Time, message.Level, () => message.Message));
			}

			#region Implementation of IMarketRuleContainer

			ProcessStates IMarketRuleContainer.ProcessState
			{
				get { return Container.ProcessState; }
			}

			void IMarketRuleContainer.ActivateRule(IMarketRule rule, Func<bool> process)
			{
				process();
			}

			bool IMarketRuleContainer.IsRulesSuspended
			{
				get { return Container.IsRulesSuspended; }
			}

			void IMarketRuleContainer.SuspendRules()
			{
				throw new NotSupportedException();
			}

			void IMarketRuleContainer.ResumeRules()
			{
				throw new NotSupportedException();
			}

			IMarketRuleList IMarketRuleContainer.Rules
			{
				get { throw new NotSupportedException(); }
			}

			#endregion

			public override string ToString()
			{
				return _innerRules.Select(r => r.ToString()).Join(" OR ");
			}
		}

		private sealed class OrRule : BaseComplexRule<object, object>
		{
			public OrRule(IEnumerable<IMarketRule> innerRules)
				: base(innerRules)
			{
			}

			protected override IMarketRule Init(IMarketRule rule)
			{
				return rule.Do(arg => Activate(arg));
			}
		}

		private sealed class OrRule<TToken, TArg> : BaseComplexRule<TToken, TArg>
		{
			public OrRule(IEnumerable<MarketRule<TToken, TArg>> innerRules)
				: base(innerRules)
			{
			}

			protected override IMarketRule Init(IMarketRule rule)
			{
				return ((MarketRule<TToken, TArg>)rule).Do(a => Activate(a));
			}
		}

		private sealed class AndRule : BaseComplexRule<object, object>
		{
			private readonly List<object> _args = new List<object>();
			private readonly SynchronizedSet<IMarketRule> _nonActivatedRules = new SynchronizedSet<IMarketRule>();

			public AndRule(IEnumerable<IMarketRule> innerRules)
				: base(innerRules)
			{
				_nonActivatedRules.AddRange(innerRules);
			}

			protected override IMarketRule Init(IMarketRule rule)
			{
				return rule.Do(a =>
				{
					var canActivate = false;

					lock (_nonActivatedRules.SyncRoot)
					{
						if (_nonActivatedRules.Remove(rule))
						{
							_args.Add(a);

							if (_nonActivatedRules.IsEmpty())
								canActivate = true;
						}
					}

					if (canActivate)
						Activate(_args);
				});
			}
		}

		private sealed class AndRule<TToken, TArg> : BaseComplexRule<TToken, TArg>
		{
			private readonly List<TArg> _args = new List<TArg>();
			private readonly SynchronizedSet<IMarketRule> _nonActivatedRules = new SynchronizedSet<IMarketRule>();

			public AndRule(IEnumerable<MarketRule<TToken, TArg>> innerRules)
				: base(innerRules)
			{
				_nonActivatedRules.AddRange(innerRules);
			}

			protected override IMarketRule Init(IMarketRule rule)
			{
				return ((MarketRule<TToken, TArg>)rule).Do(a =>
				{
					var canActivate = false;

					lock (_nonActivatedRules.SyncRoot)
					{
						if (_nonActivatedRules.Remove(rule))
						{
							_args.Add(a);

							if (_nonActivatedRules.IsEmpty())
								canActivate = true;
						}
					}

					if (canActivate)
						Activate(_args.FirstOrDefault());
				});
			}
		}

		/// <summary>
		/// Объединить правила по условию ИЛИ.
		/// </summary>
		/// <param name="rule">Первое правило.</param>
		/// <param name="rules">Дополнительные правила.</param>
		/// <returns>Объединенное правило.</returns>
		public static IMarketRule Or(this IMarketRule rule, params IMarketRule[] rules)
		{
			return new OrRule(new[] { rule }.Concat(rules));
		}

		/// <summary>
		/// Объединить правила по условию ИЛИ.
		/// </summary>
		/// <param name="rules">Правила.</param>
		/// <returns>Объединенное правило.</returns>
		public static IMarketRule Or(this IEnumerable<IMarketRule> rules)
		{
			return new OrRule(rules);
		}

		/// <summary>
		/// Объединить правила по условию ИЛИ.
		/// </summary>
		/// <typeparam name="TToken">Тип токена.</typeparam>
		/// <typeparam name="TArg">Тип принимаемого правилом аргумента.</typeparam>
		/// <param name="rule">Первое правило.</param>
		/// <param name="rules">Дополнительные правила.</param>
		/// <returns>Объединенное правило.</returns>
		public static MarketRule<TToken, TArg> Or<TToken, TArg>(this MarketRule<TToken, TArg> rule, params MarketRule<TToken, TArg>[] rules)
		{
			return new OrRule<TToken, TArg>(new[] { rule }.Concat(rules));
		}

		/// <summary>
		/// Объединить правила по условию И.
		/// </summary>
		/// <param name="rule">Первое правило.</param>
		/// <param name="rules">Дополнительные правила.</param>
		/// <returns>Объединенное правило.</returns>
		public static IMarketRule And(this IMarketRule rule, params IMarketRule[] rules)
		{
			return new AndRule(new[] { rule }.Concat(rules));
		}

		/// <summary>
		/// Объединить правила по условию И.
		/// </summary>
		/// <param name="rules">Правила.</param>
		/// <returns>Объединенное правило.</returns>
		public static IMarketRule And(this IEnumerable<IMarketRule> rules)
		{
			return new AndRule(rules);
		}

		/// <summary>
		/// Объединить правила по условию И.
		/// </summary>
		/// <typeparam name="TToken">Тип токена.</typeparam>
		/// <typeparam name="TArg">Тип принимаемого правилом аргумента.</typeparam>
		/// <param name="rule">Первое правило.</param>
		/// <param name="rules">Дополнительные правила.</param>
		/// <returns>Объединенное правило.</returns>
		public static MarketRule<TToken, TArg> And<TToken, TArg>(this MarketRule<TToken, TArg> rule, params MarketRule<TToken, TArg>[] rules)
		{
			return new AndRule<TToken, TArg>(new[] { rule }.Concat(rules));
		}

		#endregion

		/// <summary>
		/// Задать новое имя правила <see cref="IMarketRule.Name"/>.
		/// </summary>
		/// <typeparam name="TRule">Тип правила.</typeparam>
		/// <param name="rule">Правило.</param>
		/// <param name="name">Новое имя правила.</param>
		/// <returns>Правило.</returns>
		public static TRule UpdateName<TRule>(this TRule rule, string name)
			where TRule : IMarketRule
		{
			return rule.Modify(r => r.Name = name);
		}

		/// <summary>
		/// Установить уровень логирования.
		/// </summary>
		/// <typeparam name="TRule">Тип правила.</typeparam>
		/// <param name="rule">Правило.</param>
		/// <param name="level">Уровень, на котором осуществлять логирование.</param>
		/// <returns>Правило.</returns>
		public static TRule UpdateLogLevel<TRule>(this TRule rule, LogLevels level)
			where TRule : IMarketRule
		{
			return rule.Modify(r => r.LogLevel = level);
		}

		/// <summary>
		/// Приостановить или возобновить правило.
		/// </summary>
		/// <typeparam name="TRule">Тип правила.</typeparam>
		/// <param name="rule">Правило.</param>
		/// <param name="suspend">True - приостановить, false - возобновить.</param>
		/// <returns>Правило.</returns>
		public static TRule Suspend<TRule>(this TRule rule, bool suspend)
			where TRule : IMarketRule
		{
			return rule.Modify(r => r.IsSuspended = suspend);
		}

		///// <summary>
		///// Синхронизовать или рассинхронизовать реагирование правила с другими правилами.
		///// </summary>
		///// <typeparam name="TRule">Тип правила.</typeparam>
		///// <param name="rule">Правило.</param>
		///// <param name="syncToken">Объект синхронизации. Если значение равно null, то правило рассинхронизовывается.</param>
		///// <returns>Правило.</returns>
		//public static TRule Sync<TRule>(this TRule rule, SyncObject syncToken)
		//	where TRule : IMarketRule
		//{
		//	return rule.Modify(r => r.SyncRoot = syncToken);
		//}

		/// <summary>
		/// Сделать правило одноразовым (будет вызвано только один раз).
		/// </summary>
		/// <typeparam name="TRule">Тип правила.</typeparam>
		/// <param name="rule">Правило.</param>
		/// <returns>Правило.</returns>
		public static TRule Once<TRule>(this TRule rule)
			where TRule : IMarketRule
		{
			return rule.Modify(r => r.Until(() => true));
		}

		private static TRule Modify<TRule>(this TRule rule, Action<TRule> action)
			where TRule : IMarketRule
		{
			if (rule.IsNull())
				throw new ArgumentNullException("rule");

			action(rule);

			return rule;
		}

		/// <summary>
		/// Записать сообщение от правила.
		/// </summary>
		/// <param name="container">Контейнер правил.</param>
		/// <param name="level">Уровень лог-сообщения.</param>
		/// <param name="rule">Правило.</param>
		/// <param name="message">Текстовое сообщение.</param>
		/// <param name="args">Параметры текстового сообщения.
		/// Используются в случае, если message является форматирующей строкой.
		/// Подробнее, <see cref="string.Format(string,object[])"/>.</param>
		public static void AddRuleLog(this IMarketRuleContainer container, LogLevels level, IMarketRule rule, string message, params object[] args)
		{
			if (container == null)
				return; // правило еще не было добавлено в контейнер

			if (rule == null)
				throw new ArgumentNullException("rule");

			if (rule.LogLevel != LogLevels.Inherit && rule.LogLevel > level)
				return;

			container.AddLog(level, () => LocalizedStrings.Str1087Params.Put(rule, message.Put(args)));
		}
	}
}
