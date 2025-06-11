namespace StockSharp.Diagram.Elements;

using StockSharp.Algo.Strategies.Protective;

/// <summary>
/// The element is used to automatically protect opened positions using stop loss and take-profit.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PosProtectionKey,
	Description = LocalizedStrings.PositionProtectionElementDescriptionKey,
	GroupName = LocalizedStrings.PositionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/positions/protect.html")]
public class PositionProtectDiagramElement : DiagramElement
{
	private readonly ProtectiveController _controller = new();
	private readonly DiagramSocket _takeProfitOrder;
	private readonly DiagramSocket _stopLossOrder;
	private readonly DiagramSocket _tradeSocket;

	private IProtectivePositionController _posController;
	private Security _posSecurity;
	private Portfolio _posPortfolio;
	private string _posClientCode;
	private string _posBrokerCode;

	private bool _canTradeSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "7712FF8B-0270-49FA-9663-196A9A03BFE8".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Shield";

	private readonly DiagramElementParam<Unit> _takeValue;

	/// <summary>
	/// The protective level for the take profit. The default level is 0, which means the disabled.
	/// </summary>
	public Unit TakeValue
	{
		get => _takeValue.Value;
		set => _takeValue.Value = value;
	}

	private readonly DiagramElementParam<Unit> _stopValue;

	/// <summary>
	/// The protective level for the stop loss. The default level is 0, which means the disabled.
	/// </summary>
	public Unit StopValue
	{
		get => _stopValue.Value;
		set => _stopValue.Value = value;
	}

	private readonly DiagramElementParam<bool> _useMarketOrders;

	/// <summary>
	/// Whether to use market orders.
	/// </summary>
	public bool UseMarketOrders
	{
		get => _useMarketOrders.Value;
		set => _useMarketOrders.Value = value;
	}

	private readonly DiagramElementParam<TimeSpan> _takeProfitTimeOut;

	/// <summary>
	/// Time limit. If protection has not worked by this time, the position will be closed on the market.
	/// </summary>
	/// <remarks>
	/// The default is off.
	/// </remarks>
	public TimeSpan TakeProfitTimeOut
	{
		get => _takeProfitTimeOut.Value;
		set => _takeProfitTimeOut.Value = value;
	}

	private readonly DiagramElementParam<bool> _isTrailingStopLoss;

	/// <summary>
	/// Whether to use a trailing technique.
	/// </summary>
	/// <remarks>
	/// The default is off.
	/// </remarks>
	public bool IsTrailingStopLoss
	{
		get => _isTrailingStopLoss.Value;
		set => _isTrailingStopLoss.Value = value;
	}

	private readonly DiagramElementParam<TimeSpan> _stopLossTimeOut;

	/// <summary>
	/// Time limit. If protection has not worked by this time, the position will be closed on the market.
	/// </summary>
	/// <remarks>
	/// The default is off.
	/// </remarks>
	public TimeSpan StopLossTimeOut
	{
		get => _stopLossTimeOut.Value;
		set => _stopLossTimeOut.Value = value;
	}

	private readonly DiagramElementParam<bool> _useServer;

	/// <summary>
	/// Try use server-side stop orders if underlying connector provide it.
	/// </summary>
	/// <remarks>
	/// It is disabled by default.
	/// </remarks>
	public bool UseServer
	{
		get => _useServer.Value;
		set => _useServer.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="PositionProtectDiagramElement"/>.
	/// </summary>
	public PositionProtectDiagramElement()
	{
		AddInput(StaticSocketIds.Trade, LocalizedStrings.Trades, DiagramSocketType.MyTrade, OnProcessTrade, int.MaxValue);
		AddInput(StaticSocketIds.Price, LocalizedStrings.Price, DiagramSocketType.Unit, OnProcessPrice);
		AddInput(StaticSocketIds.MarketDepth, LocalizedStrings.BestPair, DiagramSocketType.MarketDepth, OnProcessMarketDepth);

		_takeProfitOrder = AddOutput("Take", LocalizedStrings.TakeProfit, DiagramSocketType.Order, isDynamic: false);
		_stopLossOrder = AddOutput("Stop", LocalizedStrings.StopLoss, DiagramSocketType.Order, isDynamic: false);
		_tradeSocket = AddOutput(StaticSocketIds.MyTrade, LocalizedStrings.OwnTrade, DiagramSocketType.MyTrade);

		_takeValue = AddParam<Unit>(nameof(TakeValue), new())
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.TakeProfit, LocalizedStrings.TakeProfit, LocalizedStrings.ProtectLevelTakeProfit, 40)
			.SetCanOptimize()
			.SetOnValueChangedHandler(value => UpdateName());

		_takeProfitTimeOut = AddParam<TimeSpan>(nameof(TakeProfitTimeOut))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.TakeProfit, LocalizedStrings.TimeOut, LocalizedStrings.TimeOut, 42);

		_stopValue = AddParam<Unit>(nameof(StopValue), new())
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.StopLoss, LocalizedStrings.StopLoss, LocalizedStrings.ProtectLevelStopLoss, 20)
			.SetCanOptimize()
			.SetOnValueChangedHandler(value => UpdateName());

		_isTrailingStopLoss = AddParam<bool>(nameof(IsTrailingStopLoss))
			.SetBasic(true)
			.SetCanOptimize()
			.SetDisplay(LocalizedStrings.StopLoss, LocalizedStrings.Trailing, LocalizedStrings.TrailingStopLoss, 21);

		_stopLossTimeOut = AddParam<TimeSpan>(nameof(StopLossTimeOut))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.StopLoss, LocalizedStrings.TimeOut, LocalizedStrings.TimeOut, 22);

		_useMarketOrders = AddParam(nameof(UseMarketOrders), true)
			.SetDisplay(LocalizedStrings.Strategy, LocalizedStrings.MarketOrders, LocalizedStrings.MarketOrders, 101)
			.SetCanOptimize();

		_useServer = AddParam<bool>(nameof(UseServer))
			.SetDisplay(LocalizedStrings.Strategy, LocalizedStrings.Server, LocalizedStrings.ServerStopOrders, 100);

		ShowParameters = true;
	}

	private void UpdateName()
	{
		string name = null;

		if (IsTakeSet)
			name += LocalizedStrings.TakeIs.Put(TakeValue);

		if (IsStopSet)
		{
			if (!name.IsEmpty())
				name += " ";

			name += LocalizedStrings.StopIs.Put(StopValue);
		}

		SetElementName(name);
	}

	private bool IsTakeSet => TakeValue.IsSet();
	private bool IsStopSet => StopValue.IsSet();

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_canTradeSocket = default;

		_controller.Clear();
		_posController = default;
		_posSecurity = default;
		_posPortfolio = default;
		_posClientCode = default;
		_posBrokerCode = default;
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		if (!IsTakeSet && !IsStopSet)
			throw new InvalidOperationException(LocalizedStrings.NoTakeAndStop);

		_canTradeSocket = _tradeSocket.IsConnected;

		base.OnPrepare();
	}

	private void OnProcessPrice(DiagramSocketValue value)
	{
		var info = _posController?.TryActivate(
			value.GetValue<decimal>(),
			value.Time);

		if (info is not null)
			ActiveProtection(info.Value);
	}

	private void OnProcessMarketDepth(DiagramSocketValue value)
	{
		if (_posController is not IProtectivePositionController controller)
			return;

		var ob = value.GetValue<IOrderBookMessage>();

		if (controller.Position.GetDirection() is not Sides side)
			return;

		if (ob.GetPrice(side.Invert()) is not decimal price)
			return;

		var info = controller.TryActivate(price, value.Time);

		if (info is not null)
			ActiveProtection(info.Value);
	}

	private void OnProcessTrade(DiagramSocketValue value)
	{
		var trade = value.GetValue<MyTrade>();

		var security = trade.Order.Security;
		var secId = security.ToSecurityId();
		var pfName = trade.Order.Portfolio.Name;

		if (_posController is null)
		{
			_posController = _controller.GetController(
				secId, pfName,
				UseServer
					? new ServerProtectiveBehaviourFactory(((Connector)Connector).Adapter.TryGetAdapter(pfName, out var adapter) ? adapter : throw new InvalidOperationException(LocalizedStrings.AdapterNotSpecified.Put(pfName)))
					: new LocalProtectiveBehaviourFactory(security.PriceStep, security.Decimals),
				TakeValue ?? new(),
				StopValue ?? new(),
				IsTrailingStopLoss,
				TakeProfitTimeOut,
				StopLossTimeOut,
				UseMarketOrders
			);

			_posSecurity = trade.Order.Security;
			_posPortfolio = trade.Order.Portfolio;
			_posClientCode = trade.Order.ClientCode;
			_posBrokerCode = trade.Order.BrokerCode;
		}
		else
		{
			if (_posController.SecurityId != secId)
				throw new InvalidOperationException(LocalizedStrings.WrongSecId.Put(secId, _posController.SecurityId));

			if (!_posController.PortfolioName.EqualsIgnoreCase(pfName))
				throw new InvalidOperationException(LocalizedStrings.WrongPortfolioId.Put(pfName, _posController.PortfolioName));
		}

		var info = _posController.Update(
			trade.Trade.Price,
			trade.GetPosition(),
			trade.Trade.ServerTime
		);

		if (info is not null)
			ActiveProtection(info.Value);
	}

	private void ActiveProtection((bool isTake, Sides side, decimal price, decimal volume, OrderCondition condition) info)
	{
		var order = new Order
		{
			Security = _posSecurity,
			Portfolio = _posPortfolio,
			ClientCode = _posClientCode,
			BrokerCode = _posBrokerCode,
			Volume = info.volume,
			Side = info.side,
			Type = info.price == 0 ? OrderTypes.Market : OrderTypes.Limit,
			Price = info.price,
			Condition = info.condition,
		};

		order
			.WhenRegistered(Strategy)
			.Do(ord =>
			{
				RaiseProcessOutput(info.isTake ? _takeProfitOrder : _stopLossOrder, ord.ServerTime, ord);
				Strategy.Flush(ord);
			})
			.Apply(Strategy);

		order
			.WhenNewTrade(Strategy)
			.Do(trade =>
			{
				_posController?.Update(trade.Trade.Price, trade.GetPosition(), trade.Trade.ServerTime);

				if (_canTradeSocket)
				{
					RaiseProcessOutput(_tradeSocket, trade.Trade.ServerTime, trade);
					Strategy.Flush(trade.Trade);
				}
			})
			.Apply(Strategy);

		Strategy.RegisterOrder(order);
	}
}