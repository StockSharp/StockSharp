namespace StockSharp.Diagram.Elements;

/// <summary>
/// The Black-Scholes "Greeks" evaluation element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.GreeksKey,
	Description = LocalizedStrings.GreeksElementKey,
	GroupName = LocalizedStrings.OptionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/options/greeks.html")]
public class OptionsGreekDiagramElement : OptionsBaseModelDiagramElement<IBlackScholes>
{
	private readonly DiagramSocket _outputSocket;

	private decimal? _assetPrice;
	private decimal? _deviation;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "C076ECF2-C64A-41F7-A470-8C1AB9634F34".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Theta";

	private readonly DiagramElementParam<BlackScholesGreeks> _greek;

	/// <summary>
	/// Value type.
	/// </summary>
	public BlackScholesGreeks Greek
	{
		get => _greek.Value;
		set => _greek.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OptionsGreekDiagramElement"/>.
	/// </summary>
	public OptionsGreekDiagramElement()
	{
		AddInput(StaticSocketIds.Price, LocalizedStrings.UnderlyingAssetPrice, DiagramSocketType.Unit, ProcessAssetPrice);
		AddInput(StaticSocketIds.MaxDeviation, LocalizedStrings.MaxDeviation, DiagramSocketType.Unit, ProcessDeviation);

		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Result, DiagramSocketType.Unit);

		_greek = AddParam(nameof(Greek), BlackScholesGreeks.Delta)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.Value, LocalizedStrings.Value, 20);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_assetPrice = default;
		_deviation = default;
	}

	private void ProcessAssetPrice(DiagramSocketValue value)
	{
		_assetPrice = value.GetValue<decimal?>();
		TryCalcOutputValue(value);
	}

	private void ProcessDeviation(DiagramSocketValue value)
	{
		_deviation = value.GetValue<decimal?>();
		TryCalcOutputValue(value);
	}

	private void TryCalcOutputValue(DiagramSocketValue value)
	{
		var model = Model;

		if (model is null)
			return;

		var time = value.Time;

		var greekValue = Greek switch
		{
			BlackScholesGreeks.Delta => model.Delta(time, _deviation, _assetPrice),
			BlackScholesGreeks.Gamma => model.Gamma(time, _deviation, _assetPrice),
			BlackScholesGreeks.Vega => model.Vega(time, _deviation, _assetPrice),
			BlackScholesGreeks.Theta => model.Theta(time, _deviation, _assetPrice),
			BlackScholesGreeks.Rho => model.Rho(time, _deviation, _assetPrice),
			BlackScholesGreeks.IV => model.ImpliedVolatility(time, _assetPrice ?? 0),
			BlackScholesGreeks.Premium => model.Premium(time, _deviation, _assetPrice),
			_ => throw new InvalidOperationException(Greek.To<string>()),
		};

		if (greekValue is not null)
			RaiseProcessOutput(_outputSocket, time, greekValue, value);
	}
}