namespace StockSharp.Alerts;

using Ecng.Configuration;

/// <summary>
/// Rule.
/// </summary>
public class AlertRule : IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="AlertRule"/>.
	/// </summary>
	public AlertRule()
	{
	}

	/// <summary>
	/// Message property, which will be made a comparison with the value of <see cref="Value"/> based on the criterion <see cref="Operator"/>.
	/// </summary>
	public AlertRuleField Field { get; set; }

	/// <summary>
	/// The criterion of comparison values <see cref="Value"/>.
	/// </summary>
	public ComparisonOperator Operator { get; set; }

	/// <summary>
	/// Comparison value.
	/// </summary>
	public object Value { get; set; }

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Load(SettingsStorage storage)
	{
		Field = storage.GetValue<SettingsStorage>(nameof(Field)).Load<AlertRuleField>();
		Operator = storage.GetValue<ComparisonOperator>(nameof(Operator));

		var value = storage.GetValue<string>(nameof(Value));

		if (storage.GetValue<bool>(nameof(Portfolio)))
			Value = ConfigManager.GetService<IPortfolioProvider>().LookupByPortfolioName(value);
		else
		{
			var valueType = Field.ValueType;

			Value = (valueType == typeof(SecurityId) || valueType == typeof(SecurityId?))
				? ConfigManager.GetService<ISecurityProvider>().LookupById(value)
				: value.To(valueType);
		}
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	public void Save(SettingsStorage storage)
	{
		storage.Set(nameof(Field), Field.Save());
		storage.Set(nameof(Operator), Operator);

		if (Value is Security security)
			storage.Set(nameof(Value), security.Id);
		else if (Value is Portfolio portfolio)
		{
			storage.Set(nameof(Portfolio), true);
			storage.Set(nameof(Value), portfolio.Name);
		}
		else
			storage.Set(nameof(Value), Value?.ToString());
	}
}