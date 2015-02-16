/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import com.ib.client.OrderState;

public class NewOrderState {
	private OrderStatus m_status;
	private String m_initMargin;
	private String m_maintMargin;
	private String m_equityWithLoan;
	private double m_commission;
	private double m_minCommission;
	private double m_maxCommission;
	private String m_commissionCurrency;
	private String m_warningText;

	public double commission() 					{ return m_commission; }
	public double maxCommission() 				{ return m_maxCommission; }
	public double minCommission() 				{ return m_minCommission; }
	public OrderStatus status() 				{ return m_status; }
	public String commissionCurrency() 			{ return m_commissionCurrency; }
	public String equityWithLoan() 				{ return m_equityWithLoan; }
	public String initMargin() 					{ return m_initMargin; }
	public String maintMargin() 				{ return m_maintMargin; }
	public String warningText() 				{ return m_warningText; }

	public void commission(double v) 			{ m_commission = v; }
	public void commissionCurrency(String v) 	{ m_commissionCurrency = v; }
	public void equityWithLoan(String v) 		{ m_equityWithLoan = v; }
	public void initMargin(String v) 			{ m_initMargin = v; }
	public void maintMargin(String v) 			{ m_maintMargin = v; }
	public void maxCommission(double v) 		{ m_maxCommission = v; }
	public void minCommission(double v) 		{ m_minCommission = v; }
	public void status(OrderStatus v) 			{ m_status = v; }
	public void warningText(String v) 			{ m_warningText = v; }

	public NewOrderState(OrderState orderState) {
		m_status = OrderStatus.valueOf( orderState.m_status);
		m_initMargin = orderState.m_initMargin;
		m_maintMargin = orderState.m_maintMargin;
		m_equityWithLoan = orderState.m_equityWithLoan;
		m_commission = orderState.m_commission;
		m_minCommission = orderState.m_minCommission;
		m_maxCommission = orderState.m_maxCommission;
		m_commissionCurrency = orderState.m_commissionCurrency;
		m_warningText = orderState.m_warningText;
	}
}
