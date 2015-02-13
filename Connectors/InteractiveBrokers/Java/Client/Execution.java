/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

public class Execution {
    public int 		m_orderId;
    public int 		m_clientId;
    public String 	m_execId;
    public String 	m_time;
    public String 	m_acctNumber;
    public String 	m_exchange;
    public String 	m_side;
    public int 		m_shares;
    public double 	m_price;
    public int		m_permId;
    public int         m_liquidation;
    public int		m_cumQty;
    public double	m_avgPrice;
    public String   m_orderRef;
    public String 	m_evRule;
    public double 	m_evMultiplier;

    public Execution() {
        m_orderId = 0;
        m_clientId = 0;
        m_shares = 0;
        m_price = 0;
        m_permId = 0;
        m_liquidation = 0;
        m_cumQty = 0;
        m_avgPrice = 0;
        m_evMultiplier = 0;
    }

    public Execution( int p_orderId, int p_clientId, String p_execId, String p_time,
                      String p_acctNumber, String p_exchange, String p_side, int p_shares,
                      double p_price, int p_permId, int p_liquidation, int p_cumQty,
                      double p_avgPrice, String p_orderRef, String p_evRule, double p_evMultiplier) {
        m_orderId = p_orderId;
        m_clientId = p_clientId;
        m_execId = p_execId;
        m_time = p_time;
      	m_acctNumber = p_acctNumber;
      	m_exchange = p_exchange;
      	m_side = p_side;
      	m_shares = p_shares;
      	m_price = p_price;
        m_permId = p_permId;
        m_liquidation = p_liquidation;
        m_cumQty = p_cumQty;
        m_avgPrice = p_avgPrice;
        m_orderRef = p_orderRef;
        m_evRule = p_evRule;
        m_evMultiplier = p_evMultiplier;
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
            Execution l_theOther = (Execution)p_other;
            l_bRetVal = m_execId.equals( l_theOther.m_execId);
        }
        return l_bRetVal;
    }
}
