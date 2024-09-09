﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Wave Trend Oscillator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.WTOKey,
	Description = LocalizedStrings.WaveTrendOscillatorKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/wave_trend_oscillator.html")]
public class WaveTrendOscillator : BaseComplexIndicator
{
	private readonly ChannelAveragePriceOscillator _capo = new();
	private readonly ExponentialMovingAverage _esa = new() { Length = 10 };
	private readonly ExponentialMovingAverage _d = new() { Length = 14 };
	private readonly SimpleMovingAverage _sma = new() { Length = 3 };
	private readonly WaveTrendLine _wt1 = new() { Name = "WT1" };
	private readonly WaveTrendLine _wt2 = new() { Name = "WT1" };

	/// <summary>
	/// Initializes a new instance of the <see cref="WaveTrendOscillator"/>.
	/// </summary>
	public WaveTrendOscillator()
	{
		AddInner(_wt1);
		AddInner(_wt2);
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <summary>
	/// ESA period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.EMAKey,
		Description = LocalizedStrings.ExponentialMovingAverageKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int EsaPeriod
	{
		get => _esa.Length;
		set
		{
			_esa.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// D period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.PeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int DPeriod
	{
		get => _d.Length;
		set
		{
			_d.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// Average period for WT2.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SMAKey,
		Description = LocalizedStrings.SimpleMovingAverageKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int AveragePeriod
	{
		get => _sma.Length;
		set
		{
			_sma.Length = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => _capo.NumValuesToInitialize.Max(_esa.NumValuesToInitialize) + _d.NumValuesToInitialize + base.NumValuesToInitialize;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var capoValue = _capo.Process(input);
		var esaValue = _esa.Process(capoValue);

		var result = new ComplexIndicatorValue(this, input.Time);

		if (_esa.IsFormed)
		{
			var capo = capoValue.ToDecimal();
			var esa = esaValue.ToDecimal();

			var d = _d.Process(input, Math.Abs(capo - esa));

			if (_d.IsFormed)
			{
				var dValue = d.ToDecimal();
				var ci = (capo - esa) / (0.015m * dValue);
				var wt1 = ci;
				var wt2 = _sma.Process(input, ci).ToDecimal();

				result.Add(_wt1, _wt1.Process(wt1, input.Time));
				result.Add(_wt2, _wt2.Process(wt2, input.Time));

				if (input.IsFinal)
					IsFormed = true;
			}
		}

		return result;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_capo.Reset();
		_esa.Reset();
		_d.Reset();
		_sma.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(EsaPeriod), EsaPeriod);
		storage.SetValue(nameof(DPeriod), DPeriod);
		storage.SetValue(nameof(AveragePeriod), AveragePeriod);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		EsaPeriod = storage.GetValue<int>(nameof(EsaPeriod));
		DPeriod = storage.GetValue<int>(nameof(DPeriod));
		AveragePeriod = storage.GetValue<int>(nameof(AveragePeriod));
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()} ESA={EsaPeriod} D={DPeriod} Avg={AveragePeriod}";
}

/// <summary>
/// Channel Average Price Oscillator (CAPO) indicator.
/// </summary>
[IndicatorHidden]
public class ChannelAveragePriceOscillator : BaseIndicator
{
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		var candle = input.ToCandle();
		var capo = (candle.HighPrice + candle.LowPrice + candle.ClosePrice) / 3;
		return new DecimalIndicatorValue(this, capo, input.Time);
	}
}

/// <summary>
/// Wave Trend line.
/// </summary>
[IndicatorHidden]
public class WaveTrendLine : BaseIndicator
{
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		return input;
	}
}