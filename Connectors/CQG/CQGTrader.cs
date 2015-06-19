namespace StockSharp.CQG
{
	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с системой CQG.
	/// </summary>
	public class CQGTrader : Connector
    {
		/// <summary>
		/// Создать <see cref="CQGTrader"/>.
		/// </summary>
		public CQGTrader()
		{
			CreateAssociatedSecurity = true;

			var adapter = new CQGMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(adapter.ToChannel(this));
		}
    }
}