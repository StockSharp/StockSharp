namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Класс для представления в виде списка новостей, хранящихся во внешнем хранилище.
	/// </summary>
	public class NewsList : BaseStorageEntityList<News>
	{
		/// <summary>
		/// Создать <see cref="NewsList"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public NewsList(IStorage storage)
			: base(storage)
		{
		}
	}
}