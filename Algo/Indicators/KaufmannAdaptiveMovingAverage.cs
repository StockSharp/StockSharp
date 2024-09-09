﻿namespace StockSharp.Algo.Indicators;

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
public class KaufmannAdaptiveMovingAverage : LengthIndicator<decimal>
{
	private decimal _prevFinalValue;
	private bool _isInitialized;

	/// <summary>
	/// Initializes a new instance of the <see cref="KaufmannAdaptiveMovingAverage"/>.
	/// </summary>
	public KaufmannAdaptiveMovingAverage()
	{
		FastSCPeriod = 2;
		SlowSCPeriod = 30;
	}

	/// <summary>
	/// 'Rapid' EMA period. The default value is 2.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.FastMaKey,
		Description = LocalizedStrings.FastMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int FastSCPeriod { get; set; }

	/// <summary>
	/// 'Slow' EMA period. The default value is 30.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SlowMaKey,
		Description = LocalizedStrings.SlowMaDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int SlowSCPeriod { get; set; }

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Buffer.Count > Length;

	/// <inheritdoc />
	public override void Reset()
	{
		_prevFinalValue = 0;
		_isInitialized = false;

		base.Reset();

		Buffer.Capacity = Length + 1;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();
		var lastValue = this.GetCurrentValue();

		if (input.IsFinal)
			Buffer.PushBack(newValue);

		if (!IsFormed)
			return new DecimalIndicatorValue(this, lastValue, input.Time);

		if (!_isInitialized && Buffer.Count == Length + 1)
		{
			_isInitialized = true;
			// Начальное значение - последнее входное значение.
			return new DecimalIndicatorValue(this, _prevFinalValue = newValue, input.Time);
		}

		var buff = input.IsFinal ? Buffer : (IList<decimal>)Buffer.Skip(1).Append(newValue).ToArray();

		var direction = newValue - buff[0];

		decimal volatility = 0;

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

		return new DecimalIndicatorValue(this, curValue, input.Time);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		FastSCPeriod = storage.GetValue(nameof(FastSCPeriod), FastSCPeriod);
		FastSCPeriod = storage.GetValue(nameof(FastSCPeriod), FastSCPeriod);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(FastSCPeriod), FastSCPeriod);
		storage.SetValue(nameof(SlowSCPeriod), SlowSCPeriod);
	}
}