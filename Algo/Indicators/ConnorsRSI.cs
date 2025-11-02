namespace StockSharp.Algo.Indicators;

/// <summary>
/// Connors RSI (CRSI) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CRSIKey,
	Description = LocalizedStrings.ConnorsRSIKey)]
[Doc("topics/api/indicators/list_of_indicators/connors_rsi.html")]
[IndicatorOut(typeof(IConnorsRSIValue))]
public class ConnorsRSI : BaseComplexIndicator<IConnorsRSIValue>
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

			var rsi = rsiValue.ToDecimal(Source);
			var updownRsi = updownRsiValue.ToDecimal(Source);
			var rocRsi = rocRsiValue.ToDecimal(Source);

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
	protected override IConnorsRSIValue CreateValue(DateTime time)
		=> new ConnorsRSIValue(this, time);
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
public interface IConnorsRSIValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the RSI component.
	/// </summary>
	IIndicatorValue RsiValue { get; }

	/// <summary>
	/// Gets the RSI component.
	/// </summary>
	[Browsable(false)]
	decimal? Rsi { get; }

	/// <summary>
	/// Gets the UpDown RSI component.
	/// </summary>
	IIndicatorValue UpDownRsiValue { get; }

	/// <summary>
	/// Gets the UpDown RSI component.
	/// </summary>
	[Browsable(false)]
	decimal? UpDownRsi { get; }

	/// <summary>
	/// Gets the ROC RSI component.
	/// </summary>
	IIndicatorValue RocRsiValue { get; }

	/// <summary>
	/// Gets the ROC RSI component.
	/// </summary>
	[Browsable(false)]
	decimal? RocRsi { get; }

	/// <summary>
	/// Gets the composite RSI line.
	/// </summary>
	IIndicatorValue CrsiLineValue { get; }

	/// <summary>
	/// Gets the composite RSI line.
	/// </summary>
	[Browsable(false)]
	decimal? CrsiLine { get; }
}

/// <summary>
/// ConnorsRSI indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConnorsRSIValue"/> class.
/// </remarks>
/// <param name="indicator">The parent ConnorsRSI indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class ConnorsRSIValue(ConnorsRSI indicator, DateTime time) : ComplexIndicatorValue<ConnorsRSI>(indicator, time), IConnorsRSIValue
{
	/// <inheritdoc />
	public IIndicatorValue RsiValue => this[TypedIndicator.Rsi];
	/// <inheritdoc />
	public decimal? Rsi => RsiValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue UpDownRsiValue => this[TypedIndicator.UpDownRsi];
	/// <inheritdoc />
	public decimal? UpDownRsi => UpDownRsiValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue RocRsiValue => this[TypedIndicator.RocRsi];
	/// <inheritdoc />
	public decimal? RocRsi => RocRsiValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public IIndicatorValue CrsiLineValue => this[TypedIndicator.CrsiLine];
	/// <inheritdoc />
	public decimal? CrsiLine => CrsiLineValue.ToNullableDecimal(TypedIndicator.Source);

	/// <inheritdoc />
	public override string ToString() => $"Rsi={Rsi}, UpDownRsi={UpDownRsi}, RocRsi={RocRsi}, CrsiLine={CrsiLine}";
}
