namespace StockSharp.Diagram.Elements;

/// <summary>
/// Truncate market depth element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TruncatedKey,
	Description = LocalizedStrings.TruncatedBookDescKey,
	GroupName = LocalizedStrings.MarketDepthsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/market_depths/truncated_order_book.html")]
public class MarketDepthTruncateDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "12DA021B-2102-4A01-BF89-2459C4F87728".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Cut";

	private readonly DiagramElementParam<int> _maxDepth;

	/// <summary>
	/// Max depth.
	/// </summary>
	public int MaxDepth
	{
		get => _maxDepth.Value;
		set => _maxDepth.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketDepthTruncateDiagramElement"/>.
	/// </summary>
	public MarketDepthTruncateDiagramElement()
	{
		AddInput(StaticSocketIds.MarketDepth, LocalizedStrings.MarketDepth, DiagramSocketType.MarketDepth, OnProcessMarketDepth);
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.MarketDepth, DiagramSocketType.MarketDepth);

		_maxDepth = AddParam(nameof(MaxDepth), 20)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Converter, LocalizedStrings.Depth, LocalizedStrings.MaxDepthOfBook, 10)
			.SetOnValueChangingHandler((oldValue, newValue) =>
			{
				if (newValue < 0)
					throw new ArgumentOutOfRangeException(nameof(newValue), newValue, LocalizedStrings.InvalidValue);
			});
	}

	private void OnProcessMarketDepth(DiagramSocketValue value)
	{
		var depth = value.GetValue<IOrderBookMessage>();
		RaiseProcessOutput(_outputSocket, value.Time, depth.Truncate(MaxDepth), value);
	}
}