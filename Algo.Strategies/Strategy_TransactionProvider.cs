namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	// Backing delegates for the transaction-provider event surface. The decomposed engine already exposes
	// strongly-typed events (OrderRegistered, NewMyTrade, OrderEdited, OrderRegisterFailed, OrderCancelFailed),
	// which implicitly satisfy the matching ITransactionProvider members. The handlers below cover the events
	// that have no decomposed counterpart yet: the per-order NewOrder/OrderChanged stream and the mass /
	// lookup notifications that the new pipeline does not raise on its own.

	private Action<Order> _txNewOrder;
	private Action<Order> _txOrderChanged;

	private bool _txEventsHooked;

	/// <summary>
	/// Ensure the internal pipeline hooks that feed the transaction-provider NewOrder/OrderChanged streams
	/// are installed exactly once. Wired lazily on first subscription so the cost is avoided for consumers
	/// that never listen to the obsolete order events.
	/// </summary>
	private void EnsureTxEventsHooked()
	{
		if (_txEventsHooked)
			return;

		_txEventsHooked = true;

		OrderProcessor.Registered += o => _txNewOrder?.Invoke(o);
		OrderProcessor.Changed += o => _txOrderChanged?.Invoke(o);
	}

	IdGenerator ITransactionProvider.TransactionIdGenerator => _connector?.TransactionIdGenerator;

	[Obsolete("Use ISubscriptionProvider.OrderReceived event.")]
	event Action<Order> ITransactionProvider.NewOrder
	{
		add { EnsureTxEventsHooked(); _txNewOrder += value; }
		remove => _txNewOrder -= value;
	}

	[Obsolete("Use ISubscriptionProvider.OrderReceived event.")]
	event Action<Order> ITransactionProvider.OrderChanged
	{
		add { EnsureTxEventsHooked(); _txOrderChanged += value; }
		remove => _txOrderChanged -= value;
	}

	event Action<long> ITransactionProvider.MassOrderCanceled
	{
		add { }
		remove { }
	}

	event Action<long, DateTime> ITransactionProvider.MassOrderCanceled2
	{
		add { }
		remove { }
	}

	event Action<long, Exception> ITransactionProvider.MassOrderCancelFailed
	{
		add { }
		remove { }
	}

	event Action<long, Exception, DateTime> ITransactionProvider.MassOrderCancelFailed2
	{
		add { }
		remove { }
	}

	[Obsolete("Use ISubscriptionProvider.PortfolioReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, Exception> ITransactionProvider.LookupPortfoliosResult
	{
		add { }
		remove { }
	}

	[Obsolete("Use ISubscriptionProvider.PortfolioReceived and ISubscriptionProvider.SubscriptionStopped events.")]
	event Action<PortfolioLookupMessage, IEnumerable<Portfolio>, IEnumerable<Portfolio>, Exception> ITransactionProvider.LookupPortfoliosResult2
	{
		add { }
		remove { }
	}

	/// <summary>
	/// Trades, matched during the strategy operation.
	/// </summary>
	[Browsable(false)]
	public IEnumerable<MyTrade> MyTrades => Trades.MyTrades;

	/// <summary>
	/// Try add own trade.
	/// </summary>
	/// <param name="trade"><see cref="MyTrade"/></param>
	/// <returns>Operation result.</returns>
	public bool TryAddMyTrade(MyTrade trade)
	{
		if (trade is null)
			throw new ArgumentNullException(nameof(trade));

		// Delegate deduplication, PnL/commission/slippage accounting and the TradeAdded fan-out to the
		// trade pipeline. All the side effects the monolith performed inline (NewMyTrade, RaisePnLChanged,
		// RaiseCommissionChanged, RaiseSlippageChanged, statistics, position stamping) are already wired
		// from the pipeline's events in the Strategy constructor, so a successful TryAdd reproduces them.
		return Trades.TryAdd(trade);
	}

	/// <summary>
	/// Cancel active orders matching the specified filter.
	/// </summary>
	/// <param name="isStopOrder"><see langword="true" />, if cancel only a stop orders, <see langword="false" /> - if regular orders, <see langword="null" /> - both.</param>
	/// <param name="portfolio">Portfolio. If the value is equal to <see langword="null" />, then the portfolio does not match the orders cancel filter.</param>
	/// <param name="direction">Order side. If the value is <see langword="null" />, the direction does not use.</param>
	/// <param name="board">Trading board. If the value is equal to <see langword="null" />, then the board does not match the orders cancel filter.</param>
	/// <param name="security">Instrument. If the value is equal to <see langword="null" />, then the instrument does not match the orders cancel filter.</param>
	/// <param name="securityType">Security type. If the value is <see langword="null" />, the type does not use.</param>
	/// <param name="transactionId">Order cancellation transaction id.</param>
	public void CancelActiveOrders(bool? isStopOrder = default, Portfolio portfolio = default, Sides? direction = default, ExchangeBoard board = default, Security security = default, SecurityTypes? securityType = default, long? transactionId = default)
	{
		if (ProcessState != ProcessStates.Started)
		{
			LogInfo(LocalizedStrings.WaitingCancellingAllOrders);
			return;
		}

		LogInfo(LocalizedStrings.CancelAll);

		foreach (var order in Orders.Where(o => o.State == OrderStates.Active))
		{
			if (isStopOrder is not null && isStopOrder != (order.Type == OrderTypes.Conditional))
				continue;

			if (portfolio is not null && order.Portfolio != portfolio)
				continue;

			if (direction is not null && order.Side != direction)
				continue;

			if (board is not null && order.Security?.Board != board)
				continue;

			if (security is not null && order.Security != security)
				continue;

			if (securityType is not null && order.Security?.Type != securityType)
				continue;

			if (transactionId is not null && order.TransactionId != transactionId)
				continue;

			CancelOrder(order);
		}
	}

	void ITransactionProvider.CancelOrders(bool? isStopOrder, Portfolio portfolio, Sides? direction, ExchangeBoard board, Security security, SecurityTypes? securityType, long? transactionId)
		=> CancelActiveOrders(isStopOrder, portfolio, direction, board, security, securityType, transactionId);

	/// <summary>
	/// Determines the specified order can be edited by <see cref="EditOrder"/>.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <returns><see langword="true"/> if the order is editable, <see langword="false"/> order cannot be changed, <see langword="null"/> means no information.</returns>
	public bool? IsOrderEditable(Order order) => _connector?.IsOrderEditable(order);

	/// <summary>
	/// Determines the specified order can be replaced by <see cref="ReRegisterOrder"/>.
	/// </summary>
	/// <param name="order">Order.</param>
	/// <returns><see langword="true"/> if the order is replaceable, <see langword="false"/> order cannot be replaced, <see langword="null"/> means no information.</returns>
	public bool? IsOrderReplaceable(Order order) => _connector?.IsOrderReplaceable(order);

	/// <summary>
	/// Apply an incoming remote command to the strategy.
	/// </summary>
	/// <param name="cmdMsg"><see cref="CommandMessage"/>.</param>
	public virtual void ApplyCommand(CommandMessage cmdMsg)
	{
		if (cmdMsg == null)
			throw new ArgumentNullException(nameof(cmdMsg));

		var parameters = cmdMsg.Parameters;

		switch (cmdMsg.Command)
		{
			case CommandTypes.Start:
			{
#pragma warning disable CS0618 // the Start()/Stop() shims drive the async entry points, matching the monolith.
				Start();
#pragma warning restore CS0618
				break;
			}

			case CommandTypes.Stop:
			{
#pragma warning disable CS0618 // the Start()/Stop() shims drive the async entry points, matching the monolith.
				Stop();
#pragma warning restore CS0618
				break;
			}

			case CommandTypes.CancelOrders:
			{
				CancelActiveOrders();
				break;
			}

			case CommandTypes.RegisterOrder:
			{
				var secId = parameters.TryGet(nameof(Order.Security));
				var pfName = parameters.TryGet(nameof(Order.Portfolio));
				var side = parameters[nameof(Order.Side)].To<Sides>();
				var volume = parameters[nameof(Order.Volume)].To<decimal>();
				var price = parameters.TryGet(nameof(Order.Price)).To<decimal?>() ?? 0;
				var comment = parameters.TryGet(nameof(Order.Comment));
				var clientCode = parameters.TryGet(nameof(Order.ClientCode));
				var tif = parameters.TryGet(nameof(Order.TimeInForce)).To<TimeInForce?>();

				var order = new Order
				{
					Security = secId.IsEmpty() ? Security : this.LookupById(secId),
					Portfolio = pfName.IsEmpty() ? Portfolio : Connector.LookupByPortfolioName(pfName),
					Side = side,
					Volume = volume,
					Price = price,
					Comment = comment,
					ClientCode = clientCode,
					TimeInForce = tif,
				};

				RegisterOrder(order);

				break;
			}

			case CommandTypes.CancelOrder:
			{
				var orderId = parameters[nameof(Order.Id)].To<long>();

				CancelOrder(Orders.First(o => o.Id == orderId));

				break;
			}

			case CommandTypes.ClosePosition:
			{
				ClosePosition();
				break;
			}
		}
	}
}