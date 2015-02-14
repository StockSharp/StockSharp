/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

import java.text.DateFormat;
import java.util.Date;
import java.util.Vector;

public class EWrapperMsgGenerator extends AnyWrapperMsgGenerator {
    public static final String SCANNER_PARAMETERS = "SCANNER PARAMETERS:";
    public static final String FINANCIAL_ADVISOR = "FA:";
    
	static public String tickPrice( int tickerId, int field, double price, int canAutoExecute) {
    	return "id=" + tickerId + "  " + TickType.getField( field) + "=" + price + " " + 
        ((canAutoExecute != 0) ? " canAutoExecute" : " noAutoExecute");
    }
	
    static public String tickSize( int tickerId, int field, int size) {
    	return "id=" + tickerId + "  " + TickType.getField( field) + "=" + size;
    }
    
    static public String tickOptionComputation( int tickerId, int field, double impliedVol,
    		double delta, double optPrice, double pvDividend,
    		double gamma, double vega, double theta, double undPrice) {
    	String toAdd = "id=" + tickerId + "  " + TickType.getField( field) +
    		": vol = " + ((impliedVol >= 0 && impliedVol != Double.MAX_VALUE) ? Double.toString(impliedVol) : "N/A") +
    		" delta = " + ((Math.abs(delta) <= 1) ? Double.toString(delta) : "N/A") +
    		" gamma = " + ((Math.abs(gamma) <= 1) ? Double.toString(gamma) : "N/A") +
    		" vega = " + ((Math.abs(vega) <= 1) ? Double.toString(vega) : "N/A") +
    		" theta = " + ((Math.abs(theta) <= 1) ? Double.toString(theta) : "N/A") +
    		" optPrice = " + ((optPrice >= 0 && optPrice != Double.MAX_VALUE) ? Double.toString(optPrice) : "N/A") +
    		" pvDividend = " + ((pvDividend >= 0 && pvDividend != Double.MAX_VALUE) ? Double.toString(pvDividend) : "N/A") +
    		" undPrice = " + ((undPrice >= 0 && undPrice != Double.MAX_VALUE) ? Double.toString(undPrice) : "N/A");
		return toAdd;
    }
    
    static public String tickGeneric(int tickerId, int tickType, double value) {
    	return "id=" + tickerId + "  " + TickType.getField( tickType) + "=" + value;
    }
    
    static public String tickString(int tickerId, int tickType, String value) {
    	return "id=" + tickerId + "  " + TickType.getField( tickType) + "=" + value;
    }
    
    static public String tickEFP(int tickerId, int tickType, double basisPoints,
			String formattedBasisPoints, double impliedFuture, int holdDays,
			String futureExpiry, double dividendImpact, double dividendsToExpiry) {
    	return "id=" + tickerId + "  " + TickType.getField(tickType)
		+ ": basisPoints = " + basisPoints + "/" + formattedBasisPoints
		+ " impliedFuture = " + impliedFuture + " holdDays = " + holdDays +
		" futureExpiry = " + futureExpiry + " dividendImpact = " + dividendImpact +
		" dividends to expiry = "	+ dividendsToExpiry;
    }
    
    static public String orderStatus( int orderId, String status, int filled, int remaining,
            double avgFillPrice, int permId, int parentId, double lastFillPrice,
            int clientId, String whyHeld) {
    	return "order status: orderId=" + orderId + " clientId=" + clientId + " permId=" + permId +
        " status=" + status + " filled=" + filled + " remaining=" + remaining +
        " avgFillPrice=" + avgFillPrice + " lastFillPrice=" + lastFillPrice +
        " parent Id=" + parentId + " whyHeld=" + whyHeld;
    }
    
    static public String openOrder( int orderId, Contract contract, Order order, OrderState orderState) {
        String msg = "open order: orderId=" + orderId +
        " action=" + order.m_action +
        " quantity=" + order.m_totalQuantity +
        " conid=" + contract.m_conId + 
        " symbol=" + contract.m_symbol + 
        " secType=" + contract.m_secType + 
        " expiry=" + contract.m_expiry + 
        " strike=" + contract.m_strike + 
        " right=" + contract.m_right + 
        " multiplier=" + contract.m_multiplier + 
        " exchange=" + contract.m_exchange + 
        " primaryExch=" + contract.m_primaryExch + 
        " currency=" + contract.m_currency + 
        " localSymbol=" + contract.m_localSymbol + 
        " tradingClass=" + contract.m_tradingClass + 
        " type=" + order.m_orderType +
        " lmtPrice=" + Util.DoubleMaxString(order.m_lmtPrice) +
        " auxPrice=" + Util.DoubleMaxString(order.m_auxPrice) +
        " TIF=" + order.m_tif +
        " localSymbol=" + contract.m_localSymbol +
        " client Id=" + order.m_clientId +
        " parent Id=" + order.m_parentId +
        " permId=" + order.m_permId +
        " outsideRth=" + order.m_outsideRth +
        " hidden=" + order.m_hidden +
        " discretionaryAmt=" + order.m_discretionaryAmt +
        " displaySize=" + order.m_displaySize +
        " triggerMethod=" + order.m_triggerMethod +
        " goodAfterTime=" + order.m_goodAfterTime +
        " goodTillDate=" + order.m_goodTillDate +
        " faGroup=" + order.m_faGroup +
        " faMethod=" + order.m_faMethod +
        " faPercentage=" + order.m_faPercentage +
        " faProfile=" + order.m_faProfile +
        " shortSaleSlot=" + order.m_shortSaleSlot +
        " designatedLocation=" + order.m_designatedLocation +
        " exemptCode=" + order.m_exemptCode +
        " ocaGroup=" + order.m_ocaGroup +
        " ocaType=" + order.m_ocaType +
        " rule80A=" + order.m_rule80A +
        " allOrNone=" + order.m_allOrNone +
        " minQty=" + Util.IntMaxString(order.m_minQty) +
        " percentOffset=" + Util.DoubleMaxString(order.m_percentOffset) +
        " eTradeOnly=" + order.m_eTradeOnly +
        " firmQuoteOnly=" + order.m_firmQuoteOnly +
        " nbboPriceCap=" + Util.DoubleMaxString(order.m_nbboPriceCap) +
        " optOutSmartRouting=" + order.m_optOutSmartRouting +
        " auctionStrategy=" + order.m_auctionStrategy +
        " startingPrice=" + Util.DoubleMaxString(order.m_startingPrice) +
        " stockRefPrice=" + Util.DoubleMaxString(order.m_stockRefPrice) +
        " delta=" + Util.DoubleMaxString(order.m_delta) +
        " stockRangeLower=" + Util.DoubleMaxString(order.m_stockRangeLower) +
        " stockRangeUpper=" + Util.DoubleMaxString(order.m_stockRangeUpper) +
        " volatility=" + Util.DoubleMaxString(order.m_volatility) +
        " volatilityType=" + order.m_volatilityType +
        " deltaNeutralOrderType=" + order.m_deltaNeutralOrderType +
        " deltaNeutralAuxPrice=" + Util.DoubleMaxString(order.m_deltaNeutralAuxPrice) +
        " deltaNeutralConId=" + order.m_deltaNeutralConId +
        " deltaNeutralSettlingFirm=" + order.m_deltaNeutralSettlingFirm +
        " deltaNeutralClearingAccount=" + order.m_deltaNeutralClearingAccount +
        " deltaNeutralClearingIntent=" + order.m_deltaNeutralClearingIntent +
        " deltaNeutralOpenClose=" + order.m_deltaNeutralOpenClose +
        " deltaNeutralShortSale=" + order.m_deltaNeutralShortSale +
        " deltaNeutralShortSaleSlot=" + order.m_deltaNeutralShortSaleSlot +
        " deltaNeutralDesignatedLocation=" + order.m_deltaNeutralDesignatedLocation +
        " continuousUpdate=" + order.m_continuousUpdate +
        " referencePriceType=" + order.m_referencePriceType +
        " trailStopPrice=" + Util.DoubleMaxString(order.m_trailStopPrice) +
        " trailingPercent=" + Util.DoubleMaxString(order.m_trailingPercent) +
        " scaleInitLevelSize=" + Util.IntMaxString(order.m_scaleInitLevelSize) +
        " scaleSubsLevelSize=" + Util.IntMaxString(order.m_scaleSubsLevelSize) +
        " scalePriceIncrement=" + Util.DoubleMaxString(order.m_scalePriceIncrement) +
        " scalePriceAdjustValue=" + Util.DoubleMaxString(order.m_scalePriceAdjustValue) +
        " scalePriceAdjustInterval=" + Util.IntMaxString(order.m_scalePriceAdjustInterval) +
        " scaleProfitOffset=" + Util.DoubleMaxString(order.m_scaleProfitOffset) +
        " scaleAutoReset=" + order.m_scaleAutoReset +
        " scaleInitPosition=" + Util.IntMaxString(order.m_scaleInitPosition) +
        " scaleInitFillQty=" + Util.IntMaxString(order.m_scaleInitFillQty) +
        " scaleRandomPercent=" + order.m_scaleRandomPercent +
        " hedgeType=" + order.m_hedgeType +
        " hedgeParam=" + order.m_hedgeParam +
        " account=" + order.m_account +
        " settlingFirm=" + order.m_settlingFirm +
        " clearingAccount=" + order.m_clearingAccount +
        " clearingIntent=" + order.m_clearingIntent +
        " notHeld=" + order.m_notHeld +
        " whatIf=" + order.m_whatIf
        ;

        if ("BAG".equals(contract.m_secType)) {
        	if (contract.m_comboLegsDescrip != null) {
        		msg += " comboLegsDescrip=" + contract.m_comboLegsDescrip;
        	}
        	
           	msg += " comboLegs={";
            if (contract.m_comboLegs != null) {
            	for (int i = 0; i < contract.m_comboLegs.size(); ++i) {
            		ComboLeg comboLeg = contract.m_comboLegs.get(i);
            		msg += " leg " + (i+1) + ": "; 
            		msg += "conId=" +  comboLeg.m_conId;
            		msg += " ratio=" +  comboLeg.m_ratio;
            		msg += " action=" +  comboLeg.m_action;
            		msg += " exchange=" +  comboLeg.m_exchange;
            		msg += " openClose=" +  comboLeg.m_openClose;
            		msg += " shortSaleSlot=" +  comboLeg.m_shortSaleSlot;
            		msg += " designatedLocation=" +  comboLeg.m_designatedLocation;
            		msg += " exemptCode=" +  comboLeg.m_exemptCode;
            		if (order.m_orderComboLegs != null && contract.m_comboLegs.size() == order.m_orderComboLegs.size()) {
            			OrderComboLeg orderComboLeg = order.m_orderComboLegs.get(i);
            			msg += " price=" +  Util.DoubleMaxString(orderComboLeg.m_price);
            		}
            		msg += ";";
            	}
            }
           	msg += "}";
           	
        	if (order.m_basisPoints != Double.MAX_VALUE) {
        		msg += " basisPoints=" + Util.DoubleMaxString(order.m_basisPoints);
        		msg += " basisPointsType=" + Util.IntMaxString(order.m_basisPointsType);
        	}
        }
        
    	if (contract.m_underComp != null) {
    		UnderComp underComp = contract.m_underComp;
    		msg +=
    			" underComp.conId =" + underComp.m_conId +
    			" underComp.delta =" + underComp.m_delta +
    			" underComp.price =" + underComp.m_price ;
    	}
    	
    	if (!Util.StringIsEmpty(order.m_algoStrategy)) {
    		msg += " algoStrategy=" + order.m_algoStrategy;
    		msg += " algoParams={";
    		if (order.m_algoParams != null) {
    			Vector algoParams = order.m_algoParams;
    			for (int i = 0; i < algoParams.size(); ++i) {
    				TagValue param = (TagValue)algoParams.elementAt(i);
    				if (i > 0) {
    					msg += ",";
    				}
    				msg += param.m_tag + "=" + param.m_value;
    			}
    		}
    		msg += "}";
    	}
    	
        if ("BAG".equals(contract.m_secType)) {
        	msg += " smartComboRoutingParams={";
        	if (order.m_smartComboRoutingParams != null) {
        		Vector smartComboRoutingParams = order.m_smartComboRoutingParams;
        		for (int i = 0; i < smartComboRoutingParams.size(); ++i) {
        			TagValue param = (TagValue)smartComboRoutingParams.elementAt(i);
        			if (i > 0) {
        				msg += ",";
        			}
        			msg += param.m_tag + "=" + param.m_value;
        		}
        	}
        	msg += "}";
        }
    
        String orderStateMsg =
        	" status=" + orderState.m_status
        	+ " initMargin=" + orderState.m_initMargin
        	+ " maintMargin=" + orderState.m_maintMargin
        	+ " equityWithLoan=" + orderState.m_equityWithLoan
        	+ " commission=" + Util.DoubleMaxString(orderState.m_commission)
        	+ " minCommission=" + Util.DoubleMaxString(orderState.m_minCommission)
        	+ " maxCommission=" + Util.DoubleMaxString(orderState.m_maxCommission)
        	+ " commissionCurrency=" + orderState.m_commissionCurrency
        	+ " warningText=" + orderState.m_warningText
		;

        return msg + orderStateMsg;
    }
    
    static public String openOrderEnd() {
    	return " =============== end ===============";
    }
    
    static public String updateAccountValue(String key, String value, String currency, String accountName) {
    	return "updateAccountValue: " + key + " " + value + " " + currency + " " + accountName;
    }
    
    static public String updatePortfolio(Contract contract, int position, double marketPrice,
    									 double marketValue, double averageCost, double unrealizedPNL,
    									 double realizedPNL, String accountName) {
    	String msg = "updatePortfolio: "
    		+ contractMsg(contract)
    		+ position + " " + marketPrice + " " + marketValue + " " + averageCost + " " + unrealizedPNL + " " + realizedPNL + " " + accountName;
    	return msg;
    }
    
    static public String updateAccountTime(String timeStamp) {
    	return "updateAccountTime: " + timeStamp;
    }
    
    static public String accountDownloadEnd(String accountName) {
    	return "accountDownloadEnd: " + accountName;
    }
    
    static public String nextValidId( int orderId) {
    	return "Next Valid Order ID: " + orderId;
    }
    
    static public String contractDetails(int reqId, ContractDetails contractDetails) {
    	Contract contract = contractDetails.m_summary;
    	String msg = "reqId = " + reqId + " ===================================\n"
    		+ " ---- Contract Details begin ----\n"
    		+ contractMsg(contract) + contractDetailsMsg(contractDetails)
    		+ " ---- Contract Details End ----\n";
    	return msg;
    }
    
    private static String contractDetailsMsg(ContractDetails contractDetails) {
    	String msg = "marketName = " + contractDetails.m_marketName + "\n"
        + "minTick = " + contractDetails.m_minTick + "\n"
        + "price magnifier = " + contractDetails.m_priceMagnifier + "\n"
        + "orderTypes = " + contractDetails.m_orderTypes + "\n"
        + "validExchanges = " + contractDetails.m_validExchanges + "\n"
        + "underConId = " + contractDetails.m_underConId + "\n"
        + "longName = " + contractDetails.m_longName + "\n"
        + "contractMonth = " + contractDetails.m_contractMonth + "\n"
        + "industry = " + contractDetails.m_industry + "\n"
        + "category = " + contractDetails.m_category + "\n"
        + "subcategory = " + contractDetails.m_subcategory + "\n"
        + "timeZoneId = " + contractDetails.m_timeZoneId + "\n"
        + "tradingHours = " + contractDetails.m_tradingHours + "\n"
        + "liquidHours = " + contractDetails.m_liquidHours + "\n"
        + "evRule = " + contractDetails.m_evRule + "\n"
        + "evMultiplier = " + contractDetails.m_evMultiplier + "\n"
        + contractDetailsSecIdList(contractDetails);
    	return msg;
    }
    
	static public String contractMsg(Contract contract) {
    	String msg = "conid = " + contract.m_conId + "\n"
        + "symbol = " + contract.m_symbol + "\n"
        + "secType = " + contract.m_secType + "\n"
        + "expiry = " + contract.m_expiry + "\n"
        + "strike = " + contract.m_strike + "\n"
        + "right = " + contract.m_right + "\n"
        + "multiplier = " + contract.m_multiplier + "\n"
        + "exchange = " + contract.m_exchange + "\n"
        + "primaryExch = " + contract.m_primaryExch + "\n"
        + "currency = " + contract.m_currency + "\n"
        + "localSymbol = " + contract.m_localSymbol + "\n"
        + "tradingClass = " + contract.m_tradingClass + "\n";
    	return msg;
    }
	
    static public String bondContractDetails(int reqId, ContractDetails contractDetails) {
        Contract contract = contractDetails.m_summary;
        String msg = "reqId = " + reqId + " ===================================\n"	
        + " ---- Bond Contract Details begin ----\n"
        + "symbol = " + contract.m_symbol + "\n"
        + "secType = " + contract.m_secType + "\n"
        + "cusip = " + contractDetails.m_cusip + "\n"
        + "coupon = " + contractDetails.m_coupon + "\n"
        + "maturity = " + contractDetails.m_maturity + "\n"
        + "issueDate = " + contractDetails.m_issueDate + "\n"
        + "ratings = " + contractDetails.m_ratings + "\n"
        + "bondType = " + contractDetails.m_bondType + "\n"
        + "couponType = " + contractDetails.m_couponType + "\n"
        + "convertible = " + contractDetails.m_convertible + "\n"
        + "callable = " + contractDetails.m_callable + "\n"
        + "putable = " + contractDetails.m_putable + "\n"
        + "descAppend = " + contractDetails.m_descAppend + "\n"
        + "exchange = " + contract.m_exchange + "\n"
        + "currency = " + contract.m_currency + "\n"
        + "marketName = " + contractDetails.m_marketName + "\n"
        + "tradingClass = " + contract.m_tradingClass + "\n"
        + "conid = " + contract.m_conId + "\n"
        + "minTick = " + contractDetails.m_minTick + "\n"
        + "orderTypes = " + contractDetails.m_orderTypes + "\n"
        + "validExchanges = " + contractDetails.m_validExchanges + "\n"
        + "nextOptionDate = " + contractDetails.m_nextOptionDate + "\n"
        + "nextOptionType = " + contractDetails.m_nextOptionType + "\n"
        + "nextOptionPartial = " + contractDetails.m_nextOptionPartial + "\n"
        + "notes = " + contractDetails.m_notes + "\n"
        + "longName = " + contractDetails.m_longName + "\n"
        + "evRule = " + contractDetails.m_evRule + "\n"
        + "evMultiplier = " + contractDetails.m_evMultiplier + "\n"
        + contractDetailsSecIdList(contractDetails)
        + " ---- Bond Contract Details End ----\n";
        return msg;
    }
    
    static public String contractDetailsSecIdList(ContractDetails contractDetails) {
        String msg = "secIdList={";
        if (contractDetails.m_secIdList != null) {
            Vector secIdList = contractDetails.m_secIdList;
            for (int i = 0; i < secIdList.size(); ++i) {
                TagValue param = (TagValue)secIdList.elementAt(i);
                if (i > 0) {
                    msg += ",";
                }
                msg += param.m_tag + "=" + param.m_value;
            }
        }
        msg += "}\n";
        return msg;
    }

    static public String contractDetailsEnd(int reqId) {
    	return "reqId = " + reqId + " =============== end ===============";
    }
    
    static public String execDetails( int reqId, Contract contract, Execution execution) {
        String msg = " ---- Execution Details begin ----\n"
        + "reqId = " + reqId + "\n"
        + "orderId = " + execution.m_orderId + "\n"
        + "clientId = " + execution.m_clientId + "\n"
        + contractMsg(contract)
        + "execId = " + execution.m_execId + "\n"
        + "time = " + execution.m_time + "\n"
        + "acctNumber = " + execution.m_acctNumber + "\n"
        + "executionExchange = " + execution.m_exchange + "\n"
        + "side = " + execution.m_side + "\n"
        + "shares = " + execution.m_shares + "\n"
        + "price = " + execution.m_price + "\n"
        + "permId = " + execution.m_permId + "\n"
        + "liquidation = " + execution.m_liquidation + "\n"
        + "cumQty = " + execution.m_cumQty + "\n"
        + "avgPrice = " + execution.m_avgPrice + "\n"
        + "orderRef = " + execution.m_orderRef + "\n"
        + "evRule = " + execution.m_evRule + "\n"
        + "evMultiplier = " + execution.m_evMultiplier + "\n"
        + " ---- Execution Details end ----\n";
        return msg;
    }
    
    static public String execDetailsEnd(int reqId) {
    	return "reqId = " + reqId + " =============== end ===============";
    }
    
    static public String updateMktDepth( int tickerId, int position, int operation, int side,
    									 double price, int size) {
    	return "updateMktDepth: " + tickerId + " " + position + " " + operation + " " + side + " " + price + " " + size;
    }
    
    static public String updateMktDepthL2( int tickerId, int position, String marketMaker,
    									   int operation, int side, double price, int size) {
    	return "updateMktDepth: " + tickerId + " " + position + " " + marketMaker + " " + operation + " " + side + " " + price + " " + size;
    }
    
    static public String updateNewsBulletin( int msgId, int msgType, String message, String origExchange) {
    	return "MsgId=" + msgId + " :: MsgType=" + msgType +  " :: Origin=" + origExchange + " :: Message=" + message;
    }
    
    static public String managedAccounts( String accountsList) {
    	return "Connected : The list of managed accounts are : [" + accountsList + "]";
    }
    
    static public String receiveFA(int faDataType, String xml) {
    	return FINANCIAL_ADVISOR + " " + EClientSocket.faMsgTypeName(faDataType) + " " + xml;
    }
    
    static public String historicalData(int reqId, String date, double open, double high, double low,
                      					double close, int volume, int count, double WAP, boolean hasGaps) {
    	return "id=" + reqId +
        " date = " + date +
        " open=" + open +
        " high=" + high +
        " low=" + low +
        " close=" + close +
        " volume=" + volume +
        " count=" + count +
        " WAP=" + WAP +
        " hasGaps=" + hasGaps;
    }
	public static String realtimeBar(int reqId, long time, double open,
			double high, double low, double close, long volume, double wap, int count) {
        return "id=" + reqId +
        " time = " + time +
        " open=" + open +
        " high=" + high +
        " low=" + low +
        " close=" + close +
        " volume=" + volume +
        " count=" + count +
        " WAP=" + wap;
	}
	
    static public String scannerParameters(String xml) {
    	return SCANNER_PARAMETERS + "\n" + xml;
    }
    
    static public String scannerData(int reqId, int rank, ContractDetails contractDetails,
    								 String distance, String benchmark, String projection,
    								 String legsStr) {
        Contract contract = contractDetails.m_summary;
    	return "id = " + reqId +
        " rank=" + rank +
        " symbol=" + contract.m_symbol +
        " secType=" + contract.m_secType +
        " expiry=" + contract.m_expiry +
        " strike=" + contract.m_strike +
        " right=" + contract.m_right +
        " exchange=" + contract.m_exchange +
        " currency=" + contract.m_currency +
        " localSymbol=" + contract.m_localSymbol +
        " marketName=" + contractDetails.m_marketName +
        " tradingClass=" + contract.m_tradingClass +
        " distance=" + distance +
        " benchmark=" + benchmark +
        " projection=" + projection +
        " legsStr=" + legsStr;
    }
    
    static public String scannerDataEnd(int reqId) {
    	return "id = " + reqId + " =============== end ===============";
    }
    
    static public String currentTime(long time) {
		return "current time = " + time +
		" (" + DateFormat.getDateTimeInstance().format(new Date(time * 1000)) + ")";
    }

    static public String fundamentalData(int reqId, String data) {
		return "id  = " + reqId + " len = " + data.length() + '\n' + data;
    }
    
    static public String deltaNeutralValidation(int reqId, UnderComp underComp) {
    	return "id = " + reqId
    	+ " underComp.conId =" + underComp.m_conId
    	+ " underComp.delta =" + underComp.m_delta
    	+ " underComp.price =" + underComp.m_price;
    }
    static public String tickSnapshotEnd(int tickerId) {
    	return "id=" + tickerId + " =============== end ===============";
    }
    
    static public String marketDataType(int reqId, int marketDataType){
    	return "id=" + reqId + " marketDataType = " + MarketDataType.getField(marketDataType);
    }
    
    static public String commissionReport( CommissionReport commissionReport) {
        String msg = "commission report:" +
        " execId=" + commissionReport.m_execId +
        " commission=" + Util.DoubleMaxString(commissionReport.m_commission) +
        " currency=" + commissionReport.m_currency +
        " realizedPNL=" + Util.DoubleMaxString(commissionReport.m_realizedPNL) +
        " yield=" + Util.DoubleMaxString(commissionReport.m_yield) +
        " yieldRedemptionDate=" + Util.IntMaxString(commissionReport.m_yieldRedemptionDate);
        return msg;
    }
    
    static public String position( String account, Contract contract, int position, double avgCost) {
        String msg = " ---- Position begin ----\n"
        + "account = " + account + "\n"
        + contractMsg(contract)
        + "position = " + Util.IntMaxString(position) + "\n"
        + "avgCost = " + Util.DoubleMaxString(avgCost) + "\n"
        + " ---- Position end ----\n";
        return msg;
    }    

    static public String positionEnd() {
        return " =============== end ===============";
    }

    static public String accountSummary( int reqId, String account, String tag, String value, String currency) {
        String msg = " ---- Account Summary begin ----\n"
        + "reqId = " + reqId + "\n"
        + "account = " + account + "\n"
        + "tag = " + tag + "\n"
        + "value = " + value + "\n"
        + "currency = " + currency + "\n"
        + " ---- Account Summary end ----\n";
        return msg;
    }

    static public String accountSummaryEnd( int reqId) {
    	return "id=" + reqId + " =============== end ===============";
    }

}
