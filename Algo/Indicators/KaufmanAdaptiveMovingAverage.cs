namespace StockSharp.Algo.Indicators;

/// <summary>
/// Kaufman adaptive moving average.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/kama.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.KAMAKey,
	Description = LocalizedStrings.KaufmannAdaptiveMovingAverageKey)]
[Doc("topics/api/indicators/list_of_indicators/kama.html")]
public class KaufmanAdaptiveMovingAverage : LengthIndicator<decimal>
{
	private decimal _prevFinalValue;
	private bool _isInitialized;

	/// <summary>
	/// Initializes a new instance of the <see cref="KaufmanAdaptiveMovingAverage"/>.
	/// </summary>
	public KaufmanAdaptiveMovingAverage()
	{
		FastSCPeriod = 2;
		SlowSCPeriod = 30;
	}

	private int _fastSCPeriod;

	/// <summary>
	/// 'Rapid' EMA period. The default value is 2.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FastMaKey,
		Description = LocalizedStrings.FastMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int FastSCPeriod
	{
		get => _fastSCPeriod;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_fastSCPeriod = value;

			Reset();
		}
	}

	private int _slowSCPeriod;

	/// <summary>
	/// 'Slow' EMA period. The default value is 30.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SlowMaKey,
		Description = LocalizedStrings.SlowMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int SlowSCPeriod
	{
		get => _slowSCPeriod;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_slowSCPeriod = value;

			Reset();
		}
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Buffer.Count > Length;

	/// <inheritdoc />
	public override int NumValuesToInitialize => base.NumValuesToInitialize + 1;

	/// <inheritdoc />
	protected override int GetCapacity() => Length + 1;

	/// <inheritdoc />
	public override void Reset()
	{
		_prevFinalValue = 0;
		_isInitialized = false;

		base.Reset();
	}

	/// <inheritdoc />
	protected override decimal? OnProcessDecimal(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();
		var lastValue = this.GetCurrentValue();

		if (input.IsFinal)
			Buffer.PushBack(newValue);

		if (!IsFormed)
			return lastValue;

		if (!_isInitialized && Buffer.Count == Length + 1)
		{
			_isInitialized = true;
			return _prevFinalValue = newValue;
		}

		var buff = input.IsFinal ? Buffer : (IList<decimal>)[.. Buffer.Skip(1), newValue];

		var direction = newValue - buff[0];

		var volatility = 0m;

		for (var i = 1; i < buff.Count; i++)
		{
			volatility += Math.Abs(buff[i] - buff[i - 1]);
		}

		volatility = volatility > 0 ? volatility : 0.00001m;

		var er = Math.Abs(direction / volatility);

		var fastSC = 2m / (FastSCPeriod + 1m);
		var slowSC = 2m / (SlowSCPeriod + 1m);

		var ssc = er * (fastSC - slowSC) + slowSC;
		var smooth = (ssc * ssc);

		var curValue = (newValue - _prevFinalValue) * smooth + _prevFinalValue;
		if (input.IsFinal)
			_prevFinalValue = curValue;

		return curValue;
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		FastSCPeriod = storage.GetValue(nameof(FastSCPeriod), FastSCPeriod);
		SlowSCPeriod = storage.GetValue(nameof(SlowSCPeriod), SlowSCPeriod);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(FastSCPeriod), FastSCPeriod);
		storage.SetValue(nameof(SlowSCPeriod), SlowSCPeriod);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" F={FastSCPeriod}, S={SlowSCPeriod}";
}