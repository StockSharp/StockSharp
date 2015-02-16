/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;
import static com.ib.controller.Types.AlgoParam.allowPastEndTime;
import static com.ib.controller.Types.AlgoParam.displaySize;
import static com.ib.controller.Types.AlgoParam.endTime;
import static com.ib.controller.Types.AlgoParam.forceCompletion;
import static com.ib.controller.Types.AlgoParam.getDone;
import static com.ib.controller.Types.AlgoParam.maxPctVol;
import static com.ib.controller.Types.AlgoParam.noTakeLiq;
import static com.ib.controller.Types.AlgoParam.noTradeAhead;
import static com.ib.controller.Types.AlgoParam.pctVol;
import static com.ib.controller.Types.AlgoParam.riskAversion;
import static com.ib.controller.Types.AlgoParam.startTime;
import static com.ib.controller.Types.AlgoParam.strategyType;
import static com.ib.controller.Types.AlgoParam.useOddLots;
import static com.ib.controller.Types.AlgoParam.componentSize;
import static com.ib.controller.Types.AlgoParam.timeBetweenOrders;
import static com.ib.controller.Types.AlgoParam.randomizeTime20;
import static com.ib.controller.Types.AlgoParam.randomizeSize55;
import static com.ib.controller.Types.AlgoParam.giveUp;
import static com.ib.controller.Types.AlgoParam.catchUp;
import static com.ib.controller.Types.AlgoParam.waitForFill;

import com.ib.client.IApiEnum;

public class Types {
	public static enum ComboParam {
		NonGuaranteed, PriceCondConid, CondPriceMax, CondPriceMin, ChangeToMktTime1, ChangeToMktTime2, DiscretionaryPct, DontLeginNext, LeginPrio, MaxSegSize,
	}

	public static enum AlgoParam {
		startTime, endTime, allowPastEndTime, maxPctVol, pctVol, strategyType, noTakeLiq, riskAversion, forceCompletion, displaySize, getDone, noTradeAhead, useOddLots,
		componentSize, timeBetweenOrders, randomizeTime20, randomizeSize55, giveUp, catchUp, waitForFill	
	}

	public static enum AlgoStrategy implements IApiEnum {
		None(),
		Vwap( startTime, endTime, maxPctVol, noTakeLiq, getDone, noTradeAhead, useOddLots),
		Twap( startTime, endTime, allowPastEndTime, strategyType),
		ArrivalPx( startTime, endTime, allowPastEndTime, maxPctVol, riskAversion, forceCompletion),
		DarkIce( startTime, endTime, allowPastEndTime, displaySize),
		PctVol( startTime, endTime, pctVol, noTakeLiq),
		AD( startTime, endTime, componentSize, timeBetweenOrders, randomizeTime20, randomizeSize55, giveUp, catchUp, waitForFill);

		private AlgoParam[] m_params;

		public AlgoParam[] params() { return m_params; }

		private AlgoStrategy( AlgoParam... params) {
			m_params = params;
		}

		public static AlgoStrategy get( String apiString) {
			return apiString != null && apiString.length() > 0 ? valueOf( apiString) : None;
		}

		@Override public String getApiString() {
			return this == None ? "" : super.toString();
		}
	}

	public static enum HedgeType implements IApiEnum {
		None, Delta, Beta, Fx, Pair;

		public static HedgeType get( String apiString) {
			for (HedgeType type : values() ) {
				if (type.getApiString().equals( apiString) ) {
					return type;
				}
			}
			return None;
		}

		@Override public String getApiString() {
			return this == None ? "" : String.valueOf( super.toString().charAt( 0) );
		}
	}

	public static enum Right implements IApiEnum {
		None, Put, Call;

		public static Right get( String apiString) {
			if (apiString != null && apiString.length() > 0) {
				switch( apiString.charAt( 0) ) {
					case 'P' : return Put;
					case 'C' : return Call;
				}
			}
			return None;
		}

		@Override public String getApiString() {
			return this == None ? "" : String.valueOf( toString().charAt( 0) );
		}
	}

	public static enum VolatilityType implements IApiEnum {
		None, Daily, Annual;

		public static VolatilityType get( int ordinal) {
			return ordinal == Integer.MAX_VALUE ? None : getEnum( ordinal, values() );
		}

		@Override public String getApiString() {
			return "" + ordinal();
		}
	}

	public static enum ReferencePriceType implements IApiEnum {
		None, Midpoint, BidOrAsk;

		public static ReferencePriceType get( int ordinal) {
			return getEnum( ordinal, values() );
		}

		@Override public String getApiString() {
			return "" + ordinal();
		}
	}

	public static enum TriggerMethod implements IApiEnum {
		Default( 0), DoubleBidAsk( 1), Last( 2), DoubleLast( 3), BidAsk( 4), LastOrBidAsk( 7), Midpoint( 8);

		int m_val;

		public int val() { return m_val; }

		private TriggerMethod( int val) {
			m_val = val;
		}

		public static TriggerMethod get( int val) {
			for (TriggerMethod m : values() ) {
				if (m.m_val == val) {
					return m;
				}
			}
			return null;
		}

		@Override public String getApiString() {
			return "" + m_val;
		}
	}

	public static enum Action implements IApiEnum {
		BUY, SELL, SSHORT;

		@Override public String getApiString() {
			return toString();
		}
	}

	public static enum Rule80A implements IApiEnum {
		None(""), IndivArb("J"), IndivBigNonArb("K"), IndivSmallNonArb("I"), INST_ARB("U"), InstBigNonArb("Y"), InstSmallNonArb("A");

		private String m_apiString;

		private Rule80A( String apiString) {
			m_apiString = apiString;
		}

		public static Rule80A get( String apiString) {
			for (Rule80A val : values() ) {
				if (val.m_apiString.equals( apiString) ) {
					return val;
				}
			}
			return None;
		}

		public String getApiString() {
			return m_apiString;
		}
	}

	public static enum OcaType implements IApiEnum {
		None, CancelWithBlocking, ReduceWithBlocking, ReduceWithoutBlocking;

		public static OcaType get( int ordinal) {
			return getEnum( ordinal, values() );
		}

		@Override public String getApiString() {
			return "" + ordinal();
		}
	}

	public static enum TimeInForce implements IApiEnum {
		DAY, GTC, OPG, IOC, GTD, GTT, AUC, FOK, GTX, DTC;

		@Override public String getApiString() {
			return toString();
		}
	}

	public static enum ExerciseType {
		None, Exercise, Lapse;
	}

	public static enum FundamentalType {
		ReportSnapshot, ReportsFinSummary, ReportRatios, ReportsFinStatements, RESC, CalendarReport;

		public String getApiString() {
			return super.toString();
		}

		@Override public String toString() {
			switch( this) {
				case ReportSnapshot: 		return "Company overview";
				case ReportsFinSummary:		return "Financial summary";
				case ReportRatios:			return "Financial ratios";
				case ReportsFinStatements:	return "Financial statements";
				case RESC: 					return "Analyst estimates";
				case CalendarReport:		return "Company calendar";
				default:					return null;
			}
		}
	}

	public static enum WhatToShow {
		TRADES, MIDPOINT, BID, ASK, // << only these are valid for real-time bars
        BID_ASK, HISTORICAL_VOLATILITY, OPTION_IMPLIED_VOLATILITY, YIELD_ASK, YIELD_BID, YIELD_BID_ASK, YIELD_LAST
	}

	public static enum BarSize {
		_1_secs, _5_secs, _10_secs, _15_secs, _30_secs, _1_min, _2_mins, _3_mins, _5_mins, _10_mins, _15_mins, _20_mins, _30_mins, _1_hour, _4_hours, _1_day, _1_week;

		public String toString() {
			return super.toString().substring( 1).replace( '_', ' ');
		}
	}

	public static enum DurationUnit {
		SECOND, DAY, WEEK, MONTH, YEAR;
	}

	public static enum DeepType {
	    INSERT, UPDATE, DELETE;

	    public static DeepType get( int ordinal) {
	    	return getEnum( ordinal, values() );
	    }
	}

	public static enum DeepSide {
	    SELL, BUY;

	    public static DeepSide get( int ordinal) {
	    	return getEnum( ordinal, values() );
	    }
	}

	public enum NewsType {
		UNKNOWN, BBS, LIVE_EXCH, DEAD_EXCH, HTML, POPUP_TEXT, POPUP_HTML;

		static NewsType get( int ordinal) {
			return getEnum( ordinal, values() );
		}
	}

	public enum FADataType {
		UNUSED, GROUPS, PROFILES, ALIASES;

		public static FADataType get( int ordinal) {
			return getEnum( ordinal, values() );
		}
	}

	public enum SecIdType implements IApiEnum {
	    None, CUSIP, SEDOL, ISIN, RIC;

		public static SecIdType get(String str) {
			return str == null || str.length() == 0 ? None : valueOf( str);
		}

		@Override public String getApiString() {
			return this == None ? "" : super.toString();
		}
	}

	public enum SecType implements IApiEnum {
		None, STK, OPT, FUT, CASH, BOND, CFD, FOP, WAR, IOPT, FWD, BAG, IND, BILL, FUND, FIXED, SLB, NEWS, CMDTY, BSK, ICU, ICS;

		@Override public String getApiString() {
			return this == None ? "" : super.toString();
		}
	}

	public enum MktDataType {
		Unknown, Realtime, Frozen;

		public static MktDataType get( int ordinal) {
			return getEnum( ordinal, values() );
		}
	}

	public enum Method implements IApiEnum {
		None, EqualQuantity, AvailableEquity, NetLiq, PctChange;

	    public static Method get( String str) {
	    	return str == null || str.length() == 0 ? None : valueOf( str);
	    }

	    @Override public String getApiString() {
			return this == None ? "" : super.toString();
		}
	}

	/** Lookup enum by ordinal. Use Enum.valueOf() to lookup by string. */
	public static <T extends Enum<T>> T getEnum(int ordinal, T[] values) {
		if (ordinal == Integer.MAX_VALUE) {
			return null;
		}

		for (T val : values) {
			if (val.ordinal() == ordinal) {
				return val;
			}
		}
		String str = String.format( "Error: %s is not a valid value for enum %s", ordinal, values[0].getClass().getName() );
		throw new IllegalArgumentException( str);
	}
}
