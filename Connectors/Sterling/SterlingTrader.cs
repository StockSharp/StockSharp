namespace StockSharp.Sterling
{
	using Ecng.ComponentModel;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;

	/// <summary>
	/// The interface <see cref="IConnector"/> implementation which provides a connection to the Sterling.
	/// </summary>
	[Icon("Sterling_logo.png")]
	public class SterlingTrader : Connector
    {
		/// <summary>
		/// Initializes a new instance of the <see cref="SterlingTrader"/>.
		/// </summary>
		public SterlingTrader()
		{
			CreateAssociatedSecurity = true;

			var adapter = new SterlingMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(adapter.ToChannel(this));
		}

		//public void StartExport()
		//{
		//	SendInMessage(new PortfolioLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });

		//	var exm = new ExecutionMessage {ExtensionInfo = new Dictionary<object, object>()};
		
		//	exm.ExtensionInfo.Add(new KeyValuePair<object, object>("GetMyTrades", null));
		//	exm.ExtensionInfo.Add(new KeyValuePair<object, object>("GetOrders", null));

		//	SendInMessage(exm);

		//	var posm = new PositionMessage { ExtensionInfo = new Dictionary<object, object>() };
			
		//	posm.ExtensionInfo.Add(new KeyValuePair<object, object>("GetPositions", null));

		//	SendInMessage(posm);
		//}
    }
}