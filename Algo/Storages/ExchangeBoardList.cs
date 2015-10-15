namespace StockSharp.Algo.Storages
{
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Serialization;

	using StockSharp.BusinessEntities;

	/// <summary>
	/// The class for representation in the form of list of exchange sites, stored in the external storage.
	/// </summary>
	public class ExchangeBoardList : BaseStorageEntityList<ExchangeBoard>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="ExchangeBoardList"/>.
		/// </summary>
		/// <param name="storage">The special interface for direct access to the storage.</param>
		public ExchangeBoardList(IStorage storage)
			: base(storage)
		{
		}

		/// <summary>
		/// To get identifiers.
		/// </summary>
		/// <returns>Identifiers.</returns>
		public virtual IEnumerable<string> GetIds()
		{
			return this.Select(b => b.Code);
		}
	}
}