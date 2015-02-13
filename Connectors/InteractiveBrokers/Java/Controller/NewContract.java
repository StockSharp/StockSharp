/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import java.util.ArrayList;
import java.util.Vector;

import com.ib.client.ComboLeg;
import com.ib.client.Contract;
import com.ib.client.UnderComp;
import com.ib.controller.Types.Right;
import com.ib.controller.Types.SecIdType;
import com.ib.controller.Types.SecType;

public class NewContract implements Cloneable {
    private int m_conid;
	private String m_symbol;
    private SecType m_secType = SecType.None;
    private String m_expiry;
    private double m_strike;
    private Right m_right = Right.None;
    private String m_multiplier; // should be double
    private String m_exchange;
    private String m_currency;
    private String m_localSymbol;
    private String m_tradingClass;
	private String m_primaryExch;
    private SecIdType m_secIdType = SecIdType.None;
    private String m_secId;
    public DeltaNeutralContract m_underComp;    // what is this?
    private ArrayList<NewComboLeg> m_comboLegs = new ArrayList<NewComboLeg>(); // would be final except for clone

    public double strike() { return m_strike; }
    public int conid() { return m_conid; }
    public SecIdType secIdType() { return m_secIdType; }
    public SecType secType() { return m_secType; }
    public String currency() { return m_currency; }
    public String exchange() { return m_exchange; }
    public String expiry() { return m_expiry; }
    public String localSymbol() { return m_localSymbol; }
    public String tradingClass() { return m_tradingClass; }
    public String multiplier() { return m_multiplier; }
	public String primaryExch() { return m_primaryExch; }
    public Right right() { return m_right; }
    public String secId() { return m_secId; }
    public String symbol() { return m_symbol; }
    public DeltaNeutralContract underComp() { return m_underComp; }
    public ArrayList<NewComboLeg> comboLegs() { return m_comboLegs; }

    public void conid(int v) { m_conid = v; }
    public void currency(String v) { m_currency = v; }
    public void exchange(String v) { m_exchange = v; }
    public void expiry(String v) { m_expiry = v; }
    public void localSymbol(String v) { m_localSymbol = v; }
    public void tradingClass(String v) { m_tradingClass = v; }
    public void multiplier(String v) { m_multiplier = v; }
    public void primaryExch(String v) { m_primaryExch = v; }
    public void right(Right v) { m_right = v; }
    public void secId(String v) { m_secId = v; }
    public void secIdType(SecIdType v) { m_secIdType = v; }
    public void secType(SecType v) { m_secType = v; }
    public void strike(double v) { m_strike = v; }
    public void symbol(String v) { m_symbol = v; }
    public void underComp( DeltaNeutralContract v) { m_underComp = v; }

    public NewContract() {
    	m_secType = SecType.None;
    	m_secIdType = SecIdType.None;
    }

    public NewContract( Contract c) {
    	m_conid = c.m_conId;
    	m_symbol = c.m_symbol;
    	m_secType = c.m_secType == null ? SecType.None : SecType.valueOf( c.m_secType);
    	m_expiry = c.m_expiry == null || c.m_expiry.equals( "0") ? "" : c.m_expiry;
    	m_strike = c.m_strike;
    	m_right = Right.get( c.m_right);
    	m_multiplier = c.m_multiplier;
    	m_exchange = c.m_exchange;
    	m_currency = c.m_currency;
    	m_localSymbol = c.m_localSymbol;
    	m_tradingClass = c.m_tradingClass;
		m_primaryExch = c.m_primaryExch;
    	m_secIdType = SecIdType.get( c.m_secIdType);
    	m_secId = c.m_secId;
    	m_underComp = c.m_underComp != null ? new DeltaNeutralContract( c.m_underComp.m_conId, c.m_underComp.m_delta, c.m_underComp.m_price) : null;

    	m_comboLegs.clear();
    	if (c.m_comboLegs != null) {
    		for (ComboLeg leg : c.m_comboLegs) {
    			m_comboLegs.add( new NewComboLeg( leg) );
    		}
    	}
    }

	public Contract getContract() {
		Contract c = new Contract();
		c.m_conId = m_conid;
		c.m_symbol = m_symbol;
		c.m_secType = m_secType.toString();
		c.m_expiry = m_expiry;
		c.m_strike = m_strike;
		c.m_right = m_right.getApiString();
		c.m_multiplier = m_multiplier;
		c.m_exchange = m_exchange;
		c.m_primaryExch = m_primaryExch;
		c.m_currency = m_currency;
		c.m_localSymbol = m_localSymbol;
		c.m_tradingClass = m_tradingClass;
		c.m_primaryExch = m_primaryExch;
		c.m_secIdType = m_secIdType.getApiString();
		c.m_secId = m_secId;

		if (m_underComp != null) {
			c.m_underComp = new UnderComp();
			c.m_underComp.m_conId = m_underComp.conid();
			c.m_underComp.m_delta = m_underComp.delta();
			c.m_underComp.m_price = m_underComp.price();
		}

		c.m_comboLegs = new Vector<ComboLeg>();
		for (NewComboLeg leg : m_comboLegs) {
			c.m_comboLegs.add( leg.getComboLeg() );
		}

		return c;
	}

	/** Returns a text description that can be used for display. */
	public String description() {
		StringBuilder sb = new StringBuilder();

		if (isCombo() ) {
			int i = 0;
			for (NewComboLeg leg : m_comboLegs) {
				if (i++ > 0) {
					sb.append( "/");
				}
				sb.append( leg.toString() );
			}
		}
		else {
			sb.append( m_symbol);
			app( sb, m_secType);
			app( sb, m_exchange);

			if (m_exchange != null && m_exchange.equals( "SMART") && m_primaryExch != null) {
				app( sb, m_primaryExch);
			}

			app( sb, m_expiry);

			if (m_strike != 0) {
				app( sb, m_strike);
			}

			if (m_right != Right.None) {
				app( sb, m_right);
			}
		}

		return sb.toString();
	}

	private static void app(StringBuilder buf, Object obj) {
		if (obj != null) {
			buf.append( " ");
			buf.append( obj);
		}
	}

	public boolean isCombo() {
		return m_comboLegs.size() > 0;
	}

	@Override public String toString() {
	    StringBuilder sb = new StringBuilder();

	    add( sb, "conid", m_conid);
	    add( sb, "symbol", m_symbol);
	    add( sb, "secType", m_secType);
	    add( sb, "expiry", m_expiry);
	    add( sb, "strike", m_strike);
	    add( sb, "right", m_right);
	    add( sb, "multiplier", m_multiplier);
	    add( sb, "exchange", m_exchange);
	    add( sb, "currency", m_currency);
	    add( sb, "localSymbol", m_localSymbol);
	    add( sb, "tradingClass", m_tradingClass);
	    add( sb, "primaryExch", m_primaryExch);
	    add( sb, "secIdType", m_secIdType);
	    add( sb, "secId", m_secId);

	    return sb.toString();
	}

	public static void add(StringBuilder sb, String tag, Object val) {
	    if (val == null || val instanceof String && ((String)val).length() == 0) {
	        return;
	    }

        sb.append( tag);
        sb.append( '\t');
        sb.append( val);
        sb.append( '\n');
	}

	@Override public NewContract clone() {
		try {
			NewContract copy = (NewContract)super.clone();
			copy.m_comboLegs = new ArrayList<NewComboLeg>( copy.m_comboLegs);
			return copy;
		}
		catch (CloneNotSupportedException e) {
			e.printStackTrace();
			return null;
		}
	}
}
