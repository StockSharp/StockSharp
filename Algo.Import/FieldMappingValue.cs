namespace StockSharp.Algo.Import;

/// <summary>
/// Mapping value.
/// </summary>
public class FieldMappingValue : IAsyncPersistable
{
	/// <summary>
	/// File value.
	/// </summary>
	public string ValueFile { get; set; }

	/// <summary>
	/// S# value.
	/// </summary>
	public object ValueStockSharp { get; set; }

	async Task IAsyncPersistable.LoadAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		ValueFile = storage.GetValue<string>(nameof(ValueFile));
		ValueStockSharp = storage.GetValue<SettingsStorage>(nameof(ValueStockSharp)) is { } valueStockSharpStorage ? await valueStockSharpStorage.FromStorageAsync(cancellationToken) : null;
	}

	async Task IAsyncPersistable.SaveAsync(SettingsStorage storage, CancellationToken cancellationToken)
	{
		storage.SetValue(nameof(ValueFile), ValueFile);
		storage.SetValue(nameof(ValueStockSharp), ValueStockSharp is null ? null : await ValueStockSharp.ToStorageAsync(false, cancellationToken));
	}
}