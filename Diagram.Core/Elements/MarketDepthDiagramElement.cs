namespace StockSharp.Diagram.Elements;

/// <summary>
/// Security market depth changes receiving element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MarketDepthKey,
	Description = LocalizedStrings.MarketDepthElementKey,
	GroupName = LocalizedStrings.MarketDepthsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/market_depths/order_book.html")]
public class MarketDepthDiagramElement : SubscriptionDiagramElement
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "143CC455-34D4-44F0-870B-124A5F531978".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "UpDown";

	private readonly DiagramElementParam<bool> _supportEmptyDepth;

	/// <summary>
	/// Translates empty depth.
	/// </summary>
	/// <remarks>
	/// The default is off.
	/// </remarks>
	public bool SupportEmptyDepth
	{
		get => _supportEmptyDepth.Value;
		set => _supportEmptyDepth.Value = value;
	}

	/// <inheritdoc />
	public MarketDepthDiagramElement()
		: base(LocalizedStrings.MarketDepth)
	{
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.MarketDepth, DiagramSocketType.MarketDepth);

		_supportEmptyDepth = AddParam<bool>(nameof(SupportEmptyDepth))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.MarketDepth, LocalizedStrings.Empty, LocalizedStrings.SupportEmptyDepth, 21);
	}

	/// <inheritdoc />
	protected override Subscription OnCreateSubscription(Security security)
	{
		var subscription = new Subscription(DataType.MarketDepth, security);

		subscription
			 .WhenOrderBookReceived(Strategy)
			 .Do(d =>
			 {
				 if (!SupportEmptyDepth && d.IsFullEmpty())
					 return;

				 RaiseProcessOutput(_outputSocket, d.ServerTime, d, null, subscription);
				 Strategy.Flush(d);
			 })
			 .Apply(Strategy);

		return subscription;
	}
}