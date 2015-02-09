namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Класс для представления в виде списка заявок, хранящихся во внешнем хранилище.
	/// </summary>
	public class OrderList : BaseStorageEntityList<Order>
	{
		/// <summary>
		/// Создать <see cref="OrderList"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public OrderList(IStorage storage)
			: base(storage)
		{
		}
	}
}