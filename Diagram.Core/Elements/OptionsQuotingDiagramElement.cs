namespace StockSharp.Diagram.Elements;

using StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// Options quoting types.
/// </summary>
public enum OptionsQuotingTypes
{
	/// <summary>
	/// Option volatility quoting.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VolatilityKey)]
	Volalitity,

	/// <summary>
	/// Option theoretical price quoting.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TheorPriceKey)]
	TheorPrice,
}

/// <summary>
/// Options quoting diagram element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.QuotingKey,
	Description = LocalizedStrings.OptionsQuotingDiagramElementKey,
	GroupName = LocalizedStrings.OptionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/options/options_quoting.html")]
public class OptionsQuotingDiagramElement : OptionsBaseModelDiagramElement<IBlackScholes>
{
	private readonly DiagramSocket _outputSocket;

	private QuotingProcessor _processor;
	private decimal? _volume;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "20ACF36B-7793-48C2-B717-565E5FB7090E".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "OptionChart";

	private readonly DiagramElementParam<OptionsQuotingTypes> _quotingType;

	/// <summary>
	/// Quoting type.
	/// </summary>
	public OptionsQuotingTypes QuotingType
	{
		get => _quotingType.Value;
		set => _quotingType.Value = value;
	}

	private readonly DiagramElementParam<Sides> _quotingSide;

	/// <summary>
	/// Quoting direction.
	/// </summary>
	public Sides QuotingSide
	{
		get => _quotingSide.Value;
		set => _quotingSide.Value = value;
	}

	private readonly DiagramElementParam<decimal?> _min;

	/// <summary>
	/// Min.
	/// </summary>
	public decimal? Min
	{
		get => _min.Value;
		set => _min.Value = value;
	}

	private readonly DiagramElementParam<decimal?> _max;

	/// <summary>
	/// Max.
	/// </summary>
	public decimal? Max
	{
		get => _max.Value;
		set => _max.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OptionsQuotingDiagramElement"/>.
	/// </summary>
	public OptionsQuotingDiagramElement()
	{
		AddInput(StaticSocketIds.Volume, LocalizedStrings.Volume, DiagramSocketType.Unit, OnProcessVolume);

		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Order, DiagramSocketType.Order);

		_quotingType = AddParam(nameof(QuotingType), OptionsQuotingTypes.Volalitity)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.Quoting, LocalizedStrings.Quoting + ".", 10);

		_quotingSide = AddParam(nameof(QuotingSide), Sides.Buy)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.Direction, LocalizedStrings.DirBuyOrSell, 20);

		_min = AddParam<decimal?>(nameof(Min), null)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.Minimum, LocalizedStrings.Minimum + ".", 30);

		_max = AddParam<decimal?>(nameof(Max), null)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.Maximum, LocalizedStrings.Maximum + ".", 40);
	}

	private void OnProcessVolume(DiagramSocketValue value)
	{
		_volume = value.GetValue<decimal>();
	}

	/// <inheritdoc />
	protected override void ProcessModel(DiagramSocketValue value)
	{
		if (_volume is null)
			return;

		if (Model is null && QuotingType == OptionsQuotingTypes.Volalitity)
			return;

		IQuotingBehavior behavior = QuotingType switch
		{
			OptionsQuotingTypes.Volalitity => new VolatilityQuotingBehavior(new(Min ?? 0, Max ?? 100), Model),
			OptionsQuotingTypes.TheorPrice => new TheorPriceQuotingBehavior(new(Min ?? 0, Max ?? decimal.MaxValue)),
			_ => throw new InvalidOperationException(QuotingType.ToString()),
		};

		_processor = new(behavior, Strategy.Security, Strategy.Portfolio, QuotingSide, _volume.Value, Strategy.Volume, default, Strategy, Strategy, Strategy, Strategy, Strategy, Strategy.IsFormedAndOnlineAndAllowTrading, true, false)
		{
			Parent = this
		};

		_processor.Finished += OnProcessorFinished;
		_processor.OrderRegistered += OnProcessOrderRegistered;

		_processor.Start();
	}

	private void OnProcessorFinished(bool success)
	{
		_processor.Finished -= OnProcessorFinished;
		_processor.OrderRegistered -= OnProcessOrderRegistered;

		_processor.Dispose();
		_processor = null;
	}

	private void OnProcessOrderRegistered(Order order)
		=> RaiseProcessOutput(_outputSocket, order.ServerTime, order);

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_volume = default;

		if (_processor is null)
			return;

		OnProcessorFinished(true);
	}
}