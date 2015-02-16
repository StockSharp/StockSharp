using System.Collections.Generic;
using StockSharp.Algo;
using StockSharp.BusinessEntities;
using StockSharp.Messages;

namespace StockSharp.Sterling
{
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
			var sessionHolder = new SterlingSessionHolder(TransactionIdGenerator);

			TransactionAdapter = new SterlingMessageAdapter(MessageAdapterTypes.Transaction, sessionHolder);
			MarketDataAdapter = new SterlingMessageAdapter(MessageAdapterTypes.MarketData, sessionHolder);

			ApplyMessageProcessor(MessageDirections.In, true, true);
			ApplyMessageProcessor(MessageDirections.Out, true, true);
		}

		public void StartExport()
		{
			MarketDataAdapter.SendInMessage(new PortfolioLookupMessage { TransactionId = TransactionIdGenerator.GetNextId() });

			var exm = new ExecutionMessage {ExtensionInfo = new Dictionary<object, object>()};
		
			exm.ExtensionInfo.Add(new KeyValuePair<object, object>("GetMyTrades", null));
			exm.ExtensionInfo.Add(new KeyValuePair<object, object>("GetOrders", null));

			MarketDataAdapter.SendInMessage(exm);

			var posm = new PositionMessage { ExtensionInfo = new Dictionary<object, object>() };
			
			posm.ExtensionInfo.Add(new KeyValuePair<object, object>("GetPositions", null));

			MarketDataAdapter.SendInMessage(posm);
		}
    }
}