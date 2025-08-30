namespace StockSharp.Alerts;

using System.Collections;

using Ecng.Reflection;

/// <summary>
/// Tracking field info.
/// </summary>
public class AlertRuleField : Equatable<AlertRuleField>, IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AlertRuleField"/>.
	/// </summary>
	public AlertRuleField()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AlertRuleField"/>.
	/// </summary>
	/// <param name="property">Tracking property.</param>
	/// <param name="extraField">Extra info for <see cref="Property"/>.</param>
	public AlertRuleField(PropertyInfo property, object extraField = null)
	{
		Property = property ?? throw new ArgumentNullException(nameof(property));
		ExtraField = extraField;

		UpdateState();
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="AlertRuleField"/>.
	/// </summary>
	/// <param name="displayName">Display name.</param>
	public AlertRuleField(string displayName)
	{
		if (displayName.IsEmpty())
			throw new ArgumentNullException(nameof(displayName));

		DisplayName = displayName;
	}

	/// <summary>
	/// Display name.
	/// </summary>
	public string DisplayName { get; private set; }

	/// <summary>
	/// Tracking property.
	/// </summary>
	public PropertyInfo Property { get; private set; }

	/// <summary>
	/// Extra info for <see cref="Property"/>.
	/// </summary>
	public object ExtraField { get; private set; }

	/// <summary>
	/// Value type.
	/// </summary>
	public Type ValueType { get; private set; }

	private const string _propKey = nameof(Property) + "_new";

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		if (storage.ContainsKey(_propKey))
			Property = storage.GetValue<SettingsStorage>(_propKey).ToMember<PropertyInfo>();
		else
		{
			var parts = storage.GetValue<string>(nameof(Property)).Split('/');

			if (parts.Length > 1)
				Property = parts[0].To<Type>().GetMember<PropertyInfo>(parts[1]);
		}

		ExtraField = storage.GetValue<SettingsStorage>(nameof(ExtraField))?.FromStorage();

		UpdateState();
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.SetValue(_propKey, Property.ToStorage(false));
		storage.SetValue(nameof(ExtraField), ExtraField?.ToStorage());
	}

	private void UpdateState()
	{
		if (ExtraField == null)
		{
			DisplayName = Property.GetDisplayName(Property.Name);
			ValueType = Property.PropertyType;
		}
		else
		{
			DisplayName = ExtraField.GetDisplayName();
			
			ValueType = typeof(decimal);

			if (ExtraField is Level1Fields l1)
			{
				ValueType = l1.ToType();
			}
		}
	}

	/// <summary>
	/// Invoke field.
	/// </summary>
	/// <param name="message">Message.</param>
	/// <returns>Value.</returns>
	public object Invoke(Message message)
	{
		if (message == null)
			throw new ArgumentNullException(nameof(message));

		var value = Property.GetValue(message, null);

		if (ExtraField != null)
			value = ((IDictionary)value)[ExtraField];

		return value;
	}

	/// <inheritdoc />
	public override string ToString()
	{
		if (ExtraField == null)
			return $"{Property?.Name}";
		else
			return $"{Property?.Name}[{ExtraField}]";
	}

	/// <summary>Serves as a hash function for a particular type. </summary>
	/// <returns>A hash code for the current <see cref="T:System.Object" />.</returns>
	public override int GetHashCode()
	{
		return (Property?.GetHashCode() ?? 0) ^ (ExtraField?.GetHashCode() ?? 0);
	}

	/// <summary>
	/// Compare <see cref="AlertRuleField"/> on the equivalence.
	/// </summary>
	/// <param name="other">Another value with which to compare.</param>
	/// <returns><see langword="true" />, if the specified object is equal to the current object, otherwise, <see langword="false" />.</returns>
	protected override bool OnEquals(AlertRuleField other)
	{
		return Property == other.Property && Equals(ExtraField, other.ExtraField);
	}

	/// <summary>
	/// Create a copy of <see cref="AlertRuleField"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override AlertRuleField Clone()
	{
		return PersistableHelper.Clone(this);
	}
}