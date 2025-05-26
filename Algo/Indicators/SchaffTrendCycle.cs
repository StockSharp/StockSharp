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
	private decimal _prevStochK;

	/// <summary>
	/// Initializes a new instance of the <see cref="SchaffTrendCycle"/>.
	/// </summary>
	public SchaffTrendCycle()
	{
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

		Buffer.MaxComparer = Buffer.MinComparer = Comparer<decimal>.Default;

		Length = 10;
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

		_prevStochK = default;
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Macd.IsFormed && StochasticK.IsFormed && base.CalcIsFormed();

	/// <inheritdoc />
	public override int NumValuesToInitialize => Macd.NumValuesToInitialize + StochasticK.NumValuesToInitialize + base.NumValuesToInitialize;

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		if (input.IsFinal)
			Buffer.PushBack(input.ToDecimal());

		var macdVal = (ComplexIndicatorValue)Macd.Process(input);

		if (!Macd.IsFormed)
			return null;

		var macdHist = macdVal[Macd.Macd].ToDecimal() - macdVal[Macd.SignalMa].ToDecimal();
		var den = Buffer.Max.Value - Buffer.Min.Value;
		var stochK = den == 0 ? _prevStochK : StochasticK.Process(input, (macdHist - Buffer.Min.Value) / den).ToDecimal();

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