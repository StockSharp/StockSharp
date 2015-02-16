/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import static com.ib.controller.NewContract.add;

import java.util.Vector;

import com.ib.client.ContractDetails;
import com.ib.client.TagValue;

public class NewContractDetails {
	private NewContract m_contract;
	private String m_marketName;
	private double m_minTick;
	private int m_priceMagnifier;
	private String m_orderTypes;
	private String m_validExchanges;
	private int m_underConid;
	private String m_longName;
	private String m_contractMonth;
	private String m_industry;
	private String m_category;
	private String m_subcategory;
	private String m_timeZoneId;
	private String m_tradingHours;
	private String m_liquidHours;
	private String m_evRule;
	private double m_evMultiplier;
	private Vector<TagValue> m_secIdList; // CUSIP/ISIN/etc.

	// BOND values
	private String m_cusip;
	private String m_ratings;
	private String m_descAppend;
	private String m_bondType;
	private String m_couponType;
	private boolean m_callable= false;
	private boolean m_putable= false;
	private double m_coupon= 0;
	private boolean m_convertible= false;
	private String m_maturity;
	private String m_issueDate;
	private String m_nextOptionDate;
	private String m_nextOptionType;
	private boolean m_nextOptionPartial = false;
	private String m_notes;

	public int conid() 					{ return m_contract.conid(); }
	public NewContract contract() 		{ return m_contract; }
	public String marketName() 			{ return m_marketName; }
	public double minTick() 			{ return m_minTick; }
	public int PripeMagnifier() 		{ return m_priceMagnifier; }
	public String orderTypes() 			{ return m_orderTypes; }
	public String validExchanges() 		{ return m_validExchanges; }
	public int underConid() 			{ return m_underConid; }
	public String longName() 			{ return m_longName; }
	public String contractMonth() 		{ return m_contractMonth; }
	public String industry() 			{ return m_industry; }
	public String category() 			{ return m_category; }
	public String subcategory() 		{ return m_subcategory; }
	public String timeZoneId() 			{ return m_timeZoneId; }
	public String tradingHours() 		{ return m_tradingHours; }
	public String liquidHours() 		{ return m_liquidHours; }
	public String evRule() 				{ return m_evRule; }
	public double evMultiplier() 		{ return m_evMultiplier; }
	public Vector<TagValue> secIdList() { return m_secIdList; }
	public String cusip() 				{ return m_cusip; }
	public String ratings() 			{ return m_ratings; }
	public String descAppend() 			{ return m_descAppend; }
	public String bondType() 			{ return m_bondType; }
	public String couponType() 			{ return m_couponType; }
	public boolean callable() 			{ return m_callable; }
	public boolean putable() 			{ return m_putable; }
	public double coupon() 				{ return m_coupon; }
	public boolean convertible() 		{ return m_convertible; }
	public String maturity() 			{ return m_maturity; }
	public String issueDate() 			{ return m_issueDate; }
	public String nextOptionDate() 		{ return m_nextOptionDate; }
	public String nextOptionType() 		{ return m_nextOptionType; }
	public boolean nextOptionPartial() 	{ return m_nextOptionPartial; }
	public String notes() 				{ return m_notes; }

	public NewContractDetails( ContractDetails other) {
		m_contract = new NewContract( other.m_summary);
		m_marketName = other.m_marketName;
		m_minTick = other.m_minTick;
		m_priceMagnifier = other.m_priceMagnifier;
		m_orderTypes = other.m_orderTypes;
		m_validExchanges = other.m_validExchanges;
		m_underConid = other.m_underConId;
		m_longName = other.m_longName;
		m_contractMonth = other.m_contractMonth;
		m_industry = other.m_industry;
		m_category = other.m_category;
		m_subcategory = other.m_subcategory;
		m_timeZoneId = other.m_timeZoneId;
		m_tradingHours = other.m_tradingHours;
		m_liquidHours = other.m_liquidHours;
		m_evRule = other.m_evRule;
		m_evMultiplier = other.m_evMultiplier;
		m_secIdList = other.m_secIdList;
		m_cusip = other.m_cusip;
		m_ratings = other.m_ratings;
		m_descAppend = other.m_descAppend;
		m_bondType = other.m_bondType;
		m_couponType = other.m_couponType;
		m_callable = other.m_callable;
		m_putable = other.m_putable;
		m_coupon = other.m_coupon;
		m_convertible = other.m_convertible;
		m_maturity = other.m_maturity;
		m_issueDate = other.m_issueDate;
		m_nextOptionDate = other.m_nextOptionDate;
		m_nextOptionType = other.m_nextOptionType;
		m_nextOptionPartial = other.m_nextOptionPartial;
		m_notes = other.m_notes;
	}

	@Override public String toString() {
	    StringBuilder sb = new StringBuilder( m_contract.toString() );

	    add( sb, "marketName", m_marketName);
	    add( sb, "minTick", m_minTick);
	    add( sb, "priceMagnifier", m_priceMagnifier);
	    add( sb, "orderTypes", m_orderTypes);
	    add( sb, "validExchanges", m_validExchanges);
	    add( sb, "underConId", m_underConid);
	    add( sb, "longName", m_longName);
	    add( sb, "contractMonth", m_contractMonth);
	    add( sb, "industry", m_industry);
	    add( sb, "category", m_category);
	    add( sb, "subcategory", m_subcategory);
	    add( sb, "timeZoneId", m_timeZoneId);
	    add( sb, "tradingHours", m_tradingHours);
	    add( sb, "liquidHours", m_liquidHours);
	    add( sb, "evRule", m_evRule);
	    add( sb, "evMultiplier", m_evMultiplier);

	    add( sb, "cusip", m_cusip);
	    add( sb, "ratings", m_ratings);
	    add( sb, "descAppend", m_descAppend);
	    add( sb, "bondType", m_bondType);
	    add( sb, "couponType", m_couponType);
	    add( sb, "callable", m_callable);
	    add( sb, "putable", m_putable);
	    add( sb, "coupon", m_coupon);
	    add( sb, "convertible", m_convertible);
	    add( sb, "maturity", m_maturity);
	    add( sb, "issueDate", m_issueDate);
	    add( sb, "nextOptionDate", m_nextOptionDate);
	    add( sb, "nextOptionType", m_nextOptionType);
	    add( sb, "nextOptionPartial", m_nextOptionPartial);
	    add( sb, "notes", m_notes);

	    return sb.toString();
	}
}
