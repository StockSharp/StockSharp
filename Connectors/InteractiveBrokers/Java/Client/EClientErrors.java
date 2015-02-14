/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;


public class EClientErrors {
    public static final int NO_VALID_ID = -1;
    public static final CodeMsgPair NOT_CONNECTED = new CodeMsgPair(504, "Not connected");
    public static final CodeMsgPair UPDATE_TWS = new CodeMsgPair(503, "The TWS is out of date and must be upgraded.");
    static final CodeMsgPair ALREADY_CONNECTED = new CodeMsgPair(501, "Already connected.");
    static final CodeMsgPair CONNECT_FAIL = new CodeMsgPair(502, "Couldn't connect to TWS.  Confirm that \"Enable ActiveX and Socket Clients\" is enabled on the TWS \"Configure->API\" menu.");
    static final CodeMsgPair FAIL_SEND = new CodeMsgPair(509, "Failed to send message - "); // generic message; all future messages should use this
    static final CodeMsgPair UNKNOWN_ID = new CodeMsgPair(505, "Fatal Error: Unknown message id.");
    static final CodeMsgPair FAIL_SEND_REQMKT = new CodeMsgPair(510, "Request Market Data Sending Error - ");
    static final CodeMsgPair FAIL_SEND_CANMKT = new CodeMsgPair(511, "Cancel Market Data Sending Error - ");
    static final CodeMsgPair FAIL_SEND_ORDER = new CodeMsgPair(512, "Order Sending Error - ");
    static final CodeMsgPair FAIL_SEND_ACCT = new CodeMsgPair(513, "Account Update Request Sending Error -");
    static final CodeMsgPair FAIL_SEND_EXEC = new CodeMsgPair(514, "Request For Executions Sending Error -");
    static final CodeMsgPair FAIL_SEND_CORDER = new CodeMsgPair(515, "Cancel Order Sending Error -");
    static final CodeMsgPair FAIL_SEND_OORDER = new CodeMsgPair(516, "Request Open Order Sending Error -");
    static final CodeMsgPair UNKNOWN_CONTRACT = new CodeMsgPair(517, "Unknown contract. Verify the contract details supplied.");
    static final CodeMsgPair FAIL_SEND_REQCONTRACT = new CodeMsgPair(518, "Request Contract Data Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQMKTDEPTH = new CodeMsgPair(519, "Request Market Depth Sending Error - ");
    static final CodeMsgPair FAIL_SEND_CANMKTDEPTH = new CodeMsgPair(520, "Cancel Market Depth Sending Error - ");
    static final CodeMsgPair FAIL_SEND_SERVER_LOG_LEVEL = new CodeMsgPair(521, "Set Server Log Level Sending Error - ");
    static final CodeMsgPair FAIL_SEND_FA_REQUEST = new CodeMsgPair(522, "FA Information Request Sending Error - ");
    static final CodeMsgPair FAIL_SEND_FA_REPLACE = new CodeMsgPair(523, "FA Information Replace Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQSCANNER = new CodeMsgPair(524, "Request Scanner Subscription Sending Error - ");
    static final CodeMsgPair FAIL_SEND_CANSCANNER = new CodeMsgPair(525, "Cancel Scanner Subscription Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQSCANNERPARAMETERS = new CodeMsgPair(526, "Request Scanner Parameter Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQHISTDATA = new CodeMsgPair(527, "Request Historical Data Sending Error - ");
    static final CodeMsgPair FAIL_SEND_CANHISTDATA = new CodeMsgPair(528, "Request Historical Data Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQRTBARS = new CodeMsgPair(529, "Request Real-time Bar Data Sending Error - ");
    static final CodeMsgPair FAIL_SEND_CANRTBARS = new CodeMsgPair(530, "Cancel Real-time Bar Data Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQCURRTIME = new CodeMsgPair(531, "Request Current Time Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQFUNDDATA = new CodeMsgPair(532, "Request Fundamental Data Sending Error - ");
    static final CodeMsgPair FAIL_SEND_CANFUNDDATA = new CodeMsgPair(533, "Cancel Fundamental Data Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQCALCIMPLIEDVOLAT = new CodeMsgPair(534, "Request Calculate Implied Volatility Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQCALCOPTIONPRICE = new CodeMsgPair(535, "Request Calculate Option Price Sending Error - ");
    static final CodeMsgPair FAIL_SEND_CANCALCIMPLIEDVOLAT = new CodeMsgPair(536, "Cancel Calculate Implied Volatility Sending Error - ");
    static final CodeMsgPair FAIL_SEND_CANCALCOPTIONPRICE = new CodeMsgPair(537, "Cancel Calculate Option Price Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQGLOBALCANCEL = new CodeMsgPair(538, "Request Global Cancel Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQMARKETDATATYPE = new CodeMsgPair(539, "Request Market Data Type Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQPOSITIONS = new CodeMsgPair(540, "Request Positions Sending Error - ");
    static final CodeMsgPair FAIL_SEND_CANPOSITIONS = new CodeMsgPair(541, "Cancel Positions Sending Error - ");
    static final CodeMsgPair FAIL_SEND_REQACCOUNTDATA = new CodeMsgPair(542, "Request Account Data Sending Error - ");
    static final CodeMsgPair FAIL_SEND_CANACCOUNTDATA = new CodeMsgPair(543, "Cancel Account Data Sending Error - ");
    static final CodeMsgPair FAIL_SEND_VERIFYREQUEST = new CodeMsgPair(544, "Verify Request Sending Error - ");
    static final CodeMsgPair FAIL_SEND_VERIFYMESSAGE = new CodeMsgPair(545, "Verify Message Sending Error - ");
    static final CodeMsgPair FAIL_SEND_QUERYDISPLAYGROUPS = new CodeMsgPair(546, "Query Display Groups Sending Error - ");
    static final CodeMsgPair FAIL_SEND_SUBSCRIBETOGROUPEVENTS = new CodeMsgPair(547, "Subscribe To Group Events Sending Error - ");
    static final CodeMsgPair FAIL_SEND_UPDATEDISPLAYGROUP = new CodeMsgPair(548, "Update Display Group Sending Error - ");
    static final CodeMsgPair FAIL_SEND_UNSUBSCRIBEFROMGROUPEVENTS = new CodeMsgPair(549, "Unsubscribe From Group Events Sending Error - ");
    static final CodeMsgPair FAIL_SEND_STARTAPI = new CodeMsgPair(550, "Start API Sending Error - ");

    public EClientErrors() {
    }

    static public class CodeMsgPair {

        // members vars
        int m_errorCode;
        String m_errorMsg;

        // Get/Set methods
        public int code()    { return m_errorCode; }
        public String msg()  { return m_errorMsg; }

        /** Constructor */
        public CodeMsgPair(int i, String errString) {
            m_errorCode = i;
            m_errorMsg = errString;
        }
    }
}
