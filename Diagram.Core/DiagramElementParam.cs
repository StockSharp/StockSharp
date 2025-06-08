namespace StockSharp.Diagram;

/// <summary>
/// The diagram element parameter.
/// </summary>
/// <typeparam name="T">Value type.</typeparam>
public class DiagramElementParam<T> : NotifiableObject, IDiagramElementParam
{
	/// <summary>
	/// The parameter value change start event.
	/// </summary>
	public event Action<T, T> ValueChanging;

	/// <summary>
	/// The parameter value change event.
	/// </summary>
	public event Action<T> ValueChanged;

	private Func<T, T, bool> _valueValidating = (o, n) => !EqualityComparer<T>.Default.Equals(o, n);

	/// <summary>
	/// Validate the parameter value.
	/// </summary>
	public Func<T, T, bool> ValueValidating
	{
		get => _valueValidating;
		set => _valueValidating = value ?? throw new ArgumentNullException(nameof(value));
	}

	private string _name;

	/// <inheritdoc />
	public string Name
	{
		get => _name;
		set
		{
			_name = value;
			NotifyChanged();
		}
	}

	/// <inheritdoc />
	public Type Type => typeof(T);

	/// <summary>
	/// Can change value.
	/// </summary>
	public bool CanChangeValue { get; set; } = true;

	private readonly List<Attribute> _attributes = [];

	/// <inheritdoc />
	public IList<Attribute> Attributes => _attributes;

	private T _value;
	private bool _hasValue;

	/// <summary>
	/// The parameter value.
	/// </summary>
	public virtual T Value
	{
		get => _value;
		set
		{
			if (!CanChangeValue)
				return;

			if (!ValueValidating(_value, value))
				return;

			if (NotifyOnChanged)
				NotifyChanging();

			ValueChanging?.Invoke(_value, value);

			IgnoreOnSave = false;
			_value = value;
			_hasValue = true;

			ValueChanged?.Invoke(_value);

			if (NotifyOnChanged)
				NotifyChanged();
		}
	}

	/// <inheritdoc />
	public bool IsDefault => !_hasValue;

	/// <inheritdoc />
	public bool CanOptimize { get; set; }

	/// <inheritdoc />
	public bool IgnoreOnSave { get; set; }

	/// <inheritdoc />
	public void SetValueWithIgnoreOnSave(object value)
	{
		IgnoreOnSave = true;
		Value = (T)value;
		IgnoreOnSave = true;
	}

	/// <inheritdoc />
	public bool NotifyOnChanged { get; set; } = true;

	object IDiagramElementParam.Value
	{
		get => Value;
		set => Value = (T)value;
	}

	/// <summary>
	/// The parameter value saving handler.
	/// </summary>
	public Func<T, SettingsStorage> SaveHandler { get; set; }

	/// <summary>
	/// The parameter value loading handler.
	/// </summary>
	public Func<SettingsStorage, T> LoadHandler { get; set; }

	/// <summary>
	/// To set the <see cref="ExpandableObjectConverter"/> attribute for the diagram element parameter.
	/// </summary>
	/// <param name="expandable">Value.</param>
	/// <returns>The diagram element parameter.</returns>
	public DiagramElementParam<T> SetExpandable(bool expandable)
		=> this.SetAttribute(expandable, () => new TypeConverterAttribute(typeof(ExpandableObjectConverter)));

	/// <summary>
	/// To add the attribute <see cref="Attribute"/> for the diagram element parameter.
	/// </summary>
	/// <typeparam name="TEditor">Editor type.</typeparam>
	/// <param name="editor">Attribute.</param>
	/// <returns>The diagram element parameter.</returns>
	public DiagramElementParam<T> SetEditor<TEditor>(TEditor editor)
		where TEditor : Attribute
	{
		if (editor == null)
			throw new ArgumentNullException(nameof(editor));

		return this.SetAttribute(true, () => editor);
	}

	/// <summary>
	/// To set the <see cref="DisplayAttribute"/> attribute for the diagram element parameter.
	/// </summary>
	/// <param name="groupName">The category of the diagram element parameter.</param>
	/// <param name="displayName">The display name.</param>
	/// <param name="description">The description of the diagram element parameter.</param>
	/// <param name="order">The property order.</param>
	/// <returns>The diagram element parameter.</returns>
	public DiagramElementParam<T> SetDisplay(string groupName, string displayName, string description, int order)
		=> this.SetAttribute(true, () => new DisplayAttribute
		{
			Name = displayName,
			Description = description,
			GroupName = groupName,
			Order = order,
		});

	/// <summary>
	/// To set the <see cref="ReadOnlyAttribute"/> attribute for the diagram element parameter.
	/// </summary>
	/// <param name="readOnly">Read-only.</param>
	/// <returns>The diagram element parameter.</returns>
	public DiagramElementParam<T> SetReadOnly(bool readOnly = true)
		=> this.SetAttribute(readOnly, () => new ReadOnlyAttribute(true));

	/// <summary>
	/// To set the <see cref="BasicSettingAttribute"/> attribute for the diagram element parameter.
	/// </summary>
	/// <param name="isBasic">Is basic parameter.</param>
	/// <returns>The diagram element parameter.</returns>
	public DiagramElementParam<T> SetBasic(bool isBasic = true)
		=> this.SetAttribute(isBasic, () => new BasicSettingAttribute());

	/// <summary>
	/// To set the <see cref="BrowsableAttribute"/> attribute for the diagram element parameter.
	/// </summary>
	/// <param name="nonBrowsable">Hidden parameter.</param>
	/// <returns>The diagram element parameter.</returns>
	public DiagramElementParam<T> SetNonBrowsable(bool nonBrowsable = true)
		=> this.SetAttribute(nonBrowsable, () => new BrowsableAttribute(false));

	/// <summary>
	/// To modify <see cref="IDiagramElementParam.CanOptimize"/>.
	/// </summary>
	/// <param name="value">Value.</param>
	/// <returns>The diagram element parameter.</returns>
	public DiagramElementParam<T> SetCanOptimize(bool value = true)
	{
		CanOptimize = value;
		return this;
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		if (LoadHandler != null)
		{
			storage.SafeGetValue<SettingsStorage>(nameof(Value), s => Value = s == null ? default : LoadHandler(s), true);
		}
		else if (typeof(T).IsPersistable())
		{
			storage.SafeGetValue<SettingsStorage>(nameof(Value), s =>
			{
				if (s == null)
					Value = default;
				else
				{
					// 2025-03-27 remove few years later
					var type = s.GetValue<string>("type");

					if (type == "StockSharp.Algo.Candles.CandleSeries, StockSharp.Algo")
					{
						type = "StockSharp.Algo.Candles.CandleSeries, StockSharp.BusinessEntities";
						
						var copy = new SettingsStorage();

						copy.AddRange(s);
						copy.SetValue("type", type);

						s = copy;
					}

					var v = s.LoadEntire<IPersistable>();

#pragma warning disable CS0618 // Type or member is obsolete
					if (v is CandleSeries cs && typeof(T) == typeof(DataType))
						v = cs.ToDataType();
#pragma warning restore CS0618 // Type or member is obsolete

					Value = (T)v;
				}
			}, true);
		}
		else
			Value = storage.GetValue<T>(nameof(Value));
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		if (SaveHandler != null)
		{
			storage.SetValue(nameof(Value), SaveHandler(Value));
		}
		else if (Value is IPersistable pers)
		{
			if (Value.IsNull())
				return;

			storage.SetValue(nameof(Value), pers.SaveEntire(false));
		}
		else
			storage.SetValue(nameof(Value), Value);
	}

	/// <inheritdoc />
	public override string ToString() => $"{Name}: {Value}";
}