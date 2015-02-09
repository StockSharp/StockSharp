namespace StockSharp.Algo.Storages
{
	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Класс для представления в виде списка бирж, хранящихся во внешнем хранилище.
	/// </summary>
	public class ExchangeList : BaseStorageEntityList<Exchange>
	{
		/// <summary>
		/// Создать <see cref="StockSharp.Algo.Storages.ExchangeList"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public ExchangeList(IStorage storage)
			: base(storage)
		{
		}
	}
}
