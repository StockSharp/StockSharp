namespace StockSharp.Algo.Import;

/// <summary>
/// Mapping value.
/// </summary>
public class FieldMappingValue : IPersistable
{
	/// <summary>
	/// File value.
	/// </summary>
	public string ValueFile { get; set; }

	/// <summary>
	/// S# value.
	/// </summary>
	public object ValueStockSharp { get; set; }

	void IPersistable.Load(SettingsStorage storage)
	{
		ValueFile = storage.GetValue<string>(nameof(ValueFile));

		try
		{
			ValueStockSharp = storage.GetValue<SettingsStorage>(nameof(ValueStockSharp))?.FromStorage();
		}
		catch (Exception)
		{
			// 2022-08-08 remove 1 year later
			ValueStockSharp = storage.GetValue<string>(nameof(ValueStockSharp));
		}
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(ValueFile), ValueFile);
		storage.SetValue(nameof(ValueStockSharp), ValueStockSharp?.ToStorage());
	}
}