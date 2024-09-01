namespace StockSharp.Algo.Indicators;

/// <summary>
/// The base class for indicators with one resulting value and based on the period.
/// </summary>
/// <typeparam name="TResult">Result values type.</typeparam>
public abstract class LengthIndicator<TResult> : BaseIndicator
{
	/// <summary>
	/// Initialize <see cref="LengthIndicator{T}"/>.
	/// </summary>
	protected LengthIndicator()
	{
		Buffer = new(Length);
	}

	/// <inheritdoc />
	public override void Reset()
	{
		Buffer.Capacity = Length;
		base.Reset();
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => Length;

	private int _length = 1;

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PeriodKey,
		Description = LocalizedStrings.PeriodLengthKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public virtual int Length
	{
		get => _length;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_length = value;

			Reset();
		}
	}

	/// <inheritdoc />
	protected override bool CalcIsFormed() => Buffer.Count >= Length;

	/// <summary>
	/// The buffer for data storage.
	/// </summary>
	[Browsable(false)]
	protected CircularBufferEx<TResult> Buffer { get; }

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Length = storage.GetValue<int>(nameof(Length));
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage.SetValue(nameof(Length), Length);
	}

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;
}
