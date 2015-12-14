#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.InteractiveBrokers.Native.InteractiveBrokers
File: AccountSummaryTag.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.InteractiveBrokers.Native
{
	enum AccountSummaryTag
	{
		AccountType,

		// balances
		NetLiquidation,
		TotalCashValue,					// Total cash including futures pnl
		SettledCash,					// For cash accounts, this is the same as TotalCashValue
		AccruedCash,					// Net accrued interest
		BuyingPower,					// The maximum amount of marginable US stocks the account can buy
		EquityWithLoanValue,			// Cash + stocks + bonds + mutual funds
		PreviousEquityWithLoanValue,
		GrossPositionValue,				// The sum of the absolute value of all stock and equity option positions
		RegTEquity,
		RegTMargin,
		SMA,							// Special Memorandum Account

		// current margin
		InitMarginReq,
		MaintMarginReq,
		AvailableFunds,
		ExcessLiquidity,
		Cushion,						// Excess liquidity as a percentage of net liquidation value

		// overnight margin
		FullInitMarginReq,
		FullMaintMarginReq,
		FullAvailableFunds,
		FullExcessLiquidity,

		// look-ahead margin
		LookAheadNextChange,			// Time when look-ahead values take effect
		LookAheadInitMarginReq,
		LookAheadMaintMarginReq,
		LookAheadAvailableFunds,
		LookAheadExcessLiquidity,

		// misc
		HighestSeverity,				// A measure of how close the account is to liquidation
		DayTradesRemaining,				// The Number of Open/Close trades one could do before Pattern Day Trading is detected; a value of "-1" means user can do unlimited day trades.
		Leverage,						// GrossPositionValue / NetLiquidation
	}
}