namespace StockSharp.CQG
{
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface <see cref="IConnector"/> implementation which provides a connection to the CQG.
	/// </summary>
	[Icon("CQG_logo.png")]
	public class CQGTrader : Connector
    {
		/// <summary>
		/// Initializes a new instance of the <see cref="CQGTrader"/>.
		/// </summary>
		public CQGTrader()
		{
			CreateAssociatedSecurity = true;

			var adapter = new CQGMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(adapter);
		}
    }
}