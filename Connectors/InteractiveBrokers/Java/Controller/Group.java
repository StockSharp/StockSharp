/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import java.util.ArrayList;
import java.util.StringTokenizer;

import com.ib.controller.Types.Method;

public class Group {
	private String m_name;
	private Method m_defaultMethod;
	private ArrayList<String> m_accounts = new ArrayList<String>();

	public String name() 					{ return m_name; }
	public Method defaultMethod() 			{ return m_defaultMethod; }
	public ArrayList<String> accounts() 	{ return m_accounts; }

	public void name( String v) 			{ m_name = v; }
	public void defaultMethod( Method v) 	{ m_defaultMethod = v; }
	public void addAccount( String acct) 	{ m_accounts.add( acct); }

	/** @param val is a comma or space delimited string of accounts */
	public void setAllAccounts(String val) {
		m_accounts.clear();

		StringTokenizer st = new StringTokenizer( val, " ,");
		while( st.hasMoreTokens() ) {
			m_accounts.add( st.nextToken() );
		}
	}
}
