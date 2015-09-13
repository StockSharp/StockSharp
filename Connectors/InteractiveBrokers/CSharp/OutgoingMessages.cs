/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBApi
{
    public enum OutgoingMessages
    {
        RequestMarketData = 1,
        CancelMarketData = 2,
        PlaceOrder = 3,
        CancelOrder = 4,
        RequestOpenOrders = 5,
        RequestAccountData = 6,
        RequestExecutions = 7,
        RequestIds = 8,
        RequestContractData = 9,
        RequestMarketDepth = 10,
        CancelMarketDepth = 11,
        RequestNewsBulletins = 12,
        CancelNewsBulletin = 13,
        ChangeServerLog = 14,
        RequestAutoOpenOrders = 15,
        RequestAllOpenOrders = 16,
        RequestManagedAccounts = 17,
        RequestFA = 18,
        ReplaceFA = 19,
        RequestHistoricalData = 20,
        ExerciseOptions = 21,
        RequestScannerSubscription = 22,
        CancelScannerSubscription = 23,
        RequestScannerParameters = 24,
        CancelHistoricalData = 25,
        RequestCurrentTime = 49,
        RequestRealTimeBars = 50,
        CancelRealTimeBars = 51,
        RequestFundamentalData = 52,
        CancelFundamentalData = 53,
        ReqCalcImpliedVolat = 54,
        ReqCalcOptionPrice = 55,
        CancelImpliedVolatility = 56,
        CancelOptionPrice = 57,
        RequestGlobalCancel = 58,
        RequestMarketDataType = 59,
        RequestPositions = 61,
        RequestAccountSummary = 62,
        CancelAccountSummary = 63,
        CancelPositions = 64,
        VerifyRequest = 65,
        VerifyMessage = 66,
        QueryDisplayGroups = 67,
        SubscribeToGroupEvents = 68,
        UpdateDisplayGroup = 69,
        UnsubscribeFromGroupEvents = 70,
        StartApi = 71,
        VerifyAndAuthRequest = 72,
        VerifyAndAuthMessage = 73,
    }
}
