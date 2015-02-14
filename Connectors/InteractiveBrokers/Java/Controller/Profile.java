/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import java.util.ArrayList;
import java.util.StringTokenizer;

public class Profile {
	static final String SEP = "/";

    private String m_name;
	private Type m_type;
	private ArrayList<Allocation> m_allocations = new ArrayList<Allocation>();

	public String name() { return m_name; }
	public Type type() { return m_type; }
	public ArrayList<Allocation> allocations() { return m_allocations; }

	public void name( String v) { m_name = v; }
	public void type( Type v) { m_type = v; }
	public void add( Allocation v) { m_allocations.add( v); }

	public void setAllocations(String val) {
		m_allocations.clear();

		StringTokenizer st = new StringTokenizer( val, ", ");
		while( st.hasMoreTokens() ) {
			String tok = st.nextToken();
			StringTokenizer st2 = new StringTokenizer( tok, SEP);

			Allocation alloc = new Allocation();
			alloc.account( st2.nextToken() );
			alloc.amount( st2.nextToken() );

			m_allocations.add( alloc);
		}
	}

	public static enum Type {
    	NONE, Percents, Ratios, Shares;

    	public static Type get( int ordinal) {
    		return Types.getEnum( ordinal, values() );
    	}
    };

    public static class Allocation {
		private String m_account;
		private String m_amount;

		public String account() { return m_account; }
		public String amount() { return m_amount; }

		public void account( String v) { m_account = v; }
		public void amount( String v) { m_amount = v; }

		@Override public String toString() {
			return m_account + SEP + m_amount;
		}
	}
}
