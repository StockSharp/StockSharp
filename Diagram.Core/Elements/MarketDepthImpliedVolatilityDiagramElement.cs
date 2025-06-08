namespace StockSharp.Diagram.Elements;

/// <summary>
/// Implied volatility market depth element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ImpliedVolatilityKey,
	Description = LocalizedStrings.ImpliedVolatilityMarketDepthKey,
	GroupName = LocalizedStrings.OptionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/options/iv_book.html")]
public class MarketDepthImpliedVolatilityDiagramElement : OptionsBaseModelDiagramElement<IBlackScholes>
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "2E1BA0BC-B7EB-47A8-9C45-18E5DEA6E27B".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "UpDown";

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketDepthImpliedVolatilityDiagramElement"/>.
	/// </summary>
	public MarketDepthImpliedVolatilityDiagramElement()
	{
		AddInput(StaticSocketIds.MarketDepth, LocalizedStrings.MarketDepth, DiagramSocketType.MarketDepth, OnProcessMarketDepth);
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.MarketDepth, DiagramSocketType.MarketDepth);
	}

	private void OnProcessMarketDepth(DiagramSocketValue value)
	{
		if (Model is null)
			return;

		var depth = value.GetValue<IOrderBookMessage>();

		var impliedDepth = depth.ImpliedVolatility(Model, value.Time);

		RaiseProcessOutput(_outputSocket, value.Time, impliedDepth, value);
	}
}