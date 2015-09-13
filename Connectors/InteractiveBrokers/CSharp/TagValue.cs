/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace IBApi
{
    public class TagValue
    {
        private string tag;
        private string value;

        public string Tag
        {
            get { return tag; }
            set { tag = value; }
        }
        

        public string Value
        {
            get { return this.value; }
            set { this.value = value; }
        }

        public TagValue()
        {
        }

        public TagValue(string p_tag, string p_value)
        {
            tag = p_tag;
            value = p_value;
        }

        public override bool Equals(Object other)
        {

            if (this == other)
                return true;

            if (other == null)
                return false;

            TagValue l_theOther = (TagValue)other;

            if (Util.StringCompare(Tag, l_theOther.Tag) != 0 ||
                Util.StringCompare(Value, l_theOther.Value) != 0)
            {
                return false;
            }

            return true;
        }
    }
}
