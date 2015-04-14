namespace StockSharp.Sterling
{
	using System.Collections.Generic;

	using StockSharp.Algo;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Реализация интерфейса <see cref="IConnector"/> для взаимодействия с терминалом Sterling.
	/// </summary>
	public class SterlingTrader : Connector
    {
		/// <summary>
		/// Создать <see cref="SterlingTrader"/>.
		/// </summary>
		public SterlingTrader()
		{
			var adapter = new SterlingMessageAdapter(TransactionIdGenerator);

			Adapter.InnerAdapters.Add(adapter.ToChannel(this));
		}

		public void StartExport()
		{
			SendInMessage(new PortfolioLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });

			var exm = new ExecutionMessage {ExtensionInfo = new Dictionary<object, object>()};
		
			exm.ExtensionInfo.Add(new KeyValuePair<object, object>("GetMyTrades", null));
			exm.ExtensionInfo.Add(new KeyValuePair<object, object>("GetOrders", null));

			SendInMessage(exm);

			var posm = new PositionMessage { ExtensionInfo = new Dictionary<object, object>() };
			
			posm.ExtensionInfo.Add(new KeyValuePair<object, object>("GetPositions", null));

			SendInMessage(posm);
		}
    }
}