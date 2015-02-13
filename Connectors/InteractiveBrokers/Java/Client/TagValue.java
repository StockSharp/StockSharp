/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

public class TagValue {

	public String m_tag;
	public String m_value;

	public TagValue() {
	}

	public TagValue(String p_tag, String p_value) {
		m_tag = p_tag;
		m_value = p_value;
	}

	public boolean equals(Object p_other) {

		if( this == p_other)
            return true;

        if( p_other == null)
            return false;

        TagValue l_theOther = (TagValue)p_other;

        if( Util.StringCompare(m_tag, l_theOther.m_tag) != 0 ||
        	Util.StringCompare(m_value, l_theOther.m_value) != 0) {
        	return false;
        }

		return true;
	}
}
