namespace StockSharp.Diagram.Elements;

/// <summary>
/// Order replacing element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderReplacingKey,
	Description = LocalizedStrings.OrderReplacingKey,
	GroupName = LocalizedStrings.OrdersKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/orders/modify.html")]
public class OrderReplaceDiagramElement : OrderBaseDiagramElement
{
	private readonly DiagramSocket _priceSocket;
	private readonly DiagramSocket _volumeSocket;

	private readonly DiagramSocket _orderOutSocket;
	private readonly DiagramSocket _orderFailSocket;
	private readonly DiagramSocket _tradeSocket;
	private readonly DiagramSocket _orderCancelledSocket;
	private readonly DiagramSocket _orderMatchedSocket;
	private readonly DiagramSocket _orderFinishedSocket;

	private Order _order;

	private bool _isOrderSocket;
	private bool _isOrderFailSocket;
	private bool _isTradeSocket;
	private bool _isCancelledSocket;
	private bool _isMatchedSocket;
	private bool _isFinishedSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "3CB7ED18-DE35-4B4E-AA09-C04A83F8F7DD".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "ShoppingCartMove";

	private readonly DiagramElementParam<bool> _shrinkPrice;

	/// <summary>
	/// Shrink order price.
	/// </summary>
	public bool ShrinkPrice
	{
		get => _shrinkPrice.Value;
		set => _shrinkPrice.Value = value;
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

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderReplaceDiagramElement"/>.
	/// </summary>
	public OrderReplaceDiagramElement()
	{
		AddInput(StaticSocketIds.Order, LocalizedStrings.Order, DiagramSocketType.Order, OnOrderProcess);

		_priceSocket = AddInput(StaticSocketIds.Price, LocalizedStrings.Price, DiagramSocketType.Unit);
		_volumeSocket = AddInput(StaticSocketIds.Volume, LocalizedStrings.Volume, DiagramSocketType.Unit);

		_orderOutSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Order, DiagramSocketType.Order);
		_orderFailSocket = AddOutput(StaticSocketIds.OrderFail, LocalizedStrings.OrderFail, DiagramSocketType.OrderFail);
		_tradeSocket = AddOutput(StaticSocketIds.MyTrade, LocalizedStrings.OwnTrade, DiagramSocketType.MyTrade);

		_orderCancelledSocket	= AddOutput("Cancelled", LocalizedStrings.Cancel, DiagramSocketType.Order, isDynamic: false);
		_orderMatchedSocket		= AddOutput("Matched", LocalizedStrings.Match, DiagramSocketType.Order, isDynamic: false);
		_orderFinishedSocket	= AddOutput("Finished", LocalizedStrings.Finished, DiagramSocketType.Order, isDynamic: false);

		_shrinkPrice = AddParam(nameof(ShrinkPrice), true)
			.SetDisplay(LocalizedStrings.Order, LocalizedStrings.ShrinkPrice, LocalizedStrings.ShrinkPrice, 90);

		_zeroAsMarket = AddParam<bool>(nameof(ZeroAsMarket))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Order, LocalizedStrings.ZeroPrice, LocalizedStrings.ZeroAsMarket, 91);
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		_isOrderSocket		= _orderOutSocket.IsConnected;
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

		_order = default;

		_isOrderSocket = _isOrderFailSocket = _isTradeSocket =
		_isCancelledSocket = _isMatchedSocket = _isFinishedSocket = default;
	}

	private void OnOrderProcess(DiagramSocketValue value)
	{
		_order = (Order)value.Value;
	}

	/// <inheritdoc />
	protected override void OnProcess(DateTimeOffset time, IDictionary<DiagramSocket, DiagramSocketValue> values, DiagramSocketValue source)
	{
		if (!CanProcess(values))
			return;

		if (_order?.State != OrderStates.Active)
			return;

		if (!values.TryGetValue(_priceSocket, out var priceValue))
			throw new InvalidOperationException(LocalizedStrings.OrderPriceNotSpecified);

		var price = priceValue.GetValue<decimal>();

		var volume = values.TryGetValue(_volumeSocket)?.GetValue<decimal>();

		if (price != 0 && ShrinkPrice)
			price = _order.Security.ShrinkPrice(price);

		var clone = _order.ReRegisterClone(price, volume);

		if (price == 0 && ZeroAsMarket)
			clone.Type = OrderTypes.Market;

		if (_isOrderSocket)
		{
			clone
				.WhenRegistered(Strategy)
				.Do(ord =>
				{
					RaiseProcessOutput(_orderOutSocket, ord.ServerTime, ord);
					Strategy.Flush(ord);
				})
				.Apply(Strategy);
		}
			
		if (_isOrderFailSocket || _isFinishedSocket)
		{
			clone
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
			_order
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
			_order
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

		Strategy.ReRegisterOrder(_order, clone);
		_order = null;
	}
}