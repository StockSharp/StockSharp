namespace StockSharp.Diagram.Elements;

/// <summary>
/// Market depth panel.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.PanelKey,
	Description = LocalizedStrings.MarketDepthPanelKey,
	GroupName = LocalizedStrings.MarketDepthsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/market_depths/order_book_panel.html")]
public class MarketDepthPanelDiagramElement : DiagramElement, IOrderBookSource
{
	/// <inheritdoc />
	public override Guid TypeId { get; } = "A2B9E4C3-4A04-484C-A486-D82CA2CA202A".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Table";

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketDepthPanelDiagramElement"/>.
	/// </summary>
	public MarketDepthPanelDiagramElement()
	{
		AddInput(StaticSocketIds.MarketDepth, LocalizedStrings.MarketDepth, DiagramSocketType.MarketDepth, ProcessMarketDepth);
		AddInput(StaticSocketIds.Order, LocalizedStrings.Order, DiagramSocketType.Order, ProcessOrder);
		AddInput(StaticSocketIds.OrderFail, LocalizedStrings.OrderFail, DiagramSocketType.OrderFail, ProcessOrderFail);
	}

	private void ProcessMarketDepth(DiagramSocketValue value)
	{
		var depth = value.GetValue<IOrderBookMessage>();

		if (depth is null)
			return;

		Strategy.DrawOrderBook(value.Subscription, this, depth);
	}

	private void ProcessOrder(DiagramSocketValue value)
	{
		var order = value.GetValue<Order>();

		if (order is null)
			return;

		Strategy.DrawOrderBookOrder(value.Subscription, this, order);
	}

	private void ProcessOrderFail(DiagramSocketValue value)
	{
		var fail = value.GetValue<OrderFail>();

		if (fail is null)
			return;

		Strategy.DrawOrderBookOrderFail(value.Subscription, this, fail);
	}
}