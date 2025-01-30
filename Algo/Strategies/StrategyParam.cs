namespace StockSharp.Algo.Strategies;

/// <summary>
/// The strategy parameter.
/// </summary>
public interface IStrategyParam : IPersistable, INotifyPropertyChanged
{
	/// <summary>
	/// Parameter identifier.
	/// </summary>
	string Id { get; }

	/// <summary>
	/// Parameter name.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Attributes.
	/// </summary>
	IList<Attribute> Attributes { get; }

	/// <summary>
	/// Parameter description.
	/// </summary>
	string Description { get; }

	/// <summary>
	/// Parameter category.
	/// </summary>
	string Category { get; }

	/// <summary>
	/// The type of the parameter value.
	/// </summary>
	Type Type { get; }

	/// <summary>
	/// The parameter value.
	/// </summary>
	object Value { get; set; }

	/// <summary>
	/// Check can optimize parameter.
	/// </summary>
	bool CanOptimize { get; set; }

	/// <summary>
	/// The From value at optimization.
	/// </summary>
	object OptimizeFrom { get; set; }

	/// <summary>
	/// The To value at optimization.
	/// </summary>
	object OptimizeTo { get; set; }

	/// <summary>
	/// The Increment value at optimization.
	/// </summary>
	object OptimizeStep { get; set; }
}

/// <summary>
/// Wrapper for typified access to the strategy parameter.
/// </summary>
/// <typeparam name="T">The type of the parameter value.</typeparam>
public class StrategyParam<T> : NotifiableObject, IStrategyParam
{
	private readonly IEqualityComparer<T> _comparer;

	/// <summary>
	/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
	/// </summary>
	/// <param name="name">Parameter name.</param>
	public StrategyParam(string name)
		: this(name, name, default)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
	/// </summary>
	/// <param name="id">Parameter identifier.</param>
	/// <param name="name">Parameter name.</param>
	/// <param name="initialValue">The initial value.</param>
	public StrategyParam(string id, string name, T initialValue)
	{
		if (id.IsEmpty())
			throw new ArgumentNullException(nameof(id));

		if (name.IsEmpty())
			throw new ArgumentNullException(nameof(name));

		Id = id;
		Name = name;
		_value = initialValue;

		CanOptimize = typeof(T).CanOptimize();

		_comparer = EqualityComparer<T>.Default;
	}

	/// <inheritdoc />
	public string Id { get; private set; }

	/// <inheritdoc />
	public string Name { get; private set; }

	private T _value;

	/// <inheritdoc />
	public virtual T Value
	{
		get => _value;
		set
		{
			if (Validator?.Invoke(value) == false)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			if (_comparer.Equals(_value, value))
				return;

			if (_value is INotifyPropertyChanged propChange)
				propChange.PropertyChanged -= OnValueInnerStateChanged;

			_value = value;
			NotifyChanged(nameof(Value));

			if (_value is INotifyPropertyChanged propChange2)
				propChange2.PropertyChanged += OnValueInnerStateChanged;
		}
	}

	/// <summary>
	/// <see cref="Value"/> validator.
	/// </summary>
	public Func<T, bool> Validator { get; set; }

	Type IStrategyParam.Type => typeof(T);

	object IStrategyParam.Value
	{
		get => Value;
		set => Value = (T)value;
	}

	/// <inheritdoc />
	public bool CanOptimize { get; set; }

	/// <inheritdoc />
	public object OptimizeFrom { get; set; }

	/// <inheritdoc />
	public object OptimizeTo { get; set; }

	/// <inheritdoc />
	public object OptimizeStep { get; set; }

	/// <inheritdoc />
	public string Description { get; set; }

	/// <inheritdoc />
	public string Category { get; set; }

	/// <inheritdoc />
	public IList<Attribute> Attributes => [];

	/// <summary>
	/// Fill optimization parameters.
	/// </summary>
	/// <param name="optimizeFrom">The From value at optimization.</param>
	/// <param name="optimizeTo">The To value at optimization.</param>
	/// <param name="optimizeStep">The Increment value at optimization.</param>
	/// <returns>The strategy parameter.</returns>
	public StrategyParam<T> SetOptimize(T optimizeFrom = default, T optimizeTo = default, T optimizeStep = default)
	{
		OptimizeFrom = optimizeFrom;
		OptimizeTo = optimizeTo;
		OptimizeStep = optimizeStep;

		return this;
	}

	/// <summary>
	/// Set <see cref="StrategyParam{T}.CanOptimize"/> value.
	/// </summary>
	/// <param name="canOptimize">The value of <see cref="StrategyParam{T}.CanOptimize"/>.</param>
	/// <returns>The strategy parameter.</returns>
	public StrategyParam<T> SetCanOptimize(bool canOptimize)
	{
		CanOptimize = canOptimize;
		return this;
	}

	/// <summary>
	/// Set display settings.
	/// </summary>
	/// <param name="displayName">The display name.</param>
	/// <param name="description">The description of the diagram element parameter.</param>
	/// <param name="category">The category of the diagram element parameter.</param>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetDisplay(string displayName, string description, string category)
	{
		Name = displayName;
		Description = description;
		Category = category;

		return this;
	}

	/// <summary>
	/// Set <see cref="BrowsableAttribute"/>.
	/// </summary>
	/// <param name="hidden">Is the parameter hidden in the editor.</param>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetHidden(bool hidden = true)
	{
		if (hidden)
			Attributes.Add(new BrowsableAttribute(false));
		else
			Attributes.RemoveWhere(a => a is BrowsableAttribute);

		return this;
	}

	/// <summary>
	/// Set <see cref="ReadOnlyAttribute"/>.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetReadOnly(bool value = true)
	{
		if (value)
			Attributes.Add(new ReadOnlyAttribute(true));
		else
			Attributes.RemoveWhere(a => a is ReadOnlyAttribute);

		return this;
	}

	private void OnValueInnerStateChanged(object sender, PropertyChangedEventArgs e)
	{
		NotifyChanged(nameof(Value));
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		Id = storage.GetValue<string>(nameof(Id));
		Name = storage.GetValue<string>(nameof(Name));

		try
		{
			Value = storage.GetValue<T>(nameof(Value));
		}
		catch (Exception ex)
		{
			ex.LogError();
		}

		CanOptimize = storage.GetValue(nameof(CanOptimize), CanOptimize);
		OptimizeFrom = storage.GetValue<SettingsStorage>(nameof(OptimizeFrom))?.FromStorage();
		OptimizeTo = storage.GetValue<SettingsStorage>(nameof(OptimizeTo))?.FromStorage();
		OptimizeStep = storage.GetValue<SettingsStorage>(nameof(OptimizeStep))?.FromStorage();
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(Id), Id)
			.Set(nameof(Name), Name)
			.Set(nameof(Value), Value)
			.Set(nameof(CanOptimize), CanOptimize)
			.Set(nameof(OptimizeFrom), OptimizeFrom?.ToStorage())
			.Set(nameof(OptimizeTo), OptimizeTo?.ToStorage())
			.Set(nameof(OptimizeStep), OptimizeStep?.ToStorage())
		;
	}

	/// <inheritdoc />
	public override string ToString() => $"{Name}={Value}";
}