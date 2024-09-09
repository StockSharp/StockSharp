﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Last oscillator.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/uo.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.UltimateOscKey,
	Description = LocalizedStrings.UltimateOscillatorKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/uo.html")]
public class UltimateOscillator : BaseIndicator
{
	private const decimal _stoProcentov = 100m;

	private const int _period7 = 7;
	private const int _period14 = 14;
	private const int _period28 = 28;

	private const int _weight1 = 1;
	private const int _weight2 = 2;
	private const int _weight4 = 4;

	private readonly Sum _period7BpSum;
	private readonly Sum _period14BpSum;
	private readonly Sum _period28BpSum;

	private readonly Sum _period7TrSum;
	private readonly Sum _period14TrSum;
	private readonly Sum _period28TrSum;

	private decimal? _previouseClosePrice;

	/// <summary>
	/// To create the indicator <see cref="UltimateOscillator"/>.
	/// </summary>
	public UltimateOscillator()
	{
		_period7BpSum = new Sum { Length = _period7 };
		_period14BpSum = new Sum { Length = _period14 };
		_period28BpSum = new Sum { Length = _period28 };

		_period7TrSum = new Sum { Length = _period7 };
		_period14TrSum = new Sum { Length = _period14 };
		_period28TrSum = new Sum { Length = _period28 };
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => new[] { _period7BpSum, _period14BpSum, _period28BpSum, _period7TrSum, _period14TrSum, _period28TrSum }.Select(i => i.NumValuesToInitialize).Sum();

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	/// <inheritdoc />
	protected override bool CalcIsFormed()
		=>	_period7BpSum.IsFormed && _period14BpSum.IsFormed &&
	        _period28BpSum.IsFormed && _period7TrSum.IsFormed &&
	        _period14TrSum.IsFormed && _period28TrSum.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		if (_previouseClosePrice != null)
		{
			var min = _previouseClosePrice.Value < candle.LowPrice ? _previouseClosePrice.Value : candle.LowPrice;
			var max = _previouseClosePrice.Value > candle.HighPrice ? _previouseClosePrice.Value : candle.HighPrice;

			input = input.SetValue(this, candle.ClosePrice - min);

			var p7BpValue = _period7BpSum.Process(input).ToDecimal();
			var p14BpValue = _period14BpSum.Process(input).ToDecimal();
			var p28BpValue = _period28BpSum.Process(input).ToDecimal();

			input = input.SetValue(this, max - min);

			var p7TrValue = _period7TrSum.Process(input).ToDecimal();
			var p14TrValue = _period14TrSum.Process(input).ToDecimal();
			var p28TrValue = _period28TrSum.Process(input).ToDecimal();

			if (input.IsFinal)
				_previouseClosePrice = candle.ClosePrice;

			if (p7TrValue != 0 && p14TrValue != 0 && p28TrValue != 0)
			{
				var average7 = p7BpValue / p7TrValue;
				var average14 = p14BpValue / p14TrValue;
				var average28 = p28BpValue / p28TrValue;
				return new DecimalIndicatorValue(this, _stoProcentov * (_weight4 * average7 + _weight2 * average14 + _weight1 * average28) / (_weight4 + _weight2 + _weight1), input.Time);
			}

			return new DecimalIndicatorValue(this, input.Time);
		}

		if (input.IsFinal)
			_previouseClosePrice = candle.ClosePrice;

		return new DecimalIndicatorValue(this, input.Time);
	}
}
