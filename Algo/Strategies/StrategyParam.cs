namespace StockSharp.Algo.Strategies;

/// <summary>
/// The strategy parameter.
/// </summary>
public interface IStrategyParam : IPersistable, INotifyPropertyChanged, IAttributesEntity
{
	/// <summary>
	/// Parameter identifier.
	/// </summary>
	string Id { get; }

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

	/// <summary>
	/// Explicit values for optimization (for types like Security, DataType that don't support ranges).
	/// </summary>
	IEnumerable OptimizeValues { get; set; }
}

/// <summary>
/// Wrapper for typified access to the strategy parameter.
/// </summary>
/// <typeparam name="T">The type of the parameter value.</typeparam>
public class StrategyParam<T> : NotifiableObject, IStrategyParam
{
	private readonly EqualityComparer<T> _comparer;
	private static readonly Type _valueType = typeof(T).GetUnderlyingType() ?? typeof(T);
	private static readonly bool _isNullable = Nullable.GetUnderlyingType(typeof(T)) != null;

	/// <summary>
	/// Initializes a new instance of the <see cref="StrategyParam{T}"/>.
	/// </summary>
	/// <param name="id">Parameter identifier.</param>
	/// <param name="initialValue">The initial value.</param>
	public StrategyParam(string id, T initialValue = default)
	{
		if (id.IsEmpty())
			throw new ArgumentNullException(nameof(id));

		Id = id;
		_value = initialValue;

		CanOptimize = typeof(T).CanOptimize();

		_comparer = EqualityComparer<T>.Default;
	}

	/// <inheritdoc />
	public string Id { get; private set; }

	private T _value;

	/// <inheritdoc />
	public T Value
	{
		get => _value;
		set
		{
			if (_comparer.Equals(_value, value))
				return;

			if (!this.IsValid(value))
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_value = value;
			NotifyChanged();
		}
	}

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

	private T[] _optimizeValues = [];

	/// <summary>
	/// Explicit values for optimization (for types like Security, DataType that don't support ranges).
	/// </summary>
	public IEnumerable<T> OptimizeValues
	{
		get => _optimizeValues;
		set => _optimizeValues = value?.ToArray() ?? throw new ArgumentNullException(nameof(value));
	}

	IEnumerable IStrategyParam.OptimizeValues
	{
		get => OptimizeValues;
		set => OptimizeValues = (IEnumerable<T>)value;
	}

	/// <inheritdoc />
	public IList<Attribute> Attributes { get; } = [];

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
	/// Set explicit values for optimization (for types like Security, DataType).
	/// </summary>
	/// <param name="values">The values to iterate during optimization.</param>
	/// <returns>The strategy parameter.</returns>
	public StrategyParam<T> SetOptimizeValues(IEnumerable<T> values)
	{
		OptimizeValues = values ?? throw new ArgumentNullException(nameof(values));
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
		=> this.ModifyAttributes(true, () => new DisplayAttribute
		{
			Name = displayName,
			Description = description,
			GroupName = category,
		});

	/// <summary>
	/// Set <see cref="BrowsableAttribute"/>.
	/// </summary>
	/// <param name="hidden">Is the parameter hidden in the editor.</param>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetHidden(bool hidden = true)
		=> this.ModifyAttributes(hidden, () => new BrowsableAttribute(false));

	/// <summary>
	/// Set <see cref="BasicSettingAttribute"/>.
	/// </summary>
	/// <param name="basic">Value.</param>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetBasic(bool basic = true)
		=> this.ModifyAttributes(basic, () => new BasicSettingAttribute());

	/// <summary>
	/// Set <see cref="ReadOnlyAttribute"/>.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetReadOnly(bool value = true)
		=> this.ModifyAttributes(value, () => new ReadOnlyAttribute(true));

	/// <summary>
	/// Set greater than zero validator.
	/// </summary>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetGreaterThanZero()
	{
		ValidationAttribute attr = _valueType switch
		{
			_ when _valueType == typeof(int) => new IntGreaterThanZeroAttribute(),
			_ when _valueType == typeof(long) => new LongGreaterThanZeroAttribute(),
			_ when _valueType == typeof(decimal) => new DecimalGreaterThanZeroAttribute(),
			_ when _valueType == typeof(double) => new DoubleGreaterThanZeroAttribute(),
			_ when _valueType == typeof(float) => new FloatGreaterThanZeroAttribute(),
			_ when _valueType == typeof(TimeSpan) => new TimeSpanGreaterThanZeroAttribute(),
			_ when _valueType == typeof(Unit) => new UnitGreaterThanZeroAttribute(),
			_ => throw new InvalidOperationException(_valueType.Name)
		};

		return this.ModifyAttributes(true, attr);
	}

	/// <summary>
	/// Set <see langword="null"/> or more zero validator.
	/// </summary>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetNullOrMoreZero()
	{
		ValidationAttribute attr = _valueType switch
		{
			_ when _valueType == typeof(int) => new IntNullOrMoreZeroAttribute(),
			_ when _valueType == typeof(long) => new LongNullOrMoreZeroAttribute(),
			_ when _valueType == typeof(decimal) => new DecimalNullOrMoreZeroAttribute(),
			_ when _valueType == typeof(double) => new DoubleNullOrMoreZeroAttribute(),
			_ when _valueType == typeof(float) => new FloatNullOrMoreZeroAttribute(),
			_ when _valueType == typeof(TimeSpan) => new TimeSpanNullOrMoreZeroAttribute(),
			_ when _valueType == typeof(Unit) => new UnitNullOrMoreZeroAttribute(),
			_ => throw new InvalidOperationException(_valueType.Name)
		};

		return this.ModifyAttributes(true, attr);
	}

	/// <summary>
	/// Set <see langword="null"/> or not negative validator.
	/// </summary>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetNullOrNotNegative()
	{
		ValidationAttribute attr = _valueType switch
		{
			_ when _valueType == typeof(int) => new IntNullOrNotNegativeAttribute(),
			_ when _valueType == typeof(long) => new LongNullOrNotNegativeAttribute(),
			_ when _valueType == typeof(decimal) => new DecimalNullOrNotNegativeAttribute(),
			_ when _valueType == typeof(double) => new DoubleNullOrNotNegativeAttribute(),
			_ when _valueType == typeof(float) => new FloatNullOrNotNegativeAttribute(),
			_ when _valueType == typeof(TimeSpan) => new TimeSpanNullOrNotNegativeAttribute(),
			_ when _valueType == typeof(Unit) => new UnitNullOrNotNegativeAttribute(),
			_ => throw new InvalidOperationException(_valueType.Name)
		};

		return this.ModifyAttributes(true, attr);
	}

	/// <summary>
	/// Set not negative validator.
	/// </summary>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetNotNegative()
	{
		ValidationAttribute attr = _valueType switch
		{
			_ when _valueType == typeof(int) => new IntNotNegativeAttribute(),
			_ when _valueType == typeof(long) => new LongNotNegativeAttribute(),
			_ when _valueType == typeof(decimal) => new DecimalNotNegativeAttribute(),
			_ when _valueType == typeof(double) => new DoubleNotNegativeAttribute(),
			_ when _valueType == typeof(float) => new FloatNotNegativeAttribute(),
			_ when _valueType == typeof(TimeSpan) => new TimeSpanNotNegativeAttribute(),
			_ when _valueType == typeof(Unit) => new UnitNotNegativeAttribute(),
			_ => throw new InvalidOperationException(_valueType.Name)
		};

		return this.ModifyAttributes(true, attr);
	}

	/// <summary>
	/// Set range validator.
	/// </summary>
	/// <param name="min">Minimum value.</param>
	/// <param name="max">Maximum value.</param>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetRange(T min, T max)
		=> this.ModifyAttributes(true, () =>
		{
			if (_valueType == typeof(int))
				return new RangeAttribute(min.To<int>(), max.To<int>());
			else if (_valueType == typeof(Unit))
				return new UnitRangeAttribute(min.To<Unit>(), max.To<Unit>()) { DisableNullCheck = _isNullable };
			else if (_valueType == typeof(TimeSpan))
				return new TimeSpanRangeAttribute(min.To<TimeSpan>(), max.To<TimeSpan>()) { DisableNullCheck = _isNullable };
			else
				return Do.Invariant(() => new RangeAttribute(_valueType, min.To<string>(), max.To<string>()) { ParseLimitsInInvariantCulture = true });
		});

	/// <summary>
	/// Set required validator.
	/// </summary>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetRequired()
		=> Ecng.ComponentModel.Extensions.SetRequired(this);

	/// <summary>
	/// Set values step restriction (value must equal base + N*step).
	/// </summary>
	/// <param name="step">Step (>0).</param>
	/// <param name="baseValue">Base value (default 0).</param>
	/// <returns><see cref="StrategyParam{T}"/>.</returns>
	public StrategyParam<T> SetStep(T step, T baseValue = default)
	{
		if (step is null)
			throw new ArgumentNullException(nameof(step));

		if (baseValue is null)
		{
			if (_isNullable)
				baseValue = _valueType.CreateInstance<T>();
			else
				throw new ArgumentNullException(nameof(baseValue));
		}

		StepAttribute attr;

		if (_valueType == typeof(Unit))
		{
			var s = step.To<Unit>();
			var b = baseValue as Unit ?? new(0m, s.Type);
			attr = new UnitStepAttribute(s, b) { DisableNullCheck = _isNullable };
		}
		else if (_valueType == typeof(TimeSpan))
		{
			var s = step.To<TimeSpan>();
			var b = (baseValue as TimeSpan?) ?? default;
			attr = new TimeSpanStepAttribute(s, b) { DisableNullCheck = _isNullable };
		}
		else
		{
			decimal stepDec;
			decimal baseDec;

			if (_valueType.IsNumericInteger())
			{
				stepDec = step.To<long>();
				baseDec = baseValue.To<long>();
			}
			else if (_valueType.IsNumeric())
			{
				stepDec = step.To<decimal>();
				baseDec = baseValue.To<decimal>();
			}
			else
				throw new NotSupportedException(_valueType.FullName);

			attr = new StepAttribute(stepDec, baseDec) { DisableNullCheck = _isNullable };
		}

		if (!attr.IsValid(Value))
			throw new ArgumentOutOfRangeException(nameof(step), step, LocalizedStrings.InvalidValue);

		return this.ModifyAttributes(true, attr);
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		Id = storage.GetValue<string>(nameof(Id));

		try
		{
			TValue getValue<TValue>()
				=> storage.GetValue<TValue>(nameof(Value));

			if (typeof(T).Is<Security>())
			{
				var secId = getValue<string>();
				if (!secId.IsEmpty())
					Value = (ServicesRegistry.TrySecurityProvider?.LookupById(secId)).To<T>();
			}
			else if (typeof(T).Is<Portfolio>())
			{
				var pfName = getValue<string>();
				if (!pfName.IsEmpty())
					Value = (ServicesRegistry.TryPortfolioProvider?.LookupByPortfolioName(pfName)).To<T>();
			}
			else
				Value = getValue<T>();
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
		object saveValue()
		{
			var v = Value;

			return v switch
			{
				IPersistable ps => ps.Save(),
				Security s => s.Id,
				Portfolio pf => pf.Name,
				_ => v
			};
		}

		storage
			.Set(nameof(Id), Id)
			.Set(nameof(Value), saveValue())
			.Set(nameof(CanOptimize), CanOptimize)
			.Set(nameof(OptimizeFrom), OptimizeFrom?.ToStorage())
			.Set(nameof(OptimizeTo), OptimizeTo?.ToStorage())
			.Set(nameof(OptimizeStep), OptimizeStep?.ToStorage());
	}

	/// <inheritdoc />
	public override string ToString() => $"{this.GetName()}={Value}";
}