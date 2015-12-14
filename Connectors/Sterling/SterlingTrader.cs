#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Sterling.Sterling
File: SterlingTrader.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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

			Adapter.InnerAdapters.Add(adapter);
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