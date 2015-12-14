#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.InteractiveBrokers.Native.InteractiveBrokers
File: RequestMessages.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.InteractiveBrokers.Native
{
	enum RequestMessages
	{
		SubscribeMarketData = 1,
		UnSubscribeMarketData = 2,
		
		RegisterOrder = 3,
		CancelOrder = 4,
		RequestOpenOrders = 5,
		RequestAccountData = 6,
		RequestTrades = 7,
		RequestIds = 8,
		RequestContractData = 9,
		
		SubscribeMarketDepth = 10,
		UnSubscribeMarketDepth = 11,
		
		SubscribeNewsBulletins = 12,
		UnSubscribeNewsBulletins = 13,
		
		SetServerLogLevel = 14,
		RequestAutoOpenOrders = 15,
		RequestAllOpenOrders = 16,
		RequestPortfolios = 17,
		
		RequestFinancialAdvisor = 18,
		ReplaceFinancialAdvisor = 19,

		SubscribeHistoricalData = 20,
		
		ExerciseOptions = 21,
		
		SubscribeScanner = 22,
		UnSubscribeScanner = 23,
		RequestScannerParameters = 24,
		
		UnSubscribeHistoricalData = 25,

		RequestCurrentTime = 49,

		SubscribeRealTimeCandles = 50,
		UnSubscribeRealTimeCandles = 51,

		SubscribeFundamentalData = 52,
		UnSubscribeFundamentalData = 53,

		SubscribeCalcImpliedVolatility = 54,
		SubscribeCalcOptionPrice = 55,
		UnSubscribeCalcImpliedVolatility = 56,
		UnSubscribeCalcOptionPrice = 57,

		RequestGlobalCancel = 58,
		
		SetMarketDataType = 59,

		SubscribePosition = 61,
		SubscribeAccountSummary = 62,
		UnSubscribeAccountSummary = 63,
		UnSubscribePosition = 64,

		VerifyRequest = 65,
		VerifyMessage = 66,

		QueryDisplayGroups = 67,
		SubscribeToGroupEvents = 68,
		UpdateDisplayGroup = 69,
		UnSubscribeFromGroupEvents = 70,

		StartApi = 71,
	}
}