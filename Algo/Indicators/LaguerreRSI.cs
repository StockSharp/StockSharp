namespace StockSharp.Algo.Indicators;

/// <summary>
/// Laguerre RSI indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.LRSIKey,
	Description = LocalizedStrings.LaguerreRSIKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/laguerre_rsi.html")]
public class LaguerreRSI : BaseIndicator
{
	private decimal _l0, _l1, _l2, _l3;
	private decimal _prevCU, _prevCD;

	/// <summary>
	/// Initializes a new instance of the <see cref="LaguerreRSI"/>.
	/// </summary>
	public LaguerreRSI()
	{
		Gamma = 0.7m;
	}

	/// <inheritdoc />
	public override IndicatorMeasures Measure => IndicatorMeasures.Percent;

	private decimal _gamma;

	/// <summary>
	/// Gamma parameter.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.GammaKey,
		Description = LocalizedStrings.GammaDescriptionKey,
		GroupName = LocalizedStrings.GeneralKey)]
	[Range(0.000001, 0.999999)]
	public decimal Gamma
	{
		get => _gamma;
		set
		{
			if (value <= 0 || value >= 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			if (value == Gamma)
				return;

			_gamma = value;
			Reset();
		}
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var price = input.ToDecimal();

		var gamma = Gamma;
		var gamma1 = 1 - gamma;

		var l0 = (1 - gamma) * price + gamma * _l0;
		var l1 = -gamma * l0 + _l0 + gamma * _l1;
		var l2 = -gamma * l1 + _l1 + gamma * _l2;
		var l3 = -gamma * l2 + _l2 + gamma * _l3;

		var cu = 0m;
		var cd = 0m;

		if (l0 >= l1)
			cu = l0 - l1;
		else
			cd = l1 - l0;

		if (l1 >= l2)
			cu += l1 - l2;
		else
			cd += l2 - l1;

		if (l2 >= l3)
			cu += l2 - l3;
		else
			cd += l3 - l2;

		var smoothCU = gamma1 * cu + gamma * _prevCU;
		var smoothCD = gamma1 * cd + gamma * _prevCD;

		var lrsi = smoothCU + smoothCD != 0 ? smoothCU / (smoothCU + smoothCD) * 100 : 50;

		if (input.IsFinal)
		{
			_l0 = l0;
			_l1 = l1;
			_l2 = l2;
			_l3 = l3;
			_prevCU = smoothCU;
			_prevCD = smoothCD;
			IsFormed = true;
		}

		return new DecimalIndicatorValue(this, lrsi, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_l0 = _l1 = _l2 = _l3 = 0;
		_prevCU = _prevCD = 0;
		base.Reset();
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Gamma), Gamma);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Gamma = storage.GetValue<decimal>(nameof(Gamma));
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + $" G={Gamma}";
}