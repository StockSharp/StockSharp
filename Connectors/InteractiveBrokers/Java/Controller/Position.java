/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

public class Position {
	private NewContract m_contract;
	private String m_account;
	private int m_position;
	private double m_marketPrice;
	private double m_marketValue;
	private double m_averageCost;
	private double m_unrealPnl;
	private double m_realPnl;

	public NewContract contract()   { return m_contract; }
	public int conid()				{ return m_contract.conid(); }
	public double averageCost() 	{ return m_averageCost;}
	public double marketPrice() 	{ return m_marketPrice;}
	public double marketValue() 	{ return m_marketValue;}
	public double realPnl() 		{ return m_realPnl;}
	public double unrealPnl() 		{ return m_unrealPnl;}
	public int position() 			{ return m_position;}
	public String account() 		{ return m_account;}

//	public void account(String v) 		{ m_account = v;}
//	public void averageCost(double v) 	{ m_averageCost = v;}
//	public void marketPrice(double v) 	{ m_marketPrice = v;}
//	public void marketValue(double v) 	{ m_marketValue = v;}
//	public void position(int v) 		{ m_position = v;}
//	public void realPnl(double v) 		{ m_realPnl = v;}
//	public void unrealPnl(double v) 	{ m_unrealPnl = v;}

	public Position( NewContract contract, String account, int position, double marketPrice, double marketValue, double averageCost, double unrealPnl, double realPnl) {
		m_contract = contract;
		m_account = account;
		m_position = position;
		m_marketPrice = marketPrice;
		m_marketValue =marketValue;
		m_averageCost = averageCost;
		m_unrealPnl = unrealPnl;
		m_realPnl = realPnl;
	}
}
