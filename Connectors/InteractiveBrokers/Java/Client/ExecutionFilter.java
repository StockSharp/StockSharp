/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

public class ExecutionFilter{
    public int 		m_clientId; // zero means no filtering on this field
    public String 	m_acctCode;
    public String 	m_time;
    public String 	m_symbol;
    public String 	m_secType;
    public String 	m_exchange;
    public String 	m_side;

    public ExecutionFilter() {
        m_clientId = 0;
    }

    public ExecutionFilter( int p_clientId, String p_acctCode, String p_time,
    		String p_symbol, String p_secType, String p_exchange, String p_side) {
        m_clientId = p_clientId;
        m_acctCode = p_acctCode;
        m_time = p_time;
        m_symbol = p_symbol;
        m_secType = p_secType;
        m_exchange = p_exchange;
        m_side = p_side;
    }

    public boolean equals(Object p_other) {
        boolean l_bRetVal = false;

        if ( p_other == null ) {
            l_bRetVal = false;
		}
        else if ( this == p_other ) {
            l_bRetVal = true;
        }
        else {
            ExecutionFilter l_theOther = (ExecutionFilter)p_other;
            l_bRetVal = (m_clientId == l_theOther.m_clientId &&
                    m_acctCode.equalsIgnoreCase( l_theOther.m_acctCode) &&
                    m_time.equalsIgnoreCase( l_theOther.m_time) &&
                    m_symbol.equalsIgnoreCase( l_theOther.m_symbol) &&
                    m_secType.equalsIgnoreCase( l_theOther.m_secType) &&
                    m_exchange.equalsIgnoreCase( l_theOther.m_exchange) &&
                    m_side.equalsIgnoreCase( l_theOther.m_side) );
        }
        return l_bRetVal;
    }
}
