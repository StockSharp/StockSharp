﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Kase Peak Oscillator indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.KPOKey,
	Description = LocalizedStrings.KasePeakOscillatorKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/indicators/kase_peak_oscillator.html")]
public class KasePeakOscillator : BaseComplexIndicator
{
	private readonly AverageTrueRange _atr = new() { Length = 10 };
	private readonly CircularBufferEx<decimal> _peakBuffer = new(2) { MaxComparer = Comparer<decimal>.Default };
	private readonly CircularBufferEx<decimal> _valleyBuffer = new(2) { MinComparer = Comparer<decimal>.Default };
	private readonly KasePeakOscillatorPart _shortTerm = new();
	private readonly KasePeakOscillatorPart _longTerm = new();
	private decimal _prevClose;

	/// <summary>
	/// Initializes a new instance of the <see cref="KasePeakOscillator"/>.
	/// </summary>
	public KasePeakOscillator()
	{
		AddInner(_shortTerm);
		AddInner(_longTerm);

		ShortPeriod = 9;
		LongPeriod = 18;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <summary>
	/// Short period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortKey,
		Description = LocalizedStrings.ShortPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int ShortPeriod
	{
		get => _shortTerm.Length;
		set => _shortTerm.Length = value;
	}

	/// <summary>
	/// Long period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LongKey,
		Description = LocalizedStrings.LongPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int LongPeriod
	{
		get => _longTerm.Length;
		set => _longTerm.Length = value;
	}

	/// <summary>
	/// Short-term oscillator.
	/// </summary>
	[Browsable(false)]
	public KasePeakOscillatorPart ShortTerm => _shortTerm;

	/// <summary>
	/// Long-term oscillator.
	/// </summary>
	[Browsable(false)]
	public KasePeakOscillatorPart LongTerm => _longTerm;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();
		var atrValue = _atr.Process(input);

		var result = new ComplexIndicatorValue(this, input.Time);

		if (_atr.IsFormed)
		{
			var atr = atrValue.ToDecimal();
			var peak = candle.HighPrice;
			var valley = candle.LowPrice;

			if (_prevClose != 0)
			{
				if (candle.ClosePrice > _prevClose)
				{
					peak = Math.Max(candle.HighPrice, _prevClose + atr);
					valley = Math.Max(candle.LowPrice, _prevClose - 0.5m * atr);
				}
				else if (candle.ClosePrice < _prevClose)
				{
					peak = Math.Min(candle.HighPrice, _prevClose + 0.5m * atr);
					valley = Math.Min(candle.LowPrice, _prevClose - atr);
				}
			}

			decimal shortTermOscillator, longTermOscillator;

			if (input.IsFinal)
			{
				_peakBuffer.PushBack(peak);
				_valleyBuffer.PushBack(valley);

				_prevClose = candle.ClosePrice;

				var minValue = _valleyBuffer.Min.Value;
				var maxValue = _peakBuffer.Max.Value;

				var den1 = maxValue - minValue;
				var den2 = _peakBuffer[0] - _valleyBuffer[0];

				shortTermOscillator = den1 == 0 ? 0 : 100 * (candle.ClosePrice - minValue) / den1;
				longTermOscillator = den2 == 0 ? 0 : 100 * (candle.ClosePrice - _valleyBuffer[0]) / den2;
			}
			else
			{
				var minValue = _valleyBuffer.Min.Value.Min(valley);
				var maxValue = _peakBuffer.Max.Value.Max(peak);

				var den1 = maxValue - minValue;
				var den2 = _peakBuffer[0] - _valleyBuffer[0];

				shortTermOscillator = den1 == 0 ? 0 : 100 * (candle.ClosePrice - minValue) / den1;
				longTermOscillator = den2 == 0 ? 0 : 100 * (candle.ClosePrice - _valleyBuffer[0]) / den2;
			}

			result.Add(ShortTerm, _shortTerm.Process(input, shortTermOscillator));
			result.Add(LongTerm, _longTerm.Process(input, longTermOscillator));
		}

		return result;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_atr.Reset();
		_peakBuffer.Clear();
		_valleyBuffer.Clear();
		_prevClose = 0;

		base.Reset();
	}
}

/// <summary>
/// Represents a part (short-term or long-term) of the Kase Peak Oscillator.
/// </summary>
[IndicatorHidden]
public class KasePeakOscillatorPart : LengthIndicator<decimal>
{
	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var value = input.GetValue<decimal>();

		if (input.IsFinal)
			Buffer.PushBack(value);

		return new DecimalIndicatorValue(this, value, input.Time);
	}
}