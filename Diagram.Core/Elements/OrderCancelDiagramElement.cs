namespace StockSharp.Diagram.Elements;

/// <summary>
/// Order cancelling element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderCancellingKey,
	Description = LocalizedStrings.OrderCancellingKey,
	GroupName = LocalizedStrings.OrdersKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/trading/cancel_order.html")]
public class OrderCancelDiagramElement : OrderBaseDiagramElement
{
	private Order _order;

	private readonly DiagramSocket _orderOutSocket;
	private readonly DiagramSocket _orderFailSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "CC13DEB7-C290-4CB4-8AC0-CAB63A72CC2F".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "ShoppingCartDelete";

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderCancelDiagramElement"/>.
	/// </summary>
	public OrderCancelDiagramElement()
	{
		TriggerSocket.Action = OnTrigger;

		AddInput(StaticSocketIds.Order, LocalizedStrings.Order, DiagramSocketType.Order, OnOrderProcess);

		_orderOutSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Order, DiagramSocketType.Order);
		_orderFailSocket = AddOutput(StaticSocketIds.OrderFail, LocalizedStrings.OrderFail, DiagramSocketType.OrderFail);
	}

	/// <inheritdoc/>
	protected override void OnReseted()
	{
		base.OnReseted();

		_order = default;
	}

	private void OnOrderProcess(DiagramSocketValue value)
	{
		_order = (Order)value.Value;
	}

	private void OnTrigger(DiagramSocketValue value)
	{
		if (!CanProcess(value))
			return;

		if (_order?.State != OrderStates.Active)
			return;

		_order
			.WhenCanceled(Strategy)
			.Do(ord =>
			{
				RaiseProcessOutput(_orderOutSocket, ord.ServerTime, ord);
				Strategy.Flush(ord);
			})
			.Apply(Strategy);

		_order
			.WhenCancelFailed(Strategy)
			.Do(fail =>
			{
				RaiseProcessOutput(_orderFailSocket, fail.ServerTime, fail);
				Strategy.Flush(fail);
			})
			.Apply(Strategy);

		Strategy.CancelOrder(_order);
		_order = null;
	}
}