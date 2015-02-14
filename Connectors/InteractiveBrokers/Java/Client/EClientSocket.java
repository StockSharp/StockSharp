/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

import java.io.DataInputStream;
import java.io.DataOutputStream;
import java.io.FilterOutputStream;
import java.io.IOException;
import java.net.Socket;
import java.util.List;
import java.util.Vector;

public class EClientSocket {

    // Client version history
    //
    // 	6 = Added parentId to orderStatus
    // 	7 = The new execDetails event returned for an order filled status and reqExecDetails
    //     Also market depth is available.
    // 	8 = Added lastFillPrice to orderStatus() event and permId to execution details
    //  9 = Added 'averageCost', 'unrealizedPNL', and 'unrealizedPNL' to updatePortfolio event
    // 10 = Added 'serverId' to the 'open order' & 'order status' events.
    //      We send back all the API open orders upon connection.
    //      Added new methods reqAllOpenOrders, reqAutoOpenOrders()
    //      Added FA support - reqExecution has filter.
    //                       - reqAccountUpdates takes acct code.
    // 11 = Added permId to openOrder event.
    // 12 = requsting open order attributes ignoreRth, hidden, and discretionary
    // 13 = added goodAfterTime
    // 14 = always send size on bid/ask/last tick
    // 15 = send allocation description string on openOrder
    // 16 = can receive account name in account and portfolio updates, and fa params in openOrder
    // 17 = can receive liquidation field in exec reports, and notAutoAvailable field in mkt data
    // 18 = can receive good till date field in open order messages, and request intraday backfill
    // 19 = can receive rthOnly flag in ORDER_STATUS
    // 20 = expects TWS time string on connection after server version >= 20.
    // 21 = can receive bond contract details.
    // 22 = can receive price magnifier in version 2 contract details message
    // 23 = support for scanner
    // 24 = can receive volatility order parameters in open order messages
	// 25 = can receive HMDS query start and end times
	// 26 = can receive option vols in option market data messages
	// 27 = can receive delta neutral order type and delta neutral aux price in place order version 20: API 8.85
	// 28 = can receive option model computation ticks: API 8.9
	// 29 = can receive trail stop limit price in open order and can place them: API 8.91
	// 30 = can receive extended bond contract def, new ticks, and trade count in bars
	// 31 = can receive EFP extensions to scanner and market data, and combo legs on open orders
	//    ; can receive RT bars
	// 32 = can receive TickType.LAST_TIMESTAMP
	//    ; can receive "whyHeld" in order status messages
	// 33 = can receive ScaleNumComponents and ScaleComponentSize is open order messages
	// 34 = can receive whatIf orders / order state
	// 35 = can receive contId field for Contract objects
	// 36 = can receive outsideRth field for Order objects
	// 37 = can receive clearingAccount and clearingIntent for Order objects
	// 38 = can receive multiplier and primaryExchange in portfolio updates
	//    ; can receive cumQty and avgPrice in execution
	//    ; can receive fundamental data
	//    ; can receive underComp for Contract objects
	//    ; can receive reqId and end marker in contractDetails/bondContractDetails
 	//    ; can receive ScaleInitComponentSize and ScaleSubsComponentSize for Order objects
	// 39 = can receive underConId in contractDetails
	// 40 = can receive algoStrategy/algoParams in openOrder
	// 41 = can receive end marker for openOrder
	//    ; can receive end marker for account download
	//    ; can receive end marker for executions download
	// 42 = can receive deltaNeutralValidation
	// 43 = can receive longName(companyName)
	//    ; can receive listingExchange
	//    ; can receive RTVolume tick
	// 44 = can receive end market for ticker snapshot
	// 45 = can receive notHeld field in openOrder
	// 46 = can receive contractMonth, industry, category, subcategory fields in contractDetails
	//    ; can receive timeZoneId, tradingHours, liquidHours fields in contractDetails
	// 47 = can receive gamma, vega, theta, undPrice fields in TICK_OPTION_COMPUTATION
	// 48 = can receive exemptCode in openOrder
	// 49 = can receive hedgeType and hedgeParam in openOrder
	// 50 = can receive optOutSmartRouting field in openOrder
	// 51 = can receive smartComboRoutingParams in openOrder
	// 52 = can receive deltaNeutralConId, deltaNeutralSettlingFirm, deltaNeutralClearingAccount and deltaNeutralClearingIntent in openOrder
	// 53 = can receive orderRef in execution
	// 54 = can receive scale order fields (PriceAdjustValue, PriceAdjustInterval, ProfitOffset, AutoReset,
	//      InitPosition, InitFillQty and RandomPercent) in openOrder
	// 55 = can receive orderComboLegs (price) in openOrder
	// 56 = can receive trailingPercent in openOrder
	// 57 = can receive commissionReport message
	// 58 = can receive CUSIP/ISIN/etc. in contractDescription/bondContractDescription
	// 59 = can receive evRule, evMultiplier in contractDescription/bondContractDescription/executionDetails
	//      can receive multiplier in executionDetails
	// 60 = can receive deltaNeutralOpenClose, deltaNeutralShortSale, deltaNeutralShortSaleSlot and deltaNeutralDesignatedLocation in openOrder
	// 61 = can receive multiplier in openOrder
	//      can receive tradingClass in openOrder, updatePortfolio, execDetails and position
	// 62 = can receive avgCost in position message
	// 63 = can receive verifyMessageAPI, verifyCompleted, displayGroupList and displayGroupUpdated messages

    private static final int CLIENT_VERSION = 63;
    private static final int SERVER_VERSION = 38;
    private static final byte[] EOL = {0};
    private static final String BAG_SEC_TYPE = "BAG";

    // FA msg data types
    public static final int GROUPS = 1;
    public static final int PROFILES = 2;
    public static final int ALIASES = 3;

    public static String faMsgTypeName(int faDataType) {
        switch (faDataType) {
            case GROUPS:
                return "GROUPS";
            case PROFILES:
                return "PROFILES";
            case ALIASES:
                return "ALIASES";
        }
        return null;
    }

    // outgoing msg id's
    private static final int REQ_MKT_DATA = 1;
    private static final int CANCEL_MKT_DATA = 2;
    private static final int PLACE_ORDER = 3;
    private static final int CANCEL_ORDER = 4;
    private static final int REQ_OPEN_ORDERS = 5;
    private static final int REQ_ACCOUNT_DATA = 6;
    private static final int REQ_EXECUTIONS = 7;
    private static final int REQ_IDS = 8;
    private static final int REQ_CONTRACT_DATA = 9;
    private static final int REQ_MKT_DEPTH = 10;
    private static final int CANCEL_MKT_DEPTH = 11;
    private static final int REQ_NEWS_BULLETINS = 12;
    private static final int CANCEL_NEWS_BULLETINS = 13;
    private static final int SET_SERVER_LOGLEVEL = 14;
    private static final int REQ_AUTO_OPEN_ORDERS = 15;
    private static final int REQ_ALL_OPEN_ORDERS = 16;
    private static final int REQ_MANAGED_ACCTS = 17;
    private static final int REQ_FA = 18;
    private static final int REPLACE_FA = 19;
    private static final int REQ_HISTORICAL_DATA = 20;
    private static final int EXERCISE_OPTIONS = 21;
    private static final int REQ_SCANNER_SUBSCRIPTION = 22;
    private static final int CANCEL_SCANNER_SUBSCRIPTION = 23;
    private static final int REQ_SCANNER_PARAMETERS = 24;
    private static final int CANCEL_HISTORICAL_DATA = 25;
    private static final int REQ_CURRENT_TIME = 49;
    private static final int REQ_REAL_TIME_BARS = 50;
    private static final int CANCEL_REAL_TIME_BARS = 51;
    private static final int REQ_FUNDAMENTAL_DATA = 52;
    private static final int CANCEL_FUNDAMENTAL_DATA = 53;
    private static final int REQ_CALC_IMPLIED_VOLAT = 54;
    private static final int REQ_CALC_OPTION_PRICE = 55;
    private static final int CANCEL_CALC_IMPLIED_VOLAT = 56;
    private static final int CANCEL_CALC_OPTION_PRICE = 57;
    private static final int REQ_GLOBAL_CANCEL = 58;
    private static final int REQ_MARKET_DATA_TYPE = 59;
    private static final int REQ_POSITIONS = 61;
    private static final int REQ_ACCOUNT_SUMMARY = 62;
    private static final int CANCEL_ACCOUNT_SUMMARY = 63;
    private static final int CANCEL_POSITIONS = 64;
    private static final int VERIFY_REQUEST = 65;
    private static final int VERIFY_MESSAGE = 66;
    private static final int QUERY_DISPLAY_GROUPS = 67;
    private static final int SUBSCRIBE_TO_GROUP_EVENTS = 68;
    private static final int UPDATE_DISPLAY_GROUP = 69;
    private static final int UNSUBSCRIBE_FROM_GROUP_EVENTS = 70;
    private static final int START_API = 71;

	private static final int MIN_SERVER_VER_REAL_TIME_BARS = 34;
	private static final int MIN_SERVER_VER_SCALE_ORDERS = 35;
	private static final int MIN_SERVER_VER_SNAPSHOT_MKT_DATA = 35;
	private static final int MIN_SERVER_VER_SSHORT_COMBO_LEGS = 35;
	private static final int MIN_SERVER_VER_WHAT_IF_ORDERS = 36;
	private static final int MIN_SERVER_VER_CONTRACT_CONID = 37;
	private static final int MIN_SERVER_VER_PTA_ORDERS = 39;
	private static final int MIN_SERVER_VER_FUNDAMENTAL_DATA = 40;
	private static final int MIN_SERVER_VER_UNDER_COMP = 40;
	private static final int MIN_SERVER_VER_CONTRACT_DATA_CHAIN = 40;
	private static final int MIN_SERVER_VER_SCALE_ORDERS2 = 40;
	private static final int MIN_SERVER_VER_ALGO_ORDERS = 41;
	private static final int MIN_SERVER_VER_EXECUTION_DATA_CHAIN = 42;
	private static final int MIN_SERVER_VER_NOT_HELD = 44;
	private static final int MIN_SERVER_VER_SEC_ID_TYPE = 45;
	private static final int MIN_SERVER_VER_PLACE_ORDER_CONID = 46;
	private static final int MIN_SERVER_VER_REQ_MKT_DATA_CONID = 47;
    private static final int MIN_SERVER_VER_REQ_CALC_IMPLIED_VOLAT = 49;
    private static final int MIN_SERVER_VER_REQ_CALC_OPTION_PRICE = 50;
    private static final int MIN_SERVER_VER_CANCEL_CALC_IMPLIED_VOLAT = 50;
    private static final int MIN_SERVER_VER_CANCEL_CALC_OPTION_PRICE = 50;
    private static final int MIN_SERVER_VER_SSHORTX_OLD = 51;
    private static final int MIN_SERVER_VER_SSHORTX = 52;
    private static final int MIN_SERVER_VER_REQ_GLOBAL_CANCEL = 53;
    private static final int MIN_SERVER_VER_HEDGE_ORDERS = 54;
    private static final int MIN_SERVER_VER_REQ_MARKET_DATA_TYPE = 55;
    private static final int MIN_SERVER_VER_OPT_OUT_SMART_ROUTING = 56;
    private static final int MIN_SERVER_VER_SMART_COMBO_ROUTING_PARAMS = 57;
    private static final int MIN_SERVER_VER_DELTA_NEUTRAL_CONID = 58;
    private static final int MIN_SERVER_VER_SCALE_ORDERS3 = 60;
    private static final int MIN_SERVER_VER_ORDER_COMBO_LEGS_PRICE = 61;
    private static final int MIN_SERVER_VER_TRAILING_PERCENT = 62;
    private static final int MIN_SERVER_VER_DELTA_NEUTRAL_OPEN_CLOSE = 66;
    private static final int MIN_SERVER_VER_ACCT_SUMMARY = 67;
    private static final int MIN_SERVER_VER_TRADING_CLASS = 68;
    private static final int MIN_SERVER_VER_SCALE_TABLE = 69;
    private static final int MIN_SERVER_VER_LINKING = 70;
    private static final int MIN_SERVER_VER_ALGO_ID = 71;

    private AnyWrapper m_anyWrapper;    // msg handler
    protected DataOutputStream m_dos;   // the socket output stream
    private boolean m_connected;        // true if we are connected
    private EReader m_reader;           // thread which reads msgs from socket
    private int m_serverVersion;
    private String m_TwsTime;
    private int m_clientId;
    private boolean m_extraAuth;

    public int serverVersion()          { return m_serverVersion;   }
    public String TwsConnectionTime()   { return m_TwsTime; }
    public AnyWrapper wrapper() 		{ return m_anyWrapper; }
    public EReader reader()             { return m_reader; }
    public boolean isConnected() 		{ return m_connected; }

    protected synchronized void setExtraAuth(boolean extraAuth){
        m_extraAuth = extraAuth;
    }

    public EClientSocket( AnyWrapper anyWrapper) {
        m_anyWrapper = anyWrapper;
        m_clientId = -1;
        m_extraAuth = false;
        m_connected = false;
        m_serverVersion = 0;
    }
    
    public synchronized void eConnect( String host, int port, int clientId) {
        eConnect(host, port, clientId, false);
    }
    
    public synchronized void eConnect( String host, int port, int clientId, boolean extraAuth) {
        // already connected?
        host = checkConnected(host);

        m_clientId = clientId;
        m_extraAuth = extraAuth;

        if(host == null){
            return;
        }
        try{
            Socket socket = new Socket( host, port);
            eConnect(socket);
        }
        catch( Exception e) {
        	eDisconnect();
            connectionError();
        }
    }

    protected void connectionError() {
        m_anyWrapper.error( EClientErrors.NO_VALID_ID, EClientErrors.CONNECT_FAIL.code(),
                EClientErrors.CONNECT_FAIL.msg());
        m_reader = null;
    }

    protected String checkConnected(String host) {
        if( m_connected) {
            m_anyWrapper.error(EClientErrors.NO_VALID_ID, EClientErrors.ALREADY_CONNECTED.code(),
                    EClientErrors.ALREADY_CONNECTED.msg());
            return null;
        }
        if( isNull( host) ) {
            host = "127.0.0.1";
        }
        return host;
    }

    public EReader createReader(EClientSocket socket, DataInputStream dis) {
        return new EReader(socket, dis);
    }

    public synchronized void eConnect(Socket socket, int clientId) throws IOException {
        m_clientId = clientId;
        eConnect(socket);
    }
    
    public synchronized void eConnect(Socket socket) throws IOException {

        // create io streams
        m_dos = new DataOutputStream( socket.getOutputStream() );

        // set client version
        send( CLIENT_VERSION);

        // start reader thread
        m_reader = createReader(this, new DataInputStream(
        		socket.getInputStream()));

        // check server version
        m_serverVersion = m_reader.readInt();
        System.out.println("Server Version:" + m_serverVersion);
        if ( m_serverVersion >= 20 ){
            m_TwsTime = m_reader.readStr();
            System.out.println("TWS Time at connection:" + m_TwsTime);
        }
        if( m_serverVersion < SERVER_VERSION) {
        	eDisconnect();
            m_anyWrapper.error( EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS.code(), EClientErrors.UPDATE_TWS.msg());
            return;
        }

        // set connected flag
        m_connected = true;

        // Send the client id
        if ( m_serverVersion >= 3 ){
            if ( m_serverVersion < MIN_SERVER_VER_LINKING) {
                send( m_clientId);
            }
            else if (!m_extraAuth){
                startAPI();
             }
        }

        m_reader.start();

    }

    public synchronized void eDisconnect() {
        // not connected?
        if( m_dos == null) {
            return;
        }

        m_connected = false;
        m_extraAuth = false;
        m_clientId = -1;
        m_serverVersion = 0;
        m_TwsTime = "";

        FilterOutputStream dos = m_dos;
        m_dos = null;

        EReader reader = m_reader;
        m_reader = null;

        try {
            // stop reader thread; reader thread will close input stream
            if( reader != null) {
                reader.interrupt();
            }
        }
        catch( Exception e) {
        }

        try {
            // close output stream
            if( dos != null) {
                dos.close();
            }
        }
        catch( Exception e) {
        }
    }

    protected synchronized void startAPI() {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 1;

        try {
            send(START_API);
            send(VERSION);
            send(m_clientId);
        }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID,
                   EClientErrors.FAIL_SEND_STARTAPI, "" + e);
            close();
        }
    }

    public synchronized void cancelScannerSubscription( int tickerId) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < 24) {
          error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
                "  It does not support API scanner subscription.");
          return;
        }

        final int VERSION = 1;

        // send cancel mkt data msg
        try {
            send( CANCEL_SCANNER_SUBSCRIPTION);
            send( VERSION);
            send( tickerId);
        }
        catch( Exception e) {
            error( tickerId, EClientErrors.FAIL_SEND_CANSCANNER, "" + e);
            close();
        }
    }

    public synchronized void reqScannerParameters() {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < 24) {
          error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
                "  It does not support API scanner subscription.");
          return;
        }

        final int VERSION = 1;

        try {
            send(REQ_SCANNER_PARAMETERS);
            send(VERSION);
        }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID,
                   EClientErrors.FAIL_SEND_REQSCANNERPARAMETERS, "" + e);
            close();
        }
    }

    public synchronized void reqScannerSubscription( int tickerId, ScannerSubscription subscription, Vector<TagValue> scannerSubscriptionOptions) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < 24) {
          error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
                "  It does not support API scanner subscription.");
          return;
        }

        final int VERSION = 4;

        try {
            send(REQ_SCANNER_SUBSCRIPTION);
            send(VERSION);
            send(tickerId);
            sendMax(subscription.numberOfRows());
            send(subscription.instrument());
            send(subscription.locationCode());
            send(subscription.scanCode());
            sendMax(subscription.abovePrice());
            sendMax(subscription.belowPrice());
            sendMax(subscription.aboveVolume());
            sendMax(subscription.marketCapAbove());
            sendMax(subscription.marketCapBelow());
            send(subscription.moodyRatingAbove());
            send(subscription.moodyRatingBelow());
            send(subscription.spRatingAbove());
            send(subscription.spRatingBelow());
            send(subscription.maturityDateAbove());
            send(subscription.maturityDateBelow());
            sendMax(subscription.couponRateAbove());
            sendMax(subscription.couponRateBelow());
            send(subscription.excludeConvertible());
            if (m_serverVersion >= 25) {
                sendMax(subscription.averageOptionVolumeAbove());
                send(subscription.scannerSettingPairs());
            }
            if (m_serverVersion >= 27) {
                send(subscription.stockTypeFilter());
            }
            
            // send scannerSubscriptionOptions parameter
            if(m_serverVersion >= MIN_SERVER_VER_LINKING) {
                StringBuilder scannerSubscriptionOptionsStr = new StringBuilder();
                int scannerSubscriptionOptionsCount = scannerSubscriptionOptions == null ? 0 : scannerSubscriptionOptions.size();
                if( scannerSubscriptionOptionsCount > 0) {
                    for( int i = 0; i < scannerSubscriptionOptionsCount; ++i) {
                        TagValue tagValue = (TagValue)scannerSubscriptionOptions.get(i);
                        scannerSubscriptionOptionsStr.append( tagValue.m_tag);
                        scannerSubscriptionOptionsStr.append( "=");
                        scannerSubscriptionOptionsStr.append( tagValue.m_value);
                        scannerSubscriptionOptionsStr.append( ";");
                    }
                }
                send( scannerSubscriptionOptionsStr.toString());
            }
            
        }
        catch( Exception e) {
            error( tickerId, EClientErrors.FAIL_SEND_REQSCANNER, "" + e);
            close();
        }
    }

    public synchronized void reqMktData(int tickerId, Contract contract,
    		String genericTickList, boolean snapshot, List<TagValue> mktDataOptions) {
        if (!m_connected) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.NOT_CONNECTED, "");
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_SNAPSHOT_MKT_DATA && snapshot) {
        	error(tickerId, EClientErrors.UPDATE_TWS,
        			"  It does not support snapshot market data requests.");
        	return;
        }

        if (m_serverVersion < MIN_SERVER_VER_UNDER_COMP) {
        	if (contract.m_underComp != null) {
        		error(tickerId, EClientErrors.UPDATE_TWS,
        			"  It does not support delta-neutral orders.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_REQ_MKT_DATA_CONID) {
            if (contract.m_conId > 0) {
                error(tickerId, EClientErrors.UPDATE_TWS,
                    "  It does not support conId parameter.");
                return;
            }
        }

        if (m_serverVersion < MIN_SERVER_VER_TRADING_CLASS) {
            if (!IsEmpty(contract.m_tradingClass)) {
                error(tickerId, EClientErrors.UPDATE_TWS,
                    "  It does not support tradingClass parameter in reqMarketData.");
                return;
            }
        }

        final int VERSION = 11;

        try {
            // send req mkt data msg
            send(REQ_MKT_DATA);
            send(VERSION);
            send(tickerId);

            // send contract fields
            if (m_serverVersion >= MIN_SERVER_VER_REQ_MKT_DATA_CONID) {
                send(contract.m_conId);
            }
            send(contract.m_symbol);
            send(contract.m_secType);
            send(contract.m_expiry);
            send(contract.m_strike);
            send(contract.m_right);
            if (m_serverVersion >= 15) {
                send(contract.m_multiplier);
            }
            send(contract.m_exchange);
            if (m_serverVersion >= 14) {
                send(contract.m_primaryExch);
            }
            send(contract.m_currency);
            if(m_serverVersion >= 2) {
                send( contract.m_localSymbol);
            }
            if(m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
                send( contract.m_tradingClass);
            }
            if(m_serverVersion >= 8 && BAG_SEC_TYPE.equalsIgnoreCase(contract.m_secType)) {
                if ( contract.m_comboLegs == null ) {
                    send( 0);
                }
                else {
                    send( contract.m_comboLegs.size());

                    ComboLeg comboLeg;
                    for (int i=0; i < contract.m_comboLegs.size(); i ++) {
                        comboLeg = contract.m_comboLegs.get(i);
                        send( comboLeg.m_conId);
                        send( comboLeg.m_ratio);
                        send( comboLeg.m_action);
                        send( comboLeg.m_exchange);
                    }
                }
            }

            if (m_serverVersion >= MIN_SERVER_VER_UNDER_COMP) {
         	   if (contract.m_underComp != null) {
         		   UnderComp underComp = contract.m_underComp;
         		   send( true);
         		   send( underComp.m_conId);
         		   send( underComp.m_delta);
         		   send( underComp.m_price);
         	   }
         	   else {
         		   send( false);
         	   }
            }

            if (m_serverVersion >= 31) {
            	/*
            	 * Note: Even though SHORTABLE tick type supported only
            	 *       starting server version 33 it would be relatively
            	 *       expensive to expose this restriction here.
            	 *
            	 *       Therefore we are relying on TWS doing validation.
            	 */
            	send( genericTickList);
            }
            if (m_serverVersion >= MIN_SERVER_VER_SNAPSHOT_MKT_DATA) {
            	send (snapshot);
            }
            
            // send mktDataOptions parameter
            if(m_serverVersion >= MIN_SERVER_VER_LINKING) {
                StringBuilder mktDataOptionsStr = new StringBuilder();
                int mktDataOptionsCount = mktDataOptions == null ? 0 : mktDataOptions.size();
                if( mktDataOptionsCount > 0) {
                    for( int i = 0; i < mktDataOptionsCount; ++i) {
                        TagValue tagValue = (TagValue)mktDataOptions.get(i);
                        mktDataOptionsStr.append( tagValue.m_tag);
                        mktDataOptionsStr.append( "=");
                        mktDataOptionsStr.append( tagValue.m_value);
                        mktDataOptionsStr.append( ";");
                    }
                }
                send( mktDataOptionsStr.toString());
            }
            
        }
        catch( Exception e) {
            error( tickerId, EClientErrors.FAIL_SEND_REQMKT, "" + e);
            close();
        }
    }

    public synchronized void cancelHistoricalData( int tickerId ) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < 24) {
          error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
                "  It does not support historical data query cancellation.");
          return;
        }

        final int VERSION = 1;

        // send cancel mkt data msg
        try {
            send( CANCEL_HISTORICAL_DATA);
            send( VERSION);
            send( tickerId);
        }
        catch( Exception e) {
            error( tickerId, EClientErrors.FAIL_SEND_CANHISTDATA, "" + e);
            close();
        }
    }

    public void cancelRealTimeBars(int tickerId) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_REAL_TIME_BARS) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
                  "  It does not support realtime bar data query cancellation.");
            return;
        }

        final int VERSION = 1;

        // send cancel mkt data msg
        try {
            send( CANCEL_REAL_TIME_BARS);
            send( VERSION);
            send( tickerId);
        }
        catch( Exception e) {
            error( tickerId, EClientErrors.FAIL_SEND_CANRTBARS, "" + e);
            close();
        }
    }

    /** Note that formatData parameter affects intra-day bars only; 1-day bars always return with date in YYYYMMDD format. */
    public synchronized void reqHistoricalData( int tickerId, Contract contract,
                                                String endDateTime, String durationStr,
                                                String barSizeSetting, String whatToShow,
                                                int useRTH, int formatDate, List<TagValue> chartOptions) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 6;

        try {
          if (m_serverVersion < 16) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
                  "  It does not support historical data backfill.");
            return;
          }

          if (m_serverVersion < MIN_SERVER_VER_TRADING_CLASS) {
              if (!IsEmpty(contract.m_tradingClass) || (contract.m_conId > 0)) {
                  error(tickerId, EClientErrors.UPDATE_TWS,
                      "  It does not support conId and tradingClass parameters in reqHistroricalData.");
                  return;
              }
          }

          send(REQ_HISTORICAL_DATA);
          send(VERSION);
          send(tickerId);

          // send contract fields
          if (m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
              send(contract.m_conId);
          }
          send(contract.m_symbol);
          send(contract.m_secType);
          send(contract.m_expiry);
          send(contract.m_strike);
          send(contract.m_right);
          send(contract.m_multiplier);
          send(contract.m_exchange);
          send(contract.m_primaryExch);
          send(contract.m_currency);
          send(contract.m_localSymbol);
          if (m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
              send(contract.m_tradingClass);
          }
          if (m_serverVersion >= 31) {
        	  send(contract.m_includeExpired ? 1 : 0);
          }
          if (m_serverVersion >= 20) {
              send(endDateTime);
              send(barSizeSetting);
          }
          send(durationStr);
          send(useRTH);
          send(whatToShow);
          if (m_serverVersion > 16) {
              send(formatDate);
          }
          if (BAG_SEC_TYPE.equalsIgnoreCase(contract.m_secType)) {
              if (contract.m_comboLegs == null) {
                  send(0);
              }
              else {
                  send(contract.m_comboLegs.size());

                  ComboLeg comboLeg;
                  for (int i = 0; i < contract.m_comboLegs.size(); i++) {
                      comboLeg = contract.m_comboLegs.get(i);
                      send(comboLeg.m_conId);
                      send(comboLeg.m_ratio);
                      send(comboLeg.m_action);
                      send(comboLeg.m_exchange);
                  }
              }
          }
          
          // send chartOptions parameter
          if(m_serverVersion >= MIN_SERVER_VER_LINKING) {
              StringBuilder chartOptionsStr = new StringBuilder();
              int chartOptionsCount = chartOptions == null ? 0 : chartOptions.size();
              if( chartOptionsCount > 0) {
                  for( int i = 0; i < chartOptionsCount; ++i) {
                      TagValue tagValue = (TagValue)chartOptions.get(i);
                      chartOptionsStr.append( tagValue.m_tag);
                      chartOptionsStr.append( "=");
                      chartOptionsStr.append( tagValue.m_value);
                      chartOptionsStr.append( ";");
                  }
              }
              send( chartOptionsStr.toString());
          }
          
        }
        catch (Exception e) {
          error(tickerId, EClientErrors.FAIL_SEND_REQHISTDATA, "" + e);
          close();
        }
    }

    public synchronized void reqRealTimeBars(int tickerId, Contract contract, int barSize, String whatToShow, boolean useRTH, Vector<TagValue> realTimeBarsOptions) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_REAL_TIME_BARS) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
                  "  It does not support real time bars.");
            return;
        }
        if (m_serverVersion < MIN_SERVER_VER_TRADING_CLASS) {
            if (!IsEmpty(contract.m_tradingClass) || (contract.m_conId > 0)) {
                  error(tickerId, EClientErrors.UPDATE_TWS,
                      "  It does not support conId and tradingClass parameters in reqRealTimeBars.");
                  return;
            }
        }

        final int VERSION = 3;

        try {
            // send req mkt data msg
            send(REQ_REAL_TIME_BARS);
            send(VERSION);
            send(tickerId);

            // send contract fields
            if (m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
                send(contract.m_conId);
            }
            send(contract.m_symbol);
            send(contract.m_secType);
            send(contract.m_expiry);
            send(contract.m_strike);
            send(contract.m_right);
            send(contract.m_multiplier);
            send(contract.m_exchange);
            send(contract.m_primaryExch);
            send(contract.m_currency);
            send(contract.m_localSymbol);
            if (m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
                send(contract.m_tradingClass);
            }
            send(barSize);  // this parameter is not currently used
            send(whatToShow);
            send(useRTH);

            // send realTimeBarsOptions parameter
            if(m_serverVersion >= MIN_SERVER_VER_LINKING) {
                StringBuilder realTimeBarsOptionsStr = new StringBuilder();
                int realTimeBarsOptionsCount = realTimeBarsOptions == null ? 0 : realTimeBarsOptions.size();
                if( realTimeBarsOptionsCount > 0) {
                    for( int i = 0; i < realTimeBarsOptionsCount; ++i) {
                        TagValue tagValue = (TagValue)realTimeBarsOptions.get(i);
                        realTimeBarsOptionsStr.append( tagValue.m_tag);
                        realTimeBarsOptionsStr.append( "=");
                        realTimeBarsOptionsStr.append( tagValue.m_value);
                        realTimeBarsOptionsStr.append( ";");
                    }
                }
                send( realTimeBarsOptionsStr.toString());
            }
            
        }
        catch( Exception e) {
            error( tickerId, EClientErrors.FAIL_SEND_REQRTBARS, "" + e);
            close();
        }
    }

    public synchronized void reqContractDetails(int reqId, Contract contract) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        // This feature is only available for versions of TWS >=4
        if( m_serverVersion < 4) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS.code(),
                            EClientErrors.UPDATE_TWS.msg());
            return;
        }

        if( m_serverVersion < MIN_SERVER_VER_SEC_ID_TYPE) {
        	if (!IsEmpty(contract.m_secIdType) || !IsEmpty(contract.m_secId)) {
        		error(reqId, EClientErrors.UPDATE_TWS,
        			"  It does not support secIdType and secId parameters.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_TRADING_CLASS) {
            if (!IsEmpty(contract.m_tradingClass)) {
                  error(reqId, EClientErrors.UPDATE_TWS,
                      "  It does not support tradingClass parameter in reqContractDetails.");
                  return;
            }
        }

        final int VERSION = 7;

        try {
            // send req mkt data msg
            send( REQ_CONTRACT_DATA);
            send( VERSION);

            if (m_serverVersion >= MIN_SERVER_VER_CONTRACT_DATA_CHAIN) {
            	send( reqId);
            }

            // send contract fields
            if (m_serverVersion >= MIN_SERVER_VER_CONTRACT_CONID) {
            	send(contract.m_conId);
            }
            send( contract.m_symbol);
            send( contract.m_secType);
            send( contract.m_expiry);
            send( contract.m_strike);
            send( contract.m_right);
            if (m_serverVersion >= 15) {
                send(contract.m_multiplier);
            }
            send( contract.m_exchange);
            send( contract.m_currency);
            send( contract.m_localSymbol);
            if (m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
                send(contract.m_tradingClass);
            }
            if (m_serverVersion >= 31) {
                send(contract.m_includeExpired);
            }
            if (m_serverVersion >= MIN_SERVER_VER_SEC_ID_TYPE) {
            	send( contract.m_secIdType);
            	send( contract.m_secId);
            }

        }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_REQCONTRACT, "" + e);
            close();
        }
    }

    public synchronized void reqMktDepth( int tickerId, Contract contract, int numRows, Vector<TagValue> mktDepthOptions) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        // This feature is only available for versions of TWS >=6
        if( m_serverVersion < 6) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS.code(),
                    EClientErrors.UPDATE_TWS.msg());
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_TRADING_CLASS) {
            if (!IsEmpty(contract.m_tradingClass) || (contract.m_conId > 0)) {
                  error(tickerId, EClientErrors.UPDATE_TWS,
                      "  It does not support conId and tradingClass parameters in reqMktDepth.");
                  return;
            }
        }

        final int VERSION = 5;

        try {
            // send req mkt data msg
            send( REQ_MKT_DEPTH);
            send( VERSION);
            send( tickerId);

            // send contract fields
            if (m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
                send(contract.m_conId);
            }
            send( contract.m_symbol);
            send( contract.m_secType);
            send( contract.m_expiry);
            send( contract.m_strike);
            send( contract.m_right);
            if (m_serverVersion >= 15) {
              send(contract.m_multiplier);
            }
            send( contract.m_exchange);
            send( contract.m_currency);
            send( contract.m_localSymbol);
            if (m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
                send(contract.m_tradingClass);
            }
            if (m_serverVersion >= 19) {
                send( numRows);
            }
            
            // send mktDepthOptions parameter
            if(m_serverVersion >= MIN_SERVER_VER_LINKING) {
                StringBuilder mktDepthOptionsStr = new StringBuilder();
                int mktDepthOptionsCount = mktDepthOptions == null ? 0 : mktDepthOptions.size();
                if( mktDepthOptionsCount > 0) {
                    for( int i = 0; i < mktDepthOptionsCount; ++i) {
                        TagValue tagValue = (TagValue)mktDepthOptions.get(i);
                        mktDepthOptionsStr.append( tagValue.m_tag);
                        mktDepthOptionsStr.append( "=");
                        mktDepthOptionsStr.append( tagValue.m_value);
                        mktDepthOptionsStr.append( ";");
                    }
                }
                send( mktDepthOptionsStr.toString());
            }
            
        }
        catch( Exception e) {
            error( tickerId, EClientErrors.FAIL_SEND_REQMKTDEPTH, "" + e);
            close();
        }
    }

    public synchronized void cancelMktData( int tickerId) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 1;

        // send cancel mkt data msg
        try {
            send( CANCEL_MKT_DATA);
            send( VERSION);
            send( tickerId);
        }
        catch( Exception e) {
            error( tickerId, EClientErrors.FAIL_SEND_CANMKT, "" + e);
            close();
        }
    }

    public synchronized void cancelMktDepth( int tickerId) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        // This feature is only available for versions of TWS >=6
        if( m_serverVersion < 6) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS.code(),
                    EClientErrors.UPDATE_TWS.msg());
            return;
        }

        final int VERSION = 1;

        // send cancel mkt data msg
        try {
            send( CANCEL_MKT_DEPTH);
            send( VERSION);
            send( tickerId);
        }
        catch( Exception e) {
            error( tickerId, EClientErrors.FAIL_SEND_CANMKTDEPTH, "" + e);
            close();
        }
    }

    public synchronized void exerciseOptions( int tickerId, Contract contract,
                                              int exerciseAction, int exerciseQuantity,
                                              String account, int override) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 2;

        try {
          if (m_serverVersion < 21) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
                  "  It does not support options exercise from the API.");
            return;
          }

          if (m_serverVersion < MIN_SERVER_VER_TRADING_CLASS) {
              if (!IsEmpty(contract.m_tradingClass) || (contract.m_conId > 0)) {
                    error(tickerId, EClientErrors.UPDATE_TWS,
                        "  It does not support conId and tradingClass parameters in exerciseOptions.");
                    return;
              }
          }

          send(EXERCISE_OPTIONS);
          send(VERSION);
          send(tickerId);

          // send contract fields
          if (m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
              send(contract.m_conId);
          }
          send(contract.m_symbol);
          send(contract.m_secType);
          send(contract.m_expiry);
          send(contract.m_strike);
          send(contract.m_right);
          send(contract.m_multiplier);
          send(contract.m_exchange);
          send(contract.m_currency);
          send(contract.m_localSymbol);
          if (m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
              send(contract.m_tradingClass);
          }
          send(exerciseAction);
          send(exerciseQuantity);
          send(account);
          send(override);
      }
      catch (Exception e) {
        error(tickerId, EClientErrors.FAIL_SEND_REQMKT, "" + e);
        close();
      }
    }

    public synchronized void placeOrder( int id, Contract contract, Order order) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_SCALE_ORDERS) {
        	if (order.m_scaleInitLevelSize != Integer.MAX_VALUE ||
        		order.m_scalePriceIncrement != Double.MAX_VALUE) {
        		error(id, EClientErrors.UPDATE_TWS,
            		"  It does not support Scale orders.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_SSHORT_COMBO_LEGS) {
        	if (!contract.m_comboLegs.isEmpty()) {
                ComboLeg comboLeg;
                for (int i = 0; i < contract.m_comboLegs.size(); ++i) {
                    comboLeg = contract.m_comboLegs.get(i);
                    if (comboLeg.m_shortSaleSlot != 0 ||
                    	!IsEmpty(comboLeg.m_designatedLocation)) {
                		error(id, EClientErrors.UPDATE_TWS,
                			"  It does not support SSHORT flag for combo legs.");
                		return;
                    }
                }
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_WHAT_IF_ORDERS) {
        	if (order.m_whatIf) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support what-if orders.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_UNDER_COMP) {
        	if (contract.m_underComp != null) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support delta-neutral orders.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_SCALE_ORDERS2) {
        	if (order.m_scaleSubsLevelSize != Integer.MAX_VALUE) {
        		error(id, EClientErrors.UPDATE_TWS,
            		"  It does not support Subsequent Level Size for Scale orders.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_ALGO_ORDERS) {
        	if (!IsEmpty(order.m_algoStrategy)) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support algo orders.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_NOT_HELD) {
        	if (order.m_notHeld) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support notHeld parameter.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_SEC_ID_TYPE) {
        	if (!IsEmpty(contract.m_secIdType) || !IsEmpty(contract.m_secId)) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support secIdType and secId parameters.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_PLACE_ORDER_CONID) {
        	if (contract.m_conId > 0) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support conId parameter.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_SSHORTX) {
        	if (order.m_exemptCode != -1) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support exemptCode parameter.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_SSHORTX) {
        	if (!contract.m_comboLegs.isEmpty()) {
                ComboLeg comboLeg;
                for (int i = 0; i < contract.m_comboLegs.size(); ++i) {
                    comboLeg = contract.m_comboLegs.get(i);
                    if (comboLeg.m_exemptCode != -1) {
                		error(id, EClientErrors.UPDATE_TWS,
                			"  It does not support exemptCode parameter.");
                		return;
                    }
                }
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_HEDGE_ORDERS) {
        	if (!IsEmpty(order.m_hedgeType)) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support hedge orders.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_OPT_OUT_SMART_ROUTING) {
        	if (order.m_optOutSmartRouting) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support optOutSmartRouting parameter.");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_DELTA_NEUTRAL_CONID) {
        	if (order.m_deltaNeutralConId > 0
        			|| !IsEmpty(order.m_deltaNeutralSettlingFirm)
        			|| !IsEmpty(order.m_deltaNeutralClearingAccount)
        			|| !IsEmpty(order.m_deltaNeutralClearingIntent)
        			) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support deltaNeutral parameters: ConId, SettlingFirm, ClearingAccount, ClearingIntent");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_DELTA_NEUTRAL_OPEN_CLOSE) {
        	if (!IsEmpty(order.m_deltaNeutralOpenClose)
        			|| order.m_deltaNeutralShortSale
        			|| order.m_deltaNeutralShortSaleSlot > 0
        			|| !IsEmpty(order.m_deltaNeutralDesignatedLocation)
        			) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support deltaNeutral parameters: OpenClose, ShortSale, ShortSaleSlot, DesignatedLocation");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_SCALE_ORDERS3) {
        	if (order.m_scalePriceIncrement > 0 && order.m_scalePriceIncrement != Double.MAX_VALUE) {
        		if (order.m_scalePriceAdjustValue != Double.MAX_VALUE ||
        			order.m_scalePriceAdjustInterval != Integer.MAX_VALUE ||
        			order.m_scaleProfitOffset != Double.MAX_VALUE ||
        			order.m_scaleAutoReset ||
        			order.m_scaleInitPosition != Integer.MAX_VALUE ||
        			order.m_scaleInitFillQty != Integer.MAX_VALUE ||
        			order.m_scaleRandomPercent) {
        			error(id, EClientErrors.UPDATE_TWS,
        				"  It does not support Scale order parameters: PriceAdjustValue, PriceAdjustInterval, " +
        				"ProfitOffset, AutoReset, InitPosition, InitFillQty and RandomPercent");
        			return;
        		}
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_ORDER_COMBO_LEGS_PRICE && BAG_SEC_TYPE.equalsIgnoreCase(contract.m_secType)) {
        	if (!order.m_orderComboLegs.isEmpty()) {
        		OrderComboLeg orderComboLeg;
        		for (int i = 0; i < order.m_orderComboLegs.size(); ++i) {
        			orderComboLeg = order.m_orderComboLegs.get(i);
        			if (orderComboLeg.m_price != Double.MAX_VALUE) {
        			error(id, EClientErrors.UPDATE_TWS,
        				"  It does not support per-leg prices for order combo legs.");
        			return;
        			}
        		}
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_TRAILING_PERCENT) {
        	if (order.m_trailingPercent != Double.MAX_VALUE) {
        		error(id, EClientErrors.UPDATE_TWS,
        			"  It does not support trailing percent parameter");
        		return;
        	}
        }

        if (m_serverVersion < MIN_SERVER_VER_TRADING_CLASS) {
            if (!IsEmpty(contract.m_tradingClass)) {
                  error(id, EClientErrors.UPDATE_TWS,
                      "  It does not support tradingClass parameters in placeOrder.");
                  return;
            }
        }
        
        if (m_serverVersion < MIN_SERVER_VER_ALGO_ID && !IsEmpty(order.m_algoId) ) {
        		  error(id, EClientErrors.UPDATE_TWS, " It does not support algoId parameter");
        	}

        if (m_serverVersion < MIN_SERVER_VER_SCALE_TABLE) {
            if (!IsEmpty(order.m_scaleTable) || !IsEmpty(order.m_activeStartTime) || !IsEmpty(order.m_activeStopTime)) {
                  error(id, EClientErrors.UPDATE_TWS,
                      "  It does not support scaleTable, activeStartTime and activeStopTime parameters.");
                  return;
            }
        }

        int VERSION = (m_serverVersion < MIN_SERVER_VER_NOT_HELD) ? 27 : 43;

        // send place order msg
        try {
            send( PLACE_ORDER);
            send( VERSION);
            send( id);

            // send contract fields
            if( m_serverVersion >= MIN_SERVER_VER_PLACE_ORDER_CONID) {
                send(contract.m_conId);
            }
            send( contract.m_symbol);
            send( contract.m_secType);
            send( contract.m_expiry);
            send( contract.m_strike);
            send( contract.m_right);
            if (m_serverVersion >= 15) {
                send(contract.m_multiplier);
            }
            send( contract.m_exchange);
            if( m_serverVersion >= 14) {
              send(contract.m_primaryExch);
            }
            send( contract.m_currency);
            if( m_serverVersion >= 2) {
                send (contract.m_localSymbol);
            }
            if (m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
                send(contract.m_tradingClass);
            }
            if( m_serverVersion >= MIN_SERVER_VER_SEC_ID_TYPE){
            	send( contract.m_secIdType);
            	send( contract.m_secId);
            }

            // send main order fields
            send( order.m_action);
            send( order.m_totalQuantity);
            send( order.m_orderType);
            if (m_serverVersion < MIN_SERVER_VER_ORDER_COMBO_LEGS_PRICE) {
                send( order.m_lmtPrice == Double.MAX_VALUE ? 0 : order.m_lmtPrice);
            }
            else {
                sendMax( order.m_lmtPrice);
            }
            if (m_serverVersion < MIN_SERVER_VER_TRAILING_PERCENT) {
                send( order.m_auxPrice == Double.MAX_VALUE ? 0 : order.m_auxPrice);
            }
            else {
                sendMax( order.m_auxPrice);
            }

            // send extended order fields
            send( order.m_tif);
            send( order.m_ocaGroup);
            send( order.m_account);
            send( order.m_openClose);
            send( order.m_origin);
            send( order.m_orderRef);
            send( order.m_transmit);
            if( m_serverVersion >= 4 ) {
                send (order.m_parentId);
            }

            if( m_serverVersion >= 5 ) {
                send (order.m_blockOrder);
                send (order.m_sweepToFill);
                send (order.m_displaySize);
                send (order.m_triggerMethod);
                if (m_serverVersion < 38) {
                	// will never happen
                	send(/* order.m_ignoreRth */ false);
                }
                else {
                	send (order.m_outsideRth);
                }
            }

            if(m_serverVersion >= 7 ) {
                send(order.m_hidden);
            }

            // Send combo legs for BAG requests
            if(m_serverVersion >= 8 && BAG_SEC_TYPE.equalsIgnoreCase(contract.m_secType)) {
                if ( contract.m_comboLegs == null ) {
                    send( 0);
                }
                else {
                    send( contract.m_comboLegs.size());

                    ComboLeg comboLeg;
                    for (int i=0; i < contract.m_comboLegs.size(); i ++) {
                        comboLeg = contract.m_comboLegs.get(i);
                        send( comboLeg.m_conId);
                        send( comboLeg.m_ratio);
                        send( comboLeg.m_action);
                        send( comboLeg.m_exchange);
                        send( comboLeg.m_openClose);

                        if (m_serverVersion >= MIN_SERVER_VER_SSHORT_COMBO_LEGS) {
                        	send( comboLeg.m_shortSaleSlot);
                        	send( comboLeg.m_designatedLocation);
                        }
                        if (m_serverVersion >= MIN_SERVER_VER_SSHORTX_OLD) {
                            send( comboLeg.m_exemptCode);
                        }
                    }
                }
            }

            // Send order combo legs for BAG requests
            if(m_serverVersion >= MIN_SERVER_VER_ORDER_COMBO_LEGS_PRICE && BAG_SEC_TYPE.equalsIgnoreCase(contract.m_secType)) {
                if ( order.m_orderComboLegs == null ) {
                    send( 0);
                }
                else {
                    send( order.m_orderComboLegs.size());

                    for (int i = 0; i < order.m_orderComboLegs.size(); i++) {
                        OrderComboLeg orderComboLeg = order.m_orderComboLegs.get(i);
                        sendMax( orderComboLeg.m_price);
                    }
                }
            }

            if(m_serverVersion >= MIN_SERVER_VER_SMART_COMBO_ROUTING_PARAMS && BAG_SEC_TYPE.equalsIgnoreCase(contract.m_secType)) {
                java.util.Vector smartComboRoutingParams = order.m_smartComboRoutingParams;
                int smartComboRoutingParamsCount = smartComboRoutingParams == null ? 0 : smartComboRoutingParams.size();
                send( smartComboRoutingParamsCount);
                if( smartComboRoutingParamsCount > 0) {
                    for( int i = 0; i < smartComboRoutingParamsCount; ++i) {
                        TagValue tagValue = (TagValue)smartComboRoutingParams.get(i);
                        send( tagValue.m_tag);
                        send( tagValue.m_value);
                    }
                }
            }

            if ( m_serverVersion >= 9 ) {
            	// send deprecated sharesAllocation field
                send( "");
            }

            if ( m_serverVersion >= 10 ) {
                send( order.m_discretionaryAmt);
            }

            if ( m_serverVersion >= 11 ) {
                send( order.m_goodAfterTime);
            }

            if ( m_serverVersion >= 12 ) {
                send( order.m_goodTillDate);
            }

            if ( m_serverVersion >= 13 ) {
               send( order.m_faGroup);
               send( order.m_faMethod);
               send( order.m_faPercentage);
               send( order.m_faProfile);
           }
           if (m_serverVersion >= 18) { // institutional short sale slot fields.
               send( order.m_shortSaleSlot);      // 0 only for retail, 1 or 2 only for institution.
               send( order.m_designatedLocation); // only populate when order.m_shortSaleSlot = 2.
           }
           if (m_serverVersion >= MIN_SERVER_VER_SSHORTX_OLD) {
               send( order.m_exemptCode);
           }
           if (m_serverVersion >= 19) {
               send( order.m_ocaType);
               if (m_serverVersion < 38) {
            	   // will never happen
            	   send( /* order.m_rthOnly */ false);
               }
               send( order.m_rule80A);
               send( order.m_settlingFirm);
               send( order.m_allOrNone);
               sendMax( order.m_minQty);
               sendMax( order.m_percentOffset);
               send( order.m_eTradeOnly);
               send( order.m_firmQuoteOnly);
               sendMax( order.m_nbboPriceCap);
               sendMax( order.m_auctionStrategy);
               sendMax( order.m_startingPrice);
               sendMax( order.m_stockRefPrice);
               sendMax( order.m_delta);
        	   // Volatility orders had specific watermark price attribs in server version 26
        	   double lower = (m_serverVersion == 26 && order.m_orderType.equals("VOL"))
        	   		? Double.MAX_VALUE
        	   		: order.m_stockRangeLower;
        	   double upper = (m_serverVersion == 26 && order.m_orderType.equals("VOL"))
   	   				? Double.MAX_VALUE
   	   				: order.m_stockRangeUpper;
               sendMax( lower);
               sendMax( upper);
           }

           if (m_serverVersion >= 22) {
               send( order.m_overridePercentageConstraints);
           }

           if (m_serverVersion >= 26) { // Volatility orders
               sendMax( order.m_volatility);
               sendMax( order.m_volatilityType);
               if (m_serverVersion < 28) {
            	   send( order.m_deltaNeutralOrderType.equalsIgnoreCase("MKT"));
               } else {
            	   send( order.m_deltaNeutralOrderType);
            	   sendMax( order.m_deltaNeutralAuxPrice);

                   if (m_serverVersion >= MIN_SERVER_VER_DELTA_NEUTRAL_CONID && !IsEmpty(order.m_deltaNeutralOrderType)){
                       send( order.m_deltaNeutralConId);
                       send( order.m_deltaNeutralSettlingFirm);
                       send( order.m_deltaNeutralClearingAccount);
                       send( order.m_deltaNeutralClearingIntent);
                   }

                   if (m_serverVersion >= MIN_SERVER_VER_DELTA_NEUTRAL_OPEN_CLOSE && !IsEmpty(order.m_deltaNeutralOrderType)){
                       send( order.m_deltaNeutralOpenClose);
                       send( order.m_deltaNeutralShortSale);
                       send( order.m_deltaNeutralShortSaleSlot);
                       send( order.m_deltaNeutralDesignatedLocation);
                   }
               }
               send( order.m_continuousUpdate);
               if (m_serverVersion == 26) {
            	   // Volatility orders had specific watermark price attribs in server version 26
            	   double lower = order.m_orderType.equals("VOL") ? order.m_stockRangeLower : Double.MAX_VALUE;
            	   double upper = order.m_orderType.equals("VOL") ? order.m_stockRangeUpper : Double.MAX_VALUE;
                   sendMax( lower);
                   sendMax( upper);
               }
               sendMax( order.m_referencePriceType);
           }

           if (m_serverVersion >= 30) { // TRAIL_STOP_LIMIT stop price
               sendMax( order.m_trailStopPrice);
           }

           if( m_serverVersion >= MIN_SERVER_VER_TRAILING_PERCENT){
               sendMax( order.m_trailingPercent);
           }

           if (m_serverVersion >= MIN_SERVER_VER_SCALE_ORDERS) {
        	   if (m_serverVersion >= MIN_SERVER_VER_SCALE_ORDERS2) {
        		   sendMax (order.m_scaleInitLevelSize);
        		   sendMax (order.m_scaleSubsLevelSize);
        	   }
        	   else {
        		   send ("");
        		   sendMax (order.m_scaleInitLevelSize);

        	   }
        	   sendMax (order.m_scalePriceIncrement);
           }

           if (m_serverVersion >= MIN_SERVER_VER_SCALE_ORDERS3 && order.m_scalePriceIncrement > 0.0 && order.m_scalePriceIncrement != Double.MAX_VALUE) {
               sendMax (order.m_scalePriceAdjustValue);
               sendMax (order.m_scalePriceAdjustInterval);
               sendMax (order.m_scaleProfitOffset);
               send (order.m_scaleAutoReset);
               sendMax (order.m_scaleInitPosition);
               sendMax (order.m_scaleInitFillQty);
               send (order.m_scaleRandomPercent);
           }

           if (m_serverVersion >= MIN_SERVER_VER_SCALE_TABLE) {
               send (order.m_scaleTable);
               send (order.m_activeStartTime);
               send (order.m_activeStopTime);
           }

           if (m_serverVersion >= MIN_SERVER_VER_HEDGE_ORDERS) {
        	   send (order.m_hedgeType);
        	   if (!IsEmpty(order.m_hedgeType)) {
        		   send (order.m_hedgeParam);
        	   }
           }

           if (m_serverVersion >= MIN_SERVER_VER_OPT_OUT_SMART_ROUTING) {
               send (order.m_optOutSmartRouting);
           }

           if (m_serverVersion >= MIN_SERVER_VER_PTA_ORDERS) {
        	   send (order.m_clearingAccount);
        	   send (order.m_clearingIntent);
           }

           if (m_serverVersion >= MIN_SERVER_VER_NOT_HELD) {
        	   send (order.m_notHeld);
           }

           if (m_serverVersion >= MIN_SERVER_VER_UNDER_COMP) {
        	   if (contract.m_underComp != null) {
        		   UnderComp underComp = contract.m_underComp;
        		   send( true);
        		   send( underComp.m_conId);
        		   send( underComp.m_delta);
        		   send( underComp.m_price);
        	   }
        	   else {
        		   send( false);
        	   }
           }

           if (m_serverVersion >= MIN_SERVER_VER_ALGO_ORDERS) {
        	   send( order.m_algoStrategy);
        	   if( !IsEmpty(order.m_algoStrategy)) {
        		   java.util.Vector algoParams = order.m_algoParams;
        		   int algoParamsCount = algoParams == null ? 0 : algoParams.size();
        		   send( algoParamsCount);
        		   if( algoParamsCount > 0) {
        			   for( int i = 0; i < algoParamsCount; ++i) {
        				   TagValue tagValue = (TagValue)algoParams.get(i);
        				   send( tagValue.m_tag);
        				   send( tagValue.m_value);
        			   }
        		   }
        	   }
           }
           
           if (m_serverVersion >= MIN_SERVER_VER_ALGO_ID) {
        	   send(order.m_algoId);
           }

           if (m_serverVersion >= MIN_SERVER_VER_WHAT_IF_ORDERS) {
        	   send (order.m_whatIf);
           }
           
           // send orderMiscOptions parameter
           if(m_serverVersion >= MIN_SERVER_VER_LINKING) {
               StringBuilder orderMiscOptionsStr = new StringBuilder();
               java.util.Vector orderMiscOptions = order.m_orderMiscOptions;
               int orderMiscOptionsCount = orderMiscOptions == null ? 0 : orderMiscOptions.size();
               if( orderMiscOptionsCount > 0) {
                   for( int i = 0; i < orderMiscOptionsCount; ++i) {
                       TagValue tagValue = (TagValue)orderMiscOptions.get(i);
                       orderMiscOptionsStr.append( tagValue.m_tag);
                       orderMiscOptionsStr.append( "=");
                       orderMiscOptionsStr.append( tagValue.m_value);
                       orderMiscOptionsStr.append( ";");
                   }
               }
               send( orderMiscOptionsStr.toString());
           }
           
        }
        catch( Exception e) {
            error( id, EClientErrors.FAIL_SEND_ORDER, "" + e);
            close();
        }
    }

    public synchronized void reqAccountUpdates(boolean subscribe, String acctCode) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 2;

        // send cancel order msg
        try {
            send( REQ_ACCOUNT_DATA );
            send( VERSION);
            send( subscribe);

            // Send the account code. This will only be used for FA clients
            if ( m_serverVersion >= 9 ) {
                send( acctCode);
            }
        }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_ACCT, "" + e);
            close();
        }
    }

    public synchronized void reqExecutions(int reqId, ExecutionFilter filter) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 3;

        // send cancel order msg
        try {
            send( REQ_EXECUTIONS);
            send( VERSION);

            if (m_serverVersion >= MIN_SERVER_VER_EXECUTION_DATA_CHAIN) {
            	send( reqId);
            }

            // Send the execution rpt filter data
            if ( m_serverVersion >= 9 ) {
                send( filter.m_clientId);
                send( filter.m_acctCode);

                // Note that the valid format for m_time is "yyyymmdd-hh:mm:ss"
                send( filter.m_time);
                send( filter.m_symbol);
                send( filter.m_secType);
                send( filter.m_exchange);
                send( filter.m_side);
            }
        }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_EXEC, "" + e);
            close();
        }
    }

    public synchronized void cancelOrder( int id) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 1;

        // send cancel order msg
        try {
            send( CANCEL_ORDER);
            send( VERSION);
            send( id);
        }
        catch( Exception e) {
            error( id, EClientErrors.FAIL_SEND_CORDER, "" + e);
            close();
        }
    }

    public synchronized void reqOpenOrders() {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 1;

        // send cancel order msg
        try {
            send( REQ_OPEN_ORDERS);
            send( VERSION);
        }
        catch( Exception e) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_OORDER, "" + e);
            close();
        }
    }

    public synchronized void reqIds( int numIds) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 1;

        try {
            send( REQ_IDS);
            send( VERSION);
            send( numIds);
        }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_CORDER, "" + e);
            close();
        }
    }

    public synchronized void reqNewsBulletins( boolean allMsgs) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 1;

        try {
            send( REQ_NEWS_BULLETINS);
            send( VERSION);
            send( allMsgs);
        }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_CORDER, "" + e);
            close();
        }
    }

    public synchronized void cancelNewsBulletins() {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 1;

        // send cancel order msg
        try {
            send( CANCEL_NEWS_BULLETINS);
            send( VERSION);
        }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_CORDER, "" + e);
            close();
        }
    }

    public synchronized void setServerLogLevel(int logLevel) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 1;

                // send the set server logging level message
                try {
                        send( SET_SERVER_LOGLEVEL);
                        send( VERSION);
                        send( logLevel);
                }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_SERVER_LOG_LEVEL, "" + e);
            close();
        }
    }

    public synchronized void reqAutoOpenOrders(boolean bAutoBind) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 1;

        // send req open orders msg
        try {
            send( REQ_AUTO_OPEN_ORDERS);
            send( VERSION);
            send( bAutoBind);
        }
        catch( Exception e) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_OORDER, "" + e);
            close();
        }
    }

    public synchronized void reqAllOpenOrders() {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 1;

        // send req all open orders msg
        try {
            send( REQ_ALL_OPEN_ORDERS);
            send( VERSION);
        }
        catch( Exception e) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_OORDER, "" + e);
            close();
        }
    }

    public synchronized void reqManagedAccts() {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        final int VERSION = 1;

        // send req FA managed accounts msg
        try {
            send( REQ_MANAGED_ACCTS);
            send( VERSION);
        }
        catch( Exception e) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_OORDER, "" + e);
            close();
        }
    }

    public synchronized void requestFA( int faDataType ) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        // This feature is only available for versions of TWS >= 13
        if( m_serverVersion < 13) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS.code(),
                    EClientErrors.UPDATE_TWS.msg());
            return;
        }

        final int VERSION = 1;

        try {
            send( REQ_FA );
            send( VERSION);
            send( faDataType);
        }
        catch( Exception e) {
            error( faDataType, EClientErrors.FAIL_SEND_FA_REQUEST, "" + e);
            close();
        }
    }

    public synchronized void replaceFA( int faDataType, String xml ) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        // This feature is only available for versions of TWS >= 13
        if( m_serverVersion < 13) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS.code(),
                    EClientErrors.UPDATE_TWS.msg());
            return;
        }

        final int VERSION = 1;

        try {
            send( REPLACE_FA );
            send( VERSION);
            send( faDataType);
            send( xml);
        }
        catch( Exception e) {
            error( faDataType, EClientErrors.FAIL_SEND_FA_REPLACE, "" + e);
            close();
        }
    }

    public synchronized void reqCurrentTime() {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        // This feature is only available for versions of TWS >= 33
        if( m_serverVersion < 33) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
                  "  It does not support current time requests.");
            return;
        }

        final int VERSION = 1;

        try {
            send( REQ_CURRENT_TIME );
            send( VERSION);
        }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_REQCURRTIME, "" + e);
            close();
        }
    }

    public synchronized void reqFundamentalData(int reqId, Contract contract, String reportType) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if( m_serverVersion < MIN_SERVER_VER_FUNDAMENTAL_DATA) {
        	error( reqId, EClientErrors.UPDATE_TWS,
        			"  It does not support fundamental data requests.");
        	return;
        }

        if( m_serverVersion < MIN_SERVER_VER_TRADING_CLASS) {
            if( contract.m_conId > 0) {
                  error(reqId, EClientErrors.UPDATE_TWS,
                      "  It does not support conId parameter in reqFundamentalData.");
                  return;
            }
        }

        final int VERSION = 2;

        try {
            // send req fund data msg
            send( REQ_FUNDAMENTAL_DATA);
            send( VERSION);
            send( reqId);

            // send contract fields
            if( m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
                send(contract.m_conId);
            }
            send( contract.m_symbol);
            send( contract.m_secType);
            send( contract.m_exchange);
            send( contract.m_primaryExch);
            send( contract.m_currency);
            send( contract.m_localSymbol);

            send( reportType);
        }
        catch( Exception e) {
            error( reqId, EClientErrors.FAIL_SEND_REQFUNDDATA, "" + e);
            close();
        }
    }

    public synchronized void cancelFundamentalData(int reqId) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if( m_serverVersion < MIN_SERVER_VER_FUNDAMENTAL_DATA) {
        	error( reqId, EClientErrors.UPDATE_TWS,
        			"  It does not support fundamental data requests.");
        	return;
        }

        final int VERSION = 1;

        try {
            // send req mkt data msg
            send( CANCEL_FUNDAMENTAL_DATA);
            send( VERSION);
            send( reqId);
        }
        catch( Exception e) {
            error( reqId, EClientErrors.FAIL_SEND_CANFUNDDATA, "" + e);
            close();
        }
    }

    public synchronized void calculateImpliedVolatility(int reqId, Contract contract,
            double optionPrice, double underPrice) {

        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_REQ_CALC_IMPLIED_VOLAT) {
            error(reqId, EClientErrors.UPDATE_TWS,
                    "  It does not support calculate implied volatility requests.");
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_TRADING_CLASS) {
            if (!IsEmpty(contract.m_tradingClass)) {
                  error(reqId, EClientErrors.UPDATE_TWS,
                      "  It does not support tradingClass parameter in calculateImpliedVolatility.");
                  return;
            }
        }

        final int VERSION = 2;

        try {
            // send calculate implied volatility msg
            send( REQ_CALC_IMPLIED_VOLAT);
            send( VERSION);
            send( reqId);

            // send contract fields
            send( contract.m_conId);
            send( contract.m_symbol);
            send( contract.m_secType);
            send( contract.m_expiry);
            send( contract.m_strike);
            send( contract.m_right);
            send( contract.m_multiplier);
            send( contract.m_exchange);
            send( contract.m_primaryExch);
            send( contract.m_currency);
            send( contract.m_localSymbol);
            if( m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
                send(contract.m_tradingClass);
            }

            send( optionPrice);
            send( underPrice);
        }
        catch( Exception e) {
            error( reqId, EClientErrors.FAIL_SEND_REQCALCIMPLIEDVOLAT, "" + e);
            close();
        }
    }

    public synchronized void cancelCalculateImpliedVolatility(int reqId) {

        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_CANCEL_CALC_IMPLIED_VOLAT) {
            error(reqId, EClientErrors.UPDATE_TWS,
                    "  It does not support calculate implied volatility cancellation.");
            return;
        }

        final int VERSION = 1;

        try {
            // send cancel calculate implied volatility msg
            send( CANCEL_CALC_IMPLIED_VOLAT);
            send( VERSION);
            send( reqId);
        }
        catch( Exception e) {
            error( reqId, EClientErrors.FAIL_SEND_CANCALCIMPLIEDVOLAT, "" + e);
            close();
        }
    }

    public synchronized void calculateOptionPrice(int reqId, Contract contract,
            double volatility, double underPrice) {

        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_REQ_CALC_OPTION_PRICE) {
            error(reqId, EClientErrors.UPDATE_TWS,
                    "  It does not support calculate option price requests.");
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_TRADING_CLASS) {
            if (!IsEmpty(contract.m_tradingClass)) {
                  error(reqId, EClientErrors.UPDATE_TWS,
                      "  It does not support tradingClass parameter in calculateOptionPrice.");
                  return;
            }
        }

        final int VERSION = 2;

        try {
            // send calculate option price msg
            send( REQ_CALC_OPTION_PRICE);
            send( VERSION);
            send( reqId);

            // send contract fields
            send( contract.m_conId);
            send( contract.m_symbol);
            send( contract.m_secType);
            send( contract.m_expiry);
            send( contract.m_strike);
            send( contract.m_right);
            send( contract.m_multiplier);
            send( contract.m_exchange);
            send( contract.m_primaryExch);
            send( contract.m_currency);
            send( contract.m_localSymbol);
            if( m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
                send(contract.m_tradingClass);
            }

            send( volatility);
            send( underPrice);
        }
        catch( Exception e) {
            error( reqId, EClientErrors.FAIL_SEND_REQCALCOPTIONPRICE, "" + e);
            close();
        }
    }

    public synchronized void cancelCalculateOptionPrice(int reqId) {

        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_CANCEL_CALC_OPTION_PRICE) {
            error(reqId, EClientErrors.UPDATE_TWS,
                    "  It does not support calculate option price cancellation.");
            return;
        }

        final int VERSION = 1;

        try {
            // send cancel calculate option price msg
            send( CANCEL_CALC_OPTION_PRICE);
            send( VERSION);
            send( reqId);
        }
        catch( Exception e) {
            error( reqId, EClientErrors.FAIL_SEND_CANCALCOPTIONPRICE, "" + e);
            close();
        }
    }

    public synchronized void reqGlobalCancel() {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_REQ_GLOBAL_CANCEL) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
                    "  It does not support globalCancel requests.");
            return;
        }

        final int VERSION = 1;

        // send request global cancel msg
        try {
            send( REQ_GLOBAL_CANCEL);
            send( VERSION);
        }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_REQGLOBALCANCEL, "" + e);
            close();
        }
    }

    public synchronized void reqMarketDataType(int marketDataType) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_REQ_MARKET_DATA_TYPE) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
                    "  It does not support marketDataType requests.");
            return;
        }

        final int VERSION = 1;

        // send the reqMarketDataType message
        try {
            send( REQ_MARKET_DATA_TYPE);
            send( VERSION);
            send( marketDataType);
        }
        catch( Exception e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_REQMARKETDATATYPE, "" + e);
            close();
        }
    }

    public synchronized void reqPositions() {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_ACCT_SUMMARY) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
            "  It does not support position requests.");
            return;
        }

        final int VERSION = 1;

        Builder b = new Builder();
        b.send( REQ_POSITIONS);
        b.send( VERSION);


        try {
            m_dos.write( b.getBytes() );
        }
        catch (IOException e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_REQPOSITIONS, "" + e);
        }
    }

    public synchronized void cancelPositions() {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_ACCT_SUMMARY) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
            "  It does not support position cancellation.");
            return;
        }

        final int VERSION = 1;

        Builder b = new Builder();
        b.send( CANCEL_POSITIONS);
        b.send( VERSION);

        try {
            m_dos.write( b.getBytes() );
        }
        catch (IOException e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_CANPOSITIONS, "" + e);
        }
    }

    public synchronized void reqAccountSummary( int reqId, String group, String tags) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_ACCT_SUMMARY) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
            "  It does not support account summary requests.");
            return;
        }

        final int VERSION = 1;

        Builder b = new Builder();
        b.send( REQ_ACCOUNT_SUMMARY);
        b.send( VERSION);
        b.send( reqId);
        b.send( group);
        b.send( tags);

        try {
           m_dos.write( b.getBytes() );
        }
        catch (IOException e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_REQACCOUNTDATA, "" + e);
        }
    }

	public synchronized void cancelAccountSummary( int reqId) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_ACCT_SUMMARY) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
            "  It does not support account summary cancellation.");
            return;
        }

        final int VERSION = 1;

        Builder b = new Builder();
        b.send( CANCEL_ACCOUNT_SUMMARY);
        b.send( VERSION);
        b.send( reqId);

        try {
            m_dos.write( b.getBytes() );
        }
        catch (IOException e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_CANACCOUNTDATA, "" + e);
        }
    }
	
	public synchronized void verifyRequest( String apiName, String apiVersion) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_LINKING) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
            "  It does not support verification request.");
            return;
        }

        if (!m_extraAuth) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_VERIFYMESSAGE,
            "  Intent to authenticate needs to be expressed during initial connect request.");
            return;
        	
        }

        final int VERSION = 1;

        Builder b = new Builder();
        b.send( VERIFY_REQUEST);
        b.send( VERSION);
        b.send( apiName);
        b.send( apiVersion);

        try {
            m_dos.write( b.getBytes() );
        }
        catch (IOException e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_VERIFYREQUEST, "" + e);
        }
    }

	public synchronized void verifyMessage( String apiData) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_LINKING) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
            "  It does not support verification message sending.");
            return;
        }

        final int VERSION = 1;

        Builder b = new Builder();
        b.send( VERIFY_MESSAGE);
        b.send( VERSION);
        b.send( apiData);

        try {
            m_dos.write( b.getBytes() );
        }
        catch (IOException e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_VERIFYMESSAGE, "" + e);
        }
    }

	public synchronized void queryDisplayGroups( int reqId) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_LINKING) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
            "  It does not support queryDisplayGroups request.");
            return;
        }

        final int VERSION = 1;

        Builder b = new Builder();
        b.send( QUERY_DISPLAY_GROUPS);
        b.send( VERSION);
        b.send( reqId);

        try {
            m_dos.write( b.getBytes() );
        }
        catch (IOException e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_QUERYDISPLAYGROUPS, "" + e);
        }
    }
	
	public synchronized void subscribeToGroupEvents( int reqId, int groupId) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_LINKING) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
            "  It does not support subscribeToGroupEvents request.");
            return;
        }

        final int VERSION = 1;

        Builder b = new Builder();
        b.send( SUBSCRIBE_TO_GROUP_EVENTS);
        b.send( VERSION);
        b.send( reqId);
        b.send( groupId);

        try {
            m_dos.write( b.getBytes() );
        }
        catch (IOException e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_SUBSCRIBETOGROUPEVENTS, "" + e);
        }
    }	

	public synchronized void updateDisplayGroup( int reqId, String contractInfo) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_LINKING) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
            "  It does not support updateDisplayGroup request.");
            return;
        }

        final int VERSION = 1;

        Builder b = new Builder();
        b.send( UPDATE_DISPLAY_GROUP);
        b.send( VERSION);
        b.send( reqId);
        b.send( contractInfo);

        try {
            m_dos.write( b.getBytes() );
        }
        catch (IOException e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_UPDATEDISPLAYGROUP, "" + e);
        }
    }	

	public synchronized void unsubscribeFromGroupEvents( int reqId) {
        // not connected?
        if( !m_connected) {
            notConnected();
            return;
        }

        if (m_serverVersion < MIN_SERVER_VER_LINKING) {
            error(EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS,
            "  It does not support unsubscribeFromGroupEvents request.");
            return;
        }

        final int VERSION = 1;

        Builder b = new Builder();
        b.send( UNSUBSCRIBE_FROM_GROUP_EVENTS);
        b.send( VERSION);
        b.send( reqId);

        try {
            m_dos.write( b.getBytes() );
        }
        catch (IOException e) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.FAIL_SEND_UNSUBSCRIBEFROMGROUPEVENTS, "" + e);
        }
    }	
	
    /** @deprecated, never called. */
    protected synchronized void error( String err) {
        m_anyWrapper.error( err);
    }

    protected synchronized void error( int id, int errorCode, String errorMsg) {
        m_anyWrapper.error( id, errorCode, errorMsg);
    }

    protected void close() {
        eDisconnect();
        wrapper().connectionClosed();
    }

    private static boolean is( String str) {
        // return true if the string is not empty
        return str != null && str.length() > 0;
    }

    private static boolean isNull( String str) {
        // return true if the string is null or empty
        return !is( str);
    }

    protected void error(int id, EClientErrors.CodeMsgPair pair, String tail) {
        error(id, pair.code(), pair.msg() + tail);
    }

    protected void send( String str) throws IOException {
        // write string to data buffer; writer thread will
        // write it to socket
        if( !IsEmpty(str)) {
            m_dos.write( str.getBytes() );
        }
        sendEOL();
    }

    private void sendEOL() throws IOException {
        m_dos.write( EOL);
    }

    protected void send( int val) throws IOException {
        send( String.valueOf( val) );
    }

    protected void send( char val) throws IOException {
        m_dos.write( val);
        sendEOL();
    }

    protected void send( double val) throws IOException {
        send( String.valueOf( val) );
    }

    protected void send( long val) throws IOException {
        send( String.valueOf( val) );
    }

    private void sendMax( double val) throws IOException {
        if (val == Double.MAX_VALUE) {
            sendEOL();
        }
        else {
            send(String.valueOf(val));
        }
    }

    private void sendMax( int val) throws IOException {
        if (val == Integer.MAX_VALUE) {
            sendEOL();
        }
        else {
            send(String.valueOf(val));
        }
    }

    protected void send( boolean val) throws IOException {
        send( val ? 1 : 0);
    }

    private static boolean IsEmpty(String str) {
    	return Util.StringIsEmpty(str);
    }

    protected void notConnected() {
        error(EClientErrors.NO_VALID_ID, EClientErrors.NOT_CONNECTED, "");
    }
}
