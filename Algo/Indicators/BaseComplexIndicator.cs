namespace StockSharp.Algo.Indicators;

/// <summary>
/// Embedded indicators processing modes.
/// </summary>
public enum ComplexIndicatorModes
{
	/// <summary>
	/// In-series. The result of the previous indicator execution is passed to the next one,.
	/// </summary>
	Sequence,

	/// <summary>
	/// In parallel. Results of indicators execution for not depend on each other.
	/// </summary>
	Parallel,
}

class InnerIndicatorResetScope
{
}

/// <summary>
/// The base indicator, built in form of several indicators combination.
/// </summary>
public abstract class BaseComplexIndicator : BaseIndicator, IComplexIndicator
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BaseComplexIndicator"/>.
	/// </summary>
	/// <param name="innerIndicators">Embedded indicators.</param>
	protected BaseComplexIndicator(params IIndicator[] innerIndicators)
	{
		if (innerIndicators == null)
			throw new ArgumentNullException(nameof(innerIndicators));

		foreach (var inner in innerIndicators)
			AddInner(inner);

		Mode = ComplexIndicatorModes.Parallel;
	}

	/// <summary>
	/// Embedded indicators processing mode. The default equals to <see cref="ComplexIndicatorModes.Parallel"/>.
	/// </summary>
	[Browsable(false)]
	public ComplexIndicatorModes Mode { get; protected set; }

	private void InnerReseted()
	{
		if (Scope<InnerIndicatorResetScope>.IsDefined)
			return;

		Reset();
	}

	/// <summary>
	/// Add to <see cref="InnerIndicators"/>.
	/// </summary>
	/// <param name="inner">Indicator.</param>
	protected void AddInner(IIndicator inner)
	{
		_innerIndicators.Add(inner ?? throw new ArgumentNullException(nameof(inner)));
		inner.Reseted += InnerReseted;
	}

	/// <summary>
	/// Remove from <see cref="InnerIndicators"/>.
	/// </summary>
	/// <param name="inner">Indicator.</param>
	protected void RemoveInner(IIndicator inner)
	{
		_innerIndicators.Remove(inner ?? throw new ArgumentNullException(nameof(inner)));
		inner.Reseted -= InnerReseted;
	}

	/// <summary>
	/// Clear <see cref="InnerIndicators"/>.
	/// </summary>
	protected void ClearInner()
	{
		foreach (var sma in InnerIndicators.ToArray())
			RemoveInner(sma);
	}

	private readonly List<IIndicator> _innerIndicators = [];

	/// <inheritdoc />
	[Browsable(false)]
	public IReadOnlyList<IIndicator> InnerIndicators => _innerIndicators;

	/// <inheritdoc />
	[Browsable(false)]
	public override int NumValuesToInitialize =>
		Mode == ComplexIndicatorModes.Parallel
			? InnerIndicators.Select(i => i.NumValuesToInitialize).Max()
			: InnerIndicators.Select(i => i.NumValuesToInitialize).Sum();

	/// <inheritdoc />
	protected override bool CalcIsFormed() => InnerIndicators.All(i => i.IsFormed);

	/// <inheritdoc />
	public override Type ResultType { get; } = typeof(ComplexIndicatorValue);

	/// <summary>
	/// Create empty value.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	/// <param name="time">Time</param>
	/// <returns>Empty value.</returns>
	protected virtual IIndicatorValue CreateEmpty(IIndicator indicator, DateTimeOffset time)
		=> new DecimalIndicatorValue(indicator, time);

	/// <inheritdoc />
	public override IIndicatorValue Process(IIndicatorValue input)
	{
		var output = base.Process(input);

		var cv = (ComplexIndicatorValue)output;

		foreach (var inner in InnerIndicators)
			cv.InnerValues.TryAdd(inner, CreateEmpty(inner, input.Time));

		return output;
	}

	/// <inheritdoc />
	protected override IIndicatorValue OnProcess(IIndicatorValue input)
	{
		var value = new ComplexIndicatorValue(this, input.Time);

		foreach (var indicator in InnerIndicators)
		{
			var result = indicator.Process(input);

			value.Add(indicator, result);

			if (Mode != ComplexIndicatorModes.Sequence)
				continue;

			if (!indicator.IsFormed)
				break;

			input = result;
		}

		return value;
	}

	/// <inheritdoc />
	public override void Reset()
	{
		base.Reset();

		using (new InnerIndicatorResetScope().ToScope())
			InnerIndicators.ForEach(i => i.Reset());
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		var index = 0;

		foreach (var indicator in InnerIndicators)
		{
			var innerSettings = new SettingsStorage();
			indicator.Save(innerSettings);
			storage.SetValue(indicator.Name + index, innerSettings);
			index++;
		}
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		var index = 0;

		foreach (var indicator in InnerIndicators)
		{
			indicator.Load(storage, indicator.Name + index);
			index++;
		}
	}
}
