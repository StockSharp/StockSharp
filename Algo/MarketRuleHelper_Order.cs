namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class MarketRuleHelper
	{
		private abstract class OrderRule<TArg> : MarketRule<Order, TArg>
		{
			protected OrderRule(Order order, ITransactionProvider provider)
				: base(order)
			{
				Order = order ?? throw new ArgumentNullException(nameof(order));
				Provider = provider ?? throw new ArgumentNullException(nameof(provider));
			}

			protected override bool CanFinish()
			{
				return base.CanFinish() || CheckOrderState();
			}

			protected virtual bool CheckOrderState()
			{
				return Order.State.IsFinal();
			}

			protected Order Order { get; }

			protected ITransactionProvider Provider { get; }

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

			/// <inheritdoc />
			public override string ToString()
			{
				var order = Order;
				var strId = order.Id == null ? order.StringId : order.Id.To<string>();
				return $"{Name} {order.TransactionId}/{strId}";
			}
		}

		private sealed class RegisterFailedOrderRule : OrderRule<OrderFail>
		{
			public RegisterFailedOrderRule(Order order, ITransactionProvider provider)
				: base(order, provider)
			{
				Name = LocalizedStrings.Str717 + " ";
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				Provider.OrderRegisterFailed += OnOrderRegisterFailed;
			}

			protected override void UnSubscribe()
			{
				Provider.OrderRegisterFailed -= OnOrderRegisterFailed;
			}

			private void OnOrderRegisterFailed(OrderFail fail)
			{
				if (fail.Order == Order)
					Activate(fail);
			}
		}

		private sealed class CancelFailedOrderRule : OrderRule<OrderFail>
		{
			public CancelFailedOrderRule(Order order, ITransactionProvider provider)
				: base(order, provider)
			{
				Name = LocalizedStrings.Str1030;
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				Provider.OrderCancelFailed += OnOrderCancelFailed;
			}

			protected override void UnSubscribe()
			{
				Provider.OrderCancelFailed -= OnOrderCancelFailed;
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

			public ChangedOrNewOrderRule(Order order, ITransactionProvider provider)
				: this(order, provider, o => true)
			{
			}

			public ChangedOrNewOrderRule(Order order, ITransactionProvider provider, Func<Order, bool> condition)
				: base(order, provider)
			{
				_condition = condition ?? throw new ArgumentNullException(nameof(condition));

				Name = LocalizedStrings.Str1031;

				TrySubscribe();
			}

			protected override void Subscribe()
			{
				Provider.OrderChanged += OnOrderChanged;
				Provider.NewOrder += OnOrderChanged;
			}

			protected override void UnSubscribe()
			{
				Provider.OrderChanged -= OnOrderChanged;
				Provider.NewOrder -= OnOrderChanged;
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

		private sealed class EditedOrderRule : OrderRule<Order>
		{
			public EditedOrderRule(Order order, ITransactionProvider provider)
				: base(order, provider)
			{
				Name = "Order edit";
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				Provider.OrderEdited += OnOrderEdited;
			}

			protected override void UnSubscribe()
			{
				Provider.OrderEdited -= OnOrderEdited;
			}

			private void OnOrderEdited(long transactionId, Order order)
			{
				if (order == Order)
					Activate(order);
			}
		}

		private sealed class EditFailedOrderRule : OrderRule<OrderFail>
		{
			public EditFailedOrderRule(Order order, ITransactionProvider provider)
				: base(order, provider)
			{
				Name = "Order edit failed";
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				Provider.OrderEditFailed += OnOrderEditFailed;
			}

			protected override void UnSubscribe()
			{
				Provider.OrderEditFailed -= OnOrderEditFailed;
			}

			private void OnOrderEditFailed(long transactionId, OrderFail fail)
			{
				if (fail.Order == Order)
					Activate(fail);
			}
		}

		private class NewTradeOrderRule : OrderRule<MyTrade>
		{
			private decimal _receivedVolume;

			private bool AllTradesReceived => Order.State == OrderStates.Done && Order.GetMatchedVolume() == _receivedVolume;

			public NewTradeOrderRule(Order order, ITransactionProvider provider)
				: base(order, provider)
			{
				Name = LocalizedStrings.Str1032;
				TrySubscribe();
			}

			protected override void Subscribe()
			{
				Provider.NewMyTrade += OnNewMyTrade;
			}

			protected override void UnSubscribe()
			{
				Provider.NewMyTrade -= OnNewMyTrade;
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

			public AllTradesOrderRule(Order order, ITransactionProvider provider)
				: base(order, provider)
			{
				Name = LocalizedStrings.Str1033;
				TrySubscribe();
			}

			private bool AllTradesReceived => Order.State == OrderStates.Done && Order.GetMatchedVolume() == _receivedVolume;

			protected override void Subscribe()
			{
				Provider.OrderChanged += OnOrderChanged;
				Provider.NewOrder += OnOrderChanged;

				Provider.NewMyTrade += OnNewMyTrade;
			}

			protected override void UnSubscribe()
			{
				Provider.OrderChanged -= OnOrderChanged;
				Provider.NewOrder -= OnOrderChanged;

				Provider.NewMyTrade -= OnNewMyTrade;
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
			private readonly IMarketDataProvider _marketDataProvider;
			private decimal? _bestBidPrice;
			private decimal? _bestAskPrice;
			private decimal _averagePrice;

			private readonly List<Tuple<decimal, decimal>> _trades = new List<Tuple<decimal, decimal>>();

			public OrderTakeProfitStopLossRule(Order order, Unit offset, bool isTake, ITransactionProvider transactionProvider, IMarketDataProvider marketDataProvider)
				: base(order, transactionProvider)
			{
				if (offset == null)
					throw new ArgumentNullException(nameof(offset));

				if (offset.Value <= 0)
					throw new ArgumentOutOfRangeException(nameof(offset));

				_offset = offset;
				_isTake = isTake;
				_marketDataProvider = marketDataProvider ?? throw new ArgumentNullException(nameof(marketDataProvider));

				Name = _isTake ? LocalizedStrings.TakeProfit : LocalizedStrings.StopLoss;

				TrySubscribe();
			}

			protected override void Subscribe()
			{
#pragma warning disable CS0618 // Type or member is obsolete
				_marketDataProvider.MarketDepthChanged += OnMarketDepthChanged;
#pragma warning restore CS0618 // Type or member is obsolete
				Provider.NewMyTrade += OnNewMyTrade;
			}

			protected override void UnSubscribe()
			{
#pragma warning disable CS0618 // Type or member is obsolete
				_marketDataProvider.MarketDepthChanged -= OnMarketDepthChanged;
#pragma warning restore CS0618 // Type or member is obsolete
				Provider.NewMyTrade -= OnNewMyTrade;
			}

			private void OnMarketDepthChanged(MarketDepth depth)
			{
				if (depth.Security != Order.Security)
					return;

				_bestBidPrice = depth.BestBid2?.Price;
				_bestAskPrice = depth.BestAsk2?.Price;

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
		/// <param name="provider">The transactional provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenRegistered(this Order order, ITransactionProvider provider)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			return new ChangedOrNewOrderRule(order, provider, o => o.State == OrderStates.Active || o.State == OrderStates.Done) { Name = LocalizedStrings.Str1034 }.Once();
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
		/// <param name="provider">The transactional provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenPartiallyMatched(this Order order, ITransactionProvider provider)
		{
			if (order == null)
				throw new ArgumentNullException(nameof(order));

			var balance = order.Volume;
			var hasVolume = balance != 0;

			return new ChangedOrNewOrderRule(order, provider, o =>
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
		/// <param name="provider">The transactional provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, OrderFail> WhenRegisterFailed(this Order order, ITransactionProvider provider)
		{
			return new RegisterFailedOrderRule(order, provider).Once();
		}

		/// <summary>
		/// To create a rule for the event of unsuccessful order cancelling on exchange.
		/// </summary>
		/// <param name="order">The order to be traced for unsuccessful cancelling event.</param>
		/// <param name="provider">The transactional provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, OrderFail> WhenCancelFailed(this Order order, ITransactionProvider provider)
		{
			return new CancelFailedOrderRule(order, provider);
		}

		/// <summary>
		/// To create a rule for the order cancelling event.
		/// </summary>
		/// <param name="order">The order to be traced for cancelling event.</param>
		/// <param name="provider">The transactional provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenCanceled(this Order order, ITransactionProvider provider)
		{
			return new ChangedOrNewOrderRule(order, provider, o => o.IsCanceled()) { Name = LocalizedStrings.Str1037 }.Once();
		}

		/// <summary>
		/// To create a rule for the event of order fully matching.
		/// </summary>
		/// <param name="order">The order to be traced for the fully matching event.</param>
		/// <param name="provider">The transactional provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenMatched(this Order order, ITransactionProvider provider)
		{
			return new ChangedOrNewOrderRule(order, provider, o => o.IsMatched()) { Name = LocalizedStrings.Str1038 }.Once();
		}

		/// <summary>
		/// To create a rule for the order change event.
		/// </summary>
		/// <param name="order">The order to be traced for the change event.</param>
		/// <param name="provider">The transactional provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenChanged(this Order order, ITransactionProvider provider)
		{
			return new ChangedOrNewOrderRule(order, provider);
		}

		/// <summary>
		/// To create a rule for the order <see cref="ITransactionProvider.OrderEdited"/> event.
		/// </summary>
		/// <param name="order">The order to be traced.</param>
		/// <param name="provider">The transactional provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenEdited(this Order order, ITransactionProvider provider)
		{
			return new EditedOrderRule(order, provider);
		}

		/// <summary>
		/// To create a rule for the order <see cref="ITransactionProvider.OrderEditFailed"/> event.
		/// </summary>
		/// <param name="order">The order to be traced.</param>
		/// <param name="provider">The transactional provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, OrderFail> WhenEditFailed(this Order order, ITransactionProvider provider)
		{
			return new EditFailedOrderRule(order, provider);
		}

		/// <summary>
		/// To create a rule for the event of trade occurrence for the order.
		/// </summary>
		/// <param name="order">The order to be traced for trades occurrence events.</param>
		/// <param name="provider">The transactional provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, MyTrade> WhenNewTrade(this Order order, ITransactionProvider provider)
		{
			return new NewTradeOrderRule(order, provider);
		}

		/// <summary>
		/// To create a rule for the event of all trades occurrence for the order.
		/// </summary>
		/// <param name="order">The order to be traced for all trades occurrence event.</param>
		/// <param name="provider">The transactional provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, IEnumerable<MyTrade>> WhenAllTrades(this Order order, ITransactionProvider provider)
		{
			return new AllTradesOrderRule(order, provider);
		}

		/// <summary>
		/// To create a rule for the order's profit more on offset.
		/// </summary>
		/// <param name="order">The order to be traced for profit.</param>
		/// <param name="profitOffset">Profit offset.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenProfitMore(this Order order, Unit profitOffset, IConnector connector)
		{
			return WhenProfitMore(order, profitOffset, connector, connector);
		}

		/// <summary>
		/// To create a rule for the order's profit more on offset.
		/// </summary>
		/// <param name="order">The order to be traced for profit.</param>
		/// <param name="profitOffset">Profit offset.</param>
		/// <param name="transactionProvider">The transactional provider.</param>
		/// <param name="marketDataProvider">The market data provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenProfitMore(this Order order, Unit profitOffset, ITransactionProvider transactionProvider, IMarketDataProvider marketDataProvider)
		{
			return new OrderTakeProfitStopLossRule(order, profitOffset, true, transactionProvider, marketDataProvider);
		}

		/// <summary>
		/// To create a rule for the order's loss more on offset.
		/// </summary>
		/// <param name="order">The order to be traced for loss.</param>
		/// <param name="profitOffset">Loss offset.</param>
		/// <param name="connector">Connection to the trading system.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenLossMore(this Order order, Unit profitOffset, IConnector connector)
		{
			return WhenLossMore(order, profitOffset, connector, connector);
		}

		/// <summary>
		/// To create a rule for the order's loss more on offset.
		/// </summary>
		/// <param name="order">The order to be traced for loss.</param>
		/// <param name="profitOffset">Loss offset.</param>
		/// <param name="transactionProvider">The transactional provider.</param>
		/// <param name="marketDataProvider">The market data provider.</param>
		/// <returns>Rule.</returns>
		public static MarketRule<Order, Order> WhenLossMore(this Order order, Unit profitOffset, ITransactionProvider transactionProvider, IMarketDataProvider marketDataProvider)
		{
			return new OrderTakeProfitStopLossRule(order, profitOffset, false, transactionProvider, marketDataProvider);
		}
	}
}
