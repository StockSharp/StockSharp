namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Класс для представления в виде списка заявок с ошибками, хранящихся во внешнем хранилище.
	/// </summary>
	public class OrderFailList : BaseStorageEntityList<OrderFail>
	{
		/// <summary>
		/// Создать <see cref="OrderFailList"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public OrderFailList(IStorage storage)
			: base(storage)
		{
		}
	}
}