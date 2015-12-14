#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.InteractiveBrokers.Native.InteractiveBrokers
File: ResponseMessages.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.InteractiveBrokers.Native
{
	internal enum ResponseMessages
	{
		/// <summary>
		/// Undefined Incoming Message.
		/// </summary>
		Undefined = 0,

		Error = -1,

		TickPrice = 1,
		TickVolume = 2,
		OrderStatus = 3,
		ErrorMessage = 4,
		OpenOrder = 5,
		Portfolio = 6,
		PortfolioPosition = 7,
		PortfolioUpdateTime = 8,
		NextOrderId = 9,
		SecurityInfo = 10,
		MyTrade = 11,
		MarketDepth = 12,
		MarketDepthL2 = 13,
		NewsBulletins = 14,
		ManagedAccounts = 15,
		FinancialAdvice = 16,
		HistoricalData = 17,
		BondInfo = 18,
		ScannerParameters = 19,
		ScannerData = 20,
		TickOptionComputation = 21,
		TickGeneric = 45,
		TickString = 46,
		TickEfp = 47,
		CurrentTime = 49,
		RealTimeBars = 50,
		FundamentalData = 51,
		SecurityInfoEnd = 52,
		OpenOrderEnd = 53,
		AccountDownloadEnd = 54,
		MyTradeEnd = 55,
		DeltaNuetralValidation = 56,
		TickSnapshotEnd = 57,
		MarketDataType = 58,
		CommissionReport = 59,
		Position = 61,
		PositionEnd = 62,
		AccountSummary = 63,
		AccountSummaryEnd = 64,
		VerifyMessageApi = 65,
		VerifyCompleted = 66,
		DisplayGroupList = 67,
		DisplayGroupUpdated = 68,
	}
}