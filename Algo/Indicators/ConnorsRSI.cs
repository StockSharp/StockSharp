namespace StockSharp.Algo.Indicators;

/// <summary>
/// Connors RSI (CRSI) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CRSIKey,
	Description = LocalizedStrings.ConnorsRSIKey)]
[Doc("topics/api/indicators/list_of_indicators/connors_rsi.html")]
[IndicatorOut(typeof(ConnorsRSIValue))]
public class ConnorsRSI : BaseComplexIndicator<ConnorsRSIValue>
{
	private readonly RateOfChange _roc = new();
	private readonly CircularBuffer<decimal> _streakBuffer = new(2);

	/// <summary>
	/// RSI indicator.
	/// </summary>
	[Browsable(false)]
	public RelativeStrengthIndex Rsi { get; } = new();

	/// <summary>
	/// Up/down RSI indicator.
	/// </summary>
	[Browsable(false)]
	public RelativeStrengthIndex UpDownRsi { get; } = new();

	/// <summary>
	/// ROC RSI indicator.
	/// </summary>
	[Browsable(false)]
	public RelativeStrengthIndex RocRsi { get; } = new();

	/// <summary>
	/// Composite RSI line.
	/// </summary>
	[Browsable(false)]
	public CrsiLine CrsiLine { get; } = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="ConnorsRSI"/>.
	/// </summary>
	public ConnorsRSI()
	{
		AddInner(Rsi);
		AddInner(UpDownRsi);
		AddInner(RocRsi);
		AddInner(CrsiLine);

		RSIPeriod = 3;
		StreakRSIPeriod = 2;
		ROCRSIPeriod = 100;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <summary>
	/// RSI Period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RSIKey,
		Description = LocalizedStrings.RSIPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int RSIPeriod
	{
		get => Rsi.Length;
		set => Rsi.Length = value;
	}

	/// <summary>
	/// Streak RSI Period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StreakKey,
		Description = LocalizedStrings.StreakRSIPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int StreakRSIPeriod
	{
		get => UpDownRsi.Length;
		set => UpDownRsi.Length = value;
	}

	/// <summary>
	/// ROC RSI Period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ROCRSIKey,
		Description = LocalizedStrings.ROCRSIPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int ROCRSIPeriod
	{
		get => RocRsi.Length;
		set
		{
			RocRsi.Length = value;
			_roc.Length = value;
		}
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize
		=> Rsi.NumValuesToInitialize
		.Max(UpDownRsi.NumValuesToInitialize)
		.Max(_roc.NumValuesToInitialize)
		.Max(RocRsi.NumValuesToInitialize);

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var rsiValue = Rsi.Process(input);

		var streak = CalculateStreak(candle.ClosePrice, input.IsFinal);
		var updownRsiValue = UpDownRsi.Process(input, streak);

		var rocValue = _roc.Process(input);

		var rocRsiValue = rocValue.IsEmpty
			? new DecimalIndicatorValue(RocRsi, input.Time)
			: RocRsi.Process(rocValue);

		var result = new ConnorsRSIValue(this, input.Time);

		if (!rocValue.IsEmpty && Rsi.IsFormed && UpDownRsi.IsFormed && RocRsi.IsFormed && _roc.IsFormed)
		{
			if (input.IsFinal)
				IsFormed = true;

			var rsi = rsiValue.ToDecimal();
			var updownRsi = updownRsiValue.ToDecimal();
			var rocRsi = rocRsiValue.ToDecimal();

			var crsi = (rsi + updownRsi + rocRsi) / 3;
			var crsiValue = CrsiLine.Process(input, crsi);

			result.Add(Rsi, rsiValue);
			result.Add(UpDownRsi, updownRsiValue);
			result.Add(RocRsi, rocRsiValue);
			result.Add(CrsiLine, crsiValue);
		}

		return result;
	}

	private decimal CalculateStreak(decimal currentPrice, bool isFinal)
	{
		var streak = 1m;

		if (_streakBuffer.Count == 2)
		{
			var prevStreak = _streakBuffer[0];
			var prevPrice = _streakBuffer[1];

			if (currentPrice > prevPrice)
				streak = prevStreak > 0 ? prevStreak + 1 : 1;
			else if (currentPrice < prevPrice)
				streak = prevStreak < 0 ? prevStreak - 1 : -1;
			else
				streak = 0;

			if (isFinal)
				_streakBuffer.PopFront();
		}

		if (isFinal)
		{
			_streakBuffer.PushBack(streak);
			_streakBuffer.PushBack(currentPrice);
		}

		return streak;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_roc.Reset();
		_streakBuffer.Clear();

		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
			.Set(nameof(RSIPeriod), RSIPeriod)
			.Set(nameof(StreakRSIPeriod), StreakRSIPeriod)
			.Set(nameof(ROCRSIPeriod), ROCRSIPeriod)
		;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		RSIPeriod = storage.GetValue<int>(nameof(RSIPeriod));
		StreakRSIPeriod = storage.GetValue<int>(nameof(StreakRSIPeriod));
		ROCRSIPeriod = storage.GetValue<int>(nameof(ROCRSIPeriod));
	}

	/// <inheritdoc />
	protected override ConnorsRSIValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// Connors RSI line indicator.
/// </summary>
[IndicatorHidden]
public class CrsiLine : BaseIndicator
{
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		return input;
	}
}

/// <summary>
/// <see cref="ConnorsRSI"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnorsRSIValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="ConnorsRSI"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class ConnorsRSIValue(ConnorsRSI indicator, DateTimeOffset time) : ComplexIndicatorValue<ConnorsRSI>(indicator, time)
{
	/// <summary>
	/// Gets the RSI component.
	/// </summary>
	public IIndicatorValue RsiValue => this[TypedIndicator.Rsi];

	/// <summary>
	/// Gets the RSI component.
	/// </summary>
	[Browsable(false)]
	public decimal? Rsi => RsiValue.ToNullableDecimal();

	/// <summary>
	/// Gets the UpDown RSI component.
	/// </summary>
	public IIndicatorValue UpDownRsiValue => this[TypedIndicator.UpDownRsi];

	/// <summary>
	/// Gets the UpDown RSI component.
	/// </summary>
	[Browsable(false)]
	public decimal? UpDownRsi => UpDownRsiValue.ToNullableDecimal();

	/// <summary>
	/// Gets the ROC RSI component.
	/// </summary>
	public IIndicatorValue RocRsiValue => this[TypedIndicator.RocRsi];

	/// <summary>
	/// Gets the ROC RSI component.
	/// </summary>
	[Browsable(false)]
	public decimal? RocRsi => RocRsiValue.ToNullableDecimal();

	/// <summary>
	/// Gets the composite RSI line.
	/// </summary>
	public IIndicatorValue CrsiLineValue => this[TypedIndicator.CrsiLine];

	/// <summary>
	/// Gets the composite RSI line.
	/// </summary>
	[Browsable(false)]
	public decimal? CrsiLine => CrsiLineValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"Rsi={Rsi}, UpDownRsi={UpDownRsi}, RocRsi={RocRsi}, CrsiLine={CrsiLine}";
}
