namespace StockSharp.Diagram.Elements;

/// <summary>
/// Security new trades receiving element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TicksKey,
	Description = LocalizedStrings.TickTradeKey,
	GroupName = LocalizedStrings.SourcesKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/data_sources/ticks.html")]
public class TradesDiagramElement : SubscriptionDiagramElement
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "7E472E79-11C3-4144-932D-D6DDB37CA695".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Deal";

	/// <inheritdoc />
	public TradesDiagramElement()
		: base(LocalizedStrings.Trades)
	{
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Trades, DiagramSocketType.Trade);
	}

	/// <inheritdoc />
	protected override Subscription OnCreateSubscription(Security security)
	{
		var subscription = new Subscription(DataType.Ticks, security);

		subscription
			.WhenTickTradeReceived(Strategy)
			.Do(t =>
			{
				RaiseProcessOutput(_outputSocket, t.ServerTime, t, null, subscription);
				Strategy.Flush(t);
			})
			.Apply(Strategy);

		return subscription;
	}
}