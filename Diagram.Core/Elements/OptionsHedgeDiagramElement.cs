namespace StockSharp.Diagram.Elements;

/// <summary>
/// Options hedging diagram element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.HedgingKey,
	Description = LocalizedStrings.OptionsHedgeDiagramElementKey,
	GroupName = LocalizedStrings.OptionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/options/hedging.html")]
[Obsolete("Child strategies no longer supported.")]
public class OptionsHedgeDiagramElement : OptionsBaseModelDiagramElement<BasketBlackScholes>
{
	// TODO

	private readonly DiagramSocket _outputSocket;

	private Security _asset;
	private Strategy _hedgeStrategy;
	private decimal? _hedgeVolume;
	private decimal? _assetPosition;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "270F28D2-62FC-4322-A0D0-7C000A539292".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Shield";

	private readonly DiagramElementParam<BlackScholesGreeks?> _hedgeType;

	/// <summary>
	/// Hedge type.
	/// </summary>
	public BlackScholesGreeks? HedgeType
	{
		get => _hedgeType.Value;
		set => _hedgeType.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OptionsHedgeDiagramElement"/>.
	/// </summary>
	public OptionsHedgeDiagramElement()
	{
		AddInput(StaticSocketIds.UnderlyingAsset, LocalizedStrings.UnderlyingAsset, DiagramSocketType.Security, ProcessUnderlyingAsset);
		AddInput(StaticSocketIds.Volume, LocalizedStrings.Volume, DiagramSocketType.Unit, ProcessHedgeValue);
		AddInput(StaticSocketIds.Position, LocalizedStrings.UnderlyingAssetPosition, DiagramSocketType.Unit, ProcessUnderlyingAssetPosition);
		AddInput(StaticSocketIds.Signal, LocalizedStrings.Signal, DiagramSocketType.Bool, ProcessSignal);

		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.OrderOut, DiagramSocketType.Order);

		_hedgeType = AddParam<BlackScholesGreeks?>(nameof(HedgeType))
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.Type, LocalizedStrings.Type + ".", 10)
			.SetOnValueChangedHandler(value => SetElementName(value?.GetDisplayName()));
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_hedgeStrategy?.Dispose();
		_hedgeStrategy = default;
		_asset = default;
		_assetPosition = default;
		_hedgeVolume = default;
	}

	/// <inheritdoc />
	protected override void OnStop()
	{
		if (_hedgeStrategy != null)
			Strategy.ChildStrategies.Remove(_hedgeStrategy);

		base.OnStop();
	}

	private void ProcessSignal(DiagramSocketValue value)
	{
		if (_assetPosition == null || _hedgeVolume == null || _asset == null)
			return;

		if (!value.GetValue<bool>())
			return;

		if (_hedgeStrategy == null)
			CreateHedge();
	}

	private void ProcessUnderlyingAssetPosition(DiagramSocketValue value)
	{
		_assetPosition = value.GetValue<decimal>();
	}

	private void ProcessHedgeValue(DiagramSocketValue value)
	{
		_hedgeVolume = value.GetValue<decimal>();
	}

	private void ProcessUnderlyingAsset(DiagramSocketValue value)
	{
		_asset = (Security)value.Value;
	}

	private void CreateHedge()
	{
		_hedgeStrategy = HedgeType switch
		{
			BlackScholesGreeks.Delta or /*=> new DeltaHedgeStrategy(Model) { Volume = _hedgeVolume.Value },*/

			BlackScholesGreeks.Gamma or BlackScholesGreeks.Vega or
			BlackScholesGreeks.Theta or BlackScholesGreeks.Rho
				=> throw new NotSupportedException(),

			_ => throw new InvalidOperationException(HedgeType.To<string>()),
		};

		_hedgeStrategy.Security = _asset;

		_hedgeStrategy
			.WhenOrderRegistered()
			.Do(ord => RaiseProcessOutput(_outputSocket, ord.ServerTime, ord))
			.Apply(Strategy);

		Strategy.ChildStrategies.Add(_hedgeStrategy);
	}
}