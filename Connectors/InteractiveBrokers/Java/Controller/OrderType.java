/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import com.ib.client.IApiEnum;


public enum OrderType implements IApiEnum {
	None( ""),
	MKT( "MKT"),
	LMT( "LMT"),
	STP( "STP"),
	STP_LMT( "STP LMT"),
	REL( "REL"),
	TRAIL( "TRAIL"),
	BOX_TOP( "BOX TOP"),
	FIX_PEGGED( "FIX PEGGED"),
	LIT( "LIT"),
	LMT_PLUS_MKT( "LMT + MKT"),
	LOC( "LOC"),
	MIT( "MIT"),
	MKT_PRT( "MKT PRT"),
	MOC( "MOC"),
	MTL( "MTL"),
	PASSV_REL( "PASSV REL"),
	PEG_BENCH( "PEG BENCH"),
	PEG_MID( "PEG MID"),
	PEG_MKT( "PEG MKT"),
	PEG_PRIM( "PEG PRIM"),
	PEG_STK( "PEG STK"),
	REL_PLUS_LMT( "REL + LMT"),
	REL_PLUS_MKT( "REL + MKT"),
	STP_PRT( "STP PRT"),
	TRAIL_LIMIT( "TRAIL LIMIT"),
	TRAIL_LIT( "TRAIL LIT"),
	TRAIL_LMT_PLUS_MKT( "TRAIL LMT + MKT"),
	TRAIL_MIT( "TRAIL MIT"),
	TRAIL_REL_PLUS_MKT( "TRAIL REL + MKT"),
	VOL( "VOL"),
	VWAP( "VWAP");

	private String m_apiString;

	private OrderType( String apiString) {
		m_apiString = apiString;
	}

	public static OrderType get(String apiString) {
		if (apiString != null && apiString.length() > 0 && !apiString.equals( "None") ) {
			for (OrderType type : values() ) {
				if (type.m_apiString.equals( apiString) ) {
					return type;
				}
			}
		}
		return None;
	}

	@Override public String toString() {
		return this == None ? super.toString() : m_apiString;
	}

	@Override public String getApiString() {
		return m_apiString;
	}
}
