namespace StockSharp.Algo;

partial class MarketRuleHelper
{
	private abstract class OrderRule<TArg>(Order order) : MarketRule<Order, TArg>(order)
	{
		protected override bool CanFinish()
		{
			return base.CanFinish() || CheckOrderState();
		}

		protected virtual bool CheckOrderState()
		{
			return Order.State.IsFinal();
		}

		protected Order Order { get; } = order ?? throw new ArgumentNullException(nameof(order));

		protected void TrySubscribe()
		{
			Subscribe();
			Container.AddRuleLog(LogLevels.Debug, this, LocalizedStrings.Subscribe);
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

	private abstract class ProviderOrderRule<TArg>(Order order, ISubscriptionProvider provider) : OrderRule<TArg>(order)
	{
		protected ISubscriptionProvider Provider { get; } = provider ?? throw new ArgumentNullException(nameof(provider));
	}

	private class RegisterFailedOrderRule : ProviderOrderRule<OrderFail>
	{
		public RegisterFailedOrderRule(Order order, ISubscriptionProvider provider)
			: base(order, provider)
		{
			Name = LocalizedStrings.ErrorRegistering;
			TrySubscribe();
		}

		protected override void Subscribe()
		{
			Provider.OrderRegisterFailReceived += OnOrderRegisterFailReceived;
		}

		protected override void UnSubscribe()
		{
			Provider.OrderRegisterFailReceived -= OnOrderRegisterFailReceived;
		}

		private void OnOrderRegisterFailReceived(Subscription subscription, OrderFail fail)
		{
			if (fail.Order == Order)
				Activate(fail);
		}
	}

	private class CancelFailedOrderRule : ProviderOrderRule<OrderFail>
	{
		public CancelFailedOrderRule(Order order, ISubscriptionProvider provider)
			: base(order, provider)
		{
			Name = LocalizedStrings.ErrorCancelling;
			TrySubscribe();
		}

		protected override void Subscribe()
		{
			Provider.OrderCancelFailReceived += OnOrderCancelFailReceived;
		}

		protected override void UnSubscribe()
		{
			Provider.OrderCancelFailReceived -= OnOrderCancelFailReceived;
		}

		private void OnOrderCancelFailReceived(Subscription subscription, OrderFail fail)
		{
			if (fail.Order == Order)
				Activate(fail);
		}
	}

	private class ChangedOrNewOrderRule : ProviderOrderRule<Order>
	{
		private readonly Func<Order, bool> _condition;
		private bool _activated;

		public ChangedOrNewOrderRule(Order order, ISubscriptionProvider provider)
			: this(order, provider, o => true)
		{
		}

		public ChangedOrNewOrderRule(Order order, ISubscriptionProvider provider, Func<Order, bool> condition)
			: base(order, provider)
		{
			_condition = condition ?? throw new ArgumentNullException(nameof(condition));

			Name = LocalizedStrings.OrderChange;

			TrySubscribe();
		}

		protected override void Subscribe()
		{
			Provider.OrderReceived += OnOrderReceived;
		}

		protected override void UnSubscribe()
		{
			Provider.OrderReceived -= OnOrderReceived;
		}

		private void OnOrderReceived(Subscription subscription, Order order)
		{
			if (!_activated && order == Order && _condition(order))
			{
				_activated = true;
				Activate(order);
			}
		}
	}

	[Obsolete]
	private class EditedOrderRule : OrderRule<Order>
	{
		private readonly ITransactionProvider _provider;

		public EditedOrderRule(Order order, ITransactionProvider provider)
			: base(order)
		{
			Name = "Order edit";

			_provider = provider ?? throw new ArgumentNullException(nameof(provider));

			TrySubscribe();
		}

		protected override void Subscribe()
		{
			_provider.OrderEdited += OnOrderEdited;
		}

		protected override void UnSubscribe()
		{
			_provider.OrderEdited -= OnOrderEdited;
		}

		private void OnOrderEdited(long transactionId, Order order)
		{
			if (order == Order)
				Activate(order);
		}
	}

	private class EditFailedOrderRule : ProviderOrderRule<OrderFail>
	{
		public EditFailedOrderRule(Order order, ISubscriptionProvider provider)
			: base(order, provider)
		{
			Name = nameof(ISubscriptionProvider.OrderEditFailReceived);
			TrySubscribe();
		}

		protected override void Subscribe()
		{
			Provider.OrderEditFailReceived += OnOrderEditFailedReceived;
		}

		protected override void UnSubscribe()
		{
			Provider.OrderEditFailReceived -= OnOrderEditFailedReceived;
		}

		private void OnOrderEditFailedReceived(Subscription subscription, OrderFail fail)
		{
			if (fail.Order == Order)
				Activate(fail);
		}
	}

	private class NewTradeOrderRule : ProviderOrderRule<MyTrade>
	{
		private decimal _receivedVolume;

		private bool AllTradesReceived => Order.State == OrderStates.Done && Order.GetMatchedVolume() == _receivedVolume;

		public NewTradeOrderRule(Order order, ISubscriptionProvider provider)
			: base(order, provider)
		{
			Name = LocalizedStrings.NewTrades;
			TrySubscribe();
		}

		protected override void Subscribe()
		{
			Provider.OwnTradeReceived += OnOwnTradeReceived;
		}

		protected override void UnSubscribe()
		{
			Provider.OwnTradeReceived -= OnOwnTradeReceived;
		}

		protected override bool CheckOrderState()
		{
			return Order.State == OrderStates.Failed || AllTradesReceived;
		}

		private void OnOwnTradeReceived(Subscription subscription, MyTrade trade)
		{
			if (trade.Order != Order /*&& (Order.Type != OrderTypes.Conditional || trade.Order != Order.DerivedOrder)*/)
				return;

			_receivedVolume += trade.Trade.Volume;
			Activate(trade);
		}
	}

	private class AllTradesOrderRule : ProviderOrderRule<IEnumerable<MyTrade>>
	{
		private decimal _receivedVolume;

		private readonly CachedSynchronizedList<MyTrade> _trades = [];

		public AllTradesOrderRule(Order order, ISubscriptionProvider provider)
			: base(order, provider)
		{
			Name = LocalizedStrings.AllTradesForOrder;
			TrySubscribe();
		}

		private bool AllTradesReceived => Order.State == OrderStates.Done && Order.GetMatchedVolume() == _receivedVolume;

		protected override void Subscribe()
		{
			Provider.OrderReceived += OnOrderReceived;
			Provider.OwnTradeReceived += OnOwnTradeReceived;
		}

		protected override void UnSubscribe()
		{
			Provider.OrderReceived -= OnOrderReceived;
			Provider.OwnTradeReceived -= OnOwnTradeReceived;
		}

		private void OnOrderReceived(Subscription subscription, Order order)
		{
			if (order == Order)
			{
				TryActivate();
			}
		}

		private void OnOwnTradeReceived(Subscription subscription, MyTrade trade)
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

	/// <summary>
	/// To create a rule for the event of successful order registration on exchange.
	/// </summary>
	/// <param name="order">The order to be traced for the event of successful registration.</param>
	/// <param name="provider">The transactional provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Order, Order> WhenRegistered(this Order order, ISubscriptionProvider provider)
	{
		if (order == null)
			throw new ArgumentNullException(nameof(order));

		return new ChangedOrNewOrderRule(order, provider, o => o.State is OrderStates.Active or OrderStates.Done) { Name = LocalizedStrings.OrderRegistering }.Once();
	}

	/// <summary>
	/// To create a rule for the event of order partial matching.
	/// </summary>
	/// <param name="order">The order to be traced for partial matching event.</param>
	/// <param name="provider">The transactional provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Order, Order> WhenPartiallyMatched(this Order order, ISubscriptionProvider provider)
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
			Name = LocalizedStrings.OrderFilledPartially,
		};
	}

	/// <summary>
	/// To create a for the event of order unsuccessful registration on exchange.
	/// </summary>
	/// <param name="order">The order to be traced for unsuccessful registration event.</param>
	/// <param name="provider">The transactional provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Order, OrderFail> WhenRegisterFailed(this Order order, ISubscriptionProvider provider)
	{
		return new RegisterFailedOrderRule(order, provider).Once();
	}

	/// <summary>
	/// To create a rule for the event of unsuccessful order cancelling on exchange.
	/// </summary>
	/// <param name="order">The order to be traced for unsuccessful cancelling event.</param>
	/// <param name="provider">The transactional provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Order, OrderFail> WhenCancelFailed(this Order order, ISubscriptionProvider provider)
	{
		return new CancelFailedOrderRule(order, provider);
	}

	/// <summary>
	/// To create a rule for the order cancelling event.
	/// </summary>
	/// <param name="order">The order to be traced for cancelling event.</param>
	/// <param name="provider">The transactional provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Order, Order> WhenCanceled(this Order order, ISubscriptionProvider provider)
	{
		return new ChangedOrNewOrderRule(order, provider, o => o.IsCanceled()) { Name = LocalizedStrings.CancelOrders }.Once();
	}

	/// <summary>
	/// To create a rule for the event of order fully matching.
	/// </summary>
	/// <param name="order">The order to be traced for the fully matching event.</param>
	/// <param name="provider">The transactional provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Order, Order> WhenMatched(this Order order, ISubscriptionProvider provider)
	{
		return new ChangedOrNewOrderRule(order, provider, o => o.IsMatched()) { Name = LocalizedStrings.Matching }.Once();
	}

	/// <summary>
	/// To create a rule for the order change event.
	/// </summary>
	/// <param name="order">The order to be traced for the change event.</param>
	/// <param name="provider">The transactional provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Order, Order> WhenChanged(this Order order, ISubscriptionProvider provider)
	{
		return new ChangedOrNewOrderRule(order, provider);
	}

	/// <summary>
	/// To create a rule for the order <see cref="ITransactionProvider.OrderEdited"/> event.
	/// </summary>
	/// <param name="order">The order to be traced.</param>
	/// <param name="provider">The transactional provider.</param>
	/// <returns>Rule.</returns>
	[Obsolete("Use WhenChanged rule.")]
	public static MarketRule<Order, Order> WhenEdited(this Order order, ITransactionProvider provider)
	{
		return new EditedOrderRule(order, provider);
	}

	/// <summary>
	/// To create a rule for the order <see cref="ISubscriptionProvider.OrderEditFailReceived"/> event.
	/// </summary>
	/// <param name="order">The order to be traced.</param>
	/// <param name="provider">The transactional provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Order, OrderFail> WhenEditFailed(this Order order, ISubscriptionProvider provider)
	{
		return new EditFailedOrderRule(order, provider);
	}

	/// <summary>
	/// To create a rule for the event of trade occurrence for the order.
	/// </summary>
	/// <param name="order">The order to be traced for trades occurrence events.</param>
	/// <param name="provider">The transactional provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Order, MyTrade> WhenNewTrade(this Order order, ISubscriptionProvider provider)
	{
		return new NewTradeOrderRule(order, provider);
	}

	/// <summary>
	/// To create a rule for the event of all trades occurrence for the order.
	/// </summary>
	/// <param name="order">The order to be traced for all trades occurrence event.</param>
	/// <param name="provider">The transactional provider.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<Order, IEnumerable<MyTrade>> WhenAllTrades(this Order order, ISubscriptionProvider provider)
	{
		return new AllTradesOrderRule(order, provider);
	}
}
