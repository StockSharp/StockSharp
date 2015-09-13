/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBApi
{
    public class IncomingMessage
    {
        public const int NotValid = -1;
        public const int TickPrice = 1;
        public const int TickSize = 2;
        public const int OrderStatus = 3;
        public const int Error = 4;
        public const int OpenOrder = 5;
        public const int AccountValue = 6;
        public const int PortfolioValue = 7;
        public const int AccountUpdateTime = 8;
        public const int NextValidId = 9;
        public const int ContractData = 10;
        public const int ExecutionData = 11;
        public const int MarketDepth = 12;
        public const int MarketDepthL2 = 13;
        public const int NewsBulletins = 14;
        public const int ManagedAccounts = 15;
        public const int ReceiveFA = 16;
        public const int HistoricalData = 17;
        public const int BondContractData = 18;
        public const int ScannerParameters = 19;
        public const int ScannerData = 20;
        public const int TickOptionComputation = 21;
        public const int TickGeneric = 45;
        public const int Tickstring = 46;
        public const int TickEFP = 47;//TICK EFP 47
        public const int CurrentTime = 49;
        public const int RealTimeBars = 50;
        public const int FundamentalData = 51;
        public const int ContractDataEnd = 52;
        public const int OpenOrderEnd = 53;
        public const int AccountDownloadEnd = 54;
        public const int ExecutionDataEnd = 55;
        public const int DeltaNeutralValidation = 56;
        public const int TickSnapshotEnd = 57;
        public const int MarketDataType = 58;
        public const int CommissionsReport = 59;
        public const int Position = 61;
        public const int PositionEnd = 62;
        public const int AccountSummary = 63;
        public const int AccountSummaryEnd = 64;
        public const int VerifyMessageApi = 65;
        public const int VerifyCompleted = 66;
        public const int DisplayGroupList = 67;
        public const int DisplayGroupUpdated = 68;
        public const int VerifyAndAuthMessageApi = 69;
        public const int VerifyAndAuthCompleted = 70;
    }
}
