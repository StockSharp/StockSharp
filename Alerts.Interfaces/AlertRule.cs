namespace StockSharp.Alerts;

/// <summary>
/// Rule.
/// </summary>
public class AlertRule : IAsyncPersistable
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

	private object _value;

	/// <summary>
	/// Comparison value, in the form a message carries it: an instrument is kept as its <see cref="SecurityId"/>,
	/// a portfolio as its name.
	/// </summary>
	public object Value
	{
		get => _value;
		set => _value = value switch
		{
			Security security => security.ToSecurityId(),
			Portfolio portfolio => portfolio.Name,
			_ => value,
		};
	}

	/// <summary>
	/// Load settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public async Task LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		Field = await storage.GetValue<SettingsStorage>(nameof(Field)).LoadAsync<AlertRuleField>(cancellationToken);
		Operator = storage.GetValue<ComparisonOperator>(nameof(Operator));

		var value = storage.GetValue<string>(nameof(Value));
		var valueType = Field.ValueType;

		Value = valueType == typeof(SecurityId) || valueType == typeof(SecurityId?)
			? value?.ToSecurityId()
			: value.To(valueType);
	}

	/// <summary>
	/// Save settings.
	/// </summary>
	/// <param name="storage">Settings storage.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="Task"/></returns>
	public async Task SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage.Set(nameof(Field), await Field.SaveAsync(cancellationToken));
		storage.Set(nameof(Operator), Operator);

		storage.Set(nameof(Value), Value is SecurityId securityId ? securityId.ToStringId() : Value?.ToString());
	}
}
