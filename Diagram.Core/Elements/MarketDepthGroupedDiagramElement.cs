namespace StockSharp.Diagram.Elements;

/// <summary>
/// Grouped market depth element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.GroupedKey,
	Description = LocalizedStrings.GroupedMarketDepthKey,
	GroupName = LocalizedStrings.MarketDepthsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/market_depths/grouped_order_book.html")]
public class MarketDepthGroupedDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "9EB4B558-57DA-4C45-BFCF-AD032D41277B".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Collapse";

	private readonly DiagramElementParam<decimal> _priceRange;

	/// <summary>
	/// Price range.
	/// </summary>
	public decimal PriceRange
	{
		get => _priceRange.Value;
		set => _priceRange.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketDepthGroupedDiagramElement"/>.
	/// </summary>
	public MarketDepthGroupedDiagramElement()
	{
		AddInput(StaticSocketIds.MarketDepth, LocalizedStrings.MarketDepth, DiagramSocketType.MarketDepth, OnProcessMarketDepth);
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.MarketDepth, DiagramSocketType.MarketDepth);

		_priceRange = AddParam<decimal>(nameof(PriceRange), 5)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.MarketDepth, LocalizedStrings.PriceRange, LocalizedStrings.PriceRange, 10)
			.SetOnValueChangingHandler((o, n) =>
			{
				if (n <= 0)
					throw new ArgumentOutOfRangeException(nameof(n), n, LocalizedStrings.InvalidValue);
			})
			.SetOnValueChangedHandler(value => SetElementName(LocalizedStrings.Group + " " + value));
	}

	private void OnProcessMarketDepth(DiagramSocketValue value)
	{
		var depth = value.GetValue<IOrderBookMessage>();

		var result = depth.Group(PriceRange);

		RaiseProcessOutput(_outputSocket, value.Time, result, value);
	}
}