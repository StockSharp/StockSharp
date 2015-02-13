/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

public class OrderState {

	public String m_status;

	public String m_initMargin;
	public String m_maintMargin;
	public String m_equityWithLoan;

	public double m_commission;
	public double m_minCommission;
	public double m_maxCommission;
	public String m_commissionCurrency;

	public String m_warningText;

	OrderState() {
		this (null, null, null, null, 0.0, 0.0, 0.0, null, null);
	}

	OrderState(String status, String initMargin, String maintMargin,
			String equityWithLoan, double commission, double minCommission,
			double maxCommission, String commissionCurrency, String warningText) {

		m_initMargin = initMargin;
		m_maintMargin = maintMargin;
		m_equityWithLoan = equityWithLoan;
		m_commission = commission;
		m_minCommission = minCommission;
		m_maxCommission = maxCommission;
		m_commissionCurrency = commissionCurrency;
		m_warningText = warningText;
	}

	public boolean equals(Object other) {

        if (this == other)
            return true;

        if (other == null)
            return false;

        OrderState state = (OrderState)other;

        if (m_commission != state.m_commission ||
        	m_minCommission != state.m_minCommission ||
        	m_maxCommission != state.m_maxCommission) {
        	return false;
        }

        if (Util.StringCompare(m_status, state.m_status) != 0 ||
        	Util.StringCompare(m_initMargin, state.m_initMargin) != 0 ||
        	Util.StringCompare(m_maintMargin, state.m_maintMargin) != 0 ||
        	Util.StringCompare(m_equityWithLoan, state.m_equityWithLoan) != 0 ||
        	Util.StringCompare(m_commissionCurrency, state.m_commissionCurrency) != 0) {
        	return false;
        }

        return true;
	}
}
