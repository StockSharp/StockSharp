/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

import java.io.DataInputStream;
import java.io.IOException;
import java.util.Vector;

public class EReader extends Thread {

    // incoming msg id's
    static final int TICK_PRICE		= 1;
    static final int TICK_SIZE		= 2;
    static final int ORDER_STATUS	= 3;
    static final int ERR_MSG		= 4;
    static final int OPEN_ORDER         = 5;
    static final int ACCT_VALUE         = 6;
    static final int PORTFOLIO_VALUE    = 7;
    static final int ACCT_UPDATE_TIME   = 8;
    static final int NEXT_VALID_ID      = 9;
    static final int CONTRACT_DATA      = 10;
    static final int EXECUTION_DATA     = 11;
    static final int MARKET_DEPTH     	= 12;
    static final int MARKET_DEPTH_L2    = 13;
    static final int NEWS_BULLETINS    	= 14;
    static final int MANAGED_ACCTS    	= 15;
    static final int RECEIVE_FA    	    = 16;
    static final int HISTORICAL_DATA    = 17;
    static final int BOND_CONTRACT_DATA = 18;
    static final int SCANNER_PARAMETERS = 19;
    static final int SCANNER_DATA       = 20;
    static final int TICK_OPTION_COMPUTATION = 21;
    static final int TICK_GENERIC = 45;
    static final int TICK_STRING = 46;
    static final int TICK_EFP = 47;
    static final int CURRENT_TIME = 49;
    static final int REAL_TIME_BARS = 50;
    static final int FUNDAMENTAL_DATA = 51;
    static final int CONTRACT_DATA_END = 52;
    static final int OPEN_ORDER_END = 53;
    static final int ACCT_DOWNLOAD_END = 54;
    static final int EXECUTION_DATA_END = 55;
    static final int DELTA_NEUTRAL_VALIDATION = 56;
    static final int TICK_SNAPSHOT_END = 57;
    static final int MARKET_DATA_TYPE = 58;
    static final int COMMISSION_REPORT = 59;
    static final int POSITION = 61;
    static final int POSITION_END = 62;
    static final int ACCOUNT_SUMMARY = 63;
    static final int ACCOUNT_SUMMARY_END = 64;
    static final int VERIFY_MESSAGE_API = 65;
    static final int VERIFY_COMPLETED = 66;
    static final int DISPLAY_GROUP_LIST = 67;
    static final int DISPLAY_GROUP_UPDATED = 68;

    private EClientSocket 	m_parent;
    private DataInputStream m_dis;

    protected EClientSocket parent()    { return m_parent; }
    private EWrapper eWrapper()         { return (EWrapper)parent().wrapper(); }

    public EReader( EClientSocket parent, DataInputStream dis) {
        this("EReader", parent, dis);
    }

    protected EReader( String name, EClientSocket parent, DataInputStream dis) {
        setName( name);
        m_parent = parent;
        m_dis = dis;
    }

    public void run() {
        try {
            // loop until thread is terminated
            while( !isInterrupted() && processMsg(readInt()));
        }
        catch ( Exception ex ) {
        	if (parent().isConnected()) {
        		eWrapper().error( ex);
        	}
        }
        if (parent().isConnected()) {
        	m_parent.close();
        }
        try {
            m_dis.close();
            m_dis = null;
            }
            catch (IOException e) {
        }
    }

    /** Overridden in subclass. */
    protected boolean processMsg(int msgId) throws IOException{
        if( msgId == -1) return false;

        switch( msgId) {
            case TICK_PRICE: {
                int version = readInt();
                int tickerId = readInt();
                int tickType = readInt();
                double price = readDouble();
                int size = 0;
                if( version >= 2) {
                    size = readInt();
                }
                int canAutoExecute = 0;
                if (version >= 3) {
                    canAutoExecute = readInt();
                }
                eWrapper().tickPrice( tickerId, tickType, price, canAutoExecute);

                if( version >= 2) {
                    int sizeTickType = -1 ; // not a tick
                    switch (tickType) {
                        case 1: // BID
                            sizeTickType = 0 ; // BID_SIZE
                            break ;
                        case 2: // ASK
                            sizeTickType = 3 ; // ASK_SIZE
                            break ;
                        case 4: // LAST
                            sizeTickType = 5 ; // LAST_SIZE
                            break ;
                    }
                    if (sizeTickType != -1) {
                        eWrapper().tickSize( tickerId, sizeTickType, size);
                    }
                }
                break;
            }
            case TICK_SIZE: {
                int version = readInt();
                int tickerId = readInt();
                int tickType = readInt();
                int size = readInt();

                eWrapper().tickSize( tickerId, tickType, size);
                break;
            }

            case POSITION:{
                int version = readInt();
                String account = readStr();

                Contract contract = new Contract();
                contract.m_conId = readInt();
                contract.m_symbol = readStr();
                contract.m_secType = readStr();
                contract.m_expiry = readStr();
                contract.m_strike = readDouble();
                contract.m_right = readStr();
                contract.m_multiplier = readStr();
                contract.m_exchange = readStr();
                contract.m_currency = readStr();
                contract.m_localSymbol = readStr();
                if (version >= 2) {
                	contract.m_tradingClass = readStr();
                }

                int pos = readInt();
                double avgCost = 0;
                if (version >= 3) {
                	avgCost = readDouble();
                }

                eWrapper().position( account, contract, pos, avgCost);
                break;
            }

            case POSITION_END:{
                int version = readInt();
                eWrapper().positionEnd();
                break;
            }

            case ACCOUNT_SUMMARY:{
                int version = readInt();
                int reqId = readInt();
                String account = readStr();
                String tag = readStr();
                String value = readStr();
                String currency = readStr();
                eWrapper().accountSummary(reqId, account, tag, value, currency);
                break;
            }

            case ACCOUNT_SUMMARY_END:{
                int version = readInt();
                int reqId = readInt();
                eWrapper().accountSummaryEnd(reqId);
                break;
            }

            case TICK_OPTION_COMPUTATION: {
                int version = readInt();
                int tickerId = readInt();
                int tickType = readInt();
                double impliedVol = readDouble();
            	if (impliedVol < 0) { // -1 is the "not yet computed" indicator
            		impliedVol = Double.MAX_VALUE;
            	}
                double delta = readDouble();
            	if (Math.abs(delta) > 1) { // -2 is the "not yet computed" indicator
            		delta = Double.MAX_VALUE;
            	}
            	double optPrice = Double.MAX_VALUE;
            	double pvDividend = Double.MAX_VALUE;
            	double gamma = Double.MAX_VALUE;
            	double vega = Double.MAX_VALUE;
            	double theta = Double.MAX_VALUE;
            	double undPrice = Double.MAX_VALUE;
            	if (version >= 6 || tickType == TickType.MODEL_OPTION) { // introduced in version == 5
            		optPrice = readDouble();
            		if (optPrice < 0) { // -1 is the "not yet computed" indicator
            			optPrice = Double.MAX_VALUE;
            		}
            		pvDividend = readDouble();
            		if (pvDividend < 0) { // -1 is the "not yet computed" indicator
            			pvDividend = Double.MAX_VALUE;
            		}
            	}
            	if (version >= 6) {
            		gamma = readDouble();
            		if (Math.abs(gamma) > 1) { // -2 is the "not yet computed" indicator
            			gamma = Double.MAX_VALUE;
            		}
            		vega = readDouble();
            		if (Math.abs(vega) > 1) { // -2 is the "not yet computed" indicator
            			vega = Double.MAX_VALUE;
            		}
            		theta = readDouble();
            		if (Math.abs(theta) > 1) { // -2 is the "not yet computed" indicator
            			theta = Double.MAX_VALUE;
            		}
            		undPrice = readDouble();
            		if (undPrice < 0) { // -1 is the "not yet computed" indicator
            			undPrice = Double.MAX_VALUE;
            		}
            	}

            	eWrapper().tickOptionComputation( tickerId, tickType, impliedVol, delta, optPrice, pvDividend, gamma, vega, theta, undPrice);
            	break;
            }

            case TICK_GENERIC: {
                int version = readInt();
                int tickerId = readInt();
                int tickType = readInt();
                double value = readDouble();

                eWrapper().tickGeneric( tickerId, tickType, value);
                break;
            }

            case TICK_STRING: {
                int version = readInt();
                int tickerId = readInt();
                int tickType = readInt();
                String value = readStr();

                eWrapper().tickString( tickerId, tickType, value);
                break;
            }

            case TICK_EFP: {
                int version = readInt();
                int tickerId = readInt();
                int tickType = readInt();
                double basisPoints = readDouble();
                String formattedBasisPoints = readStr();
                double impliedFuturesPrice = readDouble();
                int holdDays = readInt();
                String futureExpiry = readStr();
                double dividendImpact = readDouble();
                double dividendsToExpiry = readDouble();
                eWrapper().tickEFP( tickerId, tickType, basisPoints, formattedBasisPoints,
                					impliedFuturesPrice, holdDays, futureExpiry, dividendImpact, dividendsToExpiry);
                break;
            }

            case ORDER_STATUS: {
                int version = readInt();
                int id = readInt();
                String status = readStr();
                int filled = readInt();
                int remaining = readInt();
                double avgFillPrice = readDouble();

                int permId = 0;
                if( version >= 2) {
                    permId = readInt();
                }

                int parentId = 0;
                if( version >= 3) {
                    parentId = readInt();
                }

                double lastFillPrice = 0;
                if( version >= 4) {
                    lastFillPrice = readDouble();
                }

                int clientId = 0;
                if( version >= 5) {
                    clientId = readInt();
                }

                String whyHeld = null;
                if( version >= 6) {
                	whyHeld = readStr();
                }

                eWrapper().orderStatus( id, status, filled, remaining, avgFillPrice,
                                permId, parentId, lastFillPrice, clientId, whyHeld);
                break;
            }

            case ACCT_VALUE: {
                int version = readInt();
                String key = readStr();
                String val  = readStr();
                String cur = readStr();
                String accountName = null ;
                if( version >= 2) {
                    accountName = readStr();
                }
                eWrapper().updateAccountValue(key, val, cur, accountName);
                break;
            }

            case PORTFOLIO_VALUE: {
                int version = readInt();
                Contract contract = new Contract();
                if (version >= 6) {
                	contract.m_conId = readInt();
                }
                contract.m_symbol  = readStr();
                contract.m_secType = readStr();
                contract.m_expiry  = readStr();
                contract.m_strike  = readDouble();
                contract.m_right   = readStr();
                if (version >= 7) {
                	contract.m_multiplier = readStr();
                	contract.m_primaryExch = readStr();
                }
                contract.m_currency = readStr();
                if ( version >= 2 ) {
                    contract.m_localSymbol = readStr();
                }
                if (version >= 8) {
                    contract.m_tradingClass = readStr();
                }

                int position  = readInt();
                double marketPrice = readDouble();
                double marketValue = readDouble();
                double  averageCost = 0.0;
                double  unrealizedPNL = 0.0;
                double  realizedPNL = 0.0;
                if (version >=3 ) {
                    averageCost = readDouble();
                    unrealizedPNL = readDouble();
                    realizedPNL = readDouble();
                }

                String accountName = null ;
                if( version >= 4) {
                    accountName = readStr();
                }

                if(version == 6 && m_parent.serverVersion() == 39) {
                	contract.m_primaryExch = readStr();
                }

                eWrapper().updatePortfolio(contract, position, marketPrice, marketValue,
                                averageCost, unrealizedPNL, realizedPNL, accountName);

                break;
            }

            case ACCT_UPDATE_TIME: {
                int version = readInt();
                String timeStamp = readStr();
                eWrapper().updateAccountTime(timeStamp);
                break;
            }

            case ERR_MSG: {
                int version = readInt();
                if(version < 2) {
                    String msg = readStr();
                    m_parent.error( msg);
                } else {
                    int id = readInt();
                    int errorCode    = readInt();
                    String errorMsg = readStr();
                    m_parent.error(id, errorCode, errorMsg);
                }
                break;
            }

            case OPEN_ORDER: {
                // read version
                int version = readInt();

                // read order id
                Order order = new Order();
                order.m_orderId = readInt();

                // read contract fields
                Contract contract = new Contract();
                if (version >= 17) {
                	contract.m_conId = readInt();
                }
                contract.m_symbol = readStr();
                contract.m_secType = readStr();
                contract.m_expiry = readStr();
                contract.m_strike = readDouble();
                contract.m_right = readStr();
                if ( version >= 32) {
                   contract.m_multiplier = readStr();
                }
                contract.m_exchange = readStr();
                contract.m_currency = readStr();
                if ( version >= 2 ) {
                    contract.m_localSymbol = readStr();
                }
                if (version >= 32) {
                    contract.m_tradingClass = readStr();
                }

                // read order fields
                order.m_action = readStr();
                order.m_totalQuantity = readInt();
                order.m_orderType = readStr();
                if (version < 29) {
                    order.m_lmtPrice = readDouble();
                }
                else {
                    order.m_lmtPrice = readDoubleMax();
                }
                if (version < 30) {
                    order.m_auxPrice = readDouble();
                }
                else {
                    order.m_auxPrice = readDoubleMax();
                }
                order.m_tif = readStr();
                order.m_ocaGroup = readStr();
                order.m_account = readStr();
                order.m_openClose = readStr();
                order.m_origin = readInt();
                order.m_orderRef = readStr();

                if(version >= 3) {
                    order.m_clientId = readInt();
                }

                if( version >= 4 ) {
                    order.m_permId = readInt();
                    if ( version < 18) {
                        // will never happen
                    	/* order.m_ignoreRth = */ readBoolFromInt();
                    }
                    else {
                    	order.m_outsideRth = readBoolFromInt();
                    }
                    order.m_hidden = readInt() == 1;
                    order.m_discretionaryAmt = readDouble();
                }

                if ( version >= 5 ) {
                    order.m_goodAfterTime = readStr();
                }

                if ( version >= 6 ) {
                	// skip deprecated sharesAllocation field
                    readStr();
                }

                if ( version >= 7 ) {
                    order.m_faGroup = readStr();
                    order.m_faMethod = readStr();
                    order.m_faPercentage = readStr();
                    order.m_faProfile = readStr();
                }

                if ( version >= 8 ) {
                    order.m_goodTillDate = readStr();
                }

                if ( version >= 9) {
                    order.m_rule80A = readStr();
                    order.m_percentOffset = readDoubleMax();
                    order.m_settlingFirm = readStr();
                    order.m_shortSaleSlot = readInt();
                    order.m_designatedLocation = readStr();
                    if ( m_parent.serverVersion() == 51){
                        readInt(); // exemptCode
                    }
                    else if ( version >= 23){
                    	order.m_exemptCode = readInt();
                    }
                    order.m_auctionStrategy = readInt();
                    order.m_startingPrice = readDoubleMax();
                    order.m_stockRefPrice = readDoubleMax();
                    order.m_delta = readDoubleMax();
                    order.m_stockRangeLower = readDoubleMax();
                    order.m_stockRangeUpper = readDoubleMax();
                    order.m_displaySize = readInt();
                    if ( version < 18) {
                        // will never happen
                    	/* order.m_rthOnly = */ readBoolFromInt();
                    }
                    order.m_blockOrder = readBoolFromInt();
                    order.m_sweepToFill = readBoolFromInt();
                    order.m_allOrNone = readBoolFromInt();
                    order.m_minQty = readIntMax();
                    order.m_ocaType = readInt();
                    order.m_eTradeOnly = readBoolFromInt();
                    order.m_firmQuoteOnly = readBoolFromInt();
                    order.m_nbboPriceCap = readDoubleMax();
                }

                if ( version >= 10) {
                    order.m_parentId = readInt();
                    order.m_triggerMethod = readInt();
                }

                if (version >= 11) {
                    order.m_volatility = readDoubleMax();
                    order.m_volatilityType = readInt();
                    if (version == 11) {
                    	int receivedInt = readInt();
                    	order.m_deltaNeutralOrderType = ( (receivedInt == 0) ? "NONE" : "MKT" );
                    } else { // version 12 and up
                    	order.m_deltaNeutralOrderType = readStr();
                    	order.m_deltaNeutralAuxPrice = readDoubleMax();

                        if (version >= 27 && !Util.StringIsEmpty(order.m_deltaNeutralOrderType)) {
                            order.m_deltaNeutralConId = readInt();
                            order.m_deltaNeutralSettlingFirm = readStr();
                            order.m_deltaNeutralClearingAccount = readStr();
                            order.m_deltaNeutralClearingIntent = readStr();
                        }

                        if (version >= 31 && !Util.StringIsEmpty(order.m_deltaNeutralOrderType)) {
                            order.m_deltaNeutralOpenClose = readStr();
                            order.m_deltaNeutralShortSale = readBoolFromInt();
                            order.m_deltaNeutralShortSaleSlot = readInt();
                            order.m_deltaNeutralDesignatedLocation = readStr();
                        }
                    }
                    order.m_continuousUpdate = readInt();
                    if (m_parent.serverVersion() == 26) {
                    	order.m_stockRangeLower = readDouble();
                    	order.m_stockRangeUpper = readDouble();
                    }
                    order.m_referencePriceType = readInt();
                }

                if (version >= 13) {
                	order.m_trailStopPrice = readDoubleMax();
                }

                if (version >= 30) {
                	order.m_trailingPercent = readDoubleMax();
                }

                if (version >= 14) {
                	order.m_basisPoints = readDoubleMax();
                	order.m_basisPointsType = readIntMax();
                	contract.m_comboLegsDescrip = readStr();
                }

                if (version >= 29) {
                	int comboLegsCount = readInt();
                	if (comboLegsCount > 0) {
                		contract.m_comboLegs = new Vector<ComboLeg>(comboLegsCount);
                		for (int i = 0; i < comboLegsCount; ++i) {
                			int conId = readInt();
                			int ratio = readInt();
                			String action = readStr();
                			String exchange = readStr();
                			int openClose = readInt();
                			int shortSaleSlot = readInt();
                			String designatedLocation = readStr();
                			int exemptCode = readInt();

                			ComboLeg comboLeg = new ComboLeg(conId, ratio, action, exchange, openClose,
                					shortSaleSlot, designatedLocation, exemptCode);
                			contract.m_comboLegs.add(comboLeg);
                		}
                	}

                	int orderComboLegsCount = readInt();
                	if (orderComboLegsCount > 0) {
                		order.m_orderComboLegs = new Vector<OrderComboLeg>(orderComboLegsCount);
                		for (int i = 0; i < orderComboLegsCount; ++i) {
                			double price = readDoubleMax();

                			OrderComboLeg orderComboLeg = new OrderComboLeg(price);
                			order.m_orderComboLegs.add(orderComboLeg);
                		}
                	}
                }

                if (version >= 26) {
                	int smartComboRoutingParamsCount = readInt();
                	if (smartComboRoutingParamsCount > 0) {
                		order.m_smartComboRoutingParams = new Vector<TagValue>(smartComboRoutingParamsCount);
                		for (int i = 0; i < smartComboRoutingParamsCount; ++i) {
                			TagValue tagValue = new TagValue();
                			tagValue.m_tag = readStr();
                			tagValue.m_value = readStr();
                			order.m_smartComboRoutingParams.add(tagValue);
                		}
                	}
                }

                if (version >= 15) {
                	if (version >= 20) {
                		order.m_scaleInitLevelSize = readIntMax();
                		order.m_scaleSubsLevelSize = readIntMax();
                	}
                	else {
                		/* int notSuppScaleNumComponents = */ readIntMax();
                		order.m_scaleInitLevelSize = readIntMax();
                	}
                	order.m_scalePriceIncrement = readDoubleMax();
                }

                if (version >= 28 && order.m_scalePriceIncrement > 0.0 && order.m_scalePriceIncrement != Double.MAX_VALUE) {
                    order.m_scalePriceAdjustValue = readDoubleMax();
                    order.m_scalePriceAdjustInterval = readIntMax();
                    order.m_scaleProfitOffset = readDoubleMax();
                    order.m_scaleAutoReset = readBoolFromInt();
                    order.m_scaleInitPosition = readIntMax();
                    order.m_scaleInitFillQty = readIntMax();
                    order.m_scaleRandomPercent = readBoolFromInt();
                }

                if (version >= 24) {
                	order.m_hedgeType = readStr();
                	if (!Util.StringIsEmpty(order.m_hedgeType)) {
                		order.m_hedgeParam = readStr();
                	}
                }

                if (version >= 25) {
                	order.m_optOutSmartRouting = readBoolFromInt();
                }

                if (version >= 19) {
                	order.m_clearingAccount = readStr();
                	order.m_clearingIntent = readStr();
                }

                if (version >= 22) {
                	order.m_notHeld = readBoolFromInt();
                }

                if (version >= 20) {
                    if (readBoolFromInt()) {
                        UnderComp underComp = new UnderComp();
                        underComp.m_conId = readInt();
                        underComp.m_delta = readDouble();
                        underComp.m_price = readDouble();
                        contract.m_underComp = underComp;
                    }
                }

                if (version >= 21) {
                	order.m_algoStrategy = readStr();
                	if (!Util.StringIsEmpty(order.m_algoStrategy)) {
                		int algoParamsCount = readInt();
                		if (algoParamsCount > 0) {
                			order.m_algoParams = new Vector<TagValue>(algoParamsCount);
                			for (int i = 0; i < algoParamsCount; ++i) {
                				TagValue tagValue = new TagValue();
                				tagValue.m_tag = readStr();
                				tagValue.m_value = readStr();
                				order.m_algoParams.add(tagValue);
                			}
                		}
                	}
                }

                OrderState orderState = new OrderState();

                if (version >= 16) {

                	order.m_whatIf = readBoolFromInt();

                	orderState.m_status = readStr();
                	orderState.m_initMargin = readStr();
                	orderState.m_maintMargin = readStr();
                	orderState.m_equityWithLoan = readStr();
                	orderState.m_commission = readDoubleMax();
                	orderState.m_minCommission = readDoubleMax();
                	orderState.m_maxCommission = readDoubleMax();
                	orderState.m_commissionCurrency = readStr();
                	orderState.m_warningText = readStr();
                }

                eWrapper().openOrder( order.m_orderId, contract, order, orderState);
                break;
            }

            case NEXT_VALID_ID: {
                int version = readInt();
                int orderId = readInt();
                eWrapper().nextValidId( orderId);
                break;
            }

            case SCANNER_DATA: {
                ContractDetails contract = new ContractDetails();
                int version = readInt();
                int tickerId = readInt();
                int numberOfElements = readInt();
                for (int ctr=0; ctr < numberOfElements; ctr++) {
                    int rank = readInt();
                    if (version >= 3) {
                    	contract.m_summary.m_conId = readInt();
                    }
                    contract.m_summary.m_symbol = readStr();
                    contract.m_summary.m_secType = readStr();
                    contract.m_summary.m_expiry = readStr();
                    contract.m_summary.m_strike = readDouble();
                    contract.m_summary.m_right = readStr();
                    contract.m_summary.m_exchange = readStr();
                    contract.m_summary.m_currency = readStr();
                    contract.m_summary.m_localSymbol = readStr();
                    contract.m_marketName = readStr();
                    contract.m_summary.m_tradingClass = readStr();
                    String distance = readStr();
                    String benchmark = readStr();
                    String projection = readStr();
                    String legsStr = null;
                    if (version >= 2) {
                    	legsStr = readStr();
                    }
                    eWrapper().scannerData(tickerId, rank, contract, distance,
                        benchmark, projection, legsStr);
                }
                eWrapper().scannerDataEnd(tickerId);
                break;
            }

            case CONTRACT_DATA: {
                int version = readInt();

                int reqId = -1;
                if (version >= 3) {
                	reqId = readInt();
                }

                ContractDetails contract = new ContractDetails();
                contract.m_summary.m_symbol = readStr();
                contract.m_summary.m_secType = readStr();
                contract.m_summary.m_expiry = readStr();
                contract.m_summary.m_strike = readDouble();
                contract.m_summary.m_right = readStr();
                contract.m_summary.m_exchange = readStr();
                contract.m_summary.m_currency = readStr();
                contract.m_summary.m_localSymbol = readStr();
                contract.m_marketName = readStr();
                contract.m_summary.m_tradingClass = readStr();
                contract.m_summary.m_conId = readInt();
                contract.m_minTick = readDouble();
                contract.m_summary.m_multiplier = readStr();
                contract.m_orderTypes = readStr();
                contract.m_validExchanges = readStr();
                if (version >= 2) {
                    contract.m_priceMagnifier = readInt();
                }
                if (version >= 4) {
                	contract.m_underConId = readInt();
                }
                if( version >= 5) {
                   contract.m_longName = readStr();
                   contract.m_summary.m_primaryExch = readStr();
                }
                if( version >= 6) {
                    contract.m_contractMonth = readStr();
                    contract.m_industry = readStr();
                    contract.m_category = readStr();
                    contract.m_subcategory = readStr();
                    contract.m_timeZoneId = readStr();
                    contract.m_tradingHours = readStr();
                    contract.m_liquidHours = readStr();
                 }
                if (version >= 8) {
                    contract.m_evRule = readStr();
                    contract.m_evMultiplier = readDouble();
                }
                if (version >= 7) {
                    int secIdListCount = readInt();
                        if (secIdListCount  > 0) {
                            contract.m_secIdList = new Vector<TagValue>(secIdListCount);
                            for (int i = 0; i < secIdListCount; ++i) {
                                TagValue tagValue = new TagValue();
                                tagValue.m_tag = readStr();
                                tagValue.m_value = readStr();
                                contract.m_secIdList.add(tagValue);
                            }
                        }
                }

                eWrapper().contractDetails( reqId, contract);
                break;
            }
            case BOND_CONTRACT_DATA: {
                int version = readInt();

                int reqId = -1;
                if (version >= 3) {
                	reqId = readInt();
                }

                ContractDetails contract = new ContractDetails();

                contract.m_summary.m_symbol = readStr();
                contract.m_summary.m_secType = readStr();
                contract.m_cusip = readStr();
                contract.m_coupon = readDouble();
                contract.m_maturity = readStr();
                contract.m_issueDate  = readStr();
                contract.m_ratings = readStr();
                contract.m_bondType = readStr();
                contract.m_couponType = readStr();
                contract.m_convertible = readBoolFromInt();
                contract.m_callable = readBoolFromInt();
                contract.m_putable = readBoolFromInt();
                contract.m_descAppend = readStr();
                contract.m_summary.m_exchange = readStr();
                contract.m_summary.m_currency = readStr();
                contract.m_marketName = readStr();
                contract.m_summary.m_tradingClass = readStr();
                contract.m_summary.m_conId = readInt();
                contract.m_minTick = readDouble();
                contract.m_orderTypes = readStr();
                contract.m_validExchanges = readStr();
                if (version >= 2) {
                	contract.m_nextOptionDate = readStr();
                	contract.m_nextOptionType = readStr();
                	contract.m_nextOptionPartial = readBoolFromInt();
                	contract.m_notes = readStr();
                }
                if( version >= 4) {
                   contract.m_longName = readStr();
                }
                if ( version >= 6) {
                    contract.m_evRule = readStr();
                    contract.m_evMultiplier = readDouble();
                }
                if (version >= 5) {
                    int secIdListCount = readInt();
                        if (secIdListCount  > 0) {
                            contract.m_secIdList = new Vector<TagValue>(secIdListCount);
                            for (int i = 0; i < secIdListCount; ++i) {
                                TagValue tagValue = new TagValue();
                                tagValue.m_tag = readStr();
                                tagValue.m_value = readStr();
                                contract.m_secIdList.add(tagValue);
                            }
                        }
                }

                eWrapper().bondContractDetails( reqId, contract);
                break;
            }
            case EXECUTION_DATA: {
                int version = readInt();

                int reqId = -1;
                if (version >= 7) {
                	reqId = readInt();
                }

                int orderId = readInt();

                // read contract fields
                Contract contract = new Contract();
                if (version >= 5) {
                	contract.m_conId = readInt();
                }
                contract.m_symbol = readStr();
                contract.m_secType = readStr();
                contract.m_expiry = readStr();
                contract.m_strike = readDouble();
                contract.m_right = readStr();
                if (version >= 9) {
                    contract.m_multiplier = readStr();
                }
                contract.m_exchange = readStr();
                contract.m_currency = readStr();
                contract.m_localSymbol = readStr();
                if (version >= 10) {
                    contract.m_tradingClass = readStr();
                }

                Execution exec = new Execution();
                exec.m_orderId = orderId;
                exec.m_execId = readStr();
                exec.m_time = readStr();
                exec.m_acctNumber = readStr();
                exec.m_exchange = readStr();
                exec.m_side = readStr();
                exec.m_shares = readInt();
                exec.m_price = readDouble();
                if ( version >= 2 ) {
                    exec.m_permId = readInt();
                }
                if ( version >= 3) {
                    exec.m_clientId = readInt();
                }
                if ( version >= 4) {
                    exec.m_liquidation = readInt();
                }
                if (version >= 6) {
                	exec.m_cumQty = readInt();
                	exec.m_avgPrice = readDouble();
                }
                if (version >= 8) {
                    exec.m_orderRef = readStr();
                }
                if (version >= 9) {
                    exec.m_evRule = readStr();
                    exec.m_evMultiplier = readDouble();
                }

                eWrapper().execDetails( reqId, contract, exec);
                break;
            }
            case MARKET_DEPTH: {
                int version = readInt();
                int id = readInt();

                int position = readInt();
                int operation = readInt();
                int side = readInt();
                double price = readDouble();
                int size = readInt();

                eWrapper().updateMktDepth(id, position, operation,
                                side, price, size);
                break;
            }
            case MARKET_DEPTH_L2: {
                int version = readInt();
                int id = readInt();

                int position = readInt();
                String marketMaker = readStr();
                int operation = readInt();
                int side = readInt();
                double price = readDouble();
                int size = readInt();

                eWrapper().updateMktDepthL2(id, position, marketMaker,
                                operation, side, price, size);
                break;
            }
            case NEWS_BULLETINS: {
                int version = readInt();
                int newsMsgId = readInt();
                int newsMsgType = readInt();
                String newsMessage = readStr();
                String originatingExch = readStr();

                eWrapper().updateNewsBulletin( newsMsgId, newsMsgType, newsMessage, originatingExch);
                break;
            }
            case MANAGED_ACCTS: {
                int version = readInt();
                String accountsList = readStr();

                eWrapper().managedAccounts( accountsList);
                break;
            }
            case RECEIVE_FA: {
              int version = readInt();
              int faDataType = readInt();
              String xml = readStr();

              eWrapper().receiveFA(faDataType, xml);
              break;
            }
            case HISTORICAL_DATA: {
              int version = readInt();
              int reqId = readInt();
        	  String startDateStr;
        	  String endDateStr;
        	  String completedIndicator = "finished";
              if (version >= 2) {
            	  startDateStr = readStr();
            	  endDateStr = readStr();
            	  completedIndicator += "-" + startDateStr + "-" + endDateStr;
              }
              int itemCount = readInt();
              for (int ctr = 0; ctr < itemCount; ctr++) {
                String date = readStr();
                double open = readDouble();
                double high = readDouble();
                double low = readDouble();
                double close = readDouble();
                int volume = readInt();
                double WAP = readDouble();
                String hasGaps = readStr();
                int barCount = -1;
                if (version >= 3) {
                	barCount = readInt();
                }
                eWrapper().historicalData(reqId, date, open, high, low,
                                        close, volume, barCount, WAP,
                                        Boolean.valueOf(hasGaps).booleanValue());
              }
              // send end of dataset marker
              eWrapper().historicalData(reqId, completedIndicator, -1, -1, -1, -1, -1, -1, -1, false);
              break;
            }
            case SCANNER_PARAMETERS: {
                int version = readInt();
                String xml = readStr();
                eWrapper().scannerParameters(xml);
                break;
            }
            case CURRENT_TIME: {
                /*int version =*/ readInt();
                long time = readLong();
                eWrapper().currentTime(time);
                break;
            }
            case REAL_TIME_BARS: {
                /*int version =*/ readInt();
                int reqId = readInt();
                long time = readLong();
                double open = readDouble();
                double high = readDouble();
                double low = readDouble();
                double close = readDouble();
                long volume = readLong();
                double wap = readDouble();
                int count = readInt();
                eWrapper().realtimeBar(reqId, time, open, high, low, close, volume, wap, count);
                break;
            }
            case FUNDAMENTAL_DATA: {
                /*int version =*/ readInt();
                int reqId = readInt();
                String data = readStr();
                eWrapper().fundamentalData(reqId, data);
                break;
            }
            case CONTRACT_DATA_END: {
                /*int version =*/ readInt();
                int reqId = readInt();
                eWrapper().contractDetailsEnd(reqId);
                break;
            }
            case OPEN_ORDER_END: {
                /*int version =*/ readInt();
                eWrapper().openOrderEnd();
                break;
            }
            case ACCT_DOWNLOAD_END: {
                /*int version =*/ readInt();
                String accountName = readStr();
                eWrapper().accountDownloadEnd( accountName);
                break;
            }
            case EXECUTION_DATA_END: {
                /*int version =*/ readInt();
                int reqId = readInt();
                eWrapper().execDetailsEnd( reqId);
                break;
            }
            case DELTA_NEUTRAL_VALIDATION: {
                /*int version =*/ readInt();
                int reqId = readInt();

                UnderComp underComp = new UnderComp();
                underComp.m_conId = readInt();
                underComp.m_delta = readDouble();
                underComp.m_price = readDouble();

                eWrapper().deltaNeutralValidation( reqId, underComp);
                break;
            }
            case TICK_SNAPSHOT_END: {
                /*int version =*/ readInt();
                int reqId = readInt();

                eWrapper().tickSnapshotEnd( reqId);
                break;
            }
            case MARKET_DATA_TYPE: {
                /*int version =*/ readInt();
                int reqId = readInt();
                int marketDataType = readInt();

                eWrapper().marketDataType( reqId, marketDataType);
                break;
            }
            case COMMISSION_REPORT: {
                /*int version =*/ readInt();

                CommissionReport commissionReport = new CommissionReport();
                commissionReport.m_execId = readStr();
                commissionReport.m_commission = readDouble();
                commissionReport.m_currency = readStr();
                commissionReport.m_realizedPNL = readDouble();
                commissionReport.m_yield = readDouble();
                commissionReport.m_yieldRedemptionDate = readInt();

                eWrapper().commissionReport( commissionReport);
                break;
            }
            case VERIFY_MESSAGE_API: {
                /*int version =*/ readInt();
                String apiData = readStr();

                eWrapper().verifyMessageAPI(apiData);
                break;
            }
            case VERIFY_COMPLETED: {
                /*int version =*/ readInt();
                String isSuccessfulStr = readStr();
                boolean isSuccessful = "true".equals(isSuccessfulStr);
                String errorText = readStr();


                if (isSuccessful) {
                    m_parent.startAPI();
                }

                eWrapper().verifyCompleted(isSuccessful, errorText);
                break;
            }
            case DISPLAY_GROUP_LIST: {
                /*int version =*/ readInt();
                int reqId = readInt();
                String groups = readStr();

                eWrapper().displayGroupList(reqId, groups);
                break;
            }
            case DISPLAY_GROUP_UPDATED: {
                /*int version =*/ readInt();
                int reqId = readInt();
                String contractInfo = readStr();

                eWrapper().displayGroupUpdated(reqId, contractInfo);
                break;
            }

            default: {
                m_parent.error( EClientErrors.NO_VALID_ID, EClientErrors.UNKNOWN_ID.code(), EClientErrors.UNKNOWN_ID.msg());
                return false;
            }
        }
        return true;
    }


    protected String readStr() throws IOException {
        StringBuffer buf = new StringBuffer();
        while( true) {
            byte c = m_dis.readByte();
            if( c == 0) {
                break;
            }
            buf.append( (char)c);
        }

        String str = buf.toString();
        return str.length() == 0 ? null : str;
    }


    boolean readBoolFromInt() throws IOException {
        String str = readStr();
        return str == null ? false : (Integer.parseInt( str) != 0);
    }

    protected int readInt() throws IOException {
        String str = readStr();
        return str == null ? 0 : Integer.parseInt( str);
    }

    protected int readIntMax() throws IOException {
        String str = readStr();
        return (str == null || str.length() == 0) ? Integer.MAX_VALUE
        	                                      : Integer.parseInt( str);
    }

    protected long readLong() throws IOException {
        String str = readStr();
        return str == null ? 0l : Long.parseLong(str);
    }

    protected double readDouble() throws IOException {
        String str = readStr();
        return str == null ? 0 : Double.parseDouble( str);
    }

    protected double readDoubleMax() throws IOException {
        String str = readStr();
        return (str == null || str.length() == 0) ? Double.MAX_VALUE
        	                                      : Double.parseDouble( str);
    }
}
