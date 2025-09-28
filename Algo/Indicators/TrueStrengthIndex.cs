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
[IndicatorOut(typeof(TrueStrengthIndexValue))]
public class TrueStrengthIndex : BaseComplexIndicator<TrueStrengthIndexValue>
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
			var currentPrice = input.ToDecimal();

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

			var firstSmoothedMomentum = _firstSmoothedMomentum.Process(input, momentum).ToDecimal();
			var firstSmoothedAbsMomentum = _firstSmoothedAbsMomentum.Process(input, absMomentum).ToDecimal();

			var doubleSmoothedMomentum = _doubleSmoothedMomentum.Process(input, firstSmoothedMomentum).ToDecimal();
			var doubleSmoothedAbsMomentum = _doubleSmoothedAbsMomentum.Process(input, firstSmoothedAbsMomentum).ToDecimal();

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
	protected override TrueStrengthIndexValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// TSI composite indicator value.
/// </summary>
/// <remarks>
/// Holds TSI (main) and Signal line values.
/// </remarks>
/// <param name="indicator">Parent <see cref="TrueStrengthIndex"/>.</param>
/// <param name="time"><see cref="IIndicatorValue.Time"/>.</param>
public class TrueStrengthIndexValue(TrueStrengthIndex indicator, DateTimeOffset time) : ComplexIndicatorValue<TrueStrengthIndex>(indicator, time)
{
	/// <summary>
	/// Raw TSI line value object.
	/// </summary>
	public IIndicatorValue TsiValue => this[TypedIndicator.Tsi];

	/// <summary>
	/// Raw Signal line value object.
	/// </summary>
	public IIndicatorValue SignalValue => this[TypedIndicator.Signal];

	/// <summary>
	/// TSI numeric value (if available).
	/// </summary>
	[Browsable(false)]
	public decimal? Tsi => TsiValue.ToNullableDecimal();

	/// <summary>
	/// Signal numeric value (if available).
	/// </summary>
	[Browsable(false)]
	public decimal? Signal => SignalValue.ToNullableDecimal();

	/// <inheritdoc />
	public override string ToString() => $"TSI={Tsi}, Signal={Signal}";
}