namespace StockSharp.Algo.Indicators;

/// <summary>
/// Constance Brown Composite Index indicator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/constance_brown_composite_index.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CBCIKey,
	Description = LocalizedStrings.ConstanceBrownCompositeIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/constance_brown_composite_index.html")]
[IndicatorOut(typeof(IConstanceBrownCompositeIndexValue))]
public class ConstanceBrownCompositeIndex : BaseComplexIndicator<IConstanceBrownCompositeIndexValue>
{
	private readonly RelativeStrengthIndex _rsi;
	private readonly RateOfChange _rsiRoc;
	private readonly RelativeStrengthIndex _shortRsi;
	private readonly SimpleMovingAverage _rsiMomentum;

	/// <summary>
	/// Composite index line (main line).
	/// </summary>
	[Browsable(false)]
	public CompositeIndexLine CompositeIndexLine { get; }

	/// <summary>
	/// Fast moving average of composite index.
	/// </summary>
	[Browsable(false)]
	public SimpleMovingAverage FastSma { get; }

	/// <summary>
	/// Slow moving average of composite index.
	/// </summary>
	[Browsable(false)]
	public SimpleMovingAverage SlowSma { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ConstanceBrownCompositeIndex"/>.
	/// </summary>
	public ConstanceBrownCompositeIndex()
	{
		_rsi = new() { Length = 14 };
		_rsiRoc = new() { Length = 9 };
		_shortRsi = new() { Length = 3 };
		_rsiMomentum = new() { Length = 3 };

		CompositeIndexLine = new();
		FastSma = new() { Length = 13 };
		SlowSma = new() { Length = 33 };

		AddInner(CompositeIndexLine);
		AddInner(FastSma);
		AddInner(SlowSma);
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	private int _rsiLength = 14;

	/// <summary>
	/// RSI period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int RsiLength
	{
		get => _rsiLength;
		set
		{
			_rsiLength = value;
			_rsi.Length = value;

			Reset();
		}
	}

	/// <summary>
	/// ROC period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RocLengthKey,
		Description = LocalizedStrings.RocLengthKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int RocLength
	{
		get => _rsiRoc.Length;
		set
		{
			_rsiRoc.Length = value;

			Reset();
		}
	}

	/// <summary>
	/// Short RSI period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.RSIKey,
		Description = LocalizedStrings.RSIPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int ShortRsiLength
	{
		get => _shortRsi.Length;
		set
		{
			_shortRsi.Length = value;

			Reset();
		}
	}

	/// <summary>
	/// Momentum SMA period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MomentumKey,
		Description = LocalizedStrings.MomentumKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int MomentumLength
	{
		get => _rsiMomentum.Length;
		set
		{
			_rsiMomentum.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// Fast SMA period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortMaKey,
		Description = LocalizedStrings.ShortMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int FastSmaLength
	{
		get => FastSma.Length;
		set => FastSma.Length = value;
	}

	/// <summary>
	/// Slow SMA period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LongMaKey,
		Description = LocalizedStrings.LongMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int SlowSmaLength
	{
		get => SlowSma.Length;
		set => SlowSma.Length = value;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_rsi.Reset();
		_rsiRoc.Reset();
		_shortRsi.Reset();
		_rsiMomentum.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize =>
		(_rsi.NumValuesToInitialize + _rsiRoc.NumValuesToInitialize - 1)
			.Max(_shortRsi.NumValuesToInitialize + _rsiMomentum.NumValuesToInitialize - 1)
		+ CompositeIndexLine.NumValuesToInitialize + FastSma.NumValuesToInitialize.Max(SlowSma.NumValuesToInitialize) - 2;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new ConstanceBrownCompositeIndexValue(this, input.Time);

		var rsiValue = _rsi.Process(input);
		var shortRsiValue = _shortRsi.Process(input);

		IIndicatorValue rocValue;
		if (_rsi.IsFormed)
		{
			var rsiDecimal = rsiValue.ToDecimal();
			rocValue = _rsiRoc.Process(rsiDecimal, input.Time, input.IsFinal);
		}
		else
			rocValue = new DecimalIndicatorValue(_rsiRoc, input.Time) { IsFinal = input.IsFinal };

		IIndicatorValue momentumValue;
		if (_shortRsi.IsFormed)
		{
			var shortRsiDecimal = shortRsiValue.ToDecimal();
			momentumValue = _rsiMomentum.Process(shortRsiDecimal, input.Time, input.IsFinal);
		}
		else
			momentumValue = new DecimalIndicatorValue(_rsiMomentum, input.Time) { IsFinal = input.IsFinal };

		if (_rsiRoc.IsFormed && _rsiMomentum.IsFormed)
		{
			var rsiChange = rocValue.ToDecimal();
			var rsiMom = momentumValue.ToDecimal();
			var compositeIndexValue = rsiChange + rsiMom;

			var compositeValue = CompositeIndexLine.Process(compositeIndexValue, input.Time, input.IsFinal);

			if (CompositeIndexLine.IsFormed)
			{
				var fastValue = FastSma.Process(compositeValue);
				var slowValue = SlowSma.Process(compositeValue);

				result.Add(CompositeIndexLine, compositeValue);
				result.Add(FastSma, fastValue);
				result.Add(SlowSma, slowValue);

				if (input.IsFinal && FastSma.IsFormed && SlowSma.IsFormed)
					IsFormed = true;
			}
		}

		return result;
	}

	/// <inheritdoc />
	protected override IConstanceBrownCompositeIndexValue CreateValue(DateTimeOffset time)
		=> new ConstanceBrownCompositeIndexValue(this, time);

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		RsiLength = storage.GetValue<int>(nameof(RsiLength));
		RocLength = storage.GetValue<int>(nameof(RocLength));
		ShortRsiLength = storage.GetValue<int>(nameof(ShortRsiLength));
		MomentumLength = storage.GetValue<int>(nameof(MomentumLength));
		FastSmaLength = storage.GetValue<int>(nameof(FastSmaLength));
		SlowSmaLength = storage.GetValue<int>(nameof(SlowSmaLength));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
			.Set(nameof(RsiLength), RsiLength)
			.Set(nameof(RocLength), RocLength)
			.Set(nameof(ShortRsiLength), ShortRsiLength)
			.Set(nameof(MomentumLength), MomentumLength)
			.Set(nameof(FastSmaLength), FastSmaLength)
			.Set(nameof(SlowSmaLength), SlowSmaLength)
		;
	}
}

/// <summary>
/// Composite Index Line indicator.
/// </summary>
[IndicatorHidden]
public class CompositeIndexLine : BaseIndicator
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
/// <see cref="ConstanceBrownCompositeIndex"/> indicator value.
/// </summary>
public interface IConstanceBrownCompositeIndexValue : IComplexIndicatorValue
{
	/// <summary>
	/// Gets the composite index line (main line).
	/// </summary>
	IIndicatorValue CompositeIndexLineValue { get; }

	/// <summary>
	/// Gets the composite index line value.
	/// </summary>
	[Browsable(false)]
	decimal? CompositeIndexLine { get; }

	/// <summary>
	/// Gets the fast moving average.
	/// </summary>
	IIndicatorValue FastSmaValue { get; }

	/// <summary>
	/// Gets the fast moving average value.
	/// </summary>
	[Browsable(false)]
	decimal? FastSma { get; }

	/// <summary>
	/// Gets the slow moving average.
	/// </summary>
	IIndicatorValue SlowSmaValue { get; }

	/// <summary>
	/// Gets the slow moving average value.
	/// </summary>
	[Browsable(false)]
	decimal? SlowSma { get; }
}

/// <summary>
/// ConstanceBrownCompositeIndex indicator value implementation.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="ConstanceBrownCompositeIndexValue"/> class.
/// </remarks>
/// <param name="indicator">The parent ConstanceBrownCompositeIndex indicator.</param>
/// <param name="time">Time associated with this indicator value.</param>
public class ConstanceBrownCompositeIndexValue(ConstanceBrownCompositeIndex indicator, DateTimeOffset time)
	: ComplexIndicatorValue<ConstanceBrownCompositeIndex>(indicator, time), IConstanceBrownCompositeIndexValue
{
	/// <inheritdoc />
	public IIndicatorValue CompositeIndexLineValue => this[TypedIndicator.CompositeIndexLine];

	/// <inheritdoc />
	public decimal? CompositeIndexLine => CompositeIndexLineValue.ToNullableDecimal();

	/// <inheritdoc />
	public IIndicatorValue FastSmaValue => this[TypedIndicator.FastSma];

	/// <inheritdoc />
	public decimal? FastSma => FastSmaValue.ToNullableDecimal();

	/// <inheritdoc />
	public IIndicatorValue SlowSmaValue => this[TypedIndicator.SlowSma];

	/// <inheritdoc />
	public decimal? SlowSma => SlowSmaValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"CI={CompositeIndexLine}, Fast={FastSma}, Slow={SlowSma}";
}