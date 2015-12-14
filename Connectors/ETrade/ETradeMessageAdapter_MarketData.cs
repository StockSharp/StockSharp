#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.ETrade.ETrade
File: ETradeMessageAdapter_MarketData.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.ETrade
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.ETrade.Native;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The messages adapter for ETrade.
	/// </summary>
	partial class ETradeMessageAdapter
	{
		/// <summary>
		/// Gets a value indicating whether the connector supports security lookup.
		/// </summary>
		protected override bool IsSupportNativeSecurityLookup
		{
			get { return true; }
		}

		/// <summary>Коллбэк результата поиска инструментов.</summary>
		/// <param name="lookupTransId">Номер транзакции операции Lookup.</param>
		/// <param name="data">Список инструментов, удовлетворяющих условию поиска.</param>
		/// <param name="ex">Ошибка поиска.</param>
		private void ClientOnProductLookupResult(long lookupTransId, IEnumerable<ProductInfo> data, Exception ex)
		{
			if (ex != null)
			{
				SendOutError(new ETradeException(LocalizedStrings.Str3363, ex));

				if (lookupTransId > 0)
					SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = lookupTransId });
				
				return;
			}

			foreach(var info in data.Where(info => info.securityType == "EQ"))
			{
				var secId = new SecurityId
				{
					SecurityCode = info.symbol,
					BoardCode = AssociatedBoardCode,
				};

				var msg = new SecurityMessage
				{
					SecurityId = secId,
					Name = info.companyName,
					ShortName = info.companyName,
					SecurityType = SecurityTypes.Stock,
					PriceStep = 0.01m,
					Currency = CurrencyTypes.USD,
				};

				SendOutMessage(msg);
			}

			if (lookupTransId > 0)
				SendOutMessage(new SecurityLookupResultMessage { OriginalTransactionId = lookupTransId });
		}
	}
}
