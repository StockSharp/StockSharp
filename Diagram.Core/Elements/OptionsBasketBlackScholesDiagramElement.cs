namespace StockSharp.Diagram.Elements;

/// <summary>
/// Portfolio model for calculating the values of Greeks by the Black-Scholes formula.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BasketKey,
	Description = LocalizedStrings.BasketBlackScholesKey,
	GroupName = LocalizedStrings.OptionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/options/basket.html")]
public class OptionsBasketBlackScholesDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	private BasketBlackScholes _blackScholes;
	private Security[] _options;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "BEC9BFC6-274E-43A9-A3FC-48C598E2D1F1".To<Guid>();

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
	/// Initializes a new instance of the <see cref="OptionsBasketBlackScholesDiagramElement"/>.
	/// </summary>
	public OptionsBasketBlackScholesDiagramElement()
	{
		AddInput(StaticSocketIds.Options, LocalizedStrings.Options, DiagramSocketType.Options, ProcessOptions);

		_outputSocket = AddOutput(StaticSocketIds.BlackScholes, LocalizedStrings.BlackScholes, DiagramSocketType.BasketBlackScholes);

		_useBlackModel = AddParam(nameof(UseBlackModel), false)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.BlackModel, LocalizedStrings.BlackModel, 10)
			.SetOnValueChangedHandler(f => EnsureModel());
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_blackScholes = default;
		_options = default;
	}

	private void EnsureModel()
	{
		_blackScholes = null;

		if (_options is null)
			return;

		_blackScholes = new BasketBlackScholes(Strategy, Strategy, ServicesRegistry.ExchangeInfoProvider, Strategy);

		foreach (var option in _options)
		{
			var model = UseBlackModel
				? new Black(option, Strategy, Strategy, _blackScholes.ExchangeInfoProvider)
				: new BlackScholes(option, Strategy, Strategy, _blackScholes.ExchangeInfoProvider);

			_blackScholes.InnerModels.Add(model);
		}
	}

	private void ProcessOptions(DiagramSocketValue value)
	{
		_options = value.GetValue<IEnumerable<Security>>()?.ToArray();

		EnsureModel();

		RaiseProcessOutput(_outputSocket, value.Time, _blackScholes, value);
	}
}