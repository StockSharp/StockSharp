namespace StockSharp.Algo.Indicators;

/// <summary>
/// The oscillator histogram <see cref="GatorOscillator"/>.
/// </summary>
public class GatorHistogram : BaseIndicator
{
	private readonly bool _isNegative;

	/// <summary>
	/// First line (Jaw vs Lips).
	/// </summary>
	[Browsable(false)]
	public AlligatorLine Line1 { get; }

	/// <summary>
	/// Second line (Lips vs Teeth).
	/// </summary>
	[Browsable(false)]
	public AlligatorLine Line2 { get; }

	/// <inheritdoc />
	public override int NumValuesToInitialize => Line1.NumValuesToInitialize.Max(Line2.NumValuesToInitialize);

	internal GatorHistogram(AlligatorLine line1, AlligatorLine line2, bool isNegative)
	{
		Line1 = line1 ?? throw new ArgumentNullException(nameof(line1));
		Line2 = line2 ?? throw new ArgumentNullException(nameof(line2));
		_isNegative = isNegative;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		if (input.IsFinal)
			IsFormed = true;

		var line1Curr = Line1.GetNullableCurrentValue();
		var line2Curr = Line2.GetNullableCurrentValue();

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
		return new GatorHistogram(Line1.TypedClone(), Line2.TypedClone(), _isNegative) { Name = Name };
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Line1.LoadIfNotNull(storage, "line1");
		Line2.LoadIfNotNull(storage, "line2");
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue("line1", Line1.Save());
		storage.SetValue("line2", Line2.Save());
	}
}
