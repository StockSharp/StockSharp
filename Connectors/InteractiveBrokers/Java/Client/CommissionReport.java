/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

public class CommissionReport {

    public String m_execId;
    public double m_commission;
    public String m_currency;
    public double m_realizedPNL;
    public double m_yield;
    public int    m_yieldRedemptionDate; // YYYYMMDD format

    public CommissionReport() {
        m_commission = 0;
        m_realizedPNL = 0;
        m_yield = 0;
        m_yieldRedemptionDate = 0;
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
            CommissionReport l_theOther = (CommissionReport)p_other;
            l_bRetVal = m_execId.equals( l_theOther.m_execId);
        }
        return l_bRetVal;
    }
}
