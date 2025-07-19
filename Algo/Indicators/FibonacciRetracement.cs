namespace StockSharp.Algo.Indicators;

/// <summary>
/// Fibonacci Retracement.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FRKey,
	Description = LocalizedStrings.FibonacciRetracementKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/fibonacci_retracement.html")]
[IndicatorOut(typeof(FibonacciRetracementValue))]
public class FibonacciRetracement : BaseComplexIndicator<FibonacciRetracementValue>
{
	private readonly Highest _highest;
	private readonly Lowest _lowest;

	/// <summary>
	/// Initializes a new instance of the <see cref="FibonacciRetracement"/>.
	/// </summary>
	public FibonacciRetracement()
	{
		_highest = new();
		_lowest = new();

		Levels = [.. new[] { 0.236m, 0.382m, 0.5m, 0.618m, 0.786m }.Select(l => new FibonacciLevel(l))];

		foreach (var level in Levels)
			AddInner(level);

		Length = 20;
	}

	/// <summary>
	/// Fibonacci levels.
	/// </summary>
	[Browsable(false)]
	public FibonacciLevel[] Levels { get; }

	/// <summary>
	/// Period length.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => _highest.Length;
		set
		{
			_highest.Length = value;
			_lowest.Length = value;

			Reset();
		}
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var highValue = _highest.Process(input, candle.HighPrice);
		var lowValue = _lowest.Process(input, candle.LowPrice);

		if (input.IsFinal && _highest.IsFormed && _lowest.IsFormed)
			IsFormed = true;

		var result = new FibonacciRetracementValue(this, input.Time);

		var highestHigh = highValue.ToDecimal();
		var lowestLow = lowValue.ToDecimal();

		foreach (var level in Levels)
		{
			var levelValue = lowestLow + (highestHigh - lowestLow) * level.Level;
			result.Add(level, level.Process(input, levelValue));
		}

		return result;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_highest.Reset();
		_lowest.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;

	/// <inheritdoc />
	protected override FibonacciRetracementValue CreateValue(DateTimeOffset time)
		=> new(this, time);
}

/// <summary>
/// Represents a Fibonacci retracement level.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FibonacciLevel"/>.
/// </remarks>
/// <param name="level"><see cref="Level"/></param>
[IndicatorHidden]
public class FibonacciLevel(decimal level) : BaseIndicator
{
	/// <summary>
	/// The retracement level.
	/// </summary>
	public decimal Level { get; } = level;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		return input;
	}
}

/// <summary>
/// <see cref="FibonacciRetracement"/> indicator value.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="FibonacciRetracementValue"/>.
/// </remarks>
/// <param name="indicator"><see cref="FibonacciRetracement"/></param>
/// <param name="time"><see cref="IIndicatorValue.Time"/></param>
public class FibonacciRetracementValue(FibonacciRetracement indicator, DateTimeOffset time) : ComplexIndicatorValue<FibonacciRetracement>(indicator, time)
{
	/// <summary>
	/// Gets all level values.
	/// </summary>
	public IIndicatorValue[] LevelsValues => [.. TypedIndicator.Levels.Select(ind => this[ind])];

	/// <summary>
	/// Gets all level values.
	/// </summary>
	[Browsable(false)]
	public decimal?[] Levels => [.. LevelsValues.Select(v => v.ToNullableDecimal())];

	/// <inheritdoc />
	public override string ToString() => $"Levels=[{string.Join(", ", Levels)}]";
}
