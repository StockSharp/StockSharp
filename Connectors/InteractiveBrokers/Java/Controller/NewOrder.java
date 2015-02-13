/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import java.util.ArrayList;
import java.util.List;

import com.ib.client.Order;
import com.ib.client.TagValue;
import com.ib.controller.Types.Action;
import com.ib.controller.Types.AlgoStrategy;
import com.ib.controller.Types.HedgeType;
import com.ib.controller.Types.Method;
import com.ib.controller.Types.OcaType;
import com.ib.controller.Types.ReferencePriceType;
import com.ib.controller.Types.Rule80A;
import com.ib.controller.Types.TimeInForce;
import com.ib.controller.Types.TriggerMethod;
import com.ib.controller.Types.VolatilityType;

public class NewOrder {
	// order id's
	private int m_clientId;
	private int m_orderId;
	private long m_permId;
	private int m_parentId;

	// primary attributes
	private String m_account;
	private Action m_action = Action.BUY;
	private int m_totalQuantity;
	private int m_displaySize;
	private OrderType m_orderType = OrderType.LMT;
	private double m_lmtPrice = Double.MAX_VALUE;
	private double m_auxPrice = Double.MAX_VALUE;
	private TimeInForce m_tif = TimeInForce.DAY;

	// secondary attributes
	private boolean m_allOrNone;
	private boolean m_blockOrder;
	private boolean m_eTradeOnly;
	private boolean m_firmQuoteOnly;
	private boolean m_hidden;
	private boolean m_notHeld;
	private boolean m_optOutSmartRouting;
	private boolean m_outsideRth;
	private boolean m_sweepToFill;
	private double m_delta = Double.MAX_VALUE;
	private double m_discretionaryAmt = Double.MAX_VALUE;
	private double m_nbboPriceCap = Double.MAX_VALUE;
	private double m_percentOffset = Double.MAX_VALUE;  // for Relative orders; specify the decimal, e.g. .04 not 4
	private double m_startingPrice = Double.MAX_VALUE;
	private double m_stockRangeLower = Double.MAX_VALUE;
	private double m_stockRangeUpper = Double.MAX_VALUE;
	private double m_stockRefPrice = Double.MAX_VALUE;
	private double m_trailingPercent = Double.MAX_VALUE;  // for Trailing Stop orders; specify the percentage, e.g. 3, not .03
	private double m_trailStopPrice = Double.MAX_VALUE;   // stop price for Trailing Stop orders
	private int m_minQty = Integer.MAX_VALUE;
	private String m_goodAfterTime; // FORMAT: 20060505 08:00:00 EST
	private String m_goodTillDate;  // FORMAT: 20060505 08:00:00 EST or 20060505
	private String m_ocaGroup; // one cancels all group name
	private String m_orderRef;
	private Rule80A m_rule80A = Rule80A.None;
	private OcaType m_ocaType = OcaType.None;
	private TriggerMethod m_triggerMethod = TriggerMethod.Default;

	// advisor allocation orders
	private String m_faGroup;
	private Method m_faMethod = Method.None;
	private String m_faPercentage;
	private String m_faProfile;

	// volatility orders
	private double m_volatility = Double.MAX_VALUE;  // enter percentage not decimal, e.g. 2 not .02
	private VolatilityType m_volatilityType = VolatilityType.None;
	private boolean m_continuousUpdate;
	private ReferencePriceType m_referencePriceType = ReferencePriceType.None;
	private OrderType m_deltaNeutralOrderType = OrderType.None;
	private double m_deltaNeutralAuxPrice = Double.MAX_VALUE;
	private int m_deltaNeutralConId;

	// scale orders
	private int m_scaleInitLevelSize = Integer.MAX_VALUE;
	private int m_scaleSubsLevelSize = Integer.MAX_VALUE;
	private double m_scalePriceIncrement = Double.MAX_VALUE;
	private double m_scalePriceAdjustValue = Double.MAX_VALUE;
	private int m_scalePriceAdjustInterval = Integer.MAX_VALUE;
	private double m_scaleProfitOffset = Double.MAX_VALUE;
	private boolean m_scaleAutoReset;
	private int m_scaleInitPosition = Integer.MAX_VALUE;
	private int m_scaleInitFillQty = Integer.MAX_VALUE;
	private boolean m_scaleRandomPercent;
	private String m_scaleTable;

	// hedge orders
	private HedgeType m_hedgeType = HedgeType.None;
	private String m_hedgeParam; // beta value for beta hedge (in range 0-1), ratio for pair hedge

	// algo orders
	private AlgoStrategy m_algoStrategy = AlgoStrategy.None;
	private final ArrayList<TagValue> m_algoParams = new ArrayList<TagValue>();
	private String m_algoId;

	// combo orders
	private final ArrayList<TagValue> m_smartComboRoutingParams = new ArrayList<TagValue>();
	private final ArrayList<Double> m_orderComboLegs = new ArrayList<Double>(); // array of leg prices

	// processing control
	private boolean m_whatIf;
	private boolean m_transmit = true; // if false, order will be sent to TWS but not transmited to server
	private boolean m_overridePercentageConstraints;

	// Institutional/cleared away
//	private String m_openClose = "O";          // O=Open, C=Close
//	private int m_origin;             // 0=Customer, 1=Firm
//	private int m_shortSaleSlot;      // 1 if you hold the shares, 2 if they will be delivered from elsewhere.  Only for Action="SSHORT
//	private String m_designatedLocation; // set when slot=2 only.
//	private int m_exemptCode = -1;
//	private String m_settlingFirm;
//	private String m_clearingAccount; // True beneficiary of the order
//	private String m_clearingIntent; // "" (Default), "IB", "Away", "PTA" (PostTrade)
//	private int m_auctionStrategy; // 1=AUCTION_MATCH, 2=AUCTION_IMPROVEMENT, 3=AUCTION_TRANSPARENT // need type for this. ps
//	private String m_deltaNeutralSettlingFirm;
//	private String m_deltaNeutralClearingAccount;
//	private String m_deltaNeutralClearingIntent;
//	private double m_basisPoints;      // EFP orders only, download only
//	private int m_basisPointsType;     // EFP orders only, download only



	// getters
	public Action action() { return m_action; }
	public boolean allOrNone() { return m_allOrNone; }
	public boolean blockOrder() { return m_blockOrder; }
	public boolean eTradeOnly() { return m_eTradeOnly; }
	public boolean firmQuoteOnly() { return m_firmQuoteOnly; }
	public boolean hidden() { return m_hidden; }
	public boolean notHeld() { return m_notHeld; }
	public boolean optOutSmartRouting() { return m_optOutSmartRouting; }
	public boolean outsideRth() { return m_outsideRth; }
	public boolean overridePercentageConstraints() { return m_overridePercentageConstraints; }
	public boolean scaleAutoReset() { return m_scaleAutoReset; }
	public boolean scaleRandomPercent() { return m_scaleRandomPercent; }
	public boolean sweepToFill() { return m_sweepToFill; }
	public boolean transmit() { return m_transmit; }
	public boolean whatIf() { return m_whatIf; }
	public double auxPrice() { return m_auxPrice; }
	public double delta() { return m_delta; }
	public double deltaNeutralAuxPrice() { return m_deltaNeutralAuxPrice; }
	public double discretionaryAmt() { return m_discretionaryAmt; }
	public double lmtPrice() { return m_lmtPrice; }
	public double nbboPriceCap() { return m_nbboPriceCap; }
	public double percentOffset() { return m_percentOffset; }
	public double scalePriceAdjustValue() { return m_scalePriceAdjustValue; }
	public double scalePriceIncrement() { return m_scalePriceIncrement; }
	public double scaleProfitOffset() { return m_scaleProfitOffset; }
	public double startingPrice() { return m_startingPrice; }
	public double stockRangeLower() { return m_stockRangeLower; }
	public double stockRangeUpper() { return m_stockRangeUpper; }
	public double stockRefPrice() { return m_stockRefPrice; }
	public double trailingPercent() { return m_trailingPercent; }
	public double trailStopPrice() { return m_trailStopPrice; }
	public double volatility() { return m_volatility; }
	public int clientId() { return m_clientId; }
	public boolean continuousUpdate() { return m_continuousUpdate; }
	public int deltaNeutralConId() { return m_deltaNeutralConId; }
	public int displaySize() { return m_displaySize; }
	public int minQty() { return m_minQty; }
	public int orderId() { return m_orderId; }
	public int parentId() { return m_parentId; }
	public int scaleInitFillQty() { return m_scaleInitFillQty; }
	public int scaleInitLevelSize() { return m_scaleInitLevelSize; }
	public int scaleInitPosition() { return m_scaleInitPosition; }
	public int scalePriceAdjustInterval() { return m_scalePriceAdjustInterval; }
	public int scaleSubsLevelSize() { return m_scaleSubsLevelSize; }
	public int totalQuantity() { return m_totalQuantity; }
	public long permId() { return m_permId; }
	public Method faMethod() { return m_faMethod; }
	public OcaType ocaType() { return m_ocaType; }
	public OrderType deltaNeutralOrderType() { return m_deltaNeutralOrderType; }
	public OrderType orderType() { return m_orderType; }
	public ReferencePriceType referencePriceType() { return m_referencePriceType; }
	public Rule80A rule80A() { return m_rule80A; }
	public String account() { return m_account; }
	public AlgoStrategy algoStrategy() { return m_algoStrategy; }
	public String algoId() { return m_algoId; }
	public String faGroup() { return m_faGroup; }
	public String faPercentage() { return m_faPercentage; }
	public String faProfile() { return m_faProfile; }
	public String goodAfterTime() { return m_goodAfterTime; }
	public String goodTillDate() { return m_goodTillDate; }
	public String hedgeParam() { return m_hedgeParam; }
	public HedgeType hedgeType() { return m_hedgeType; }
	public String ocaGroup() { return m_ocaGroup; }
	public String orderRef() { return m_orderRef; }
	public TimeInForce tif() { return m_tif; }
	public VolatilityType volatilityType() { return m_volatilityType; }
	public ArrayList<TagValue> smartComboRoutingParams() { return m_smartComboRoutingParams; }
	public TriggerMethod triggerMethod() { return m_triggerMethod; }
	public ArrayList<TagValue> algoParams() { return m_algoParams; }
	public ArrayList<Double> orderComboLegs() { return m_orderComboLegs; }
	public String scaleTable() { return m_scaleTable; }

	// setters
	public void account(String v) { m_account = v; }
	public void action(Action v) { m_action = v; }
	public void algoStrategy(AlgoStrategy v) { m_algoStrategy = v; }
	public void algoId(String v) { m_algoId = v; }
	public void allOrNone(boolean v) { m_allOrNone = v; }
	public void auxPrice(double v) { m_auxPrice = v; }
	public void blockOrder(boolean v) { m_blockOrder = v; }
	public void clientId(int v) { m_clientId = v; }
	public void continuousUpdate(boolean v) { m_continuousUpdate = v; }
	public void delta(double v) { m_delta = v; }
	public void deltaNeutralAuxPrice(double v) { m_deltaNeutralAuxPrice = v; }
	public void deltaNeutralConId(int v) { m_deltaNeutralConId = v; }
	public void deltaNeutralOrderType(OrderType v) { m_deltaNeutralOrderType = v; }
	public void discretionaryAmt(double v) { m_discretionaryAmt = v; }
	public void displaySize(int v) { m_displaySize = v; }
	public void eTradeOnly(boolean v) { m_eTradeOnly = v; }
	public void faGroup(String v) { m_faGroup = v; }
	public void faMethod(Method v) { m_faMethod = v; }
	public void faPercentage(String v) { m_faPercentage = v; }
	public void faProfile(String v) { m_faProfile = v; }
	public void firmQuoteOnly(boolean v) { m_firmQuoteOnly = v; }
	public void goodAfterTime(String v) { m_goodAfterTime = v; }
	public void goodTillDate(String v) { m_goodTillDate = v; }
	public void hedgeParam(String v) { m_hedgeParam = v; }
	public void hedgeType(HedgeType v) { m_hedgeType = v; }
	public void hidden(boolean v) { m_hidden = v; }
	public void lmtPrice(double v) { m_lmtPrice = v; }
	public void minQty(int v) { m_minQty = v; }
	public void nbboPriceCap(double v) { m_nbboPriceCap = v; }
	public void notHeld(boolean v) { m_notHeld = v; }
	public void ocaGroup(String v) { m_ocaGroup = v; }
	public void ocaType(OcaType v) { m_ocaType = v; }
	public void optOutSmartRouting(boolean v) { m_optOutSmartRouting = v; }
	public void orderId(int v) { m_orderId = v; }
	public void orderRef(String v) { m_orderRef = v; }
	public void orderType(OrderType v) { m_orderType = v; }
	public void outsideRth(boolean v) { m_outsideRth = v; }
	public void overridePercentageConstraints(boolean v) { m_overridePercentageConstraints = v; }
	public void parentId(int v) { m_parentId = v; }
	public void percentOffset(double v) { m_percentOffset = v; }
	public void permId(long v) { m_permId = v; }
	public void referencePriceType(ReferencePriceType v) { m_referencePriceType = v; }
	public void rule80A(Rule80A v) { m_rule80A = v; }
	public void scaleAutoReset(boolean v) { m_scaleAutoReset = v; }
	public void scaleInitFillQty(int v) { m_scaleInitFillQty = v; }
	public void scaleInitLevelSize(int v) { m_scaleInitLevelSize = v; }
	public void scaleInitPosition(int v) { m_scaleInitPosition = v; }
	public void scalePriceAdjustInterval(int v) { m_scalePriceAdjustInterval = v; }
	public void scalePriceAdjustValue(double v) { m_scalePriceAdjustValue = v; }
	public void scalePriceIncrement(double v) { m_scalePriceIncrement = v; }
	public void scaleProfitOffset(double v) { m_scaleProfitOffset = v; }
	public void scaleRandomPercent(boolean v) { m_scaleRandomPercent = v; }
	public void scaleSubsLevelSize(int v) { m_scaleSubsLevelSize = v; }
	public void startingPrice(double v) { m_startingPrice = v; }
	public void stockRangeLower(double v) { m_stockRangeLower = v; }
	public void stockRangeUpper(double v) { m_stockRangeUpper = v; }
	public void stockRefPrice(double v) { m_stockRefPrice = v; }
	public void sweepToFill(boolean v) { m_sweepToFill = v; }
	public void tif(TimeInForce v) { m_tif = v; }
	public void totalQuantity(int v) { m_totalQuantity = v; }
	public void trailingPercent(double v) { m_trailingPercent = v; }
	public void trailStopPrice(double v) { m_trailStopPrice = v; }
	public void transmit(boolean v) { m_transmit = v; }
	public void triggerMethod(TriggerMethod v) { m_triggerMethod = v; }
	public void volatility(double v) { m_volatility = v; }
	public void volatilityType(VolatilityType v) { m_volatilityType = v; }
	public void whatIf(boolean v) { m_whatIf = v; }
	public void scaleTable(String v) { m_scaleTable = v; }

	public int auctionStrategy() { return 0; }

	public NewOrder() {
	}

	public NewOrder( Order order) {
		m_clientId = order.m_clientId;
		m_orderId = order.m_orderId;
		m_permId = order.m_permId;
		m_parentId = order.m_parentId;
		m_account = order.m_account;
		m_action = Action.valueOf( order.m_action);
		m_totalQuantity = order.m_totalQuantity;
		m_displaySize = order.m_displaySize;
		m_orderType = OrderType.get( order.m_orderType);
		m_lmtPrice = order.m_lmtPrice;
		m_auxPrice = order.m_auxPrice;
		m_tif = TimeInForce.valueOf( order.m_tif);
		m_allOrNone = order.m_allOrNone;
		m_blockOrder = order.m_blockOrder;
		m_eTradeOnly = order.m_eTradeOnly;
		m_firmQuoteOnly = order.m_firmQuoteOnly;
		m_hidden = order.m_hidden;
		m_notHeld = order.m_notHeld;
		m_optOutSmartRouting = order.m_optOutSmartRouting;
		m_outsideRth = order.m_outsideRth;
		m_sweepToFill = order.m_sweepToFill;
		m_delta = order.m_delta;
		m_discretionaryAmt = order.m_discretionaryAmt;
		m_nbboPriceCap = order.m_nbboPriceCap;
		m_percentOffset = order.m_percentOffset;
		m_startingPrice = order.m_startingPrice;
		m_stockRangeLower = order.m_stockRangeLower;
		m_stockRangeUpper = order.m_stockRangeUpper;
		m_stockRefPrice = order.m_stockRefPrice;
		m_trailingPercent = order.m_trailingPercent;
		m_trailStopPrice = order.m_trailStopPrice;
		m_minQty = order.m_minQty;
		m_goodAfterTime = order.m_goodAfterTime;
		m_goodTillDate = order.m_goodTillDate;
		m_ocaGroup = order.m_ocaGroup;
		m_orderRef = order.m_orderRef;
		m_rule80A = Rule80A.get( order.m_rule80A);
		m_ocaType = OcaType.get( order.m_ocaType);
		m_triggerMethod = TriggerMethod.get( order.m_triggerMethod);
		m_faGroup = order.m_faGroup;
		m_faMethod = Method.get( order.m_faMethod);
		m_faPercentage = order.m_faPercentage;
		m_faProfile = order.m_faProfile;
		m_volatility = order.m_volatility;
		m_volatilityType = VolatilityType.get( order.m_volatilityType);
		m_continuousUpdate = order.m_continuousUpdate == 1;
		m_referencePriceType = ReferencePriceType.get( order.m_referencePriceType);
		m_deltaNeutralOrderType = OrderType.get( order.m_deltaNeutralOrderType);
		m_deltaNeutralAuxPrice = order.m_deltaNeutralAuxPrice;
		m_deltaNeutralConId = order.m_deltaNeutralConId;
		m_scaleInitLevelSize = order.m_scaleInitLevelSize;
		m_scaleSubsLevelSize = order.m_scaleSubsLevelSize;
		m_scalePriceIncrement = order.m_scalePriceIncrement;
		m_scalePriceAdjustValue = order.m_scalePriceAdjustValue;
		m_scalePriceAdjustInterval = order.m_scalePriceAdjustInterval;
		m_scaleProfitOffset = order.m_scaleProfitOffset;
		m_scaleAutoReset = order.m_scaleAutoReset;
		m_scaleInitPosition = order.m_scaleInitPosition;
		m_scaleInitFillQty = order.m_scaleInitFillQty;
		m_scaleRandomPercent = order.m_scaleRandomPercent;
		m_hedgeType = HedgeType.get( order.m_hedgeType);
		m_hedgeParam = order.m_hedgeParam;
		m_algoStrategy = AlgoStrategy.get( order.m_algoStrategy);
		m_whatIf = order.m_whatIf;
		m_transmit = order.m_transmit;
		m_overridePercentageConstraints = order.m_overridePercentageConstraints;

		fill( m_smartComboRoutingParams, order.m_smartComboRoutingParams);
		fill( m_orderComboLegs, order.m_orderComboLegs);
		fill( m_algoParams, order.m_algoParams);
	}

	public static void fill(List list1, List list2) {
		list1.clear();

		if (list2 != null) {
			for( Object obj : list2) {
				list1.add( obj);
			}
		}
	}
}
