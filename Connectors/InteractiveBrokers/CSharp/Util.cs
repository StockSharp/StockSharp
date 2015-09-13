/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IBApi
{
    public static class Util
    {
        public static bool StringIsEmpty(string str)
        {
            return str == null || str.Length == 0;
        }


        public static string NormalizeString(string str)
        {
            return str != null ? str : "";
        }

        public static int StringCompare(string lhs, string rhs)
        {
            return NormalizeString(lhs).CompareTo(NormalizeString(rhs));
        }

        public static int StringCompareIgnCase(string lhs, string rhs)
        {
            string normalisedLhs = NormalizeString(lhs);
            string normalisedRhs = NormalizeString(rhs);
            return String.Compare(normalisedLhs, normalisedRhs, true); 
        }

        public static bool VectorEqualsUnordered<T>(List<T> lhs, List<T> rhs)
        {

            if (lhs == rhs)
                return true;

            int lhsCount = lhs == null ? 0 : lhs.Count;
            int rhsCount = rhs == null ? 0 : rhs.Count;

            if (lhsCount != rhsCount)
                return false;

            if (lhsCount == 0)
                return true;

            bool[] matchedRhsElems = new bool[rhsCount];

            for (int lhsIdx = 0; lhsIdx < lhsCount; ++lhsIdx)
            {
                Object lhsElem = lhs[lhsIdx];
                int rhsIdx = 0;
                for (; rhsIdx < rhsCount; ++rhsIdx)
                {
                    if (matchedRhsElems[rhsIdx])
                    {
                        continue;
                    }
                    if (lhsElem.Equals(rhs[rhsIdx]))
                    {
                        matchedRhsElems[rhsIdx] = true;
                        break;
                    }
                }
                if (rhsIdx >= rhsCount)
                {
                    // no matching elem found
                    return false;
                }
            }

            return true;
        }

        public static string IntMaxString(int value)
        {
            return (value == Int32.MaxValue) ? "" : "" + value;
        }

        public static string DoubleMaxString(double value)
        {
            return (value == Double.MaxValue) ? "" : "" + value;
        }

    }
}
