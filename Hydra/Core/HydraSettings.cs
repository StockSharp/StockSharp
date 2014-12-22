namespace StockSharp.Hydra.Core
{
	using Ecng.Serialization;

	/// <summary>
	/// Настройки.
	/// </summary>
	public class HydraSettings
	{
		/// <summary>
		/// Создать <see cref="HydraSettings"/>.
		/// </summary>
		public HydraSettings()
		{
		}

		/// <summary>
		/// Имя.
		/// </summary>
		[Identity]
		public string Name { get; set; }

		/// <summary>
		/// Значение.
		/// </summary>
		public string Value { get; set; }
	}
}