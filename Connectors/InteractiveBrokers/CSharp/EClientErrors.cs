/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBApi
{
    public class EClientErrors
    {
        public static readonly CodeMsgPair AlreadyConnected = new CodeMsgPair(501, "Already Connected.");
        public static readonly CodeMsgPair CONNECT_FAIL = new CodeMsgPair(502, "Couldn't connect to TWS.  Confirm that \"Enable ActiveX and Socket Clients\" is enabled on the TWS \"Configure->API\" menu.");
        public static readonly CodeMsgPair UPDATE_TWS = new CodeMsgPair(503, "The TWS is out of date and must be upgraded.");
        public static readonly CodeMsgPair NOT_CONNECTED = new CodeMsgPair(504, "Not connected");
        public static readonly CodeMsgPair UNKNOWN_ID = new CodeMsgPair(505, "Fatal Error: Unknown message id.");
        public static readonly CodeMsgPair FAIL_SEND_REQMKT = new CodeMsgPair(510, "Request Market Data Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_CANMKT = new CodeMsgPair(511, "Cancel Market Data Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_ORDER = new CodeMsgPair(512, "Order Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_ACCT = new CodeMsgPair(513, "Account Update Request Sending Error -");
        public static readonly CodeMsgPair FAIL_SEND_EXEC = new CodeMsgPair(514, "Request For Executions Sending Error -");
        public static readonly CodeMsgPair FAIL_SEND_CORDER = new CodeMsgPair(515, "Cancel Order Sending Error -");
        public static readonly CodeMsgPair FAIL_SEND_OORDER = new CodeMsgPair(516, "Request Open Order Sending Error -");
        public static readonly CodeMsgPair UNKNOWN_CONTRACT = new CodeMsgPair(517, "Unknown contract. Verify the contract details supplied.");
        public static readonly CodeMsgPair FAIL_SEND_REQCONTRACT = new CodeMsgPair(518, "Request Contract Data Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQMKTDEPTH = new CodeMsgPair(519, "Request Market Depth Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_CANMKTDEPTH = new CodeMsgPair(520, "Cancel Market Depth Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_SERVER_LOG_LEVEL = new CodeMsgPair(521, "Set Server Log Level Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_FA_REQUEST = new CodeMsgPair(522, "FA Information Request Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_FA_REPLACE = new CodeMsgPair(523, "FA Information Replace Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQSCANNER = new CodeMsgPair(524, "Request Scanner Subscription Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_CANSCANNER = new CodeMsgPair(525, "Cancel Scanner Subscription Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQSCANNERPARAMETERS = new CodeMsgPair(526, "Request Scanner Parameter Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQHISTDATA = new CodeMsgPair(527, "Request Historical Data Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_CANHISTDATA = new CodeMsgPair(528, "Request Historical Data Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQRTBARS = new CodeMsgPair(529, "Request Real-time Bar Data Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_CANRTBARS = new CodeMsgPair(530, "Cancel Real-time Bar Data Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQCURRTIME = new CodeMsgPair(531, "Request Current Time Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQFUNDDATA = new CodeMsgPair(532, "Request Fundamental Data Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_CANFUNDDATA = new CodeMsgPair(533, "Cancel Fundamental Data Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQCALCIMPLIEDVOLAT = new CodeMsgPair(534, "Request Calculate Implied Volatility Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQCALCOPTIONPRICE = new CodeMsgPair(535, "Request Calculate Option Price Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_CANCALCIMPLIEDVOLAT = new CodeMsgPair(536, "Cancel Calculate Implied Volatility Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_CANCALCOPTIONPRICE = new CodeMsgPair(537, "Cancel Calculate Option Price Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQGLOBALCANCEL = new CodeMsgPair(538, "Request Global Cancel Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQMARKETDATATYPE = new CodeMsgPair(539, "Request Market Data Type Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQPOSITIONS = new CodeMsgPair(540, "Request Positions Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_CANPOSITIONS = new CodeMsgPair(541, "Cancel Positions Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_REQACCOUNTDATA = new CodeMsgPair(542, "Request Account Data Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_CANACCOUNTDATA = new CodeMsgPair(543, "Cancel Account Data Sending Error - ");

        public static readonly CodeMsgPair FAIL_SEND_VERIFYREQUEST = new CodeMsgPair(544, "Verify Request Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_VERIFYMESSAGE = new CodeMsgPair(545, "Verify Message Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_QUERYDISPLAYGROUPS = new CodeMsgPair(546, "Query Display Groups Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_SUBSCRIBETOGROUPEVENTS = new CodeMsgPair(547, "Subscribe To Group Events Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_UPDATEDISPLAYGROUP = new CodeMsgPair(548, "Update Display Group Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_UNSUBSCRIBEFROMGROUPEVENTS = new CodeMsgPair(549, "Unsubscribe From Group Events Sending Error - ");
        public static readonly CodeMsgPair BAD_LENGTH = new CodeMsgPair(507, "Bad message length");
        public static readonly CodeMsgPair BAD_MESSAGE = new CodeMsgPair(508, "Bad message");
        public static readonly CodeMsgPair UNSUPPORTED_VERSION = new CodeMsgPair(506, "Unsupported version");
    
        public static readonly CodeMsgPair FAIL_SEND_VERIFYANDAUTHREQUEST = new CodeMsgPair(551, "Verify And Auth Request Sending Error - ");
        public static readonly CodeMsgPair FAIL_SEND_VERIFYANDAUTHMESSAGE = new CodeMsgPair(552, "Verify And Auth Message Sending Error - ");

        public static readonly CodeMsgPair FAIL_GENERIC = new CodeMsgPair(-1, "Specific error message needs to be given for these requests! ");
    
    }

    public class CodeMsgPair
    {
        private int code;
        private string message;

        public CodeMsgPair(int code, string message)
        {
            this.code = code;
            this.message = message;
        }

        public int Code
        {
            get { return code; } 
        }

        public string Message
        {
            get { return message; }
        }
    }

}
