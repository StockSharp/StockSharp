namespace StockSharp.ETrade
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using StockSharp.ETrade.Native;
	using StockSharp.Messages;
	using StockSharp.Localization;

	partial class ETradeMessageAdapter
	{
		/// <summary>
		/// Поддерживается ли торговой системой поиск инструментов.
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
