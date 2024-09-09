namespace StockSharp.Algo.Indicators;

/// <summary>
/// Bollinger band.
/// </summary>
public class BollingerBand : BaseIndicator
{
	private readonly LengthIndicator<decimal> _ma;
	private readonly StandardDeviation _dev;

	/// <summary>
	/// Initializes a new instance of the <see cref="BollingerBand"/>.
	/// </summary>
	/// <param name="ma">Moving Average.</param>
	/// <param name="dev">Standard deviation.</param>
	public BollingerBand(LengthIndicator<decimal> ma, StandardDeviation dev)
	{
		_ma = ma ?? throw new ArgumentNullException(nameof(ma));
		_dev = dev ?? throw new ArgumentNullException(nameof(dev));
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => Math.Max(_ma.NumValuesToInitialize, _dev.NumValuesToInitialize);

	/// <summary>
	/// Channel width.
	/// </summary>
	public decimal Width { get; set; }

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		IsFormed = _ma.IsFormed && _dev.IsFormed;

		return new DecimalIndicatorValue(this, _ma.GetCurrentValue() + (Width * _dev.GetCurrentValue()), input.Time);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Width = storage.GetValue<decimal>(nameof(Width));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Width), Width);
	}
}
