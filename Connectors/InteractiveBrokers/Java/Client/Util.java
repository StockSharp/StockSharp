/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

import java.util.Vector;

public class Util {

	public static boolean StringIsEmpty(String str) {
		return str == null || str.length() == 0;
	}

    public static String NormalizeString(String str) {
    	return str != null ? str : "";
    }

    public static int StringCompare(String lhs, String rhs) {
    	return NormalizeString(lhs).compareTo(NormalizeString(rhs));
    }

    public static int StringCompareIgnCase(String lhs, String rhs) {
    	return NormalizeString(lhs).compareToIgnoreCase(NormalizeString(rhs));
    }

    public static boolean VectorEqualsUnordered(Vector lhs, Vector rhs) {

    	if (lhs == rhs)
    		return true;

    	int lhsCount = lhs == null ? 0 : lhs.size();
    	int rhsCount = rhs == null ? 0 : rhs.size();

    	if (lhsCount != rhsCount)
    		return false;

    	if (lhsCount == 0)
    		return true;

    	boolean[] matchedRhsElems = new boolean[rhsCount];

    	for (int lhsIdx = 0; lhsIdx < lhsCount; ++lhsIdx) {
    		Object lhsElem = lhs.get(lhsIdx);
    		int rhsIdx = 0;
    		for (; rhsIdx < rhsCount; ++rhsIdx) {
    			if (matchedRhsElems[rhsIdx]) {
    				continue;
    			}
    			if (lhsElem.equals(rhs.get(rhsIdx))) {
    				matchedRhsElems[rhsIdx] = true;
    				break;
    			}
    		}
    		if (rhsIdx >= rhsCount) {
    			// no matching elem found
    			return false;
    		}
    	}

    	return true;
    }

    public static String IntMaxString(int value) {
    	return (value == Integer.MAX_VALUE) ? "" : "" + value;
    }

    public static String DoubleMaxString(double value) {
    	return (value == Double.MAX_VALUE) ? "" : "" + value;
    }

}
