/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

import java.util.Vector;

public class Order {
    final public static int 	CUSTOMER = 0;
    final public static int 	FIRM = 1;
    final public static char    OPT_UNKNOWN='?';
    final public static char    OPT_BROKER_DEALER='b';
    final public static char    OPT_CUSTOMER ='c';
    final public static char    OPT_FIRM='f';
    final public static char    OPT_ISEMM='m';
    final public static char    OPT_FARMM='n';
    final public static char    OPT_SPECIALIST='y';
    final public static int 	AUCTION_MATCH = 1;
    final public static int 	AUCTION_IMPROVEMENT = 2;
    final public static int 	AUCTION_TRANSPARENT = 3;
    final public static String  EMPTY_STR = "";

    // main order fields
    public int 		m_orderId;
    public int 		m_clientId;
    public int  	m_permId;
    public String 	m_action;
    public int 		m_totalQuantity;
    public String 	m_orderType;
    public double 	m_lmtPrice;
    public double 	m_auxPrice;

    // extended order fields
    public String 	m_tif;  // "Time in Force" - DAY, GTC, etc.
    public String   m_activeStartTime; // GTC orders
    public String   m_activeStopTime; // GTC orders
    public String 	m_ocaGroup; // one cancels all group name
    public int      m_ocaType;  // 1 = CANCEL_WITH_BLOCK, 2 = REDUCE_WITH_BLOCK, 3 = REDUCE_NON_BLOCK
    public String 	m_orderRef;
    public boolean 	m_transmit;	// if false, order will be created but not transmited
    public int 		m_parentId;	// Parent order Id, to associate Auto STP or TRAIL orders with the original order.
    public boolean 	m_blockOrder;
    public boolean	m_sweepToFill;
    public int 		m_displaySize;
    public int 		m_triggerMethod; // 0=Default, 1=Double_Bid_Ask, 2=Last, 3=Double_Last, 4=Bid_Ask, 7=Last_or_Bid_Ask, 8=Mid-point
    public boolean 	m_outsideRth;
    public boolean  m_hidden;
    public String   m_goodAfterTime; // FORMAT: 20060505 08:00:00 {time zone}
    public String   m_goodTillDate;  // FORMAT: 20060505 08:00:00 {time zone}
    public boolean  m_overridePercentageConstraints;
    public String   m_rule80A;  // Individual = 'I', Agency = 'A', AgentOtherMember = 'W', IndividualPTIA = 'J', AgencyPTIA = 'U', AgentOtherMemberPTIA = 'M', IndividualPT = 'K', AgencyPT = 'Y', AgentOtherMemberPT = 'N'
    public boolean  m_allOrNone;
    public int      m_minQty;
    public double   m_percentOffset;    // REL orders only; specify the decimal, e.g. .04 not 4
    public double   m_trailStopPrice;   // for TRAILLIMIT orders only
    public double   m_trailingPercent;  // specify the percentage, e.g. 3, not .03

    // Financial advisors only
    public String   m_faGroup;
    public String   m_faProfile;
    public String   m_faMethod;
    public String   m_faPercentage;

    // Institutional orders only
    public String 	m_openClose;          // O=Open, C=Close
    public int 		m_origin;             // 0=Customer, 1=Firm
    public int      m_shortSaleSlot;      // 1 if you hold the shares, 2 if they will be delivered from elsewhere.  Only for Action="SSHORT
    public String   m_designatedLocation; // set when slot=2 only.
    public int      m_exemptCode;

    // SMART routing only
    public double   m_discretionaryAmt;
    public boolean  m_eTradeOnly;
    public boolean  m_firmQuoteOnly;
    public double   m_nbboPriceCap;
    public boolean  m_optOutSmartRouting;

    // BOX or VOL ORDERS ONLY
    public int      m_auctionStrategy; // 1=AUCTION_MATCH, 2=AUCTION_IMPROVEMENT, 3=AUCTION_TRANSPARENT

    // BOX ORDERS ONLY
    public double   m_startingPrice;
    public double   m_stockRefPrice;
    public double   m_delta;

    // pegged to stock or VOL orders
    public double   m_stockRangeLower;
    public double   m_stockRangeUpper;

    // VOLATILITY ORDERS ONLY
    public double   m_volatility;  // enter percentage not decimal, e.g. 2 not .02
    public int      m_volatilityType;     // 1=daily, 2=annual
    public int      m_continuousUpdate;
    public int      m_referencePriceType; // 1=Bid/Ask midpoint, 2 = BidOrAsk
    public String   m_deltaNeutralOrderType;
    public double   m_deltaNeutralAuxPrice;
    public int      m_deltaNeutralConId;
    public String   m_deltaNeutralSettlingFirm;
    public String   m_deltaNeutralClearingAccount;
    public String   m_deltaNeutralClearingIntent;
    public String   m_deltaNeutralOpenClose;
    public boolean  m_deltaNeutralShortSale;
    public int      m_deltaNeutralShortSaleSlot;
    public String   m_deltaNeutralDesignatedLocation;

    // COMBO ORDERS ONLY
    public double   m_basisPoints;      // EFP orders only, download only
    public int      m_basisPointsType;  // EFP orders only, download only

    // SCALE ORDERS ONLY
    public int      m_scaleInitLevelSize;
    public int      m_scaleSubsLevelSize;
    public double   m_scalePriceIncrement;
    public double   m_scalePriceAdjustValue;
    public int      m_scalePriceAdjustInterval;
    public double   m_scaleProfitOffset;
    public boolean  m_scaleAutoReset;
    public int      m_scaleInitPosition;
    public int      m_scaleInitFillQty;
    public boolean  m_scaleRandomPercent;
    public String   m_scaleTable;

    // HEDGE ORDERS ONLY
    public String   m_hedgeType; // 'D' - delta, 'B' - beta, 'F' - FX, 'P' - pair
    public String   m_hedgeParam; // beta value for beta hedge (in range 0-1), ratio for pair hedge

    // Clearing info
    public String 	m_account; // IB account
    public String   m_settlingFirm;
    public String   m_clearingAccount; // True beneficiary of the order
    public String   m_clearingIntent; // "" (Default), "IB", "Away", "PTA" (PostTrade)

    // ALGO ORDERS ONLY
    public String m_algoStrategy;
    public Vector<TagValue> m_algoParams;

    // What-if
    public boolean  m_whatIf;

    // Not Held
    public boolean  m_notHeld;

    // Smart combo routing params
    public Vector<TagValue> m_smartComboRoutingParams;

    // order combo legs
    public Vector<OrderComboLeg> m_orderComboLegs = new Vector<OrderComboLeg>();

    // order misc options
    public Vector<TagValue> m_orderMiscOptions;
    
    //order algo id
    public String m_algoId;
    
    public Order() {
        m_lmtPrice = Double.MAX_VALUE;
        m_auxPrice = Double.MAX_VALUE;
        m_activeStartTime = EMPTY_STR;
        m_activeStopTime = EMPTY_STR;
    	m_outsideRth = false;
        m_openClose	= "O";
        m_origin = CUSTOMER;
        m_transmit = true;
        m_designatedLocation = EMPTY_STR;
        m_exemptCode = -1;
        m_minQty = Integer.MAX_VALUE;
        m_percentOffset = Double.MAX_VALUE;
        m_nbboPriceCap = Double.MAX_VALUE;
        m_optOutSmartRouting = false;
        m_startingPrice = Double.MAX_VALUE;
        m_stockRefPrice = Double.MAX_VALUE;
        m_delta = Double.MAX_VALUE;
        m_stockRangeLower = Double.MAX_VALUE;
        m_stockRangeUpper = Double.MAX_VALUE;
        m_volatility = Double.MAX_VALUE;
        m_volatilityType = Integer.MAX_VALUE;
        m_deltaNeutralOrderType = EMPTY_STR;
        m_deltaNeutralAuxPrice = Double.MAX_VALUE;
        m_deltaNeutralConId = 0;
        m_deltaNeutralSettlingFirm = EMPTY_STR;
        m_deltaNeutralClearingAccount = EMPTY_STR;
        m_deltaNeutralClearingIntent = EMPTY_STR;
        m_deltaNeutralOpenClose = EMPTY_STR;
        m_deltaNeutralShortSale = false;
        m_deltaNeutralShortSaleSlot = 0;
        m_deltaNeutralDesignatedLocation = EMPTY_STR;
        m_referencePriceType = Integer.MAX_VALUE;
        m_trailStopPrice = Double.MAX_VALUE;
        m_trailingPercent = Double.MAX_VALUE;
        m_basisPoints = Double.MAX_VALUE;
        m_basisPointsType = Integer.MAX_VALUE;
        m_scaleInitLevelSize = Integer.MAX_VALUE;
        m_scaleSubsLevelSize = Integer.MAX_VALUE;
        m_scalePriceIncrement = Double.MAX_VALUE;
        m_scalePriceAdjustValue = Double.MAX_VALUE;
        m_scalePriceAdjustInterval = Integer.MAX_VALUE;
        m_scaleProfitOffset = Double.MAX_VALUE;
        m_scaleAutoReset = false;
        m_scaleInitPosition = Integer.MAX_VALUE;
        m_scaleInitFillQty = Integer.MAX_VALUE;
        m_scaleRandomPercent = false;
        m_scaleTable = EMPTY_STR;
        m_whatIf = false;
        m_notHeld = false;
        m_algoId = EMPTY_STR;
    }

    public boolean equals(Object p_other) {

        if ( this == p_other )
            return true;

        if ( p_other == null )
            return false;

        Order l_theOther = (Order)p_other;

        if ( m_permId == l_theOther.m_permId ) {
            return true;
        }

        if (m_orderId != l_theOther.m_orderId ||
        	m_clientId != l_theOther.m_clientId ||
        	m_totalQuantity != l_theOther.m_totalQuantity ||
        	m_lmtPrice != l_theOther.m_lmtPrice ||
        	m_auxPrice != l_theOther.m_auxPrice ||
        	m_ocaType != l_theOther.m_ocaType ||
        	m_transmit != l_theOther.m_transmit ||
        	m_parentId != l_theOther.m_parentId ||
        	m_blockOrder != l_theOther.m_blockOrder ||
        	m_sweepToFill != l_theOther.m_sweepToFill ||
        	m_displaySize != l_theOther.m_displaySize ||
        	m_triggerMethod != l_theOther.m_triggerMethod ||
        	m_outsideRth != l_theOther.m_outsideRth ||
        	m_hidden != l_theOther.m_hidden ||
        	m_overridePercentageConstraints != l_theOther.m_overridePercentageConstraints ||
        	m_allOrNone != l_theOther.m_allOrNone ||
        	m_minQty != l_theOther.m_minQty ||
        	m_percentOffset != l_theOther.m_percentOffset ||
        	m_trailStopPrice != l_theOther.m_trailStopPrice ||
        	m_trailingPercent != l_theOther.m_trailingPercent ||
        	m_origin != l_theOther.m_origin ||
        	m_shortSaleSlot != l_theOther.m_shortSaleSlot ||
        	m_discretionaryAmt != l_theOther.m_discretionaryAmt ||
        	m_eTradeOnly != l_theOther.m_eTradeOnly ||
        	m_firmQuoteOnly != l_theOther.m_firmQuoteOnly ||
        	m_nbboPriceCap != l_theOther.m_nbboPriceCap ||
        	m_optOutSmartRouting != l_theOther.m_optOutSmartRouting ||
        	m_auctionStrategy != l_theOther.m_auctionStrategy ||
        	m_startingPrice != l_theOther.m_startingPrice ||
        	m_stockRefPrice != l_theOther.m_stockRefPrice ||
        	m_delta != l_theOther.m_delta ||
        	m_stockRangeLower != l_theOther.m_stockRangeLower ||
        	m_stockRangeUpper != l_theOther.m_stockRangeUpper ||
        	m_volatility != l_theOther.m_volatility ||
        	m_volatilityType != l_theOther.m_volatilityType ||
        	m_continuousUpdate != l_theOther.m_continuousUpdate ||
        	m_referencePriceType != l_theOther.m_referencePriceType ||
        	m_deltaNeutralAuxPrice != l_theOther.m_deltaNeutralAuxPrice ||
        	m_deltaNeutralConId != l_theOther.m_deltaNeutralConId ||
        	m_deltaNeutralShortSale != l_theOther.m_deltaNeutralShortSale ||
        	m_deltaNeutralShortSaleSlot != l_theOther.m_deltaNeutralShortSaleSlot ||
        	m_basisPoints != l_theOther.m_basisPoints ||
        	m_basisPointsType != l_theOther.m_basisPointsType ||
        	m_scaleInitLevelSize != l_theOther.m_scaleInitLevelSize ||
        	m_scaleSubsLevelSize != l_theOther.m_scaleSubsLevelSize ||
        	m_scalePriceIncrement != l_theOther.m_scalePriceIncrement ||
        	m_scalePriceAdjustValue != l_theOther.m_scalePriceAdjustValue ||
        	m_scalePriceAdjustInterval != l_theOther.m_scalePriceAdjustInterval ||
        	m_scaleProfitOffset != l_theOther.m_scaleProfitOffset ||
        	m_scaleAutoReset != l_theOther.m_scaleAutoReset ||
        	m_scaleInitPosition != l_theOther.m_scaleInitPosition ||
        	m_scaleInitFillQty != l_theOther.m_scaleInitFillQty ||
        	m_scaleRandomPercent != l_theOther.m_scaleRandomPercent ||
        	m_whatIf != l_theOther.m_whatIf ||
        	m_notHeld != l_theOther.m_notHeld ||
        	m_exemptCode != l_theOther.m_exemptCode) {
        	return false;
        }

        if (Util.StringCompare(m_action, l_theOther.m_action) != 0 ||
        	Util.StringCompare(m_orderType, l_theOther.m_orderType) != 0 ||
        	Util.StringCompare(m_tif, l_theOther.m_tif) != 0 ||
        	Util.StringCompare(m_activeStartTime, l_theOther.m_activeStartTime) != 0 ||
        	Util.StringCompare(m_activeStopTime, l_theOther.m_activeStopTime) != 0 ||
        	Util.StringCompare(m_ocaGroup, l_theOther.m_ocaGroup) != 0 ||
        	Util.StringCompare(m_orderRef,l_theOther.m_orderRef) != 0 ||
        	Util.StringCompare(m_goodAfterTime, l_theOther.m_goodAfterTime) != 0 ||
        	Util.StringCompare(m_goodTillDate, l_theOther.m_goodTillDate) != 0 ||
        	Util.StringCompare(m_rule80A, l_theOther.m_rule80A) != 0 ||
        	Util.StringCompare(m_faGroup, l_theOther.m_faGroup) != 0 ||
        	Util.StringCompare(m_faProfile, l_theOther.m_faProfile) != 0 ||
        	Util.StringCompare(m_faMethod, l_theOther.m_faMethod) != 0 ||
        	Util.StringCompare(m_faPercentage, l_theOther.m_faPercentage) != 0 ||
        	Util.StringCompare(m_openClose, l_theOther.m_openClose) != 0 ||
        	Util.StringCompare(m_designatedLocation, l_theOther.m_designatedLocation) != 0 ||
        	Util.StringCompare(m_deltaNeutralOrderType, l_theOther.m_deltaNeutralOrderType) != 0 ||
        	Util.StringCompare(m_deltaNeutralSettlingFirm, l_theOther.m_deltaNeutralSettlingFirm) != 0 ||
        	Util.StringCompare(m_deltaNeutralClearingAccount, l_theOther.m_deltaNeutralClearingAccount) != 0 ||
        	Util.StringCompare(m_deltaNeutralClearingIntent, l_theOther.m_deltaNeutralClearingIntent) != 0 ||
        	Util.StringCompare(m_deltaNeutralOpenClose, l_theOther.m_deltaNeutralOpenClose) != 0 ||
        	Util.StringCompare(m_deltaNeutralDesignatedLocation, l_theOther.m_deltaNeutralDesignatedLocation) != 0 ||
        	Util.StringCompare(m_hedgeType, l_theOther.m_hedgeType) != 0 ||
        	Util.StringCompare(m_hedgeParam, l_theOther.m_hedgeParam) != 0 ||
        	Util.StringCompare(m_account, l_theOther.m_account) != 0 ||
        	Util.StringCompare(m_settlingFirm, l_theOther.m_settlingFirm) != 0 ||
        	Util.StringCompare(m_clearingAccount, l_theOther.m_clearingAccount) != 0 ||
        	Util.StringCompare(m_clearingIntent, l_theOther.m_clearingIntent) != 0 ||
        	Util.StringCompare(m_algoStrategy, l_theOther.m_algoStrategy) != 0 ||
        	Util.StringCompare(m_algoId, l_theOther.m_algoId) != 0 ||
        	Util.StringCompare(m_scaleTable, l_theOther.m_scaleTable) != 0) {
        	return false;
        }

        if (!Util.VectorEqualsUnordered(m_algoParams, l_theOther.m_algoParams)) {
        	return false;
        }

        if (!Util.VectorEqualsUnordered(m_smartComboRoutingParams, l_theOther.m_smartComboRoutingParams)) {
        	return false;
        }

    	// compare order combo legs
        if (!Util.VectorEqualsUnordered(m_orderComboLegs, l_theOther.m_orderComboLegs)) {
        	return false;
        }

        return true;
    }
}
