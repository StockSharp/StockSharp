/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

/** This class is used to build messages so the entire message can be
 *  sent to the socket in a single write. */
public class Builder {
	private static final char SEP = 0;

	private StringBuilder m_sb = new StringBuilder( 4096);

	public void send(int a) {
		send( a == Integer.MAX_VALUE ? "" : String.valueOf( a) );
	}

	public void send(double a) {
		send( a == Double.MAX_VALUE ? "" : String.valueOf( a) );
	}

	public void send( boolean a) {
		send( a ? 1 : 0);
	}

	public void send( IApiEnum a) {
		send( a.getApiString() );
	}

	public void send( String a) {
		if (a != null) {
			m_sb.append( a);
		}
		m_sb.append( SEP);
	}

	public String toString() {
		return m_sb.toString();
	}

	public byte[] getBytes() {
		return m_sb.toString().getBytes();
	}
}
