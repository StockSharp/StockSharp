namespace StockSharp.Algo.Indicators;

/// <summary>
/// SuperTrend indicator.
/// </summary>
/// <remarks>
/// Popular trend-following indicator based on ATR. Changes color/side when price crosses its line.
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.SuperTrendKey,
	Description = LocalizedStrings.SuperTrendDescKey)]
[Doc("topics/api/indicators/list_of_indicators/supertrend.html")]
public class SuperTrend : BaseIndicator
{
	private decimal? _prevSupertrend;
	private decimal? _prevClose;
	private int _trend = 1;
	private decimal? _prevUpperBand;
	private decimal? _prevLowerBand;
	private readonly AverageTrueRange _atr;

	/// <summary>
	/// Initializes a new instance of the <see cref="SuperTrend"/>.
	/// </summary>
	public SuperTrend()
		: this(new())
	{
		Length = 10;
		Multiplier = 3m;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SuperTrend"/>.
	/// </summary>
	/// <param name="atr"><see cref="AverageTrueRange"/></param>
	public SuperTrend(AverageTrueRange atr)
	{
		_atr = atr ?? throw new ArgumentNullException(nameof(atr));
	}

	/// <summary>
	/// ATR period.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.IndicatorPeriodKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public int Length
	{
		get => _atr.Length;
		set
		{
			_atr.Length = value;

			Reset();
		}
	}

	private decimal _multiplier;

	/// <summary>
	/// Multiplier for ATR.
	/// </summary>
	[Display(
		Name = LocalizedStrings.MultiplierKey,
		Description = LocalizedStrings.AverageTrueRangeKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public decimal Multiplier
	{
		get => _multiplier;
		set
		{
			if (value <= 0)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_multiplier = value;
			Reset();
		}
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => _atr.NumValuesToInitialize;

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var atrValue = _atr.Process(input);

		if (!atrValue.IsFormed)
			return new DecimalIndicatorValue(this, input.Time);

		var atr = atrValue.ToDecimal();
		var close = candle.ClosePrice;
		var hl2 = (candle.HighPrice + candle.LowPrice) / 2;

		var basicUpperBand = hl2 + Multiplier * atr;
		var basicLowerBand = hl2 - Multiplier * atr;

		var finalUpperBand = _prevUpperBand == null || basicUpperBand < _prevUpperBand || _prevClose > _prevUpperBand
			? basicUpperBand
			: _prevUpperBand.Value;

		var finalLowerBand = _prevLowerBand == null || basicLowerBand > _prevLowerBand || _prevClose < _prevLowerBand
			? basicLowerBand
			: _prevLowerBand.Value;

		decimal supertrend;
		int trend;

		if (_prevSupertrend == null)
		{
			supertrend = close >= hl2 ? finalLowerBand : finalUpperBand;
			trend = close >= hl2 ? 1 : -1;
		}
		else
		{
			if (_trend == 1)
			{
				supertrend = close <= finalLowerBand ? finalUpperBand : finalLowerBand;
				trend = close <= finalLowerBand ? -1 : 1;
			}
			else
			{
				supertrend = close >= finalUpperBand ? finalLowerBand : finalUpperBand;
				trend = close >= finalUpperBand ? 1 : -1;
			}
		}

		if (input.IsFinal)
		{
			IsFormed = true;

			_prevSupertrend = supertrend;
			_prevClose = close;
			_prevUpperBand = finalUpperBand;
			_prevLowerBand = finalLowerBand;
			_trend = trend;
		}

		return new DecimalIndicatorValue(this, supertrend, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_prevSupertrend = null;
		_prevClose = null;
		_trend = 1;
		_prevUpperBand = null;
		_prevLowerBand = null;

		_atr.Reset();

		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Length), Length);
		storage.SetValue(nameof(Multiplier), Multiplier);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Length = storage.GetValue<int>(nameof(Length));
		Multiplier = storage.GetValue<decimal>(nameof(Multiplier));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" L={Length} M={Multiplier}";
}