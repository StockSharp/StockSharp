namespace StockSharp.Algo.Indicators;

/// <summary>
/// Schaff Trend Cycle (STC) indicator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/schaff_trend_cycle.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.STCKey,
	Description = LocalizedStrings.SchaffTrendCycleKey)]
[Doc("topics/api/indicators/list_of_indicators/schaff_trend_cycle.html")]
public class SchaffTrendCycle : ExponentialMovingAverage
{
	private readonly CircularBufferEx<decimal> _buffer = new(10);
	private decimal _prevStochK;

	/// <summary>
	/// Initializes a new instance of the <see cref="SchaffTrendCycle"/>.
	/// </summary>
	public SchaffTrendCycle()
	{
		_buffer.MaxComparer = _buffer.MinComparer = Comparer<decimal>.Default;

		Macd = new()
		{
			Macd =
			{
				ShortMa = { Length = 23 },
				LongMa = { Length = 50 },
			},

			SignalMa = { Length = 3 },
		};

		StochasticK = new() { Length = 5 };

		AddResetTracking(Macd);
		AddResetTracking(StochasticK);
		
		Length = _buffer.Capacity;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <summary>
	/// Convergence/divergence of moving averages.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MACDKey,
		Description = LocalizedStrings.MACDDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public MovingAverageConvergenceDivergenceSignal Macd { get; }

	/// <summary>
	/// <see cref="StochasticK"/>.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StochasticKKey,
		Description = LocalizedStrings.StochasticKDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public StochasticK StochasticK { get; }

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_buffer.Capacity = Length;
		_prevStochK = default;
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Macd.IsFormed && StochasticK.IsFormed && base.CalcIsFormed();

	/// <inheritdoc />
	public override int NumValuesToInitialize => Macd.NumValuesToInitialize + StochasticK.NumValuesToInitialize + base.NumValuesToInitialize - 2;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		if (input.IsFinal)
			_buffer.PushBack(input.ToDecimal());

		var macdVal = (MovingAverageConvergenceDivergenceSignalValue)Macd.Process(input);

		if (!Macd.IsFormed || macdVal.Macd is not decimal macd || macdVal.Signal is not decimal signal)
			return null;

		var macdHist = macd - signal;

		var den = _buffer.Max.Value - _buffer.Min.Value;
		var stochK = den == 0 ? _prevStochK : StochasticK.Process(input, (macdHist - _buffer.Min.Value) / den).ToDecimal();

		if (!StochasticK.IsFormed)
			return null;

		if (input.IsFinal)
			_prevStochK = stochK;

		return base.OnProcessDecimal(input.SetValue(this, stochK));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Macd), Macd.Save());
		storage.SetValue(nameof(StochasticK), StochasticK.Save());
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Macd.LoadIfNotNull(storage, nameof(Macd));
		StochasticK.LoadIfNotNull(storage, nameof(StochasticK));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" FL={Macd.Macd.ShortMa.Length},SL={Macd.Macd.LongMa.Length}";
}