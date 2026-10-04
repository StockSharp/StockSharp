namespace StockSharp.Algo;

/// <summary>
/// <see cref="OrderTypes.Conditional"/> settings.
/// </summary>
public class OrderConditionSettings : IAsyncPersistable
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

	Task IAsyncPersistable.LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		AdapterType = storage.GetValue<Type>(nameof(AdapterType));

		var paramerters = storage.GetValue<SettingsStorage>(nameof(Parameters));

		foreach (var pair in paramerters)
			Parameters[pair.Key] = pair.Value;

		return Task.CompletedTask;
	}

	Task IAsyncPersistable.SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage.SetValue(nameof(AdapterType), AdapterType?.GetTypeName(false));

		var paramerters = new SettingsStorage();

		foreach (var pair in Parameters)
			paramerters.SetValue(pair.Key, pair.Value);

		storage.SetValue(nameof(Parameters), paramerters);

		return Task.CompletedTask;
	}

	/// <inheritdoc/>
	public override string ToString()
	{
		return AdapterType != null
			? $"[{Parameters.Select(p => $"[{p.Key}: {p.Value}]").JoinCommaSpace()}]"
			: string.Empty;
	}
}