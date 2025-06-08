namespace StockSharp.Diagram.Elements;

/// <summary>
/// Candles source element.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CandlesKey,
	Description = LocalizedStrings.CandleSourceElementDescriptionKey,
	GroupName = LocalizedStrings.SourcesKey
)]
[Doc("topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html")]
public sealed class CandlesDiagramElement : SubscriptionDiagramElement
{
	private readonly DiagramSocket _outputSocket;

	private readonly DiagramElementParam<DataType> _dataType;

	/// <summary>
	/// Data type.
	/// </summary>
	public DataType DataType
	{
		get => _dataType.Value;
		set => _dataType.Value = value;
	}

	private readonly DiagramElementParam<bool> _isFinishedOnly;

	/// <summary>
	/// Send only formed candles.
	/// </summary>
	public bool IsFinishedOnly
	{
		get => _isFinishedOnly.Value;
		set => _isFinishedOnly.Value = value;
	}

	private readonly DiagramElementParam<bool> _isCalcVolumeProfile;

	/// <summary>
	/// To perform the calculation <see cref="ICandleMessage.PriceLevels"/>. By default, it is disabled.
	/// </summary>
	public bool IsCalcVolumeProfile
	{
		get => _isCalcVolumeProfile.Value;
		set => _isCalcVolumeProfile.Value = value;
	}

	private readonly DiagramElementParam<bool?> _isRegularTradingHours;

	/// <summary>
	/// Use only the regular trading hours for which data will be requested.
	/// </summary>
	public bool? IsRegularTradingHours
	{
		get => _isRegularTradingHours.Value;
		set => _isRegularTradingHours.Value = value;
	}

	private readonly DiagramElementParam<bool> _allowBuildFromSmallerTimeFrame;

	/// <summary>
	/// Allow build candles from smaller timeframe.
	/// </summary>
	public bool AllowBuildFromSmallerTimeFrame
	{
		get => _allowBuildFromSmallerTimeFrame.Value;
		set => _allowBuildFromSmallerTimeFrame.Value = value;
	}

	private readonly DiagramElementParam<MarketDataBuildModes> _buildCandlesMode;

	/// <summary>
	/// Build mode.
	/// </summary>
	public MarketDataBuildModes BuildCandlesMode
	{
		get => _buildCandlesMode.Value;
		set => _buildCandlesMode.Value = value;
	}

	private readonly DiagramElementParam<DataType> _buildCandlesFrom;

	/// <summary>
	/// Which market-data type is used as a source value.
	/// </summary>
	public DataType BuildCandlesFrom
	{
		get => _buildCandlesFrom.Value;
		set => _buildCandlesFrom.Value = value;
	}

	private readonly DiagramElementParam<Level1Fields?> _buildCandlesField;

	/// <summary>
	/// Extra info for the <see cref="BuildCandlesFrom"/>.
	/// </summary>
	public Level1Fields? BuildCandlesField
	{
		get => _buildCandlesField.Value;
		set => _buildCandlesField.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CandlesDiagramElement"/>.
	/// </summary>
	public CandlesDiagramElement()
		: base(LocalizedStrings.Series)
	{
		_outputSocket = AddOutput(StaticSocketIds.Output, LocalizedStrings.Candles, DiagramSocketType.Candle);

		var dataType = TimeSpan.FromMinutes(5).TimeFrame();
		_dataType = AddParam("Series", dataType)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Series, LocalizedStrings.Series, LocalizedStrings.CandlesSeries + ".", 20)
			.SetEditor(new EditorAttribute(typeof(ICandleDataTypeEditor), typeof(ICandleDataTypeEditor)))
			.SetCanOptimize()
			.SetOnValueChangedHandler(value => SetElementName(value.To<string>()))
			;

		_isFinishedOnly = AddParam(nameof(IsFinishedOnly), true)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Series, LocalizedStrings.OnlyFormed, LocalizedStrings.ProcessOnlyFormed, 30);

		_isCalcVolumeProfile = AddParam<bool>(nameof(IsCalcVolumeProfile))
			.SetDisplay(LocalizedStrings.Series, LocalizedStrings.VolumeProfile, LocalizedStrings.VolumeProfileCalc, 40);

		_allowBuildFromSmallerTimeFrame = AddParam(nameof(AllowBuildFromSmallerTimeFrame), true)
			.SetBasic(true)
			.SetDisplay(LocalizedStrings.Series, LocalizedStrings.SmallerTimeFrame, LocalizedStrings.SmallerTimeFrameDesc, 50);

		_isRegularTradingHours = AddParam<bool?>(nameof(IsRegularTradingHours))
			.SetDisplay(LocalizedStrings.Series, LocalizedStrings.RegularHours, LocalizedStrings.RegularTradingHours, 60);

		_buildCandlesMode = AddParam<MarketDataBuildModes>(nameof(BuildCandlesMode))
			.SetDisplay(LocalizedStrings.Series, LocalizedStrings.Mode, LocalizedStrings.BuildMode, 70);

		_buildCandlesFrom = AddParam<DataType>(nameof(BuildCandlesFrom))
			.SetDisplay(LocalizedStrings.Series, LocalizedStrings.Source, LocalizedStrings.CandlesBuildSource, 80)
			//.SetEditor(new EditorAttribute(typeof(BuildCandlesFromEditor), typeof(BuildCandlesFromEditor)))
			;

		_buildCandlesField = AddParam<Level1Fields?>(nameof(BuildCandlesField))
			.SetDisplay(LocalizedStrings.Series, LocalizedStrings.Field, LocalizedStrings.Level1Field, 90)
			.SetEditor(new ItemsSourceAttribute(typeof(BuildCandlesFieldSource)));

		SetElementName(dataType.To<string>());
		ShowParameters = true;
	}

	/// <inheritdoc />
	public override Guid TypeId { get; } = "3D773273-0CEE-4D40-8EEF-ACDED2D07AB8".To<Guid>();

	/// <inheritdoc />
	public override string IconName { get; } = "CandleStick";

	/// <inheritdoc />
	protected override Subscription OnCreateSubscription(Security security)
	{
		var subscription = new Subscription(DataType, security)
		{
			MarketData =
			{
				IsCalcVolumeProfile = IsCalcVolumeProfile,
				IsRegularTradingHours = IsRegularTradingHours,
				AllowBuildFromSmallerTimeFrame = AllowBuildFromSmallerTimeFrame,
				BuildMode = BuildCandlesMode,
				BuildFrom = BuildCandlesFrom,
				BuildField = BuildCandlesField,
				IsFinishedOnly = IsFinishedOnly,
			},
		};

		subscription
			.WhenCandleReceived(Strategy)
			.Do(c =>
			{
				RaiseProcessOutput(_outputSocket, c.OpenTime, c, null, subscription);
				Strategy.Flush(c);
			})
			.Apply(Strategy);

		return subscription;
	}
}