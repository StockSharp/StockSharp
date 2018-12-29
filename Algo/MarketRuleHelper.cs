#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Algo
File: MarketRuleHelper.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Algo.Candles;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// Extension class for <see cref="IMarketRule"/>.
	/// </summary>
	public static class MarketRuleHelper
	{
		#region Order rules

		private abstract class OrderRule<TArg> : MarketRule<Order, TArg>
		{
			protected OrderRule(Order order, IConnector connector)
				: base(order)
			{
				Order = order ?? throw new ArgumentNullException(nameof(order));
				Connector = connector ?? throw new ArgumentNullException(nameof(connector));
			}

			protected override bool CanFinish()
			{
				return base.CanFinish() || CheckOrderState();
			}

			protected virtual bool CheckOrderState()
			{
				return Order.State == OrderStates.Done || Order.State == OrderStates.Failed;
			}

			protected Order Order { get; }

			protected IConnector Connector { get; }

			protected void TrySubscribe()
			{
				Subscribe();
				Container.AddRuleLog(LogLevels.Debug, this, LocalizedStrings.Str1028);
			}

			protected abstract void Subscribe();
			protected abstract void UnSubscribe();

			protected override void DisposeManaged()
			{
				//if (Order.Connector != null)
				UnSubscribe();

				base.DisposeManaged();
			}

			/// <summary>
			/// Returns a string that represents the current object.
			/// </summary>
			/// <returns>A string that represents the current object.</returns>
			public override string ToString()
			{
				return "{0} {2}/{3} (0x{1:X})".Put(Name, GetHashCode(), Order.TransactionId, (Order.Id == null ? Order.StringId : Order.Id.To<string>()));
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
			public RegisterFailedOrderRule(Order order, IConnector connector)
				: base(order, connector)
			{
				Name = LocalizedStrings.Str717 + " ";
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
					Connector.StopOrderRegisterFailed += OnOrderRegisterFailed;
				else
					Connector.OrderRegisterFailed += OnOrderRegisterFailed;
			}

			protected override void UnSubscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
					Connector.StopOrderRegisterFailed -= OnOrderRegisterFailed;
				else
					Connector.OrderRegisterFailed -= OnOrderRegisterFailed;
			}

			private void OnOrderRegisterFailed(OrderFail fail)
			{
				if (fail.Order == Order)
					Activate(fail);
			}
		}

		private sealed class CancelFailedOrderRule : OrderRule<OrderFail>
		{
			public CancelFailedOrderRule(Order order, IConnector connector)
				: base(order, connector)
			{
				Name = LocalizedStrings.Str1030;
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
					Connector.StopOrderCancelFailed += OnOrderCancelFailed;
				else
					Connector.OrderCancelFailed += OnOrderCancelFailed;
			}

			protected override void UnSubscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
					Connector.StopOrderCancelFailed -= OnOrderCancelFailed;
				else
					Connector.OrderCancelFailed -= OnOrderCancelFailed;
			}

			private void OnOrderCancelFailed(OrderFail fail)
			{
				if (fail.Order == Order)
					Activate(fail);
			}
		}

		private sealed class ChangedOrNewOrderRule : OrderRule<Order>
		{
			private readonly Func<Order, bool> _condition;
			private bool _activated;

			public ChangedOrNewOrderRule(Order order, IConnector connector)
				: this(order, connector, o => true)
			{
			}

			public ChangedOrNewOrderRule(Order order, IConnector connector, Func<Order, bool> condition)
				: base(order, connector)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

				Name = LocalizedStrings.Str1031;

				TrySubscribe();
			}

			protected override void Subscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
				{
					Connector.StopOrderChanged += OnOrderChanged;
					Connector.NewStopOrder += OnOrderChanged;
				}
				else
				{
					Connector.OrderChanged += OnOrderChanged;
					Connector.NewOrder += OnOrderChanged;
				}
			}

			protected override void UnSubscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
				{
					Connector.StopOrderChanged -= OnOrderChanged;
					Connector.NewStopOrder -= OnOrderChanged;
				}
				else
				{
					Connector.OrderChanged -= OnOrderChanged;
					Connector.NewOrder -= OnOrderChanged;
				}
			}

			private void OnOrderChanged(Order order)
			{
				if (!_activated && order == Order && _condition(order))
				{
					_activated = true;
					Activate(order);
				}
			}
		}

		private class NewTradeOrderRule : OrderRule<MyTrade>
		{
			private decimal _receivedVolume;

			private bool AllTradesReceived => Order.State == OrderStates.Done && (Order.Volume - Order.Balance == _receivedVolume);

			public NewTradeOrderRule(Order order, IConnector connector)
				: base(order, connector)
			{
				Name = LocalizedStrings.Str1032;
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				Connector.NewMyTrade += OnNewMyTrade;
			}

			protected override void UnSubscribe()
			{
				Connector.NewMyTrade -= OnNewMyTrade;
			}

			protected override bool CheckOrderState()
			{
				return Order.State == OrderStates.Failed || AllTradesReceived;
			}

			private void OnNewMyTrade(MyTrade trade)
			{
				if (trade.Order != Order /*&& (Order.Type != OrderTypes.Conditional || trade.Order != Order.DerivedOrder)*/)
					return;

				_receivedVolume += trade.Trade.Volume;
				Activate(trade);
			}
		}

		private sealed class AllTradesOrderRule : OrderRule<IEnumerable<MyTrade>>
		{
			private decimal _receivedVolume;

			private readonly CachedSynchronizedList<MyTrade> _trades = new CachedSynchronizedList<MyTrade>();

			public AllTradesOrderRule(Order order, IConnector connector)
				: base(order, connector)
			{
				Name = LocalizedStrings.Str1033;
				TrySubscribe();
			}

			private bool AllTradesReceived => Order.State == OrderStates.Done && (Order.Volume - Order.Balance == _receivedVolume);

			protected override void Subscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
				{
					Connector.StopOrderChanged += OnOrderChanged;
					Connector.NewStopOrder += OnOrderChanged;
				}
				else
				{
					Connector.OrderChanged += OnOrderChanged;
					Connector.NewOrder += OnOrderChanged;
				}

				Connector.NewMyTrade += OnNewMyTrade;
			}

			protected override void UnSubscribe()
			{
				if (Order.Type == OrderTypes.Conditional)
				{
					Connector.StopOrderChanged -= OnOrderChanged;
					Connector.NewStopOrder -= OnOrderChanged;
				}
				else
				{
					Connector.OrderChanged -= OnOrderChanged;
					Connector.NewOrder -= OnOrderChanged;
				}

				Connector.NewMyTrade -= OnNewMyTrade;
			}

			private void OnOrderChanged(Order order)
			{
				if (order == Order)
				{
					TryActivate();
				}
			}

			private void OnNewMyTrade(MyTrade trade)
			{
				if (trade.Order != Order /*&& (Order.Type != OrderTypes.Conditional || trade.Order != Order.DerivedOrder)*/)
					return;

				_receivedVolume += trade.Trade.Volume;

				_trades.Add(trade);
				TryActivate();
			}

			private void TryActivate()
			{
				if (AllTradesReceived)
				{
					Activate(_trades.Cache);
				}
			}
		}

		class OrderTakeProfitStopLossRule : OrderRule<Order>
		{
			private readonly Unit _offset;
			private readonly bool _isTake;
			private decimal? _bestBidPrice;
			private decimal? _bestAskPrice;
			private decimal _averagePrice;

			private readonly List<Tuple<decimal, decimal>> _trades = new List<Tuple<decimal, decimal>>();

			public OrderTakeProfitStopLossRule(Order order, Unit offset, bool isTake, IConnector connector)
				: base(order, connector)
			{
				if (offset == null)
					throw new ArgumentNullException(nameof(offset));

				if (offset.Value <= 0)
					throw new ArgumentOutOfRangeException(nameof(offset));

				_offset = offset;
				_isTake = isTake;

				Name = _isTake ? LocalizedStrings.TakeProfit : LocalizedStrings.StopLoss;

				TrySubscribe();
			}

			protected override void Subscribe()
			{
				Connector.MarketDepthChanged += OnMarketDepthChanged;
				Connector.NewMyTrade += OnNewMyTrade;
			}

			protected override void UnSubscribe()
			{
				Connector.MarketDepthChanged -= OnMarketDepthChanged;
				Connector.NewMyTrade -= OnNewMyTrade;
			}

			private void OnMarketDepthChanged(MarketDepth depth)
			{
				if (depth.Security != Order.Security)
					return;

				_bestBidPrice = depth.BestBid?.Price;
				_bestAskPrice = depth.BestAsk?.Price;

				TryActivate();
			}

			private void OnNewMyTrade(MyTrade trade)
			{
				if (trade.Order != Order)
					return;

				_trades.Add(Tuple.Create(trade.Trade.Price, trade.Trade.Volume));

				var numerator = 0m;
				var denominator = 0m;

				foreach (var t in _trades)
				{
					if (t.Item2 == 0)
						continue;

					numerator += t.Item1 * t.Item2;
					denominator += t.Item2;
				}

				if (denominator == 0)
					return;

				_averagePrice = numerator / denominator;

				TryActivate();
			}

			private void TryActivate()
			{
				if (_trades.Count == 0)
					return;

				bool isActivate;

				if (_isTake)
				{
					if (Order.Direction == Sides.Buy)
						isActivate = _bestAskPrice != null && _bestAskPrice.Value >= (_averagePrice + _offset);
					else
						isActivate = _bestBidPrice != null && _bestBidPrice.Value <= (_averagePrice - _offset);
				}
				else
				{
					if (Order.Direction == Sides.Buy)
						isActivate = _bestAskPrice != null && _bestAskPrice.Value <= (_averagePrice - _offset);
					else
						isActivate = _bestBidPrice != null && _bestBidPrice.Value >= (_averagePrice + _offset);
				}

				if (isActivate)
					Activate(Order);
			}
		}

		/// <summary>
		/// To create a rule for the event of successful order registration on exchange.
		/// </summary>
		/// <param name="order">The order to be traced for the event of successful registration.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenRegistered(this Order order, IConnector connector)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			return new ChangedOrNewOrderRule(order, connector, o => o.State == OrderStates.Active || o.State == OrderStates.Done) { Name = LocalizedStrings.Str1034 }.Once();
		}

		///// <summary>
		///// To create a rule for the stop order activation.
		///// </summary>
		///// <param name="stopOrder">The stop order to be traced for the activation event.</param>
		///// <param name="connector">The connection of interaction with trade systems.</param>
		///// <returns>Rule.</returns>
		//public static MarketRule<Order, Order> WhenActivated(this Order stopOrder, IConnector connector)
		//{
		//	if (stopOrder == null)
		//		throw new ArgumentNullException(nameof(stopOrder));

		//	return new ChangedOrNewOrderRule(stopOrder, connector, o => o.DerivedOrder != null) { Name = LocalizedStrings.Str1035 }.Once();
		//}

		/// <summary>
		/// To create a rule for the event of order partial matching.
		/// </summary>
		/// <param name="order">The order to be traced for partial matching event.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenPartiallyMatched(this Order order, IConnector connector)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			var balance = order.Volume;
			var hasVolume = balance != 0;

			return new ChangedOrNewOrderRule(order, connector, o =>
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
		/// To create a for the event of order unsuccessful registration on exchange.
		/// </summary>
		/// <param name="order">The order to be traced for unsuccessful registration event.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, OrderFail> WhenRegisterFailed(this Order order, IConnector connector)
		{
			return new RegisterFailedOrderRule(order, connector).Once();
		}

		/// <summary>
		/// To create a rule for the event of unsuccessful order cancelling on exchange.
		/// </summary>
		/// <param name="order">The order to be traced for unsuccessful cancelling event.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, OrderFail> WhenCancelFailed(this Order order, IConnector connector)
		{
			return new CancelFailedOrderRule(order, connector);
		}

		/// <summary>
		/// To create a rule for the order cancelling event.
		/// </summary>
		/// <param name="order">The order to be traced for cancelling event.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenCanceled(this Order order, IConnector connector)
		{
			return new ChangedOrNewOrderRule(order, connector, o => o.IsCanceled()) { Name = LocalizedStrings.Str1037 }.Once();
		}

		/// <summary>
		/// To create a rule for the event of order fully matching.
		/// </summary>
		/// <param name="order">The order to be traced for the fully matching event.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenMatched(this Order order, IConnector connector)
		{
			return new ChangedOrNewOrderRule(order, connector, o => o.IsMatched()) { Name = LocalizedStrings.Str1038 }.Once();
		}

		/// <summary>
		/// To create a rule for the order change event.
		/// </summary>
		/// <param name="order">The order to be traced for the change event.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenChanged(this Order order, IConnector connector)
		{
			return new ChangedOrNewOrderRule(order, connector);
		}

		/// <summary>
		/// To create a rule for the event of trade occurrence for the order.
		/// </summary>
		/// <param name="order">The order to be traced for trades occurrence events.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, MyTrade> WhenNewTrade(this Order order, IConnector connector)
		{
			return new NewTradeOrderRule(order, connector);
		}

		/// <summary>
		/// To create a rule for the event of all trades occurrence for the order.
		/// </summary>
		/// <param name="order">The order to be traced for all trades occurrence event.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, IEnumerable<MyTrade>> WhenAllTrades(this Order order, IConnector connector)
		{
			return new AllTradesOrderRule(order, connector);
		}

		/// <summary>
		/// To create a rule for the order's profit more on offset.
		/// </summary>
		/// <param name="order">The order to be traced for profit.</param>
		/// <param name="profitOffset">Profit offset.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenProfitMore(this Order order, Unit profitOffset, IConnector connector)
		{
			return new OrderTakeProfitStopLossRule(order, profitOffset, true, connector);
		}

		/// <summary>
		/// To create a rule for the order's loss more on offset.
		/// </summary>
		/// <param name="order">The order to be traced for loss.</param>
		/// <param name="profitOffset">Loss offset.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenLossMore(this Order order, Unit profitOffset, IConnector connector)
		{
			return new OrderTakeProfitStopLossRule(order, profitOffset, false, connector);
		}

		#endregion

		#region Portfolio rules

		private sealed class PortfolioRule : MarketRule<Portfolio, Portfolio>
		{
			private readonly Func<Portfolio, bool> _changed;
			private readonly Portfolio _portfolio;
			private readonly IConnector _connector;

			public PortfolioRule(Portfolio portfolio, IConnector connector, Func<Portfolio, bool> changed)
				: base(portfolio)
			{
				_changed = changed ?? throw new ArgumentNullException(nameof(changed));

				_portfolio = portfolio ?? throw new ArgumentNullException(nameof(portfolio));
				_connector = connector ?? throw new ArgumentNullException(nameof(connector));
				_connector.PortfolioChanged += OnPortfolioChanged;
			}

			private void OnPortfolioChanged(Portfolio portfolio)
			{
				if (portfolio == _portfolio && _changed(_portfolio))
					Activate(_portfolio);
			}

			protected override void DisposeManaged()
			{
				_connector.PortfolioChanged -= OnPortfolioChanged;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of change portfolio .
		/// </summary>
		/// <param name="portfolio">The portfolio to be traced for the event of change.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Portfolio, Portfolio> WhenChanged(this Portfolio portfolio, IConnector connector)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			return new PortfolioRule(portfolio, connector, pf => true)
			{
				Name = "Pf {0} change".Put(portfolio)
			};
		}

		/// <summary>
		/// To create a rule for the event of money decrease in portfolio below the specific level.
		/// </summary>
		/// <param name="portfolio">The portfolio to be traced for the event of money decrease below the specific level.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <param name="money">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Portfolio, Portfolio> WhenMoneyLess(this Portfolio portfolio, IConnector connector, Unit money)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			if (money == null)
				throw new ArgumentNullException(nameof(money));

			var finishMoney = money.Type == UnitTypes.Limit ? money : portfolio.CurrentValue - money;

			return new PortfolioRule(portfolio, connector, pf => pf.CurrentValue < finishMoney)
			{
				Name = LocalizedStrings.Str1040Params.Put(portfolio, finishMoney)
			};
		}

		/// <summary>
		/// To create a rule for the event of money increase in portfolio above the specific level.
		/// </summary>
		/// <param name="portfolio">The portfolio to be traced for the event of money increase above the specific level.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <param name="money">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Portfolio, Portfolio> WhenMoneyMore(this Portfolio portfolio, IConnector connector, Unit money)
		{
			if (portfolio == null)
				throw new ArgumentNullException(nameof(portfolio));

			if (money == null)
				throw new ArgumentNullException(nameof(money));

			var finishMoney = money.Type == UnitTypes.Limit ? money : portfolio.CurrentValue + money;

			return new PortfolioRule(portfolio, connector, pf => pf.CurrentValue > finishMoney)
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
			private readonly IConnector _connector;

			public PositionRule(Position position, IConnector connector)
				: this(position, connector, p => true)
			{
				Name = LocalizedStrings.Str1042 + " " + position.Portfolio.Name;
			}

			public PositionRule(Position position, IConnector connector, Func<Position, bool> changed)
				: base(position)
			{
				_changed = changed ?? throw new ArgumentNullException(nameof(changed));

				_position = position ?? throw new ArgumentNullException(nameof(position));
				_connector = connector ?? throw new ArgumentNullException(nameof(connector));
				_connector.PositionChanged += OnPositionChanged;
			}

			private void OnPositionChanged(Position position)
			{
				if (position == _position && _changed(_position))
					Activate(_position);
			}

			protected override void DisposeManaged()
			{
				_connector.PositionChanged -= OnPositionChanged;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event of position decrease below the specific level.
		/// </summary>
		/// <param name="position">The position to be traced for the event of decrease below the specific level.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Position, Position> WhenLess(this Position position, IConnector connector, Unit value)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var finishPosition = value.Type == UnitTypes.Limit ? value : position.CurrentValue - value;

			return new PositionRule(position, connector, pf => pf.CurrentValue < finishPosition)
			{
				Name = LocalizedStrings.Str1044Params.Put(position, finishPosition)
			};
		}

		/// <summary>
		/// To create a rule for the event of position increase above the specific level.
		/// </summary>
		/// <param name="position">The position to be traced of the event of increase above the specific level.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <param name="value">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Position, Position> WhenMore(this Position position, IConnector connector, Unit value)
		{
			if (position == null)
				throw new ArgumentNullException(nameof(position));

			if (value == null)
				throw new ArgumentNullException(nameof(value));

			var finishPosition = value.Type == UnitTypes.Limit ? value : position.CurrentValue + value;

			return new PositionRule(position, connector, pf => pf.CurrentValue > finishPosition)
			{
				Name = LocalizedStrings.Str1045Params.Put(position, finishPosition)
			};
		}

		/// <summary>
		/// To create a rule for the position change event.
		/// </summary>
		/// <param name="position">The position to be traced for the change event.</param>
		/// <param name="connector">The connection of interaction with trade systems.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Position, Position> Changed(this Position position, IConnector connector)
		{
			return new PositionRule(position, connector);
		}

		#endregion

		#region Security rules

		private abstract class SecurityRule<TArg> : MarketRule<Security, TArg>
		{
			protected SecurityRule(Security security, IConnector connector)
				: base(security)
			{
				Security = security ?? throw new ArgumentNullException(nameof(security));
				Connector = connector ?? throw new ArgumentNullException(nameof(connector));
			}

			protected Security Security { get; }
			protected IConnector Connector { get; }
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
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

				Name = LocalizedStrings.Str1046 + " " + security;
				Connector.SecurityChanged += OnSecurityChanged;
			}

			private void OnSecurityChanged(Security security)
			{
				if (Security is BasketSecurity basket)
				{
					if (basket.Contains(ServicesRegistry.SecurityProvider, security) && _condition(security))
						Activate(security);
				}
				else
				{
					if (security == Security && _condition(Security))
						Activate(Security);
				}
			}

			protected override void DisposeManaged()
			{
				Connector.SecurityChanged -= OnSecurityChanged;
				base.DisposeManaged();
			}
		}

		private sealed class SecurityNewTradeRule : SecurityRule<Trade>
		{
			public SecurityNewTradeRule(Security security, IConnector connector)
				: base(security, connector)
			{
				Name = LocalizedStrings.Str1047 + " " + security;
				Connector.NewTrade += OnNewTrade;
			}

			private void OnNewTrade(Trade trade)
			{
				var sec = Security;

				var basket = sec as BasketSecurity;

				var has = basket?.Contains(ServicesRegistry.SecurityProvider, trade.Security) ?? trade.Security == sec;

				if (has)
					Activate(trade);
			}

			protected override void DisposeManaged()
			{
				Connector.NewTrade -= OnNewTrade;
				base.DisposeManaged();
			}
		}

		private sealed class SecurityNewOrderLogItemRule : SecurityRule<OrderLogItem>
		{
			public SecurityNewOrderLogItemRule(Security security, IConnector connector)
				: base(security, connector)
			{
				Name = LocalizedStrings.Str1048 + " " + security;
				Connector.NewOrderLogItem += OnNewOrderLogItem;
			}

			private void OnNewOrderLogItem(OrderLogItem item)
			{
				var sec = Security;

				var basket = sec as BasketSecurity;

				var has = basket?.Contains(ServicesRegistry.SecurityProvider, item.Order.Security) ?? item.Order.Security == sec;

				if (has)
					Activate(item);
			}

			protected override void DisposeManaged()
			{
				Connector.NewOrderLogItem -= OnNewOrderLogItem;
				base.DisposeManaged();
			}
		}

		private sealed class SecurityLastTradeRule : SecurityRule<Security>
		{
			private readonly Func<Security, bool> _condition;

			public SecurityLastTradeRule(Security security, IConnector connector, Func<Security, bool> condition)
				: base(security, connector)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

				Name = LocalizedStrings.Str1049 + " " + security;

				Connector.SecurityChanged += OnSecurityChanged;
				Connector.NewTrade += OnNewTrade;
			}

			private void OnSecurityChanged(Security security)
			{
				if (CheckLastTrade(security))
					Activate(security);
			}

			private bool CheckLastTrade(Security security)
			{
				if (Security is BasketSecurity basket)
				{
					return basket.Contains(ServicesRegistry.SecurityProvider, security) && _condition(security);
				}
				else
				{
					return security == Security && _condition(Security);
				}
			}

			private void OnNewTrade(Trade trade)
			{
				if (CheckTrades(Security, trade))
					Activate(trade.Security);
			}

			private bool CheckTrades(Security security, Trade trade)
			{
				return security is BasketSecurity basket
					? basket.Contains(ServicesRegistry.SecurityProvider, trade.Security) && _condition(trade.Security)
					: trade.Security == security && _condition(trade.Security);
			}

			protected override void DisposeManaged()
			{
				Connector.NewTrade -= OnNewTrade;
				Connector.SecurityChanged -= OnSecurityChanged;

				base.DisposeManaged();
			}
		}

		private sealed class SecurityMarketDepthChangedRule : SecurityRule<MarketDepth>
		{
			public SecurityMarketDepthChangedRule(Security security, IConnector connector)
				: base(security, connector)
			{
				Name = LocalizedStrings.Str1050 + " " + security;
				Connector.MarketDepthChanged += OnMarketDepthChanged;
			}

			private void OnMarketDepthChanged(MarketDepth depth)
			{
				if (depth.Security != Security)
					return;

				Activate(depth);
			}

			protected override void DisposeManaged()
			{
				Connector.MarketDepthChanged -= OnMarketDepthChanged;
				base.DisposeManaged();
			}
		}

		private sealed class BasketSecurityMarketDepthChangedRule : SecurityRule<MarketDepth>
		{
			public BasketSecurityMarketDepthChangedRule(BasketSecurity security, IConnector connector)
				: base(security, connector)
			{
				Name = LocalizedStrings.Str1050 + " " + security;
				Connector.MarketDepthChanged += OnMarketDepthChanged;
			}

			private void OnMarketDepthChanged(MarketDepth depth)
			{
				if (CheckDepth(Security, depth))
					Activate(depth);
			}

			private static bool CheckDepth(Security security, MarketDepth depth)
			{
				var basket = security as BasketSecurity;

				return basket?.Contains(ServicesRegistry.SecurityProvider, depth.Security) ?? depth.Security == security;
			}

			protected override void DisposeManaged()
			{
				Connector.MarketDepthChanged -= OnMarketDepthChanged;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the instrument change event.
		/// </summary>
		/// <param name="security">The instrument to be traced for changes.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Security, Security> WhenChanged(this Security security, IConnector connector)
		{
			return new SecurityChangedRule(security, connector);
		}

		/// <summary>
		/// To create a rule for the event of new trade occurrence for the instrument.
		/// </summary>
		/// <param name="security">The instrument to be traced for new trade occurrence event.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Security, Trade> WhenNewTrade(this Security security, IConnector connector)
		{
			return new SecurityNewTradeRule(security, connector);
		}

		/// <summary>
		/// To create a rule for the event of new notes occurrence in the orders log for instrument.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of new notes occurrence in the orders log.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Security, OrderLogItem> WhenNewOrderLogItem(this Security security, IConnector connector)
		{
			return new SecurityNewOrderLogItemRule(security, connector);
		}

		/// <summary>
		/// To create a rule for the event of order book change by instrument.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of order book change by instrument.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Security, MarketDepth> WhenMarketDepthChanged(this Security security, IConnector connector)
		{
			return new SecurityMarketDepthChangedRule(security, connector);
		}

		/// <summary>
		/// To create a rule for the event of order book change by instruments basket.
		/// </summary>
		/// <param name="security">Instruments basket to be traced for the event of order books change by internal instruments.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Security, MarketDepth> WhenMarketDepthChanged(this BasketSecurity security, IConnector connector)
		{
			return new BasketSecurityMarketDepthChangedRule(security, connector);
		}

		/// <summary>
		/// To create a rule for the event of excess of the best bid of specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of excess of the best bid of specific level.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Security, Security> WhenBestBidPriceMore(this Security security, IConnector connector, Unit price)
		{
			return CreateSecurityCondition(security, connector, Level1Fields.BestBidPrice, price, false);
		}

		/// <summary>
		/// To create a rule for the event of dropping the best bid below the specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of dropping the best bid below the specific level.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Security, Security> WhenBestBidPriceLess(this Security security, IConnector connector, Unit price)
		{
			return CreateSecurityCondition(security, connector, Level1Fields.BestBidPrice, price, true);
		}

		/// <summary>
		/// To create a rule for the event of excess of the best offer of the specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of excess of the best offer of the specific level.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Security, Security> WhenBestAskPriceMore(this Security security, IConnector connector, Unit price)
		{
			return CreateSecurityCondition(security, connector, Level1Fields.BestAskPrice, price, false);
		}

		/// <summary>
		/// To create a rule for the event of dropping the best offer below the specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of dropping the best offer below the specific level.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Security, Security> WhenBestAskPriceLess(this Security security, IConnector connector, Unit price)
		{
			return CreateSecurityCondition(security, connector, Level1Fields.BestAskPrice, price, true);
		}

		/// <summary>
		/// To create a rule for the event of increase of the last trade price above the specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of increase of the last trade price above the specific level.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Security, Security> WhenLastTradePriceMore(this Security security, IConnector connector, IMarketDataProvider provider, Unit price)
		{
			return CreateLastTradeCondition(security, connector, provider, price, false);
		}

		/// <summary>
		/// To create a rule for the event of reduction of the last trade price below the specific level.
		/// </summary>
		/// <param name="security">The instrument to be traced for the event of reduction of the last trade price below the specific level.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="provider">The market data provider.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Security, Security> WhenLastTradePriceLess(this Security security, IConnector connector, IMarketDataProvider provider, Unit price)
		{
			return CreateLastTradeCondition(security, connector, provider, price, true);
		}

		private static SecurityChangedRule CreateSecurityCondition(Security security, IConnector connector, Level1Fields field, Unit offset, bool isLess)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (offset == null)
				throw new ArgumentNullException(nameof(offset));

			if (offset.Value == 0)
				throw new ArgumentException(LocalizedStrings.Str1051, nameof(offset));

			if (offset.Value < 0)
				throw new ArgumentException(LocalizedStrings.Str1052, nameof(offset));

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
				throw new ArgumentNullException(nameof(security));

			if (provider == null)
				throw new ArgumentNullException(nameof(provider));

			if (offset == null)
				throw new ArgumentNullException(nameof(offset));

			if (offset.Value == 0)
				throw new ArgumentException(LocalizedStrings.Str1051, nameof(offset));

			if (offset.Value < 0)
				throw new ArgumentException(LocalizedStrings.Str1052, nameof(offset));

			var price = (decimal?)provider.GetSecurityValue(security, Level1Fields.LastTradePrice);

			if (price == null && offset.Type != UnitTypes.Limit)
				throw new ArgumentException(LocalizedStrings.Str1054, nameof(security));

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

		private sealed class TimeComeRule : MarketRule<IConnector, DateTimeOffset>
		{
			private readonly MarketTimer _timer;

			public TimeComeRule(IConnector connector, IEnumerable<DateTimeOffset> times)
				: base(connector)
			{
				if (times == null)
					throw new ArgumentNullException(nameof(times));

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
				_timer?.Dispose();

				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule, activated at the exact time, specified through <paramref name="times" />.
		/// </summary>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="times">The exact time. Several values may be sent.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<IConnector, DateTimeOffset> WhenTimeCome(this IConnector connector, params DateTimeOffset[] times)
		{
			return connector.WhenTimeCome((IEnumerable<DateTimeOffset>)times);
		}

		/// <summary>
		/// To create a rule, activated at the exact time, specified through <paramref name="times" />.
		/// </summary>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="times">The exact time. Several values may be sent.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<IConnector, DateTimeOffset> WhenTimeCome(this IConnector connector, IEnumerable<DateTimeOffset> times)
		{
			return new TimeComeRule(connector, times);
		}

		#endregion

		#region MarketDepth rules

		private abstract class MarketDepthRule : MarketRule<MarketDepth, MarketDepth>
		{
			protected MarketDepthRule(MarketDepth depth)
				: base(depth)
			{
				Depth = depth ?? throw new ArgumentNullException(nameof(depth));
			}

			protected MarketDepth Depth { get; }
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
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

				Name = LocalizedStrings.Str1056 + " " + depth.Security;
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
		/// To create a rule for the order book change event.
		/// </summary>
		/// <param name="depth">The order book to be traced for change event.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenChanged(this MarketDepth depth)
		{
			return new MarketDepthChangedRule(depth);
		}

		/// <summary>
		/// To create a rule for the event of order book spread size increase on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the spread change event.</param>
		/// <param name="price">The shift value.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenSpreadMore(this MarketDepth depth, Unit price)
		{
			var pair = depth.BestPair;
			var firstPrice = pair?.SpreadPrice ?? 0;
			return new MarketDepthChangedRule(depth, d => d.BestPair != null && d.BestPair.SpreadPrice > (firstPrice + price))
			{
				Name = LocalizedStrings.Str1057Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of order book spread size decrease on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the spread change event.</param>
		/// <param name="price">The shift value.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenSpreadLess(this MarketDepth depth, Unit price)
		{
			var pair = depth.BestPair;
			var firstPrice = pair?.SpreadPrice ?? 0;
			return new MarketDepthChangedRule(depth, d => d.BestPair != null && d.BestPair.SpreadPrice < (firstPrice - price))
			{
				Name = LocalizedStrings.Str1058Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of the best bid increase on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best bid increase on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenBestBidPriceMore(this MarketDepth depth, Unit price)
		{
			return new MarketDepthChangedRule(depth, CreateDepthCondition(price, () => depth.BestBid, false))
			{
				Name = LocalizedStrings.Str1059Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of the best bid decrease on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best bid decrease on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenBestBidPriceLess(this MarketDepth depth, Unit price)
		{
			return new MarketDepthChangedRule(depth, CreateDepthCondition(price, () => depth.BestBid, true))
			{
				Name = LocalizedStrings.Str1060Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of the best offer increase on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best offer increase on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<MarketDepth, MarketDepth> WhenBestAskPriceMore(this MarketDepth depth, Unit price)
		{
			return new MarketDepthChangedRule(depth, CreateDepthCondition(price, () => depth.BestAsk, false))
			{
				Name = LocalizedStrings.Str1061Params.Put(depth.Security, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of the best offer decrease on a specific value.
		/// </summary>
		/// <param name="depth">The order book to be traced for the event of the best offer decrease on a specific value.</param>
		/// <param name="price">The shift value.</param>
		/// <returns>Rule.</returns>
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
				throw new ArgumentNullException(nameof(price));

			if (currentQuote == null)
				throw new ArgumentNullException(nameof(currentQuote));

			if (price.Value == 0)
				throw new ArgumentException(LocalizedStrings.Str1051, nameof(price));

			if (price.Value < 0)
				throw new ArgumentException(LocalizedStrings.Str1052, nameof(price));

			var curQuote = currentQuote();
			if (curQuote == null)
				throw new ArgumentException(LocalizedStrings.Str1063, nameof(currentQuote));

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
				Series = series ?? throw new ArgumentNullException(nameof(series));
			}

			protected CandleSeries Series { get; }
		}

		private abstract class CandleSeriesRule<TArg> : BaseCandleSeriesRule<TArg>
		{
			private readonly ICandleManager _candleManager;

			protected CandleSeriesRule(ICandleManager candleManager, CandleSeries series)
				: base(series)
			{
				_candleManager = candleManager ?? throw new ArgumentNullException(nameof(candleManager));
				_candleManager.Processing += OnProcessing;
			}

			private void OnProcessing(CandleSeries series, Candle candle)
			{
				if (Series != series)
					return;

				OnProcessCandle(candle);
			}

			protected abstract void OnProcessCandle(Candle candle);

			protected override void DisposeManaged()
			{
				_candleManager.Processing -= OnProcessing;
				base.DisposeManaged();
			}
		}

		private sealed class CandleStateSeriesRule : CandleSeriesRule<Candle>
		{
			private readonly CandleStates _state;
			private readonly CandleStates[] _states;

			public CandleStateSeriesRule(ICandleManager candleManager, CandleSeries series, params CandleStates[] states)
				: base(candleManager, series)
			{
				if (states == null)
					throw new ArgumentNullException(nameof(states));

				if (states.IsEmpty())
					throw new ArgumentOutOfRangeException(nameof(states));

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

		private sealed class CandleStartedRule : CandleSeriesRule<Candle>
		{
			private Candle _currCandle;

			public CandleStartedRule(ICandleManager candleManager, CandleSeries series)
				: base(candleManager, series)
			{
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (_currCandle != null && _currCandle == candle)
					return;

				_currCandle = candle;
				Activate(candle);
			}
		}

		private sealed class CandleChangedSeriesRule : CandleSeriesRule<Candle>
		{
			private readonly Func<Candle, bool> _condition;

			public CandleChangedSeriesRule(ICandleManager candleManager, CandleSeries series)
				: this(candleManager, series, c => true)
			{
			}

			public CandleChangedSeriesRule(ICandleManager candleManager, CandleSeries series, Func<Candle, bool> condition)
				: base(candleManager, series)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
				Name = LocalizedStrings.Str1064 + " " + series;
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (candle.State == CandleStates.Active && _condition(candle))
					Activate(candle);
			}
		}

		private sealed class CurrentCandleSeriesRule : CandleSeriesRule<Candle>
		{
			private readonly Func<Candle, bool> _condition;

			public CurrentCandleSeriesRule(ICandleManager candleManager, CandleSeries series, Func<Candle, bool> condition)
				: base(candleManager, series)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (candle.State == CandleStates.Active && _condition(candle))
					Activate(candle);
			}
		}

		private abstract class CandleRule : MarketRule<Candle, Candle>
		{
			private readonly ICandleManager _candleManager;

			protected CandleRule(ICandleManager candleManager, Candle candle)
				: base(candle)
			{
				_candleManager = candleManager ?? throw new ArgumentNullException(nameof(candleManager));
				_candleManager.Processing += OnProcessing;

				Candle = candle;
			}

			private void OnProcessing(CandleSeries series, Candle candle)
			{
				if (Candle != candle)
					return;

				OnProcessCandle(candle);
			}

			protected abstract void OnProcessCandle(Candle candle);

			protected override void DisposeManaged()
			{
				_candleManager.Processing -= OnProcessing;
				base.DisposeManaged();
			}

			protected Candle Candle { get; }
		}

		private sealed class ChangedCandleRule : CandleRule
		{
			private readonly Func<Candle, bool> _condition;

			public ChangedCandleRule(ICandleManager candleManager, Candle candle)
				: this(candleManager, candle, c => true)
			{
			}

			public ChangedCandleRule(ICandleManager candleManager, Candle candle, Func<Candle, bool> condition)
				: base(candleManager, candle)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));
				Name = LocalizedStrings.Str1065 + " " + candle;
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (candle.State == CandleStates.Active && Candle == candle && _condition(Candle))
					Activate(Candle);
			}
		}

		private sealed class FinishedCandleRule : CandleRule
		{
			public FinishedCandleRule(ICandleManager candleManager, Candle candle)
				: base(candleManager, candle)
			{
				Name = LocalizedStrings.Str1066 + " " + candle;
			}

			protected override void OnProcessCandle(Candle candle)
			{
				if (candle.State == CandleStates.Finished && candle == Candle)
					Activate(Candle);
			}
		}

		/// <summary>
		/// To create a rule for the event of candle closing price excess above a specific level.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle closing price excess above a specific level.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenClosePriceMore(this ICandleManager candleManager, Candle candle, Unit price)
		{
			return new ChangedCandleRule(candleManager, candle, candle.CreateCandleCondition(price, c => c.ClosePrice, false))
			{
				Name = LocalizedStrings.Str1067Params.Put(candle, price)
			};
		}

		/// <summary>
		/// To create a rule for the event of candle closing price reduction below a specific level.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle closing price reduction below a specific level.</param>
		/// <param name="price">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenClosePriceLess(this ICandleManager candleManager, Candle candle, Unit price)
		{
			return new ChangedCandleRule(candleManager, candle, candle.CreateCandleCondition(price, c => c.ClosePrice, true))
			{
				Name = LocalizedStrings.Str1068Params.Put(candle, price)
			};
		}

		private static Func<Candle, bool> CreateCandleCondition(this Candle candle, Unit price, Func<Candle, decimal> currentPrice, bool isLess)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			if (price == null)
				throw new ArgumentNullException(nameof(price));

			if (currentPrice == null)
				throw new ArgumentNullException(nameof(currentPrice));

			if (price.Value == 0)
				throw new ArgumentException(LocalizedStrings.Str1051, nameof(price));

			if (price.Value < 0)
				throw new ArgumentException(LocalizedStrings.Str1052, nameof(price));

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
		/// To create a rule for the event of candle total volume excess above a specific level.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for the event of candle total volume excess above a specific level.</param>
		/// <param name="volume">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenTotalVolumeMore(this ICandleManager candleManager, Candle candle, Unit volume)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			var finishVolume = volume.Type == UnitTypes.Limit ? volume : candle.TotalVolume + volume;

			return new ChangedCandleRule(candleManager, candle, c => c.TotalVolume > finishVolume)
			{
				Name = candle + LocalizedStrings.Str1069Params.Put(volume)
			};
		}

		/// <summary>
		/// To create a rule for the event of candle total volume excess above a specific level.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series, from which a candle will be taken.</param>
		/// <param name="volume">The level. If the <see cref="Unit.Type"/> type equals to <see cref="UnitTypes.Limit"/>, specified price is set. Otherwise, shift value is specified.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCurrentCandleTotalVolumeMore(this ICandleManager candleManager, CandleSeries series, Unit volume)
		{
			if (series == null)
				throw new ArgumentNullException(nameof(series));

			var finishVolume = volume;

			if (volume.Type != UnitTypes.Limit)
			{
				var curCandle = candleManager.GetCurrentCandle<Candle>(series);

				if (curCandle == null)
					throw new ArgumentException(LocalizedStrings.Str1070, nameof(series));

				finishVolume = curCandle.TotalVolume + volume;	
			}

			return new CurrentCandleSeriesRule(candleManager, series, candle => candle.TotalVolume > finishVolume)
			{
				Name = series + LocalizedStrings.Str1071Params.Put(volume)
			};
		}

		/// <summary>
		/// To create a rule for the event of new candles occurrence.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for new candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandlesStarted(this ICandleManager candleManager, CandleSeries series)
		{
			return new CandleStartedRule(candleManager, series) { Name = LocalizedStrings.Str1072 + " " + series };
		}

		/// <summary>
		/// To create a rule for candle change event.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for changed candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandlesChanged(this ICandleManager candleManager, CandleSeries series)
		{
			return new CandleChangedSeriesRule(candleManager, series);
		}

		/// <summary>
		/// To create a rule for candles end event.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for end of candle.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandlesFinished(this ICandleManager candleManager, CandleSeries series)
		{
			return new CandleStateSeriesRule(candleManager, series, CandleStates.Finished) { Name = LocalizedStrings.Str1073 + " " + series };
		}

		/// <summary>
		/// To create a rule for the event of candles occurrence, change and end.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">Candles series to be traced for candles.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenCandles(this ICandleManager candleManager, CandleSeries series)
		{
			return new CandleStateSeriesRule(candleManager, series, CandleStates.Active, CandleStates.Finished)
			{
				Name = LocalizedStrings.Candles + " " + series
			};
		}

		/// <summary>
		/// To create a rule for candle change event.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for change.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenChanged(this ICandleManager candleManager, Candle candle)
		{
			return new ChangedCandleRule(candleManager, candle);
		}

		/// <summary>
		/// To create a rule for candle end event.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for end.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenFinished(this ICandleManager candleManager, Candle candle)
		{
			return new FinishedCandleRule(candleManager, candle).Once();
		}

		/// <summary>
		/// To create a rule for the event of candle partial end.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="candle">The candle to be traced for partial end.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="percent">The percentage of candle completion.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Candle, Candle> WhenPartiallyFinished(this ICandleManager candleManager, Candle candle, IConnector connector, decimal percent)
		{
			var rule = (candle is TimeFrameCandle)
						? (MarketRule<Candle, Candle>)new TimeFrameCandleChangedRule(candle, connector, percent)
			           	: new ChangedCandleRule(candleManager, candle, c => c.IsCandlePartiallyFinished(percent));

			rule.Name = LocalizedStrings.Str1075Params.Put(percent);
			return rule;
		}

		/// <summary>
		/// To create a rule for the event of candle partial end.
		/// </summary>
		/// <param name="candleManager">The candles manager.</param>
		/// <param name="series">The candle series to be traced for candle partial end.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="percent">The percentage of candle completion.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<CandleSeries, Candle> WhenPartiallyFinishedCandles(this ICandleManager candleManager, CandleSeries series, IConnector connector, decimal percent)
		{
			var rule = (series.CandleType == typeof(TimeFrameCandle))
				? (MarketRule<CandleSeries, Candle>)new TimeFrameCandlesChangedSeriesRule(candleManager, series, connector, percent)
				: new CandleChangedSeriesRule(candleManager, series, candle => candle.IsCandlePartiallyFinished(percent));

			rule.Name = LocalizedStrings.Str1076Params.Put(percent);
			return rule;
		}

		private sealed class TimeFrameCandleChangedRule : MarketRule<Candle, Candle>
		{
			private readonly MarketTimer _timer;

			public TimeFrameCandleChangedRule(Candle candle, IConnector connector, decimal percent)
				: base(candle)
			{
				_timer = CreateAndActivateTimeFrameTimer(candle.Security, (TimeSpan)candle.Arg, connector, () => Activate(candle), percent, false);
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

			public TimeFrameCandlesChangedSeriesRule(ICandleManager candleManager, CandleSeries series, IConnector connector, decimal percent)
				: base(series)
			{
				if (candleManager == null)
					throw new ArgumentNullException(nameof(candleManager));

				_timer = CreateAndActivateTimeFrameTimer(series.Security, (TimeSpan)series.Arg, connector, () => Activate(candleManager.GetCurrentCandle<Candle>(Series)), percent, true);
			}

			protected override void DisposeManaged()
			{
				_timer.Dispose();
				base.DisposeManaged();
			}
		}

		private static MarketTimer CreateAndActivateTimeFrameTimer(Security security, TimeSpan timeFrame, IConnector connector, Action callback, decimal percent, bool periodical)
		{
			if (security == null)
				throw new ArgumentNullException(nameof(security));

			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			if (callback == null)
				throw new ArgumentNullException(nameof(callback));

			if (percent <= 0)
				throw new ArgumentOutOfRangeException(nameof(percent), LocalizedStrings.Str1077);

			MarketTimer timer = null;

			timer = new MarketTimer(connector, () =>
			{
				if (periodical)
					timer.Interval(timeFrame);
				else
					timer.Stop();

				callback();
			});

			var time = connector.CurrentTime;
			var candleBounds = timeFrame.GetCandleBounds(time, security.Board);

			percent = percent / 100;

			var startTime = candleBounds.Min + TimeSpan.FromMilliseconds(timeFrame.TotalMilliseconds * (double)percent);

			var diff = startTime - time;

			if (diff == TimeSpan.Zero)
				timer.Interval(timeFrame);
			else if (diff > TimeSpan.Zero)
				timer.Interval(diff);
			else
				timer.Interval(timeFrame + diff);

			return timer.Start();
		}

		private static bool IsCandlePartiallyFinished(this Candle candle, decimal percent)
		{
			if (candle == null)
				throw new ArgumentNullException(nameof(candle));

			if (percent <= 0)
				throw new ArgumentOutOfRangeException(nameof(percent), LocalizedStrings.Str1077);

			var realPercent = percent / 100;

			var type = candle.GetType();

			if (type == typeof(TickCandle))
			{
				var tickCandle = (TickCandle)candle;
				var count = realPercent * (int)candle.Arg;
				return tickCandle.TotalTicks != null && tickCandle.TotalTicks.Value >= count;
			}
			else if (type == typeof(RangeCandle))
			{
				return (decimal)(candle.LowPrice + (Unit)candle.Arg) >= realPercent * candle.HighPrice;
			}
			else if (type == typeof(VolumeCandle))
			{
				var volume = realPercent * (decimal)candle.Arg;
				return candle.TotalVolume >= volume;
			}
			else
				throw new ArgumentOutOfRangeException(nameof(candle), type, LocalizedStrings.WrongCandleType);
		}

		#endregion

		#region IConnector rules

		private abstract class ConnectorRule<TArg> : MarketRule<IConnector, TArg>
		{
			protected ConnectorRule(IConnector connector)
				: base(connector)
			{
				Connector = connector ?? throw new ArgumentNullException(nameof(connector));
			}

			protected IConnector Connector { get; }
		}

		private sealed class IntervalTimeRule : ConnectorRule<IConnector>
		{
			private readonly MarketTimer _timer;

			public IntervalTimeRule(IConnector connector, TimeSpan interval/*, bool firstTimeRun*/)
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

		private sealed class NewMyTradeTraderRule : ConnectorRule<MyTrade>
		{
			public NewMyTradeTraderRule(IConnector connector)
				: base(connector)
			{
				Name = LocalizedStrings.Str1080;
				Connector.NewMyTrade += OnNewMyTrade;
			}

			private void OnNewMyTrade(MyTrade trade)
			{
				Activate(trade);
			}

			protected override void DisposeManaged()
			{
				Connector.NewMyTrade -= OnNewMyTrade;
				base.DisposeManaged();
			}
		}

		private sealed class NewOrderTraderRule : ConnectorRule<Order>
		{
			public NewOrderTraderRule(IConnector connector)
				: base(connector)
			{
				Name = LocalizedStrings.Str1081;
				Connector.NewOrder += OnNewOrder;
			}

			private void OnNewOrder(Order order)
			{
				Activate(order);
			}

			protected override void DisposeManaged()
			{
				Connector.NewOrder -= OnNewOrder;
				base.DisposeManaged();
			}
		}

		private class ConnectedRule : ConnectorRule<IMessageAdapter>
		{
			public ConnectedRule(IConnector connector)
				: base(connector)
			{
				Name = "Connected rule";
				Connector.ConnectedEx += OnConnectedEx;
			}

			private void OnConnectedEx(IMessageAdapter adapter)
			{
				Activate(adapter);
			}

			protected override void DisposeManaged()
			{
				Connector.ConnectedEx -= OnConnectedEx;
				base.DisposeManaged();
			}
		}

		private class DisconnectedRule : ConnectorRule<IMessageAdapter>
		{
			public DisconnectedRule(IConnector connector)
				: base(connector)
			{
				Name = "Disconnected rule";
				Connector.DisconnectedEx += OnDisconnectedEx;
			}

			private void OnDisconnectedEx(IMessageAdapter adapter)
			{
				Activate(adapter);
			}

			protected override void DisposeManaged()
			{
				Connector.DisconnectedEx -= OnDisconnectedEx;
				base.DisposeManaged();
			}
		}

		private class ConnectionLostRule : ConnectorRule<Tuple<IMessageAdapter, Exception>>
		{
			public ConnectionLostRule(IConnector connector)
				: base(connector)
			{
				Name = "Connection lost rule";
				Connector.ConnectionErrorEx += OnConnectionErrorEx;
			}

			private void OnConnectionErrorEx(IMessageAdapter adapter, Exception error)
			{
				Activate(Tuple.Create(adapter, error));
			}

			protected override void DisposeManaged()
			{
				Connector.ConnectionErrorEx -= OnConnectionErrorEx;
				base.DisposeManaged();
			}
		}

		/// <summary>
		/// To create a rule for the event <see cref="IConnector.MarketTimeChanged"/>, activated after expiration of <paramref name="interval" />.
		/// </summary>
		/// <param name="connector">Connection to the trading system.</param>
		/// <param name="interval">Interval.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<IConnector, IConnector> WhenIntervalElapsed(this IConnector connector, TimeSpan interval/*, bool firstTimeRun = false*/)
		{
			/*/// <param name="firstTimeRun">Сработает ли правило в момент создания (нулевое время). False по умолчанию.</param>*/

			if (connector == null)
				throw new ArgumentNullException(nameof(connector));

			return new IntervalTimeRule(connector, interval/*, firstTimeRun*/);
		}

		/// <summary>
		/// To create a rule for the event of new trade occurrences.
		/// </summary>
		/// <param name="connector">The connection to be traced for trades occurrences.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<IConnector, MyTrade> WhenNewMyTrade(this IConnector connector)
		{
			return new NewMyTradeTraderRule(connector);
		}

		/// <summary>
		/// To create a rule for the event of new orders occurrences.
		/// </summary>
		/// <param name="connector">The connection to be traced for orders occurrences.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<IConnector, Order> WhenNewOrder(this IConnector connector)
		{
			return new NewOrderTraderRule(connector);
		}

		/// <summary>
		/// To create a rule for the event of connection established.
		/// </summary>
		/// <param name="connector">The connection to be traced for state.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<IConnector, IMessageAdapter> WhenConnected(this IConnector connector)
		{
			return new ConnectedRule(connector);
		}

		/// <summary>
		/// To create a rule for the event of disconnection.
		/// </summary>
		/// <param name="connector">The connection to be traced for state.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<IConnector, IMessageAdapter> WhenDisconnected(this IConnector connector)
		{
			return new DisconnectedRule(connector);
		}

		/// <summary>
		/// To create a rule for the event of connection lost.
		/// </summary>
		/// <param name="connector">The connection to be traced for state.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<IConnector, Tuple<IMessageAdapter, Exception>> WhenConnectionLost(this IConnector connector)
		{
			return new ConnectionLostRule(connector);
		}

		#endregion

		#region Apply

		/// <summary>
		/// To form a rule (include <see cref="IMarketRule.IsReady"/>).
		/// </summary>
		/// <param name="rule">Rule.</param>
		/// <returns>Rule.</returns>
		public static IMarketRule Apply(this IMarketRule rule)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			return rule.Apply(DefaultRuleContainer);
		}

		/// <summary>
		/// To form a rule (include <see cref="IMarketRule.IsReady"/>).
		/// </summary>
		/// <param name="rule">Rule.</param>
		/// <param name="container">The rules container.</param>
		/// <returns>Rule.</returns>
		public static IMarketRule Apply(this IMarketRule rule, IMarketRuleContainer container)
		{
			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			if (container == null)
				throw new ArgumentNullException(nameof(container));

			container.Rules.Add(rule);
			return rule;
		}

		/// <summary>
		/// To form a rule (include <see cref="IMarketRule.IsReady"/>).
		/// </summary>
		/// <typeparam name="TToken">The type of token.</typeparam>
		/// <typeparam name="TArg">The type of argument, accepted by the rule.</typeparam>
		/// <param name="rule">Rule.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TToken, TArg> Apply<TToken, TArg>(this MarketRule<TToken, TArg> rule)
		{
			return rule.Apply(DefaultRuleContainer);
		}

		/// <summary>
		/// To form a rule (include <see cref="IMarketRule.IsReady"/>).
		/// </summary>
		/// <typeparam name="TToken">The type of token.</typeparam>
		/// <typeparam name="TArg">The type of argument, accepted by the rule.</typeparam>
		/// <param name="rule">Rule.</param>
		/// <param name="container">The rules container.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<TToken, TArg> Apply<TToken, TArg>(this MarketRule<TToken, TArg> rule, IMarketRuleContainer container)
		{
			return (MarketRule<TToken, TArg>)((IMarketRule)rule).Apply(container);
		}

		/// <summary>
		/// To activate the rule.
		/// </summary>
		/// <param name="container">The rules container.</param>
		/// <param name="rule">Rule.</param>
		/// <param name="process">The handler.</param>
		public static void ActiveRule(this IMarketRuleContainer container, IMarketRule rule, Func<bool> process)
		{
			if (process == null)
				throw new ArgumentNullException(nameof(process));

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

			ProcessStates IMarketRuleContainer.ProcessState => ProcessStates.Started;

			void IMarketRuleContainer.ActivateRule(IMarketRule rule, Func<bool> process)
			{
				this.ActiveRule(rule, process);
			}

			bool IMarketRuleContainer.IsRulesSuspended => _rulesSuspendCount > 0;

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

			IMarketRuleList IMarketRuleContainer.Rules => _rules;
		}

		/// <summary>
		/// The container of rules, which will be applied by default to all rules, not included into strategy.
		/// </summary>
		public static readonly IMarketRuleContainer DefaultRuleContainer = new MarketRuleContainer();

		/// <summary>
		/// To process rules in suspended mode (for example, create several rules and start them up simultaneously). After completion of method operation all rules, attached to the container resume their activity.
		/// </summary>
		/// <param name="action">The action to be processed at suspended rules. For example, to add several rules simultaneously.</param>
		public static void SuspendRules(Action action)
		{
			DefaultRuleContainer.SuspendRules(action);
		}

		/// <summary>
		/// To process rules in suspended mode (for example, create several rules and start them up simultaneously). After completion of method operation all rules, attached to the container resume their activity.
		/// </summary>
		/// <param name="container">The rules container.</param>
		/// <param name="action">The action to be processed at suspended rules. For example, to add several rules simultaneously.</param>
		public static void SuspendRules(this IMarketRuleContainer container, Action action)
		{
			if (container == null)
				throw new ArgumentNullException(nameof(container));

			if (action == null)
				throw new ArgumentNullException(nameof(action));

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
		/// To delete a rule. If a rule is executed at the time when this method is called, it will not be deleted.
		/// </summary>
		/// <param name="container">The rules container.</param>
		/// <param name="rule">Rule.</param>
		/// <param name="checkCanFinish">To check the possibility of rule suspension.</param>
		/// <returns><see langword="true" />, if a rule was successfully deleted, <see langword="false" />, if a rule cannot be currently deleted.</returns>
		public static bool TryRemoveRule(this IMarketRuleContainer container, IMarketRule rule, bool checkCanFinish = true)
		{
			if (container == null)
				throw new ArgumentNullException(nameof(container));

			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

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
		/// To delete the rule and all opposite rules. If the rule is executed at the time when this method is called, it will not be deleted.
		/// </summary>
		/// <param name="container">The rules container.</param>
		/// <param name="rule">Rule.</param>
		/// <returns><see langword="true" />, if rule was removed, otherwise, <see langword="false" />.</returns>
		public static bool TryRemoveWithExclusive(this IMarketRuleContainer container, IMarketRule rule)
		{
			if (container == null)
				throw new ArgumentNullException(nameof(container));

			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

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
		/// To make rules mutually exclusive.
		/// </summary>
		/// <param name="rule1">First rule.</param>
		/// <param name="rule2">Second rule.</param>
		public static void Exclusive(this IMarketRule rule1, IMarketRule rule2)
		{
			if (rule1 == null)
				throw new ArgumentNullException(nameof(rule1));

			if (rule2 == null)
				throw new ArgumentNullException(nameof(rule2));

			if (rule1 == rule2)
				throw new ArgumentException(LocalizedStrings.Str1085Params.Put(rule1), nameof(rule2));

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
					throw new ArgumentNullException(nameof(innerRules));

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

			Guid ILogSource.Id { get; } = Guid.NewGuid();

			ILogSource ILogSource.Parent
			{
				get => Container;
				set => throw new NotSupportedException();
			}

			LogLevels ILogSource.LogLevel
			{
				get => Container.LogLevel;
				set => throw new NotSupportedException();
			}

			event Action<LogMessage> ILogSource.Log
			{
				add => Container.Log += value;
				remove => Container.Log -= value;
			}

			DateTimeOffset ILogSource.CurrentTime => Container.CurrentTime;

			bool ILogSource.IsRoot => Container.IsRoot;

			#endregion

			void ILogReceiver.AddLog(LogMessage message)
			{
				Container.AddLog(new LogMessage(Container, message.Time, message.Level, () => message.Message));
			}

			#region Implementation of IMarketRuleContainer

			ProcessStates IMarketRuleContainer.ProcessState => Container.ProcessState;

			void IMarketRuleContainer.ActivateRule(IMarketRule rule, Func<bool> process)
			{
				process();
			}

			bool IMarketRuleContainer.IsRulesSuspended => Container.IsRulesSuspended;

			void IMarketRuleContainer.SuspendRules()
			{
				throw new NotSupportedException();
			}

			void IMarketRuleContainer.ResumeRules()
			{
				throw new NotSupportedException();
			}

			IMarketRuleList IMarketRuleContainer.Rules => throw new NotSupportedException();

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
		/// To combine rules by OR condition.
		/// </summary>
		/// <param name="rule">First rule.</param>
		/// <param name="rules">Additional rules.</param>
		/// <returns>Combined rule.</returns>
		public static IMarketRule Or(this IMarketRule rule, params IMarketRule[] rules)
		{
			return new OrRule(new[] { rule }.Concat(rules));
		}

		/// <summary>
		/// To combine rules by OR condition.
		/// </summary>
		/// <param name="rules">Rules.</param>
		/// <returns>Combined rule.</returns>
		public static IMarketRule Or(this IEnumerable<IMarketRule> rules)
		{
			return new OrRule(rules);
		}

		/// <summary>
		/// To combine rules by OR condition.
		/// </summary>
		/// <typeparam name="TToken">The type of token.</typeparam>
		/// <typeparam name="TArg">The type of argument, accepted by the rule.</typeparam>
		/// <param name="rule">First rule.</param>
		/// <param name="rules">Additional rules.</param>
		/// <returns>Combined rule.</returns>
		public static MarketRule<TToken, TArg> Or<TToken, TArg>(this MarketRule<TToken, TArg> rule, params MarketRule<TToken, TArg>[] rules)
		{
			return new OrRule<TToken, TArg>(new[] { rule }.Concat(rules));
		}

		/// <summary>
		/// To combine rules by AND condition.
		/// </summary>
		/// <param name="rule">First rule.</param>
		/// <param name="rules">Additional rules.</param>
		/// <returns>Combined rule.</returns>
		public static IMarketRule And(this IMarketRule rule, params IMarketRule[] rules)
		{
			return new AndRule(new[] { rule }.Concat(rules));
		}

		/// <summary>
		/// To combine rules by AND condition.
		/// </summary>
		/// <param name="rules">Rules.</param>
		/// <returns>Combined rule.</returns>
		public static IMarketRule And(this IEnumerable<IMarketRule> rules)
		{
			return new AndRule(rules);
		}

		/// <summary>
		/// To combine rules by AND condition.
		/// </summary>
		/// <typeparam name="TToken">The type of token.</typeparam>
		/// <typeparam name="TArg">The type of argument, accepted by the rule.</typeparam>
		/// <param name="rule">First rule.</param>
		/// <param name="rules">Additional rules.</param>
		/// <returns>Combined rule.</returns>
		public static MarketRule<TToken, TArg> And<TToken, TArg>(this MarketRule<TToken, TArg> rule, params MarketRule<TToken, TArg>[] rules)
		{
			return new AndRule<TToken, TArg>(new[] { rule }.Concat(rules));
		}

		#endregion

		/// <summary>
		/// To assign the rule a new name <see cref="IMarketRule.Name"/>.
		/// </summary>
		/// <typeparam name="TRule">The type of the rule.</typeparam>
		/// <param name="rule">Rule.</param>
		/// <param name="name">The rule new name.</param>
		/// <returns>Rule.</returns>
		public static TRule UpdateName<TRule>(this TRule rule, string name)
			where TRule : IMarketRule
		{
			return rule.Modify(r => r.Name = name);
		}

		/// <summary>
		/// To set the logging level.
		/// </summary>
		/// <typeparam name="TRule">The type of the rule.</typeparam>
		/// <param name="rule">Rule.</param>
		/// <param name="level">The level, on which logging is performed.</param>
		/// <returns>Rule.</returns>
		public static TRule UpdateLogLevel<TRule>(this TRule rule, LogLevels level)
			where TRule : IMarketRule
		{
			return rule.Modify(r => r.LogLevel = level);
		}

		/// <summary>
		/// To suspend or resume the rule.
		/// </summary>
		/// <typeparam name="TRule">The type of the rule.</typeparam>
		/// <param name="rule">Rule.</param>
		/// <param name="suspend"><see langword="true" /> - suspend, <see langword="false" /> - resume.</param>
		/// <returns>Rule.</returns>
		public static TRule Suspend<TRule>(this TRule rule, bool suspend)
			where TRule : IMarketRule
		{
			return rule.Modify(r => r.IsSuspended = suspend);
		}

		///// <summary>
		///// Синхронизировать или рассинхронизировать реагирование правила с другими правилами.
		///// </summary>
		///// <typeparam name="TRule">Тип правила.</typeparam>
		///// <param name="rule">Правило.</param>
		///// <param name="syncToken">Объект синхронизации. Если значение равно <see langword="null"/>, то правило рассинхронизовывается.</param>
		///// <returns>Правило.</returns>
		//public static TRule Sync<TRule>(this TRule rule, SyncObject syncToken)
		//	where TRule : IMarketRule
		//{
		//	return rule.Modify(r => r.SyncRoot = syncToken);
		//}

		/// <summary>
		/// To make the rule one-time rule (will be called only once).
		/// </summary>
		/// <typeparam name="TRule">The type of the rule.</typeparam>
		/// <param name="rule">Rule.</param>
		/// <returns>Rule.</returns>
		public static TRule Once<TRule>(this TRule rule)
			where TRule : IMarketRule
		{
			return rule.Modify(r => r.Until(() => true));
		}

		private static TRule Modify<TRule>(this TRule rule, Action<TRule> action)
			where TRule : IMarketRule
		{
			if (rule.IsNull())
				throw new ArgumentNullException(nameof(rule));

			action(rule);

			return rule;
		}

		/// <summary>
		/// To write the message from the rule.
		/// </summary>
		/// <param name="container">The rules container.</param>
		/// <param name="level">The level of the log message.</param>
		/// <param name="rule">Rule.</param>
		/// <param name="message">Text message.</param>
		/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
		public static void AddRuleLog(this IMarketRuleContainer container, LogLevels level, IMarketRule rule, string message, params object[] args)
		{
			if (container == null)
				return; // правило еще не было добавлено в контейнер

			if (rule == null)
				throw new ArgumentNullException(nameof(rule));

			if (rule.LogLevel != LogLevels.Inherit && rule.LogLevel > level)
				return;

			container.AddLog(level, () => LocalizedStrings.Str1087Params.Put(rule, message.Put(args)));
		}
	}
}
