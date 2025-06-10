namespace StockSharp.Diagram.Elements;

/// <summary>
/// Sparsed market depth element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SparsedKey,
	Description = LocalizedStrings.SparsedMarketDepthKey,
	GroupName = LocalizedStrings.MarketDepthsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/market_depths/sparse_order_book.html")]
public class MarketDepthSparsedDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "24623B84-7DB5-47EC-A158-7FCF233C106D".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Expand";

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
	/// Initializes a new instance of the <see cref="MarketDepthSparsedDiagramElement"/>.
	/// </summary>
	public MarketDepthSparsedDiagramElement()
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
			.SetOnValueChangedHandler(value => SetElementName(LocalizedStrings.Sparse + " " + value));
	}

	private void OnProcessMarketDepth(DiagramSocketValue value)
	{
		var depth = value.GetValue<IOrderBookMessage>();

		var security = Strategy.LookupById(depth.SecurityId);

		var result = depth.Sparse(PriceRange, security?.PriceStep);

		RaiseProcessOutput(_outputSocket, value.Time, result, value);
	}
}