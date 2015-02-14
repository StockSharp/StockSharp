/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;


public class DeltaNeutralContract {
	private int m_conid;
	private double m_delta;
	private double m_price;

	public int conid() { return m_conid; }
	public double delta() { return m_delta; }
	public double price() { return m_price; }

	public DeltaNeutralContract(int conid, double delta, double price) {
		m_conid = conid;
		m_delta = delta;
		m_price = price;
	}
}
