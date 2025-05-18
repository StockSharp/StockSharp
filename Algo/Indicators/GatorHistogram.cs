namespace StockSharp.Algo.Indicators;

/// <summary>
/// The oscillator histogram <see cref="GatorOscillator"/>.
/// </summary>
public class GatorHistogram : BaseIndicator
{
	private readonly AlligatorLine _line1;
	private readonly AlligatorLine _line2;
	private readonly bool _isNegative;

	/// <inheritdoc />
	public override int NumValuesToInitialize => _line1.NumValuesToInitialize.Max(_line2.NumValuesToInitialize);

	internal GatorHistogram(AlligatorLine line1, AlligatorLine line2, bool isNegative)
	{
		_line1 = line1 ?? throw new ArgumentNullException(nameof(line1));
		_line2 = line2 ?? throw new ArgumentNullException(nameof(line2));
		_isNegative = isNegative;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		var line1Curr = _line1.GetNullableCurrentValue();
		var line2Curr = _line2.GetNullableCurrentValue();

		if (line1Curr == null || line2Curr == null)
			return new DecimalIndicatorValue(this, input.Time);

		return new DecimalIndicatorValue(this, (_isNegative ? -1 : 1) * Math.Abs(line1Curr.Value - line2Curr.Value), input.Time);
	}

	/// <summary>
	/// Create a copy of <see cref="GatorHistogram"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IIndicator Clone()
	{
		return new GatorHistogram(_line1.TypedClone(), _line2.TypedClone(), _isNegative) { Name = Name };
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		_line1.LoadIfNotNull(storage, "line1");
		_line2.LoadIfNotNull(storage, "line2");
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue("line1", _line1.Save());
		storage.SetValue("line2", _line2.Save());
	}
}
