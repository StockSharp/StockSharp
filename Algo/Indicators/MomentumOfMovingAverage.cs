﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Momentum of Moving Average indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.MOMAKey,
	Description = LocalizedStrings.MomentumOfMovingAverageKey)]
[Doc("topics/indicators/momentum_of_moving_average.html")]
public class MomentumOfMovingAverage : SimpleMovingAverage
{
	private readonly CircularBuffer<decimal> _maBuffer = new(2);

	/// <summary>
	/// Initializes a new instance of the <see cref="MomentumOfMovingAverage"/>.
	/// </summary>
	public MomentumOfMovingAverage()
	{
		Length = 14;
		MomentumPeriod = 10;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	private int _momentumPeriod;

	/// <summary>
	/// Momentum period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MomentumKey,
		Description = LocalizedStrings.MomentumKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int MomentumPeriod
	{
		get => _momentumPeriod;
		set
		{
			_momentumPeriod = value;
			Reset();
		}
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var maValue = base.OnProcess(input);

		if (IsFormed)
		{
			var ma = maValue.ToDecimal();
			_maBuffer.PushBack(ma);

			if (_maBuffer.IsFull && _maBuffer[0] != 0)
			{
				var momentum = (ma - _maBuffer[0]) / _maBuffer[0] * 100;
				return new DecimalIndicatorValue(this, momentum, input.Time);
			}
		}

		return new DecimalIndicatorValue(this, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_maBuffer.Clear();

		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(MomentumPeriod), MomentumPeriod);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		MomentumPeriod = storage.GetValue<int>(nameof(MomentumPeriod));
	}

	/// <inheritdoc />
	public override string ToString() => $"{base.ToString()} Mom={MomentumPeriod}";
}