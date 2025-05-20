namespace StockSharp.Algo.Statistics;

/// <summary>
/// The interface, describing statistic parameter.
/// </summary>
public interface IStatisticParameter : IPersistable, INotifyPropertyChanged
{
	/// <summary>
	/// Parameter name.
	/// </summary>
	string Name { get; }

	/// <summary>
	/// Type.
	/// </summary>
	StatisticParameterTypes Type { get; }

	/// <summary>
	/// <see cref="Value"/> type.
	/// </summary>
	Type ValueType { get; }

	/// <summary>
	/// The current value of the parameter.
	/// </summary>
	object Value { get; }

	/// <summary>
	/// The displayed parameter name.
	/// </summary>
	string DisplayName { get; }

	/// <summary>
	/// The parameter description.
	/// </summary>
	string Description { get; }

	/// <summary>
	/// Category.
	/// </summary>
	string Category { get; }

	/// <summary>
	/// Order.
	/// </summary>
	int Order { get; }

	/// <summary>
	/// To reset the parameter value.
	/// </summary>
	void Reset();
}

/// <summary>
/// The interface, describing statistic parameter with initial value.
/// </summary>
public interface IBeginValueStatisticParameter
{
	/// <summary>
	/// The initial value of the parameter.
	/// </summary>
	decimal BeginValue { get; set; }	
}

/// <summary>
/// The interface, describing statistic parameter with risk-free rate.
/// </summary>
public interface IRiskFreeRateStatisticParameter
{
	/// <summary>
	/// Annual risk-free rate (e.g., 0.03 = 3%).
	/// </summary>
	decimal RiskFreeRate { get; set; }
}

/// <summary>
/// The interface, describing statistic parameter.
/// </summary>
/// <typeparam name="TValue">The type of the parameter value.</typeparam>
public interface IStatisticParameter<TValue> : IStatisticParameter
{
	/// <summary>
	/// The current value of the parameter.
	/// </summary>
	new TValue Value { get; }
}

/// <summary>
/// The base statistics parameter.
/// </summary>
/// <typeparam name="TValue">The type of the parameter value.</typeparam>
public abstract class BaseStatisticParameter<TValue> : NotifiableObject, IStatisticParameter<TValue>
	where TValue : IComparable<TValue>
{
	/// <summary>
	/// Initialize <see cref="BaseStatisticParameter{T}"/>.
	/// </summary>
	/// <param name="type"><see cref="IStatisticParameter.Type"/></param>
	protected BaseStatisticParameter(StatisticParameterTypes type)
	{
		Type = type;

		var type2 = GetType();
		Name = type2.Name.Remove("Parameter");

		ChangeNameAndDescription();
		Order = type2.GetAttribute<DisplayAttribute>()?.Order ?? default;

		LocalizedStrings.ActiveLanguageChanged += ChangeNameAndDescription;
	}

	private void ChangeNameAndDescription()
	{
		var type2 = GetType();

		DisplayName = type2.GetDisplayName(GetReadableName(Name));
		Description = type2.GetDescription(DisplayName);
		Category = type2.GetCategory();

		NotifyChanged(nameof(DisplayName));
		NotifyChanged(nameof(Description));
		NotifyChanged(nameof(Category));
	}

	/// <inheritdoc />
	public string Name { get; }

	/// <inheritdoc />
	public StatisticParameterTypes Type { get; }

	/// <inheritdoc />
	public string DisplayName { get; private set; }

	/// <inheritdoc />
	public string Description { get; private set; }

	/// <inheritdoc />
	public string Category { get; private set; }

	/// <inheritdoc />
	public int Order { get; }

	private TValue _value;

	/// <inheritdoc />
	public virtual TValue Value
	{
		get => _value;
		protected set
		{
			if (_value.CompareTo(value) == 0)
				return;

			_value = value;
			this.Notify(nameof(Value));
		}
	}

	private static string GetReadableName(string name)
	{
		var index = 1;

		while (index < (name.Length - 1))
		{
			if (char.IsUpper(name[index]))
			{
				name = name.Insert(index, " ");
				index += 2;
			}
			else
				index++;
		}

		return name;
	}

	object IStatisticParameter.Value => Value;

	/// <inheritdoc />
	public Type ValueType => typeof(TValue);

	/// <inheritdoc />
	public virtual void Reset() => Value = default;

	/// <summary>
	/// To load the state of statistic parameter.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		Value = storage.GetValue(nameof(Value), default(TValue));
	}

	/// <summary>
	/// To save the state of statistic parameter.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(Value), Value);
	}
}