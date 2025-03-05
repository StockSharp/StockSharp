namespace StockSharp.Algo.Candles;

using System.Text.RegularExpressions;

using StockSharp.BusinessEntities;

/// <summary>
/// Candles series.
/// </summary>
[Obsolete("Use Subscription class.")]
public class CandleSeries : NotifiableObject, IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CandleSeries"/>.
	/// </summary>
	public CandleSeries()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="CandleSeries"/>.
	/// </summary>
	/// <param name="candleType">The candle type.</param>
	/// <param name="security">The instrument to be used for candles formation.</param>
	/// <param name="arg">The candle formation parameter. For example, for <see cref="TimeFrameCandle"/> this value is <see cref="TimeFrameCandle.TimeFrame"/>.</param>
	public CandleSeries(Type candleType, Security security, object arg)
	{
		if (!candleType.IsCandle())
			throw new ArgumentOutOfRangeException(nameof(candleType), candleType, LocalizedStrings.WrongCandleType);

		_security = security ?? throw new ArgumentNullException(nameof(security));
		_candleType = candleType ?? throw new ArgumentNullException(nameof(candleType));
		_arg = arg ?? throw new ArgumentNullException(nameof(arg));
		WorkingTime = security.Board?.WorkingTime;
	}

	private Security _security;

	/// <summary>
	/// The instrument to be used for candles formation.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecurityKey,
		Description = LocalizedStrings.SecurityKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 0)]
	public Security Security
	{
		get => _security;
		set
		{
			_security = value;
			NotifyChanged();
		}
	}

	private Type _candleType;

	/// <summary>
	/// The candle type.
	/// </summary>
	[Browsable(false)]
	public Type CandleType
	{
		get => _candleType;
		set
		{
			NotifyChanging();

			if (_candleType != value)
				_arg = null; // reset incompatible arg

			_candleType = value;
			NotifyChanged();
		}
	}

	private object _arg;

	/// <summary>
	/// The candle formation parameter. For example, for <see cref="TimeFrameCandle"/> this value is <see cref="TimeFrameCandle.TimeFrame"/>.
	/// </summary>
	[Browsable(false)]
	public object Arg
	{
		get => _arg;
		set
		{
			NotifyChanging();
			_arg = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// The time boundary, within which candles for give series shall be translated.
	/// </summary>
	[Browsable(false)]
	public WorkingTime WorkingTime { get; set; }

	/// <summary>
	/// To perform the calculation <see cref="Candle.PriceLevels"/>. By default, it is disabled.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VolumeProfileKey,
		Description = LocalizedStrings.VolumeProfileCalcKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 2)]
	public bool IsCalcVolumeProfile { get; set; }

	/// <summary>
	/// The initial date from which you need to get data.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FromKey,
		Description = LocalizedStrings.StartDateDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 3)]
	public DateTimeOffset? From { get; set; }

	/// <summary>
	/// The final date by which you need to get data.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.UntilKey,
		Description = LocalizedStrings.ToDateDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 4)]
	public DateTimeOffset? To { get; set; }

	/// <summary>
	/// Allow build candles from smaller timeframe.
	/// </summary>
	/// <remarks>
	/// Available only for <see cref="TimeFrameCandle"/>.
	/// </remarks>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SmallerTimeFrameKey,
		Description = LocalizedStrings.SmallerTimeFrameDescKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 5)]
	public bool AllowBuildFromSmallerTimeFrame { get; set; } = true;

	/// <summary>
	/// Use only the regular trading hours for which data will be requested.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RegularHoursKey,
		Description = LocalizedStrings.RegularTradingHoursKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 6)]
	public bool? IsRegularTradingHours { get; set; }

	/// <summary>
	/// Market-data count.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CountKey,
		Description = LocalizedStrings.CandlesCountKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 7)]
	public long? Count { get; set; }

	/// <summary>
	/// Build mode.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ModeKey,
		Description = LocalizedStrings.BuildModeKey,
		GroupName = LocalizedStrings.BuildKey,
		Order = 20)]
	public MarketDataBuildModes BuildCandlesMode { get; set; }

	/// <summary>
	/// Which market-data type is used as a source value.
	/// </summary>
	[Browsable(false)]
	[Obsolete("Use BuildCandlesFrom2 property.")]
	public MarketDataTypes? BuildCandlesFrom
	{
		get => BuildCandlesFrom2?.ToMarketDataType();
		set => BuildCandlesFrom2 = value?.ToDataType(null);
	}

	/// <summary>
	/// Which market-data type is used as a source value.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SourceKey,
		Description = LocalizedStrings.CandlesBuildSourceKey,
		GroupName = LocalizedStrings.BuildKey,
		Order = 21)]
	public Messages.DataType BuildCandlesFrom2 { get; set; }

	/// <summary>
	/// Extra info for the <see cref="BuildCandlesFrom"/>.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FieldKey,
		Description = LocalizedStrings.Level1FieldKey,
		GroupName = LocalizedStrings.BuildKey,
		Order = 22)]
	public Level1Fields? BuildCandlesField { get; set; }

	/// <summary>
	/// Request <see cref="CandleStates.Finished"/> only candles.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FinishedKey,
		Description = LocalizedStrings.FinishedKey,
		GroupName = LocalizedStrings.BuildKey,
		Order = 23)]
	public bool IsFinishedOnly { get; set; }

	/// <summary>
	/// Try fill gaps.
	/// </summary>
	[Obsolete("Use separate subscriptions.")]
	[Browsable(false)]
	public bool FillGaps { get; set; }

	/// <inheritdoc />
	public override string ToString()
	{
		return CandleType?.Name + "_" + Security + "_" + (Arg is null ? "NULL" : CandleType?.ToCandleMessageType().DataTypeArgToString(Arg));
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		var secProvider = EntitiesExtensions.TrySecurityProvider;

		if (secProvider != null)
		{
			var securityId = storage.GetValue<string>(nameof(SecurityId));

			if (!securityId.IsEmpty())
				Security = secProvider.LookupById(securityId);
		}

		var candleType = storage.GetValue<string>(nameof(CandleType));

		if (!candleType.IsEmpty())
		{
			candleType = Regex.Replace(candleType,
				@"(StockSharp\.Algo\.Candles\.(?:.*?)Candle), StockSharp\.Algo",
				"$1, StockSharp.BusinessEntities");

			CandleType = candleType.To<Type>();
		}

		if (CandleType != null)
			Arg = CandleType.ToCandleMessageType().ToDataTypeArg(storage.GetValue<string>(nameof(Arg)));

		From = storage.GetValue(nameof(From), From);
		To = storage.GetValue(nameof(To), To);
		WorkingTime = storage.GetValue<SettingsStorage>(nameof(WorkingTime))?.Load<WorkingTime>();

		IsCalcVolumeProfile = storage.GetValue(nameof(IsCalcVolumeProfile), IsCalcVolumeProfile);

		BuildCandlesMode = storage.GetValue(nameof(BuildCandlesMode), BuildCandlesMode);

		if (storage.ContainsKey(nameof(BuildCandlesFrom2)))
			BuildCandlesFrom2 = storage.GetValue<SettingsStorage>(nameof(BuildCandlesFrom2)).Load<Messages.DataType>();
		else if (storage.ContainsKey(nameof(BuildCandlesFrom)))
			BuildCandlesFrom = storage.GetValue(nameof(BuildCandlesFrom), BuildCandlesFrom);

		BuildCandlesField = storage.GetValue(nameof(BuildCandlesField), BuildCandlesField);
		AllowBuildFromSmallerTimeFrame = storage.GetValue(nameof(AllowBuildFromSmallerTimeFrame), AllowBuildFromSmallerTimeFrame);
		IsRegularTradingHours = storage.GetValue(nameof(IsRegularTradingHours), IsRegularTradingHours);
		Count = storage.GetValue(nameof(Count), Count);
		IsFinishedOnly = storage.GetValue(nameof(IsFinishedOnly), IsFinishedOnly);
		//FillGaps = storage.GetValue(nameof(FillGaps), FillGaps);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		if (Security != null)
			storage.SetValue(nameof(SecurityId), Security.Id);

		if (CandleType != null)
			storage.SetValue(nameof(CandleType), CandleType.GetTypeName(false));

		if (Arg != null && CandleType != null)
			storage.SetValue(nameof(Arg), CandleType.ToCandleMessageType().DataTypeArgToString(Arg));

		storage.SetValue(nameof(From), From);
		storage.SetValue(nameof(To), To);

		if (WorkingTime != null)
			storage.SetValue(nameof(WorkingTime), WorkingTime.Save());

		storage.SetValue(nameof(IsCalcVolumeProfile), IsCalcVolumeProfile);

		storage.SetValue(nameof(BuildCandlesMode), BuildCandlesMode);

		if (BuildCandlesFrom2 != null)
			storage.SetValue(nameof(BuildCandlesFrom2), BuildCandlesFrom2.Save());

		storage.SetValue(nameof(BuildCandlesField), BuildCandlesField);
		storage.SetValue(nameof(AllowBuildFromSmallerTimeFrame), AllowBuildFromSmallerTimeFrame);
		storage.SetValue(nameof(IsRegularTradingHours), IsRegularTradingHours);
		storage.SetValue(nameof(Count), Count);
		storage.SetValue(nameof(IsFinishedOnly), IsFinishedOnly);
		//storage.SetValue(nameof(FillGaps), FillGaps);
	}
}