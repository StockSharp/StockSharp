namespace StockSharp.Diagram.Elements;

/// <summary>
/// Filtering derivatives by underlying asset diagram element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.StrikesKey,
	Description = LocalizedStrings.OptionsStrikesDiagramElementKey,
	GroupName = LocalizedStrings.OptionsKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/options/strikes.html")]
public class OptionsStrikesDiagramElement : DiagramElement
{
	private readonly DiagramSocket _outputSocket;

	private Security _asset;

	/// <inheritdoc />
	public override Guid TypeId { get; } = "7B62274F-AD0C-4EF6-812A-C6D9CA733AFD".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "Filter";

	private readonly DiagramElementParam<OptionTypes?> _optionType;

	/// <summary>
	/// Option type.
	/// </summary>
	public OptionTypes? OptionType
	{
		get => _optionType.Value;
		set => _optionType.Value = value;
	}

	private readonly DiagramElementParam<DateTimeOffset?> _expirationDate;

	/// <summary>
	/// Expiration date.
	/// </summary>
	public DateTimeOffset? ExpirationDate
	{
		get => _expirationDate.Value;
		set => _expirationDate.Value = value;
	}

	private readonly DiagramElementParam<int?> _leftOffset;

	/// <summary>
	/// Left offset.
	/// </summary>
	public int? LeftOffset
	{
		get => _leftOffset.Value;
		set => _leftOffset.Value = value;
	}

	private readonly DiagramElementParam<int?> _rightOffset;

	/// <summary>
	/// Right offset.
	/// </summary>
	public int? RightOffset
	{
		get => _rightOffset.Value;
		set => _rightOffset.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OptionsStrikesDiagramElement"/>.
	/// </summary>
	public OptionsStrikesDiagramElement()
	{
		AddInput(StaticSocketIds.UnderlyingAsset, LocalizedStrings.UnderlyingAsset, DiagramSocketType.Security, ProcessUnderlyingAsset);

		_outputSocket = AddOutput(StaticSocketIds.Options, LocalizedStrings.Options, DiagramSocketType.Options);

		_optionType = AddParam<OptionTypes?>(nameof(OptionType), null)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.OptionType, LocalizedStrings.OptionType + ".", 10);

		_expirationDate = AddParam<DateTimeOffset?>(nameof(ExpirationDate), null)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.ExpiryDate, LocalizedStrings.ExpiryDate + ".", 20);

		_leftOffset = AddParam<int?>(nameof(LeftOffset), null)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.Strike + " (<-)", LocalizedStrings.StrikeLeftOffset, 30);

		_rightOffset = AddParam<int?>(nameof(RightOffset), null)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Options, LocalizedStrings.Strike + " (->)", LocalizedStrings.StrikeRightOffset, 40);
	}

	private void ProcessUnderlyingAsset(DiagramSocketValue value)
	{
		_asset = (Security)value.Value;

		if (_asset == null)
			return;

		if (_asset.GetCurrentPrice(ServicesRegistry.MarketDataProvider) is not decimal assetPrice)
			return;

		var options = _asset.GetDerivatives(ServicesRegistry.SecurityProvider, ExpirationDate);

		var optionType = OptionType;

		if (optionType != null)
			options = options.Filter(optionType.Value);

		var arr = options.ToArray();

		var centralStike = arr.GetCentralStrike(assetPrice);

		var leftOffset = LeftOffset;

		if (leftOffset != null)
		{
			arr =
			[
				.. arr.Where(o => o.Strike >= centralStike.Strike),
				.. arr.Where(o => o.Strike < centralStike.Strike).OrderByDescending(o => o.Strike).Take(leftOffset.Value),
			];
		}

		var rightOffset = RightOffset;

		if (rightOffset != null)
		{
			arr =
			[
				.. arr.Where(o => o.Strike <= centralStike.Strike),
				.. arr.Where(o => o.Strike > centralStike.Strike).OrderBy(o => o.Strike).Take(rightOffset.Value),
			];
		}

		options = arr;

		RaiseProcessOutput(_outputSocket, value.Time, options, value);
	}

	/// <inheritdoc/>
	protected override void OnReseted()
	{
		base.OnReseted();

		_asset = default;
	}
}
