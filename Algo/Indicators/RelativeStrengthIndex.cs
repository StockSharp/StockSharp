﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Relative Strength Index.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/rsi.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.RSIKey,
	Description = LocalizedStrings.RelativeStrengthIndexKey)]
[Doc("topics/api/indicators/list_of_indicators/rsi.html")]
public class RelativeStrengthIndex : LengthIndicator<decimal>
{
	private readonly SmoothedMovingAverage _gain;
	private readonly SmoothedMovingAverage _loss;
	private bool _isInitialized;
	private decimal _last;

	/// <summary>
	/// Initializes a new instance of the <see cref="RelativeStrengthIndex"/>.
	/// </summary>
	public RelativeStrengthIndex()
	{
		_gain = new SmoothedMovingAverage();
		_loss = new SmoothedMovingAverage();

		Length = 15;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override bool CalcIsFormed() => _gain.IsFormed;

	/// <inheritdoc />
	public override void Reset()
	{
		_loss.Length = _gain.Length = Length;
		base.Reset();
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var newValue = input.ToDecimal();

		if (!_isInitialized)
		{
			if (input.IsFinal)
			{
				_last = newValue;
				_isInitialized = true;
			}

			return new DecimalIndicatorValue(this, input.Time);
		}

		var delta = newValue - _last;

		var gainValue = _gain.Process(input, delta > 0 ? delta : 0m).ToDecimal();
		var lossValue = _loss.Process(input, delta > 0 ? 0m : -delta).ToDecimal();

		if(input.IsFinal)
			_last = newValue;

		if (lossValue == 0)
			return new DecimalIndicatorValue(this, 100m, input.Time);
		
		if (gainValue / lossValue == 1)
			return new DecimalIndicatorValue(this, 0m, input.Time);

		return new DecimalIndicatorValue(this, 100m - 100m / (1m + gainValue / lossValue), input.Time);
	}
}