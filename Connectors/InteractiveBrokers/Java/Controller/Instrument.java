/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

public enum Instrument {
	STK,
	BOND,
	EFP,
	FUT_EU,
	FUT_HK,
	FUT_NA,
	FUT_US,
	IND_EU,
	IND_HK,
	IND_US,
	PMONITOR,
	PMONITORM,
	SLB_US,
	STOCK_EU,
	STOCK_HK,
	STOCK_NA,
	WAR_EU;

	public String toString() {
		return super.toString().replace( '_', '.');
	}
}
