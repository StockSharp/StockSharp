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

		return new DecimalIndicatorValue(this, (_isNegative ? -1 : 1) * (line1Curr.Value - line2Curr.Value).Abs(), input.Time);
	}

	/// <summary>
	/// Create a copy of <see cref="GatorHistogram"/>.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Copy.</returns>
	public override async ValueTask<IIndicator> CloneAsync(CancellationToken cancellationToken)
	{
		var line1 = (AlligatorLine)await Line1.CloneAsync(cancellationToken);
		var line2 = (AlligatorLine)await Line2.CloneAsync(cancellationToken);

		return new GatorHistogram(line1, line2, _isNegative) { Name = Name };
	}

	/// <inheritdoc />
	public override async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.LoadAsync(storage, cancellationToken);

		await Line1.LoadIfNotNullAsync(storage, "line1", cancellationToken);
		await Line2.LoadIfNotNullAsync(storage, "line2", cancellationToken);
	}

	/// <inheritdoc />
	public override async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		await base.SaveAsync(storage, cancellationToken);

		storage.SetValue("line1", await Line1.SaveAsync(cancellationToken));
		storage.SetValue("line2", await Line2.SaveAsync(cancellationToken));
	}
}
