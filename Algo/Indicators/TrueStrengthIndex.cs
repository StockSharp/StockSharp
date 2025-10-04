namespace StockSharp.Algo.Indicators;

/// <summary>
/// True Strength Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/true_strength_index.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TSIKey,
	Description = LocalizedStrings.TrueStrengthIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/true_strength_index.html")]
[IndicatorOut(typeof(ITrueStrengthIndexValue))]
public class TrueStrengthIndex : BaseComplexIndicator<ITrueStrengthIndexValue>
{
	/// <summary>
	/// Internal TSI calculation line.
	/// </summary>
	[IndicatorHidden]
	public class Line : BaseIndicator
	{
		private readonly ExponentialMovingAverage _firstSmoothedMomentum;
		private readonly ExponentialMovingAverage _doubleSmoothedMomentum;
		private readonly ExponentialMovingAverage _firstSmoothedAbsMomentum;
		private readonly ExponentialMovingAverage _doubleSmoothedAbsMomentum;
		private bool _isInitialized;
		private decimal _lastPrice;

		/// <summary>
		/// Initializes a new instance of the <see cref="Line"/>.
		/// </summary>
		public Line()
		{
			_firstSmoothedMomentum = new();
			_doubleSmoothedMomentum = new();
			_firstSmoothedAbsMomentum = new();
			_doubleSmoothedAbsMomentum = new();
		}

		/// <summary>
		/// First smoothing period.
		/// </summary>
		public int FirstLength
		{
			get => _firstSmoothedMomentum.Length;
			set
			{
				_firstSmoothedMomentum.Length = value;
				_firstSmoothedAbsMomentum.Length = value;

				Reset();
			}
		}

		/// <summary>
		/// Second smoothing period.
		/// </summary>
		public int SecondLength
		{
			get => _doubleSmoothedMomentum.Length;
			set
			{
				_doubleSmoothedMomentum.Length = value;
				_doubleSmoothedAbsMomentum.Length = value;

				Reset();
			}
		}

		/// <inheritdoc />
		public override int NumValuesToInitialize
			=> _doubleSmoothedMomentum.NumValuesToInitialize + 1;

		/// <inheritdoc />
		protected override bool CalcIsFormed()
			=> _doubleSmoothedMomentum.IsFormed && _doubleSmoothedAbsMomentum.IsFormed;

		/// <inheritdoc />
		public override void Reset()
		{
			base.Reset();

			_firstSmoothedMomentum.Reset();
			_doubleSmoothedMomentum.Reset();
			_firstSmoothedAbsMomentum.Reset();
			_doubleSmoothedAbsMomentum.Reset();
			_isInitialized = false;
		}

		/// <inheritdoc />
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			var currentPrice = input.ToDecimal(Source);

			if (!_isInitialized)
			{
				if (input.IsFinal)
				{
					_lastPrice = currentPrice;
					_isInitialized = true;
				}

				return new DecimalIndicatorValue(this, input.Time);
			}

			var momentum = currentPrice - _lastPrice;
			var absMomentum = Math.Abs(momentum);

			var firstSmoothedMomentum = _firstSmoothedMomentum.Process(input, momentum).ToDecimal(Source);
			var firstSmoothedAbsMomentum = _firstSmoothedAbsMomentum.Process(input, absMomentum).ToDecimal(Source);

			var doubleSmoothedMomentum = _doubleSmoothedMomentum.Process(input, firstSmoothedMomentum).ToDecimal(Source);
			var doubleSmoothedAbsMomentum = _doubleSmoothedAbsMomentum.Process(input, firstSmoothedAbsMomentum).ToDecimal(Source);

			if (input.IsFinal)
				_lastPrice = currentPrice;

			var tsi = doubleSmoothedAbsMomentum != 0
				? 100 * doubleSmoothedMomentum / doubleSmoothedAbsMomentum
				: 0;

			return new DecimalIndicatorValue(this, tsi, input.Time);
		}
	}

	private readonly Line _tsiLine;
	private readonly ExponentialMovingAverage _signalLine;

	/// <summary>
	/// TSI main line indicator (internal line instance).
	/// </summary>
	[Browsable(false)]
	public Line Tsi => _tsiLine;

	/// <summary>
	/// Signal line (EMA applied to TSI values).
	/// </summary>
	[Browsable(false)]
	public ExponentialMovingAverage Signal => _signalLine;

	/// <summary>
	/// Initializes a new instance of the <see cref="TrueStrengthIndex"/>.
	/// </summary>
	public TrueStrengthIndex()
	{
		_tsiLine = new();
		_signalLine = new();

		FirstLength = 25;
		SecondLength = 13;
		SignalLength = 7;

		AddInner(_tsiLine);
		AddInner(_signalLine);

		Mode = ComplexIndicatorModes.Sequence;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <summary>
	/// First smoothing period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FirstKey,
		Description = LocalizedStrings.FirstSmoothingPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int FirstLength
	{
		get => _tsiLine.FirstLength;
		set
		{
			_tsiLine.FirstLength = value;
			Reset();
		}
	}

	/// <summary>
	/// Second smoothing period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecondKey,
		Description = LocalizedStrings.SecondSmoothingPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int SecondLength
	{
		get => _tsiLine.SecondLength;
		set
		{
			_tsiLine.SecondLength = value;
			Reset();
		}
	}

	/// <summary>
	/// Signal line period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SignalKey,
		Description = LocalizedStrings.SignalMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int SignalLength
	{
		get => _signalLine.Length;
		set
		{
			_signalLine.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = new TrueStrengthIndexValue(this, input.Time);

		var tsiValue = _tsiLine.Process(input);
		result.Add(_tsiLine, tsiValue);

		if (_tsiLine.IsFormed)
		{
			var signalValue = _signalLine.Process(tsiValue);
			result.Add(_signalLine, signalValue);
		}

		return result;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		FirstLength = storage.GetValue<int>(nameof(FirstLength));
		SecondLength = storage.GetValue<int>(nameof(SecondLength));
		SignalLength = storage.GetValue<int>(nameof(SignalLength));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
			.Set(nameof(FirstLength), FirstLength)
			.Set(nameof(SecondLength), SecondLength)
			.Set(nameof(SignalLength), SignalLength)
		;
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" {FirstLength}/{SecondLength}/{SignalLength}";

	/// <inheritdoc />
	protected override ITrueStrengthIndexValue CreateValue(DateTimeOffset time)
		=> new TrueStrengthIndexValue(this, time);
}

/// <summary>
/// TSI composite indicator value.
/// </summary>
public interface ITrueStrengthIndexValue : IComplexIndicatorValue
{
	/// <summary>
	/// Raw TSI line value object.
	/// </summary>
	IIndicatorValue TsiValue { get; }

	/// <summary>
	/// Raw Signal line value object.
	/// </summary>
	IIndicatorValue SignalValue { get; }

	/// <summary>
	/// TSI numeric value (if available).
	/// </summary>
	[Browsable(false)]
	decimal? Tsi { get; }

	/// <summary>
	/// Signal numeric value (if available).
	/// </summary>
	[Browsable(false)]
	decimal? Signal { get; }
}

class TrueStrengthIndexValue(TrueStrengthIndex indicator, DateTimeOffset time) : ComplexIndicatorValue<TrueStrengthIndex>(indicator, time), ITrueStrengthIndexValue
{
	public IIndicatorValue TsiValue => this[TypedIndicator.Tsi];
	public IIndicatorValue SignalValue => this[TypedIndicator.Signal];

	public decimal? Tsi => TsiValue.ToNullableDecimal(TypedIndicator.Source);
	public decimal? Signal => SignalValue.ToNullableDecimal(TypedIndicator.Source);

	public override string ToString() => $"TSI={Tsi}, Signal={Signal}";
}