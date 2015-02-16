/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import com.ib.client.ComboLeg;
import com.ib.controller.Types.Action;

public class NewComboLeg {
	enum OpenClose {
		Same, Open, Close, Unknown;

		static OpenClose get( int i) {
			return Types.getEnum( i, values() );
		}

		String getApiString() {
			return "" + ordinal();
		}
	}

	private int m_conid;
	private int m_ratio;
	private Action m_action= Action.BUY;
	private String m_exchange;
	private OpenClose m_openClose = OpenClose.Same;
	public int m_shortSaleSlot; // 1 = clearing broker, 2 = third party
	public String m_designatedLocation;
	public int m_exemptCode;

	public Action action() { return m_action; }
	public int conid() { return m_conid; }
	public int exemptCode() { return m_exemptCode; }
	public int ratio() { return m_ratio; }
	public int shortSaleSlot() { return m_shortSaleSlot; }
	public OpenClose openClose() { return m_openClose; }
	public String designatedLocation() { return m_designatedLocation; }
	public String exchange() { return m_exchange; }

	public void action(Action v) { m_action = v; }
	public void conid(int v) { m_conid = v; }
	public void designatedLocation(String v) { m_designatedLocation = v; }
	public void exchange(String v) { m_exchange = v; }
	public void exemptCode(int v) { m_exemptCode = v; }
	public void openClose(OpenClose v) { m_openClose = v; }
	public void ratio(int v) { m_ratio = v; }
	public void shortSaleSlot(int v) { m_shortSaleSlot = v; }

	public NewComboLeg() {
	}

	public NewComboLeg( ComboLeg leg) {
		m_conid = leg.m_conId;
		m_ratio = leg.m_ratio;
		m_action = Action.valueOf( leg.m_action);
		m_exchange = leg.m_exchange;
		m_openClose = OpenClose.get( leg.m_openClose);
		m_shortSaleSlot = leg.m_shortSaleSlot;
		m_designatedLocation = leg.m_designatedLocation;
		m_exemptCode = leg.m_exemptCode;
	}

	public ComboLeg getComboLeg() {
		ComboLeg leg = new ComboLeg();
		leg.m_conId = m_conid;
		leg.m_ratio = m_ratio;
		leg.m_action = m_action.toString();
		leg.m_exchange = m_exchange;
		leg.m_openClose = m_openClose.ordinal();
		leg.m_shortSaleSlot = m_shortSaleSlot;
		leg.m_designatedLocation = m_designatedLocation;
		leg.m_exemptCode = m_exemptCode;
		return leg;
	}

	@Override public String toString() {
		return String.format( "%s %s %s", m_action, m_ratio, m_conid);
	}
}
