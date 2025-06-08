namespace StockSharp.Diagram.Elements;

/// <summary>
/// Order registering element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderRegisteringKey,
	Description = LocalizedStrings.OrderRegisteringDescKey,
	GroupName = LocalizedStrings.OrdersKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/orders/register.html")]
public class OrderRegisterDiagramElement : OrderRegisterBaseDiagramElement
{
	private readonly DiagramSocket _securitySocket;
	private readonly DiagramSocket _portfolioSocket;
	private readonly DiagramSocket _volumeSocket;
	private DiagramSocket _priceSocket;

	private readonly DiagramSocket _orderSocket;
	private readonly DiagramSocket _orderFailSocket;
	private readonly DiagramSocket _tradeSocket;
	private readonly DiagramSocket _orderCancelledSocket;
	private readonly DiagramSocket _orderMatchedSocket;
	private readonly DiagramSocket _orderFinishedSocket;

	private bool _isOrderSocket;
	private bool _isOrderFailSocket;
	private bool _isTradeSocket;
	private bool _isCancelledSocket;
	private bool _isMatchedSocket;
	private bool _isFinishedSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "3038FF97-D9C6-4EF5-8A8B-280E4C754CB8".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "ShoppingCartAdd";

	private readonly DiagramElementParam<Sides> _direction;

	/// <summary>
	/// Direction.
	/// </summary>
	public Sides Direction
	{
		get => _direction.Value;
		set => _direction.Value = value;
	}

	private readonly DiagramElementParam<bool> _isMarket;

	/// <summary>
	/// Market order type.
	/// </summary>
	public bool IsMarket
	{
		get => _isMarket.Value;
		set => _isMarket.Value = value;
	}

	private readonly DiagramElementParam<bool> _zeroAsMarket;

	/// <summary>
	/// Zero price makes market order.
	/// </summary>
	public bool ZeroAsMarket
	{
		get => _zeroAsMarket.Value;
		set => _zeroAsMarket.Value = value;
	}

	private readonly DiagramElementParam<bool> _shrinkPrice;

	/// <summary>
	/// Shrink order price.
	/// </summary>
	public bool ShrinkPrice
	{
		get => _shrinkPrice.Value;
		set => _shrinkPrice.Value = value;
	}

	private readonly DiagramElementParam<DateTimeOffset?> _expiryDate;

	/// <summary>
	/// Order expiry time. The default is <see langword="null" />, which mean (GTC).
	/// </summary>
	public DateTimeOffset? ExpiryDate
	{
		get => _expiryDate.Value;
		set => _expiryDate.Value = value;
	}

	private readonly DiagramElementParam<decimal?> _slippage;

	/// <summary>
	/// Slippage in trade price.
	/// </summary>
	public decimal? Slippage
	{
		get => _slippage.Value;
		set => _slippage.Value = value;
	}

	private readonly DiagramElementParam<OrderConditionSettings> _conditionalSettings;

	/// <summary>
	/// <see cref="OrderConditionSettings"/>
	/// </summary>
	public OrderConditionSettings ConditionalSettings
	{
		get => _conditionalSettings.Value;
		set => _conditionalSettings.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderRegisterDiagramElement"/>.
	/// </summary>
	public OrderRegisterDiagramElement()
	{
		_securitySocket		= AddInput(StaticSocketIds.Security, LocalizedStrings.Security, DiagramSocketType.Security, index: 0);
		UpdatePriceSocket(true);
		_portfolioSocket	= AddInput(StaticSocketIds.Portfolio, LocalizedStrings.Portfolio, DiagramSocketType.Portfolio, index: 4);
		_volumeSocket		= AddInput(StaticSocketIds.Volume, LocalizedStrings.Volume, DiagramSocketType.Unit, index: 3);

		_orderSocket			= AddOutput(StaticSocketIds.Order, LocalizedStrings.Order, DiagramSocketType.Order);
		_orderFailSocket		= AddOutput(StaticSocketIds.OrderFail, LocalizedStrings.OrderFail, DiagramSocketType.OrderFail);
		_tradeSocket			= AddOutput(StaticSocketIds.MyTrade, LocalizedStrings.OwnTrade, DiagramSocketType.MyTrade);
		_orderCancelledSocket	= AddOutput("Cancelled", LocalizedStrings.Cancel, DiagramSocketType.Order, isDynamic: false);
		_orderMatchedSocket		= AddOutput("Matched", LocalizedStrings.Match, DiagramSocketType.Order, isDynamic: false);
		_orderFinishedSocket	= AddOutput("Finished", LocalizedStrings.Finished, DiagramSocketType.Order, isDynamic: false);

		_direction = AddParam(nameof(Direction), Sides.Buy)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Order, LocalizedStrings.Direction, LocalizedStrings.PosSide, 30)
			.SetOnValueChangedHandler(_ => UpdateElementName());

		_isMarket = AddParam<bool>(nameof(IsMarket))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Order, LocalizedStrings.Market, LocalizedStrings.PosOpenByMarket, 50)
			.SetOnValueChangedHandler(_ => UpdatePriceSocket());

		_zeroAsMarket = AddParam<bool>(nameof(ZeroAsMarket))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Order, LocalizedStrings.ZeroPrice, LocalizedStrings.ZeroAsMarket, 60);

		_expiryDate = AddParam(nameof(ExpiryDate), (DateTimeOffset?)null)
			.SetDisplay(LocalizedStrings.ExtraConditions, LocalizedStrings.Expiration, LocalizedStrings.OrderExpirationTime, 100);

		_conditionalSettings = AddParam<OrderConditionSettings>(nameof(ConditionalSettings))
			.SetDisplay(LocalizedStrings.ExtraConditions, LocalizedStrings.ConditionalOrder, LocalizedStrings.ConditionalOrder, 130);

		_shrinkPrice = AddParam(nameof(ShrinkPrice), true)
			.SetDisplay(LocalizedStrings.ExtraConditions, LocalizedStrings.ShrinkPrice, LocalizedStrings.ShrinkPrice, 150);

		_slippage = AddParam(nameof(Slippage), (decimal?)null)
			.SetDisplay(LocalizedStrings.ExtraConditions, LocalizedStrings.Slippage, LocalizedStrings.SlippageTrade, 150);
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		if (!IsMarket && _priceSocket?.IsConnected != true)
			throw new InvalidOperationException(LocalizedStrings.OrderPriceNotSpecified);

		_isOrderSocket		= _orderSocket.IsConnected;
		_isOrderFailSocket	= _orderFailSocket.IsConnected;
		_isTradeSocket		= _tradeSocket.IsConnected;
		_isCancelledSocket	= _orderCancelledSocket.IsConnected;
		_isMatchedSocket	= _orderMatchedSocket.IsConnected;
		_isFinishedSocket	= _orderFinishedSocket.IsConnected;

		base.OnPrepare();
	}

	/// <inheritdoc/>
	protected override void OnReseted()
	{
		base.OnReseted();

		_isOrderSocket = _isOrderFailSocket = _isTradeSocket =
		_isCancelledSocket = _isMatchedSocket = _isFinishedSocket = default;
	}

	/// <inheritdoc />
	protected override void OnProcess(DateTimeOffset time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
		if (!CanProcess(values))
			return;

		var security = Strategy.Security;

		if (values.TryGetValue(_securitySocket, out var securityValue))
			security = securityValue.GetValue<Security>();

		if (security == null || security.IsAllSecurity())
			throw new InvalidOperationException(LocalizedStrings.SecurityNotSpecified);

		var portfolio = Strategy.Portfolio;

		if (values.TryGetValue(_portfolioSocket, out var portfolioValue))
			portfolio = portfolioValue.GetValue<Portfolio>();

		if (portfolio is null)
			throw new InvalidOperationException(LocalizedStrings.PortfolioNotSpecified);

		if (_volumeSocket is null || !values.TryGetValue(_volumeSocket, out var volumeValue))
			throw new InvalidOperationException(LocalizedStrings.OrderVolumeNotSpecified);

		var volume = volumeValue.GetValue<decimal>();

		DiagramSocketValue priceValue = null;

		if (!IsMarket && (_priceSocket is null || !values.TryGetValue(_priceSocket, out priceValue)))
			throw new InvalidOperationException(LocalizedStrings.OrderPriceNotSpecified);

		var price = priceValue?.GetValue<decimal>();

		LogVerbose("Order dir={0}, vol={1}, price={2}", Direction, volume, price);

		var order = new Order
		{
			Portfolio = portfolio,
			Security = security,
			Side = Direction,
			Type = IsMarket || (ZeroAsMarket && (price ?? 0) == 0) ? OrderTypes.Market : OrderTypes.Limit,
			Volume = volume,
			ClientCode = ClientCode,
			BrokerCode = BrokerCode,
			IsManual = IsManual,
			MarginMode = MarginMode,
			IsMarketMaker = IsMarketMaker,
			TimeInForce = TimeInForce,
			ExpiryDate = ExpiryDate,
			Slippage = Slippage,
			Comment = Comment,
		};

		if (_conditionalSettings.Value?.AdapterType != null)
		{
			var adapterProvider = ServicesRegistry.AdapterProvider;

			var adapter = adapterProvider
					.PossibleAdapters
					.FirstOrDefault(p => p.GetType() == _conditionalSettings.Value.AdapterType)	??
					throw new InvalidOperationException($"Adapter {_conditionalSettings.Value.AdapterType} not found.");

			order.Type = OrderTypes.Conditional;
			order.Condition = adapter.CreateOrderCondition();

			foreach (var p in _conditionalSettings.Value.Parameters)
				order.Condition.Parameters[p.Key] = p.Value;
		}

		if (order.Type != OrderTypes.Market)
		{
			price ??= 0;

			if (ShrinkPrice)
				price = security.ShrinkPrice(price.Value);

			order.Price = price.Value;
		}

		if (_isOrderSocket)
		{
			order
				.WhenRegistered(Strategy)
				.Do(ord =>
				{
					RaiseProcessOutput(_orderSocket, ord.ServerTime, ord);
					Strategy.Flush(ord);
				})
				.Apply(Strategy);
		}
		
		if (_isOrderFailSocket || _isFinishedSocket)
		{
			order
				.WhenRegisterFailed(Strategy)
				.Do(fail =>
				{
					if (_isOrderFailSocket)
					{
						RaiseProcessOutput(_orderFailSocket, fail.ServerTime, fail);
						Strategy.Flush(fail);
					}

					if (_isFinishedSocket)
					{
						RaiseProcessOutput(_orderFinishedSocket, fail.ServerTime, fail);
						Strategy.Flush(fail);
					}
				})
				.Apply(Strategy);
		}
		
		if (_isTradeSocket || _isMatchedSocket || _isFinishedSocket)
		{
			order
				.WhenNewTrade(Strategy)
				.Do(trade =>
				{
					if (_isTradeSocket)
					{
						RaiseProcessOutput(_tradeSocket, trade.Trade.ServerTime, trade);
						Strategy.Flush(trade.Trade);
					}

					if ((_isMatchedSocket || _isFinishedSocket) && trade.Order.IsMatched())
					{
						if (_isMatchedSocket)
						{
							RaiseProcessOutput(_orderMatchedSocket, trade.Trade.ServerTime, trade);
							Strategy.Flush(trade.Trade);
						}

						if (_isFinishedSocket)
						{
							RaiseProcessOutput(_orderFinishedSocket, trade.Trade.ServerTime, trade);
							Strategy.Flush(trade.Trade);
						}
					}
				})
				.Apply(Strategy);
		}

		if (_isCancelledSocket || _isFinishedSocket)
		{
			order
				.WhenCanceled(Strategy)
				.Do(o =>
				{
					var time = o.CancelledTime ?? Strategy.CurrentTime;

					if (_isCancelledSocket)
					{
						RaiseProcessOutput(_orderCancelledSocket, time, o);
						Strategy.Flush(time);
					}
					
					if (_isFinishedSocket)
					{
						RaiseProcessOutput(_orderFinishedSocket, time, o);
						Strategy.Flush(time);
					}
				})
				.Apply(Strategy);
		}

		Strategy.RegisterOrder(order);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		UpdatePriceSocket();
	}

	private void UpdatePriceSocket(bool force = false)
	{
		var needPrice = force || !IsMarket;

		if (needPrice == (_priceSocket != null))
			return;

		if (needPrice)
			_priceSocket = AddInput(StaticSocketIds.Price, LocalizedStrings.Price, DiagramSocketType.Unit, index: 1);
		else
		{
			RemoveSocket(_priceSocket);
			_priceSocket = null;
		}
	}

	private void UpdateElementName()
	{
		SetElementName(Direction.GetDisplayName());
	}
}
