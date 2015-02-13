/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

public class TradeId {
	private String m_key;
	private String m_full;

	public String key() 		{ return m_key; }
	public String full() 		{ return m_full; }

	public TradeId( String id) {
		m_full = id;
		int i = id.lastIndexOf( '.');
		m_key = id.substring( i + 1);
	}
}
