namespace StockSharp.Algo.Indicators;

/// <summary>
/// The base class for indicators with one resulting value and based on the period.
/// </summary>
/// <typeparam name="TResult">Result values type.</typeparam>
/// <typeparam name="TBuffer">Buffer type for data storage.</typeparam>
/// <remarks>
/// Initialize <see cref="LengthIndicator{TResult,TBuffer}"/>.
/// </remarks>
/// <param name="buffer"><see cref="Buffer"/></param>
public abstract class LengthIndicator<TResult, TBuffer>(TBuffer buffer) : BaseIndicator
	where TBuffer : ICircularBufferEx<TResult>
{
	/// <summary>
	/// Gets the capacity of the buffer for data storage.
	/// </summary>
	/// <returns>The capacity of the buffer. By default, it is equal to <see cref="Length"/>.</returns>
	protected virtual int GetCapacity() => Length;

	/// <inheritdoc />
	public override void Reset()
	{
		Buffer.Capacity = GetCapacity();
		base.Reset();
	}

	/// <inheritdoc />
	public override int NumValuesToInitialize => Length;

	private int _length = 1;

	/// <summary>
	/// Period length. By default equal to 1.
	/// </summary>
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
	protected TBuffer Buffer { get; } = buffer ?? throw new ArgumentNullException(nameof(buffer));

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

		storage
			.Set(nameof(Length), Length)
		;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var result = OnProcessDecimal(input);

		return result is null ? new DecimalIndicatorValue(this, input.Time) : new DecimalIndicatorValue(this, result.Value, input.Time);
	}

	/// <summary>
	/// To handle the input value.
	/// </summary>
	/// <param name="input">The input value.</param>
	/// <returns>The new value of the indicator.</returns>
	protected virtual decimal? OnProcessDecimal(IIndicatorValue input)
		=> throw new NotSupportedException();

	/// <inheritdoc />
	public override string ToString() => base.ToString() + " " + Length;
}

/// <summary>
/// The base class for indicators with one resulting value and based on the period.
/// </summary>
/// <remarks>
/// Initialize <see cref="DecimalLengthIndicator"/>.
/// </remarks>
public abstract class DecimalLengthIndicator() : LengthIndicator<decimal, DecimalBuffer>(new(1))
{
}