namespace StockSharp.Algo.Indicators;

/// <summary>
/// Adaptive Laguerre Filter indicator.
/// </summary>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.ALFKey,
	Description = LocalizedStrings.AdaptiveLaguerreFilterKey)]
[Doc("topics/api/indicators/list_of_indicators/adaptive_laguerre_filter.html")]
public class AdaptiveLaguerreFilter : BaseIndicator
{
	private decimal _l0;
	private decimal _l1;
	private decimal _l2;
	private decimal _l3;

	/// <summary>
	/// Initializes a new instance of the <see cref="AdaptiveLaguerreFilter"/>.
	/// </summary>
	public AdaptiveLaguerreFilter()
	{
		Gamma = 0.8m;
	}

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

		var l0 = _l0;
		var l1 = _l1;
		var l2 = _l2;
		var l3 = _l3;

		l0 = gamma1 * price + gamma * l0;
		l1 = -gamma * l0 + l0 + gamma * l1;
		l2 = -gamma * l1 + l1 + gamma * l2;
		l3 = -gamma * l2 + l2 + gamma * l3;

		var filteredValue = (l0 + 2 * l1 + 2 * l2 + l3) / 6;

		if (input.IsFinal)
		{
			_l0 = l0;
			_l1 = l1;
			_l2 = l2;
			_l3 = l3;

			if (!IsFormed && filteredValue >= price)
				IsFormed = true;
		}

		return new DecimalIndicatorValue(this, filteredValue, input.Time);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		_l0 = _l1 = _l2 = _l3 = 0;
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
}
