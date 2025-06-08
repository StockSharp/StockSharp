namespace StockSharp.Diagram.Elements;

/// <summary>
/// The Black-Scholes model element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BlackScholesKey,
	Description = LocalizedStrings.OptionsBlackScholesDiagramElementKey,
	GroupName = LocalizedStrings.OptionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/options/black_scholes.html")]
public class OptionsBlackScholesDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	private IBlackScholes _model;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "DC329280-D204-4BB7-9584-0B0123E08F06".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "OptionChart";

	private readonly DiagramElementParam<bool> _useBlackModel;

	/// <summary>
	/// To use the model <see cref="Black"/> instead of <see cref="IBlackScholes"/> model. The default is off.
	/// </summary>
	public bool UseBlackModel
	{
		get => _useBlackModel.Value;
		set => _useBlackModel.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OptionsBlackScholesDiagramElement"/>.
	/// </summary>
	public OptionsBlackScholesDiagramElement()
	{
		AddInput(StaticSocketIds.UnderlyingAsset, LocalizedStrings.OptionsContract, DiagramSocketType.Security, ProcessStrike);

		_outputSocket = AddOutput(StaticSocketIds.BlackScholes, LocalizedStrings.Model, DiagramSocketType.BlackScholes);

		_useBlackModel = AddParam(nameof(UseBlackModel), false)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.BlackModel, LocalizedStrings.BlackModel, 10);
	}

	private void ProcessStrike(DiagramSocketValue value)
	{
		_model ??= new BlackScholes(value.GetValue<Security>(), Strategy, Strategy, ServicesRegistry.ExchangeInfoProvider);

		RaiseProcessOutput(_outputSocket, value.Time, _model, value);
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_model = default;
	}
}