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
}

/// <summary>
/// Wrapper for typified access to the strategy parameter.
/// </summary>
/// <typeparam name="T">The type of the parameter value.</typeparam>
public class StrategyParam<T> : NotifiableObject, IStrategyParam
{
	private readonly IEqualityComparer<T> _comparer;

	// step restriction
	private bool _hasStep;
	private T _stepValue;
	private T _stepBaseValue;

	private static readonly Type _valueType = typeof(T).GetUnderlyingType() ?? typeof(T);

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

			if (_hasStep && _hasStep && !IsValueMatchesStep(_valueType, value, _stepValue, _stepBaseValue))
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_value = value;
			NotifyChanged();
		}
	}

	private static bool IsValueMatchesStep(Type type, T value, T step, T baseValue)
	{
		if (type == typeof(TimeSpan))
		{
			var v = value.To<TimeSpan>();
			var b = baseValue.To<TimeSpan>();
			var s = step.To<TimeSpan>();

			var diff = v - b;

			if (diff < TimeSpan.Zero)
				return false;

			return diff.Ticks % s.Ticks == 0;
		}
		else if (type.IsNumericInteger())
		{
			var v = value.To<long>();
			var b = baseValue.To<long>();
			var s = step.To<long>();

			var diff = v - b;

			if (diff < 0)
				return false;

			return diff % s == 0;
		}
		else if (type.IsNumeric())
		{
			var v = value.To<decimal>();
			var b = baseValue.To<decimal>();
			var s = step.To<decimal>();

			var diff = v - b;

			if (diff < 0)
				return false;

			var q = diff / s;
			var rq = Math.Round(q);

			return (q - rq).Abs() < 1e-10m;
		}
		else if (type == typeof(Unit))
		{
			var v = value.To<Unit>();
			var b = baseValue.To<Unit>();
			var s = step.To<Unit>();

			if (v.Type != b.Type || v.Type != s.Type)
				return false;

			var diff = v.Value - b.Value;

			if (diff < 0)
				return false;

			var q = diff / s.Value;
			var rq = Math.Round(q);

			return (q - rq).Abs() < 1e-10m;
		}
		else
			throw new NotSupportedException(type.FullName);
	}

	/// <summary>
	/// Set values step restriction (value must equal base + N*step).
	/// </summary>
	/// <param name="step">Step (>0).</param>
	/// <param name="baseValue">Base value (default 0).</param>
	/// <returns><see cref="StrategyParam{T}"/>.</returns>
	public StrategyParam<T> SetStep(T step, T baseValue = default)
	{
		var type = _valueType;

		bool invalid;

		if (type == typeof(TimeSpan))
			invalid = step.To<TimeSpan>() <= TimeSpan.Zero;
		else if (type.IsNumericInteger())
			invalid = step.To<long>() <= 0;
		else if (type.IsNumeric())
			invalid = step.To<decimal>() <= 0;
		else if (type == typeof(Unit))
			invalid = step.To<Unit>().Value <= 0;
		else
			throw new NotSupportedException(type.FullName);

		if (invalid)
			throw new ArgumentOutOfRangeException(nameof(step), step, LocalizedStrings.IntervalMustBePositive);

		if (!IsValueMatchesStep(type, _value, step, baseValue))
			throw new ArgumentOutOfRangeException(nameof(step), step, LocalizedStrings.InvalidValue);

		_stepValue = step;
		_stepBaseValue = baseValue;
		_hasStep = true;
		return this;
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
		=> ModifyAttributes(true, () => new DisplayAttribute
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
		=> ModifyAttributes(hidden, () => new BrowsableAttribute(false));

	/// <summary>
	/// Set <see cref="BasicSettingAttribute"/>.
	/// </summary>
	/// <param name="basic">Value.</param>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetBasic(bool basic = true)
		=> ModifyAttributes(basic, () => new BasicSettingAttribute());

	/// <summary>
	/// Set <see cref="ReadOnlyAttribute"/>.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetReadOnly(bool value = true)
		=> ModifyAttributes(value, () => new ReadOnlyAttribute(true));

	/// <summary>
	/// Set greater than zero validator.
	/// </summary>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetGreaterThanZero()
	{
		var type = _valueType;

		ValidationAttribute attr = type switch
		{
			_ when type == typeof(int) => new IntGreaterThanZeroAttribute(),
			_ when type == typeof(long) => new LongGreaterThanZeroAttribute(),
			_ when type == typeof(decimal) => new DecimalGreaterThanZeroAttribute(),
			_ when type == typeof(double) => new DoubleGreaterThanZeroAttribute(),
			_ when type == typeof(float) => new FloatGreaterThanZeroAttribute(),
			_ when type == typeof(TimeSpan) => new TimeSpanGreaterThanZeroAttribute(),
			_ => throw new InvalidOperationException(type.Name)
		};

		return this.SetValidator(attr);
	}

	/// <summary>
	/// Set <see langword="null"/> or more zero validator.
	/// </summary>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetNullOrMoreZero()
	{
		var type = _valueType;

		ValidationAttribute attr = type switch
		{
			_ when type == typeof(int) => new IntNullOrMoreZeroAttribute(),
			_ when type == typeof(long) => new LongNullOrMoreZeroAttribute(),
			_ when type == typeof(decimal) => new DecimalNullOrMoreZeroAttribute(),
			_ when type == typeof(double) => new DoubleNullOrMoreZeroAttribute(),
			_ when type == typeof(float) => new FloatNullOrMoreZeroAttribute(),
			_ when type == typeof(TimeSpan) => new TimeSpanNullOrMoreZeroAttribute(),
			_ => throw new InvalidOperationException(type.Name)
		};

		return this.SetValidator(attr);
	}

	/// <summary>
	/// Set <see langword="null"/> or not negative validator.
	/// </summary>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetNullOrNotNegative()
	{
		var type = _valueType;

		ValidationAttribute attr = type switch
		{
			_ when type == typeof(int) => new IntNullOrNotNegativeAttribute(),
			_ when type == typeof(long) => new LongNullOrNotNegativeAttribute(),
			_ when type == typeof(decimal) => new DecimalNullOrNotNegativeAttribute(),
			_ when type == typeof(double) => new DoubleNullOrNotNegativeAttribute(),
			_ when type == typeof(float) => new FloatNullOrNotNegativeAttribute(),
			_ when type == typeof(TimeSpan) => new TimeSpanNullOrNotNegativeAttribute(),
			_ => throw new InvalidOperationException(type.Name)
		};

		return this.SetValidator(attr);
	}

	/// <summary>
	/// Set not negative validator.
	/// </summary>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetNotNegative()
	{
		var type = _valueType;

		ValidationAttribute attr = type switch
		{
			_ when type == typeof(int) => new IntNotNegativeAttribute(),
			_ when type == typeof(long) => new LongNotNegativeAttribute(),
			_ when type == typeof(decimal) => new DecimalNotNegativeAttribute(),
			_ when type == typeof(double) => new DoubleNotNegativeAttribute(),
			_ when type == typeof(float) => new FloatNotNegativeAttribute(),
			_ when type == typeof(TimeSpan) => new TimeSpanNotNegativeAttribute(),
			_ => throw new InvalidOperationException(type.Name)
		};

		return this.SetValidator(attr);
	}

	/// <summary>
	/// Set positive validator.
	/// </summary>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetPositive()
	{
		var type = _valueType;

		object max = type switch
		{
			_ when type == typeof(int) => int.MaxValue,
			_ when type == typeof(long) => long.MaxValue,
			_ when type == typeof(decimal) => decimal.MaxValue,
			_ when type == typeof(double) => double.MaxValue,
			_ when type == typeof(float) => float.MaxValue,
			_ when type == typeof(TimeSpan) => TimeSpan.MaxValue,
			_ => throw new InvalidOperationException(type.Name)
		};

		return SetRange(1L.To<T>(), max.To<T>());
	}

	/// <summary>
	/// Set range validator.
	/// </summary>
	/// <param name="min">Minimum value.</param>
	/// <param name="max">Maximum value.</param>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetRange(T min, T max)
	{
		var type = _valueType;

		RangeAttribute attr = type == typeof(int)
			? new RangeAttribute(min.To<int>(), max.To<int>())
			: new RangeAttribute(type, min.To<string>(), max.To<string>());

		return this.SetValidator(attr);
	}

	private StrategyParam<T> ModifyAttributes<TAttr>(bool add, Func<TAttr> create)
		where TAttr : Attribute
	{
		Attributes.RemoveWhere(a => a is TAttr);

		if (add)
			Attributes.Add(create());

		return this;
	}

	/// <summary>
	/// Set required validator.
	/// </summary>
	/// <returns><see cref="StrategyParam{T}"/></returns>
	public StrategyParam<T> SetRequired()
		=> Ecng.ComponentModel.Extensions.SetRequired(this);

	private static class Keys
	{
		public const string StepValue = nameof(StepValue);
		public const string StepBaseValue = nameof(StepBaseValue);
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

		if (!storage.ContainsKey(Keys.StepValue))
			return;

		var stepVal = storage.GetValue<T>(Keys.StepValue);
		var baseVal = storage.GetValue<T>(Keys.StepBaseValue);

		try
		{
			SetStep(stepVal, baseVal);
		}
		catch (Exception ex)
		{
			ex.LogError();
		}
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

		if (_hasStep)
		{
			storage
				.Set(Keys.StepValue, _stepValue)
				.Set(Keys.StepBaseValue, _stepBaseValue)
			;
		}
	}

	/// <inheritdoc />
	public override string ToString() => $"{this.GetName()}={Value}";
}