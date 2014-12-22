namespace StockSharp.Hydra.Core
{
	using Ecng.Serialization;

	using StockSharp.Algo.Storages;

	/// <summary>
	/// Коллекция настроек для <see cref="IHydraTask"/>.
	/// </summary>
	public class HydraTaskSettingsList : BaseStorageEntityList<HydraTaskSettings>
	{
		/// <summary>
		/// Создать <see cref="HydraTaskSettingsList"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public HydraTaskSettingsList(IStorage storage)
			: base(storage)
		{
		}
	}
}