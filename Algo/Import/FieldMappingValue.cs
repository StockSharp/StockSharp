namespace StockSharp.Algo.Import
{
	using Ecng.Serialization;

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
			ValueStockSharp = storage.GetValue<object>(nameof(ValueStockSharp));
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(ValueFile), ValueFile);
			storage.SetValue(nameof(ValueStockSharp), ValueStockSharp);
		}
	}
}