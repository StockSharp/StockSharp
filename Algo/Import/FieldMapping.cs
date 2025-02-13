namespace StockSharp.Algo.Import;

/// <summary>
/// Importing field description.
/// </summary>
public abstract class FieldMapping : NotifiableObject, IPersistable, ICloneable
{
	private FastDateTimeParser _dateParser;
	private FastTimeSpanParser _timeParser;
	private Func<string, object> _dateConverter;

	private readonly HashSet<string> _enumNames = new(StringComparer.InvariantCultureIgnoreCase);

	/// <summary>
	/// Display name.
	/// </summary>
	protected readonly Func<string> GetDisplayName;

	/// <summary>
	/// Description.
	/// </summary>
	protected readonly Func<string> GetDescription;

	/// <summary>
	/// Initializes a new instance of the <see cref="FieldMapping"/>.
	/// </summary>
	/// <param name="name">Name.</param>
	/// <param name="getDisplayName">Display name.</param>
	/// <param name="getDescription">Description.</param>
	/// <param name="type">Field type.</param>
	protected FieldMapping(string name, Func<string> getDisplayName, Func<string> getDescription, Type type)
	{
		Type = type ?? throw new ArgumentNullException(nameof(type));
		Name = name.ThrowIfEmpty(nameof(name));

		GetDisplayName = getDisplayName ?? throw new ArgumentNullException(nameof(getDisplayName));
		GetDescription = getDescription ?? throw new ArgumentNullException(nameof(getDescription));

		IsEnabled = true;

		if (Type.IsDateTime())
			Format = "yyyyMMdd";
		else if (Type == typeof(TimeSpan))
			Format = "hh:mm:ss";

		if (Type.IsEnum)
			_enumNames.AddRange(Type.GetNames());

		LocalizedStrings.ActiveLanguageChanged += OnActiveLanguageChanged;
	}

	private void OnActiveLanguageChanged()
	{
		NotifyChanged(nameof(DisplayName));
		NotifyChanged(nameof(Description));
	}

	/// <summary>
	/// Name.
	/// </summary>
	public string Name { get; private set; }

	/// <summary>
	/// Is field extended.
	/// </summary>
	public bool IsExtended { get; set; }

	/// <summary>
	/// Display name.
	/// </summary>
	public string DisplayName => GetDisplayName();

	/// <summary>
	/// Description.
	/// </summary>
	public string Description => GetDescription().IsEmpty(GetDisplayName());

	/// <summary>
	/// Date format.
	/// </summary>
	public string Format { get; set; }

	/// <summary>
	/// Field type.
	/// </summary>
	public Type Type { get; }

	/// <summary>
	/// Is field required.
	/// </summary>
	public bool IsRequired { get; set; }

	/// <summary>
	/// Is field enabled.
	/// </summary>
	public bool IsEnabled
	{
		get => Order != null;
		set
		{
			if (IsEnabled == value)
				return;

			if (value)
				Order ??= 0;
			else
				Order = null;

			NotifyChanged(nameof(IsEnabled));
			NotifyChanged(nameof(Order));
		}
	}

	private int? _order;

	/// <summary>
	/// Field order.
	/// </summary>
	public int? Order
	{
		get => _order;
		set
		{
			if (Order == value)
				return;

			if (value < 0)
				throw new ArgumentOutOfRangeException(nameof(value));

			_order = value;

			IsEnabled = value != null;

			NotifyChanged(nameof(IsEnabled));
			NotifyChanged(nameof(Order));
		}
	}

	private IEnumerable<FieldMappingValue> _values = [];

	/// <summary>
	/// Mapping values.
	/// </summary>
	public IEnumerable<FieldMappingValue> Values
	{
		get => _values;
		set => _values = value ?? throw new ArgumentNullException(nameof(value));
	}

	/// <summary>
	/// Default value.
	/// </summary>
	public string DefaultValue { get; set; }

	/// <summary>
	/// Zero as <see langword="null"/>.
	/// </summary>
	public bool ZeroAsNull { get; set; }

	/// <summary>
	/// Multiple field's instancies allowed.
	/// </summary>
	public bool IsMultiple => IsAdapter;

	/// <summary>
	/// <see cref="AdapterType"/> required.
	/// </summary>
	public bool IsAdapter { get; set; }

	/// <summary>
	/// Adapter.
	/// </summary>
	public Type AdapterType { get; set; }

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		Name = storage.GetValue<string>(nameof(Name));
		IsExtended = storage.GetValue<bool>(nameof(IsExtended));
		Values = [.. storage.GetValue<SettingsStorage[]>(nameof(Values)).Select(s => s.Load<FieldMappingValue>())];
		DefaultValue = storage.GetValue<string>(nameof(DefaultValue));
		Format = storage.GetValue<string>(nameof(Format));
		ZeroAsNull = storage.GetValue<bool>(nameof(ZeroAsNull));

		//IsEnabled = storage.GetValue(nameof(IsEnabled), IsEnabled);

		if (storage.ContainsKey(nameof(IsEnabled)))
			IsEnabled = storage.GetValue<bool>(nameof(IsEnabled));
		else
			Order = storage.GetValue<int?>(nameof(Order));

		IsAdapter = storage.GetValue(nameof(IsAdapter), IsAdapter);
		AdapterType = storage.GetValue<string>(nameof(AdapterType)).To<Type>();
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Name), Name);
		storage.SetValue(nameof(IsExtended), IsExtended);
		storage.SetValue(nameof(Values), Values.Select(v => v.Save()).ToArray());
		storage.SetValue(nameof(DefaultValue), DefaultValue);
		storage.SetValue(nameof(Format), Format);
		//storage.SetValue(nameof(IsEnabled), IsEnabled);
		storage.SetValue(nameof(Order), Order);
		storage.SetValue(nameof(ZeroAsNull), ZeroAsNull);
		storage.SetValue(nameof(IsAdapter), IsAdapter);
		storage.SetValue(nameof(AdapterType), AdapterType.To<string>());
	}

	/// <summary>
	/// Apply value.
	/// </summary>
	/// <param name="instance">Instance.</param>
	/// <param name="value">Field value.</param>
	public void ApplyFileValue(object instance, string value)
	{
		if (value.IsEmpty())
		{
			ApplyDefaultValue(instance);
			return;
		}

		if (Values.Any())
		{
			var v = Values.FirstOrDefault(vl => vl.ValueFile.EqualsIgnoreCase(value));

			if (v != null)
			{
				ApplyValue(instance, v.ValueStockSharp);
				return;
			}
		}

		if (_enumNames.Contains(value))
		{
			ApplyValue(instance, value.To(Type));
			return;
		}

		ApplyValue(instance, value);
	}

	/// <summary>
	/// Apply default value.
	/// </summary>
	/// <param name="instance">Instance.</param>
	public void ApplyDefaultValue(object instance)
	{
		ApplyValue(instance, DefaultValue);
	}

	private void EnsureDateConverter()
	{
		if(_dateConverter != null)
			return;

		object fastParserConverter(string str)
		{
			if (Type == typeof(DateTimeOffset))
			{
				var dto = _dateParser.ParseDto(str);

				if (dto.Offset == default)
				{
					var tz = Scope<TimeZoneInfo>.Current?.Value;

					if (tz != null)
						dto = dto.UtcDateTime.ApplyTimeZone(tz);
				}

				return dto;
			}

			return _dateParser.Parse(str);
		}

		Func<DateTimeOffset, object> toObj = Type == typeof(DateTimeOffset) ? dto => dto : dto => dto.DateTime;

		switch (Format.ToLowerInvariant())
		{
			case "timestamp":
			case "unix":
				_dateConverter = str => toObj(DateTimeOffset.FromUnixTimeSeconds(long.Parse(str)));
				break;
			case "timestamp_milli":
			case "unixmls":
			case "unix_mls":
				_dateConverter = str => toObj(DateTimeOffset.FromUnixTimeMilliseconds(long.Parse(str)));
				break;
			case "timestamp_micro":
				_dateConverter = str => toObj(new DateTimeOffset(long.Parse(str).MicrosecondsToTicks(), TimeSpan.Zero));
				break;
			case "timestamp_nano":
				_dateConverter = str => toObj(new DateTimeOffset(long.Parse(str).NanosecondsToTicks(), TimeSpan.Zero));
				break;
			default:
				_dateParser = new FastDateTimeParser(Format);
				_dateConverter = fastParserConverter;
				break;
		}
	}

	private void ApplyValue(object instance, object value)
	{
		if (Type == typeof(decimal))
		{
			if (value is string str)
			{
				if (str.ContainsIgnoreCase("e")) // exponential notation
					value = str.To<double>();
				else
				{
					str = str.Replace(',', '.').RemoveSpaces().ReplaceWhiteSpaces().Trim();

					if (str.IsEmpty())
						return;

					value = str;
				}
			}
		}
		else if (Type.IsDateTime())
		{
			if (value is string str)
			{
				EnsureDateConverter();
				value = _dateConverter(str);
			}
		}
		else if (Type == typeof(TimeSpan))
		{
			if (value is string str)
			{
				_timeParser ??= new FastTimeSpanParser(Format);

				value = _timeParser.Parse(str);
			}
		}

		if (value != null)
		{
			value = value.To(Type);

			if (ZeroAsNull && Type.IsNumeric() && value.To<decimal>() == 0)
				return;

			OnApply(instance, value);
		}
	}

	/// <summary>
	/// Apply value.
	/// </summary>
	/// <param name="instance">Instance.</param>
	/// <param name="value">Field value.</param>
	protected abstract void OnApply(object instance, object value);

	/// <inheritdoc />
	public override string ToString() => Name;

	/// <inheritdoc />
	public abstract object Clone();

	/// <summary>
	/// Reset state.
	/// </summary>
	public void Reset()
	{
		_dateParser = null;
		_timeParser = null;
		_dateConverter = null;
	}

	/// <summary>
	/// Get <see cref="FieldMapping"/> instance or clone dependent on <see cref="IsMultiple"/>.
	/// </summary>
	/// <returns>Field.</returns>
	public FieldMapping GetOrClone()
	{
		return IsMultiple ? (FieldMapping)Clone() : this;
	}
}

/// <summary>
/// Importing field description.
/// </summary>
/// <typeparam name="TInstance">Type, containing the field.</typeparam>
/// <typeparam name="TValue">Field value type.</typeparam>
public class FieldMapping<TInstance, TValue> : FieldMapping
{
	private readonly Action<TInstance, TValue> _apply;

	/// <summary>
	/// Initializes a new instance of the <see cref="FieldMapping{TInstance,TValue}"/>.
	/// </summary>
	/// <param name="name">Name.</param>
	/// <param name="getDisplayName">Display name.</param>
	/// <param name="getDescription">Description.</param>
	/// <param name="apply">Apply field value action.</param>
	public FieldMapping(string name, Func<string> getDisplayName, Func<string> getDescription, Action<TInstance, TValue> apply)
		: this(name, getDisplayName, getDescription, typeof(TValue), apply)
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="FieldMapping{TInstance,TValue}"/>.
	/// </summary>
	/// <param name="name">Name.</param>
	/// <param name="getDisplayName">Display name.</param>
	/// <param name="getDescription">Description.</param>
	/// <param name="type">Field type.</param>
	/// <param name="apply">Apply field value action.</param>
	public FieldMapping(string name, Func<string> getDisplayName, Func<string> getDescription, Type type, Action<TInstance, TValue> apply)
		: base(name, getDisplayName, getDescription, type)
	{
		_apply = apply ?? throw new ArgumentNullException(nameof(apply));
	}

	/// <inheritdoc />
	protected override void OnApply(object instance, object value)
	{
		_apply((TInstance)instance, (TValue)value);
	}

	/// <inheritdoc />
	public override object Clone()
	{
		var clone = new FieldMapping<TInstance, TValue>(Name, GetDisplayName, GetDescription, _apply);
		clone.Load(this.Save());
		return clone;
	}
}
