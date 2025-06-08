namespace StockSharp.Diagram.Elements;

/// <summary>
/// Panel for viewing options positions and greeks chart in respect to the underlying asset.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.OptionsPositionsKey,
	Description = LocalizedStrings.OptionsPositionsElementKey,
	GroupName = LocalizedStrings.OptionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/options/chart_positions.html")]
public class OptionsPositionChartDiagramElement : OptionsBaseModelDiagramElement<BasketBlackScholes>
{
	private IOptionPositionChart _chart;
	private decimal? _assetPrice;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "89E3761E-786A-4696-BB90-84D983D9ACD2".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "OptionChart";

	/// <summary>
	/// Initializes a new instance of the <see cref="OptionsPositionChartDiagramElement"/>.
	/// </summary>
	public OptionsPositionChartDiagramElement()
	{
		AddInput(StaticSocketIds.Price, LocalizedStrings.UnderlyingAssetPrice, DiagramSocketType.Unit, ProcessAssetPrice);
	}

	/// <inheritdoc />
	protected override void OnPrepare()
	{
		_chart = Strategy.GetOptionPositionChart();

		if (_chart == null)
		{
			LogWarning(LocalizedStrings.NeedToAddOptionPositionChart);
			return;
		}

		base.OnPrepare();
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		_chart = default;
		_assetPrice = default;

		base.OnReseted();
	}

	/// <inheritdoc />
	protected override void ProcessModel(DiagramSocketValue value)
	{
		if (_chart is null)
			return;

		_chart.Model = Model;

		if (_assetPrice is not null)
			Refresh(value);
	}

	private void ProcessAssetPrice(DiagramSocketValue value)
	{
		if (_chart is null)
			return;

		_assetPrice = value.GetValue<decimal>();
		Refresh(value);
	}

	private void Refresh(DiagramSocketValue value)
	{
		_chart.Refresh(_assetPrice.Value, value.Time);
	}
}