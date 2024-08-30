namespace StockSharp.Algo.Indicators;

/// <summary>
/// TroughBar.
/// </summary>
/// <remarks>
/// https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/troughbar.html
/// </remarks>
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TroughBarKey,
	Description = LocalizedStrings.TroughBarDescKey)]
[IndicatorIn(typeof(CandleIndicatorValue))]
[Doc("topics/api/indicators/list_of_indicators/troughbar.html")]
public class TroughBar : BaseIndicator
{
	private decimal _currentMinimum = decimal.MaxValue;
	private int _currentBarCount;
	private int _valueBarCount;

	/// <summary>
	/// Initializes a new instance of the <see cref="TroughBar"/>.
	/// </summary>
	public TroughBar()
	{
	}

	private Unit _reversalAmount = new();

	/// <summary>
	/// Indicator changes threshold.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ThresholdKey,
		Description = LocalizedStrings.ThresholdDescKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public Unit ReversalAmount
	{
		get => _reversalAmount;
		set
		{
			_reversalAmount = value ?? throw new ArgumentNullException(nameof(value));

			Reset();
		}
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var candle = input.ToCandle();

		var cm = _currentMinimum;
		var vbc = _valueBarCount;

		try
		{
			if (candle.LowPrice < cm)
			{
				cm = candle.LowPrice;
				vbc = _currentBarCount;
			}
			else if (candle.HighPrice >= (cm + ReversalAmount.Value))
			{
				if (input.IsFinal)
					IsFormed = true;

				return new DecimalIndicatorValue(this, vbc, input.Time);
			}

			return new DecimalIndicatorValue(this, this.GetCurrentValue(), input.Time);
		}
		finally
		{
			if(input.IsFinal)
			{
				_currentBarCount++;
				_currentMinimum = cm;
				_valueBarCount = vbc;
			}
		}
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		ReversalAmount.Load(storage, nameof(ReversalAmount));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(ReversalAmount), ReversalAmount.Save());
	}
}