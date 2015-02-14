namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Класс для представления в виде списка портфелей, хранящихся во внешнем хранилище.
	/// </summary>
	public class PortfolioList : BaseStorageEntityList<Portfolio>
	{
		/// <summary>
		/// Создать <see cref="PortfolioList"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public PortfolioList(IStorage storage)
			: base(storage)
		{
		}
	}
}