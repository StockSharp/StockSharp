﻿namespace StockSharp.Algo.Indicators;

/// <summary>
/// Bollinger Bands.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/bollinger_bands.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BollingerKey,
	Description = LocalizedStrings.BollingerBandsKey)]
[Doc("topics/api/indicators/list_of_indicators/bollinger_bands.html")]
public class BollingerBands : BaseComplexIndicator
{
	private readonly StandardDeviation _dev = new();

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerBands"/>.
	/// </summary>
	public BollingerBands()
		: this(new SimpleMovingAverage())
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerBands"/>.
	/// </summary>
	/// <param name="ma">Moving Average.</param>
	public BollingerBands(LengthIndicator<decimal> ma)
	{
		AddInner(MovingAverage = ma);
		AddInner(UpBand = new(MovingAverage, _dev) { Name = nameof(UpBand) });
		AddInner(LowBand = new(MovingAverage, _dev) { Name = nameof(LowBand) });
		_dev.Length = ma.Length;
		Width = 2;
	}

	/// <summary>
	/// Middle line.
	/// </summary>
	[Browsable(false)]
	public LengthIndicator<decimal> MovingAverage { get; }

	/// <summary>
	/// Upper band +.
	/// </summary>
	[Browsable(false)]
	public BollingerBand UpBand { get; }

	/// <summary>
	/// Lower band -.
	/// </summary>
	[Browsable(false)]
	public BollingerBand LowBand { get; }

	/// <summary>
	/// Period length. By default equal to 1.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => MovingAverage.Length;
		set
		{
			_dev.Length = MovingAverage.Length = value;
			Reset();
		}
	}

	/// <summary>
	/// Bollinger Bands channel width. Default value equal to 2.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ChannelWidthKey,
		Description = LocalizedStrings.ChannelWidthDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal Width
	{
		get => UpBand.Width;
		set
		{
			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			UpBand.Width = value;
			LowBand.Width = -value;

			Reset();
		}
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();
		_dev.Reset();
		//MovingAverage.Reset();
		//UpBand.Reset();
		//LowBand.Reset();
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => MovingAverage.IsFormed;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		_dev.Process(input);

		var maValue = MovingAverage.Process(input);

		var value = new ComplexIndicatorValue(this, input.Time);

		value.Add(MovingAverage, maValue);
		value.Add(UpBand, UpBand.Process(input));
		value.Add(LowBand, LowBand.Process(input));

		return value;
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;
}