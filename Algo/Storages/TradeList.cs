namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Класс для представления в виде списка тиковых сделок, хранящихся во внешнем хранилище.
	/// </summary>
	public class TradeList : BaseStorageEntityList<Trade>
	{
		/// <summary>
		/// Создать <see cref="TradeList"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public TradeList(IStorage storage)
			: base(storage)
		{
		}
	}
}