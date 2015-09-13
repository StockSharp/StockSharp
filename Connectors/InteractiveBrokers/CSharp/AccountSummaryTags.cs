/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBApi
{
    /**
     * @class AccountSummaryTags
     * @brief class containing all existing values being reported by EClientSocket::reqAccountSummary
     */
    public class AccountSummaryTags
    {
        public static readonly string AccountType = "AccountType";
        public static readonly string NetLiquidation = "NetLiquidation";
        public static readonly string TotalCashValue = "TotalCashValue";
        public static readonly string SettledCash = "SettledCash";
        public static readonly string AccruedCash = "AccruedCash";
        public static readonly string BuyingPower = "BuyingPower";
        public static readonly string EquityWithLoanValue = "EquityWithLoanValue";
        public static readonly string PreviousEquityWithLoanValue = "PreviousEquityWithLoanValue";
        public static readonly string GrossPositionValue = "GrossPositionValue";
        public static readonly string ReqTEquity = "ReqTEquity";
        public static readonly string ReqTMargin = "ReqTMargin";
        public static readonly string SMA = "SMA";
        public static readonly string InitMarginReq = "InitMarginReq";
        public static readonly string MaintMarginReq = "MaintMarginReq";
        public static readonly string AvailableFunds = "AvailableFunds";
        public static readonly string ExcessLiquidity = "ExcessLiquidity";
        public static readonly string Cushion = "Cushion";
        public static readonly string FullInitMarginReq = "FullInitMarginReq";
        public static readonly string FullMaintMarginReq = "FullMaintMarginReq";
        public static readonly string FullAvailableFunds = "FullAvailableFunds";
        public static readonly string FullExcessLiquidity = "FullExcessLiquidity";
        public static readonly string LookAheadNextChange = "LookAheadNextChange";
        public static readonly string LookAheadInitMarginReq = "LookAheadInitMarginReq";
        public static readonly string LookAheadMaintMarginReq = "LookAheadMaintMarginReq";
        public static readonly string LookAheadAvailableFunds = "LookAheadAvailableFunds";
        public static readonly string LookAheadExcessLiquidity = "LookAheadExcessLiquidity";
        public static readonly string HighestSeverity = "HighestSeverity";
        public static readonly string DayTradesRemaining = "DayTradesRemaining";
        public static readonly string Leverage = "Leverage";

        public static string GetAllTags()
        {
            return AccountType + "," + NetLiquidation + "," + TotalCashValue + "," + SettledCash + "," + AccruedCash + "," + BuyingPower + "," + EquityWithLoanValue + "," + PreviousEquityWithLoanValue + "," + GrossPositionValue + "," + ReqTEquity
                + "," + ReqTMargin + "," + SMA + "," + InitMarginReq + "," + MaintMarginReq + "," + AvailableFunds + "," + ExcessLiquidity + "," + Cushion + "," + FullInitMarginReq + "," + FullMaintMarginReq + "," + FullAvailableFunds + "," + FullExcessLiquidity
                + "," + LookAheadNextChange + "," + LookAheadInitMarginReq + "," + LookAheadMaintMarginReq + "," + LookAheadAvailableFunds + "," + LookAheadExcessLiquidity + "," + HighestSeverity + "," + DayTradesRemaining + "," + Leverage;
        }

    }
}
