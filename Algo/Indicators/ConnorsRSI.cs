namespace StockSharp.Algo.Indicators;

/// <summary>
/// Connors RSI (CRSI) indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CRSIKey,
	Description = LocalizedStrings.ConnorsRSIKey)]
[Doc("topics/api/indicators/list_of_indicators/connors_rsi.html")]
public class ConnorsRSI : BaseComplexIndicator
{
	private class CrsiLine : BaseIndicator
	{
		protected override IIndicatorValue OnProcess(IIndicatorValue input)
		{
			if (input.IsFinal)
				IsFormed = true;

			return input;
		}
	}

	private readonly RelativeStrengthIndex _rsi = new();
	private readonly RelativeStrengthIndex _updownRsi = new();
	private readonly RelativeStrengthIndex _rocRsi = new();
	private readonly RateOfChange _roc = new();
	private readonly CircularBuffer<decimal> _streakBuffer = new(2);
	private readonly CrsiLine _crsiLine = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="ConnorsRSI"/>.
	/// </summary>
	public ConnorsRSI()
	{
		AddInner(_rsi);
		AddInner(_updownRsi);
		AddInner(_rocRsi);
		AddInner(_crsiLine);

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
		get => _rsi.Length;
		set => _rsi.Length = value;
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
		get => _updownRsi.Length;
		set => _updownRsi.Length = value;
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
		get => _rocRsi.Length;
		set
		{
			_rocRsi.Length = value;
			_roc.Length = value;
		}
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var rsiValue = _rsi.Process(input);

		var streak = CalculateStreak(candle.ClosePrice);
		var updownRsiValue = _updownRsi.Process(input, streak);

		var rocValue = _roc.Process(input);
		var rocRsiValue = _rocRsi.Process(rocValue);

		var result = new ComplexIndicatorValue(this, input.Time);

		if (_rsi.IsFormed && _updownRsi.IsFormed && _rocRsi.IsFormed && _roc.IsFormed)
		{
			IsFormed = true;

			var rsi = rsiValue.ToDecimal();
			var updownRsi = updownRsiValue.ToDecimal();
			var rocRsi = rocRsiValue.ToDecimal();

			var crsi = (rsi + updownRsi + rocRsi) / 3;
			var crsiValue = _crsiLine.Process(input, crsi);

			result.Add(_rsi, rsiValue);
			result.Add(_updownRsi, updownRsiValue);
			result.Add(_rocRsi, rocRsiValue);
			result.Add(_crsiLine, crsiValue);
		}

		return result;
	}

	private decimal CalculateStreak(decimal currentPrice)
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

			_streakBuffer.PopFront();
		}

		_streakBuffer.PushBack(streak);
		_streakBuffer.PushBack(currentPrice);

		return streak;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		_streakBuffer.Clear();
	}
}
