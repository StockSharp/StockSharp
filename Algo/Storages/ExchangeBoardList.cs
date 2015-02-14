namespace StockSharp.Algo.Storages
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// Класс для представления в виде списка биржевых площадок, хранящихся во внешнем хранилище.
	/// </summary>
	public class ExchangeBoardList : BaseStorageEntityList<ExchangeBoard>
	{
		/// <summary>
		/// Создать <see cref="ExchangeBoardList"/>.
		/// </summary>
		/// <param name="storage">Специальный интерфейс для прямого доступа к хранилищу.</param>
		public ExchangeBoardList(IStorage storage)
			: base(storage)
		{
		}

		/// <summary>
		/// Получить идентификаторы.
		/// </summary>
		/// <returns>Идентификаторы.</returns>
		public virtual IEnumerable<string> GetIds()
		{
			return this.Select(b => b.Code);
		}
	}
}