namespace StockSharp.Algo;

/// <summary>
/// <see cref="OrderTypes.Conditional"/> settings.
/// </summary>
public class OrderConditionSettings : IPersistable
{
	/// <summary>
	/// <see cref="IMessageAdapter"/> type.
	/// </summary>
	public Type AdapterType { get; set; }

	/// <summary>
	/// Condition parameters.
	/// </summary>
	public IDictionary<string, object> Parameters { get; }

	/// <summary>
	/// Initializes a new instance of the <see cref="OrderConditionSettings"/>.
	/// </summary>
	public OrderConditionSettings()
	{
		Parameters = new Dictionary<string, object>();
	}

	void IPersistable.Load(SettingsStorage storage)
	{
		AdapterType = storage.GetValue<Type>(nameof(AdapterType));

		var paramerters = storage.GetValue<SettingsStorage>(nameof(Parameters));

		foreach (var pair in paramerters)
			Parameters[pair.Key] = pair.Value;
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(AdapterType), AdapterType?.GetTypeName(false));

		var paramerters = new SettingsStorage();

		foreach (var pair in Parameters)
			paramerters.SetValue(pair.Key, pair.Value);

		storage.SetValue(nameof(Parameters), paramerters);
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		return AdapterType != null
			? $"[{Parameters.Select(p => $"[{p.Key}: {p.Value}]").JoinCommaSpace()}]"
			: string.Empty;
	}
}