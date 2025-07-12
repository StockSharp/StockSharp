namespace StockSharp.Algo.Indicators;

using System.Drawing;

using Ecng.Drawing;

/// <summary>
/// The base Indicator.
/// </summary>
[IndicatorIn(typeof(DecimalIndicatorValue))]
[IndicatorOut(typeof(DecimalIndicatorValue))]
public abstract class BaseIndicator : Cloneable<IIndicator>, IIndicator
{
	private class InnerIndicatorResetScope
	{
	}

	private readonly List<IIndicator> _resetTrackings = [];

	/// <summary>
	/// Initialize <see cref="BaseIndicator"/>.
	/// </summary>
	protected BaseIndicator()
	{
		var type = GetType();

		_name = type.GetDisplayName();
	}

	/// <inheritdoc />
	[Browsable(false)]
	public Guid Id { get; private set; } = Guid.NewGuid();

	private string _name;

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.NameKey,
		Description = LocalizedStrings.IndicatorNameKey,
		GroupName = LocalizedStrings.GeneralKey)]
	public string Name
	{
		get => _name;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_name = value;
		}
	}

	/// <inheritdoc />
	[Browsable(false)]
	public virtual int NumValuesToInitialize => 1;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual DrawStyles Style => DrawStyles.Line;

	/// <inheritdoc />
	[Browsable(false)]
	public virtual Color? Color => default;

	/// <inheritdoc />
	public virtual void Reset()
	{
		_isFormed = false;
		Container.ClearValues();
		Reseted?.Invoke();

		if (_resetTrackings.Count > 0)
		{
			using (new InnerIndicatorResetScope().ToScope())
			{
				foreach (var inner in _resetTrackings)
					inner.Reset();
			}
		}
	}

	private void InnerReseted()
	{
		if (Scope<InnerIndicatorResetScope>.IsDefined)
			return;

		Reset();
	}

	/// <summary>
	/// To add inner indicator for tracking the <see cref="IIndicator.Reseted"/>.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	protected void AddResetTracking(IIndicator indicator)
	{
		ArgumentNullException.ThrowIfNull(indicator);

		indicator.Reseted += InnerReseted;
		_resetTrackings.Add(indicator);
	}

	/// <summary>
	/// To remove indicator from tracking the <see cref="IIndicator.Reseted"/>.
	/// </summary>
	/// <param name="indicator"><see cref="IIndicator"/></param>
	protected void RemoveResetTracking(IIndicator indicator)
	{
		ArgumentNullException.ThrowIfNull(indicator);

		indicator.Reseted -= InnerReseted;
		_resetTrackings.Remove(indicator);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	protected void SaveValues(SettingsStorage storage)
	{
		storage.SetValue(nameof(Id), Id);
		storage.SetValue(nameof(Name), Name);
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	protected void LoadValues(SettingsStorage storage)
	{
		Id = storage.GetValue<Guid>(nameof(Id));
		Name = storage.GetValue<string>(nameof(Name));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		SaveValues(storage);
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		LoadValues(storage);
	}

	/// <inheritdoc />
	[Browsable(false)]
	public virtual IndicatorMeasures Measure { get; } = IndicatorMeasures.Price;

	private bool _isFormed;

	/// <inheritdoc />
	[Browsable(false)]
	public bool IsFormed
	{
		get
		{
			if (_isFormed)
				return true;

			return _isFormed = CalcIsFormed();
		}
		protected set => _isFormed = value;
	}

	/// <summary>
	/// Calc <see cref="IsFormed"/>.
	/// </summary>
	/// <returns><see cref="IsFormed"/></returns>
	protected virtual bool CalcIsFormed() => false;

	/// <inheritdoc />
	[Browsable(false)]
	public IIndicatorContainer Container { get; } = new IndicatorContainer();

	/// <inheritdoc />
	public event Action<IIndicatorValue, IIndicatorValue> Changed;

	/// <inheritdoc />
	public event Action Reseted;

	/// <inheritdoc />
	public virtual IIndicatorValue Process(IIndicatorValue input)
	{
		ArgumentNullException.ThrowIfNull(input);

		if (input.IsEmpty)
			return CreateValue(input.Time, []);

		var result = OnProcess(input);

		if(result.Indicator != this)
			throw new InvalidOperationException($"invalid indicator value. expected {GetType().Name} got {result.Indicator?.GetType()}");

		//var result = value as IIndicatorValue ?? input.SetValue(value);

		if (input.IsFinal)
		{
			result.IsFinal = input.IsFinal;
			Container.AddValue(input, result);
		}

		if (!result.IsEmpty)
			RaiseChangedEvent(input, result);

		return result;
	}

	/// <summary>
	/// To handle the input value.
	/// </summary>
	/// <param name="input">The input value.</param>
	/// <returns>The resulting value.</returns>
	protected abstract IIndicatorValue OnProcess(IIndicatorValue input);

	/// <summary>
	/// To call the event <see cref="Changed"/>.
	/// </summary>
	/// <param name="input">The input value of the indicator.</param>
	/// <param name="result">The resulting value of the indicator.</param>
	protected void RaiseChangedEvent(IIndicatorValue input, IIndicatorValue result)
	{
		if (input == null)
			throw new ArgumentNullException(nameof(input));

		if (result == null)
			throw new ArgumentNullException(nameof(result));

		Changed?.Invoke(input, result);
	}

	/// <summary>
	/// Create a copy of <see cref="IIndicator"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IIndicator Clone()
		=> PersistableHelper.Clone(this);

	/// <inheritdoc />
	public override string ToString() => Name;

	/// <inheritdoc/>
	public virtual IIndicatorValue CreateValue(DateTimeOffset time, object[] values)
	{
		var value = GetType().GetValueType(false).CreateInstance<IIndicatorValue>(this, time);
		value.FromValues(values);
		return value;
	}
}
