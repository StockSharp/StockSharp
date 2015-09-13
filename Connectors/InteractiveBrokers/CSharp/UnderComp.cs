/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace IBApi
{
    /**
     * @brief Delta-Neutral Underlying Component.
     */
    public class UnderComp
    {
        private int conId;
        private double delta;
        private double price;

        /**
         * @brief
         */
        public int ConId
        {
            get { return conId; }
            set { conId = value; }
        }

        /**
        * @brief
        */
        public double Delta
        {
            get { return delta; }
            set { delta = value; }
        }

        /**
        * @brief
        */
        public double Price
        {
            get { return price; }
            set { price = value; }
        }
    }
}
