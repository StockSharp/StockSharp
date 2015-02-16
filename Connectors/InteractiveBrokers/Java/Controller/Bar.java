/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import java.text.SimpleDateFormat;
import java.util.Date;

public class Bar {
	private static final SimpleDateFormat FORMAT = new SimpleDateFormat( "yyyyMMdd HH:mm:ss"); // format for historical query

	private final long m_time;
	private final double m_high;
	private final double m_low;
	private final double m_open;
	private final double m_close;
	private final double m_wap;
	private final long m_volume;
	private final int m_count;

	public long time()		{ return m_time; }
	public double high() 	{ return m_high; }
	public double low() 	{ return m_low; }
	public double open() 	{ return m_open; }
	public double close() 	{ return m_close; }
	public double wap() 	{ return m_wap; }
	public long volume() 	{ return m_volume; }
	public int count() 		{ return m_count; }

	public Bar( long time, double high, double low, double open, double close, double wap, long volume, int count) {
		m_time = time;
		m_high = high;
		m_low = low;
		m_open = open;
		m_close = close;
		m_wap = wap;
		m_volume = volume;
		m_count = count;
	}

	public String formattedTime() {
		return Formats.fmtDate( m_time * 1000);
	}

	/** Format for query. */
	public static String format( long ms) {
		return FORMAT.format( new Date( ms) );
	}

	@Override public String toString() {
		return String.format( "%s %s %s %s %s", formattedTime(), m_open, m_high, m_low, m_close);
	}
}
