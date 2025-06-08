namespace StockSharp.Diagram.Elements;

/// <summary>
/// Trades per order element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OrderTradesKey,
	Description = LocalizedStrings.OrderTradesElementKey,
	GroupName = LocalizedStrings.CommonKey
)]
[Obsolete("Use OrderRegisterDiagramElement Trades output socket.")]
[Doc("topics/designer/strategies/using_visual_designer/elements/common/trades_by_order.html")]
public class OrderTradesDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "B4344DC8-E328-439B-B9E0-9E02F5CAE85D".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Deal";

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderTradesDiagramElement"/>.
	/// </summary>
	public OrderTradesDiagramElement()
	{
		AddInput(StaticSocketIds.Order, LocalizedStrings.Order, DiagramSocketType.Order, OnProcess);

		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Trades, DiagramSocketType.MyTrade);
	}

	private void OnProcess(DiagramSocketValue value)
	{
		var order = value.GetValue<Order>();

		order
			.WhenNewTrade(Strategy)
			.Do(trade =>
			{
				RaiseProcessOutput(_outputSocket, trade.Trade.ServerTime, trade);
				Strategy.Flush(trade.Trade);
			})
			.Apply(Strategy);
	}
}