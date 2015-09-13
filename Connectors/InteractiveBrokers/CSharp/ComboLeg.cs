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
     * @class ComboLeg
     * @brief Class representing a leg within combo orders.
     * @sa Order
     */
    public class ComboLeg
    {
        public static int SAME = 0;
        public static int 	OPEN = 1;
        public static int 	CLOSE = 2;
        public static int 	UNKNOWN = 3;

        
        private int conId;
        private int ratio;
        private string action;
        private string exchange;
        private int openClose;
        private int shortSaleSlot;
        private string designatedLocation;
        private int exemptCode;

        /**
         * @brief The Contract's IB's unique id
         */
        public int ConId
        {
            get {return conId; }
            set { conId = value; }
        }

        /**
          * @brief Select the relative number of contracts for the leg you are constructing. To help determine the ratio for a specific combination order, refer to the Interactive Analytics section of the User's Guide.
          */
        public int Ratio
        {
            get { return ratio; }
            set { ratio = value; }
        }

        /**
         * @brief The side (buy or sell) of the leg:\n
         *      - For individual accounts, only BUY and SELL are available. SSHORT is for institutions.
         */
        public string Action
        {
            get { return action; }
            set { action = value; }
        }
        /**
         * @brief The destination exchange to which the order will be routed.
         */
        public string Exchange
        {
            get { return exchange; }
            set { exchange = value; }
        }

        /**
        * @brief Specifies whether an order is an open or closing order.
        * For instituational customers to determine if this order is to open or close a position.
        *      0 - Same as the parent security. This is the only option for retail customers.\n
        *      1 - Open. This value is only valid for institutional customers.\n
        *      2 - Close. This value is only valid for institutional customers.\n
        *      3 - Unknown
        */
        public int OpenClose
        {
            get { return openClose; }
            set { openClose = value; }
        }

        /**
         * @brief For stock legs when doing short selling.
         * Set to 1 = clearing broker, 2 = third party
         */
        public int ShortSaleSlot
        {
            get { return shortSaleSlot; }
            set { shortSaleSlot = value; }
        }

        /**
         * @brief When ShortSaleSlot is 2, this field shall contain the designated location.
         */
        public string DesignatedLocation
        {
            get { return designatedLocation; }
            set { designatedLocation = value; }
        }

        /**
         * @brief -
         */
        public int ExemptCode
        {
            get { return exemptCode; }
            set { exemptCode = value; }
        }

        public ComboLeg()
        {
        }

        public ComboLeg(int conId, int ratio, string action, string exchange, int openClose, int shortSaleSlot, string designatedLocation, int exemptCode)
        {
            ConId = conId;
            Ratio = ratio;
            Action = action;
            Exchange = exchange;
            OpenClose = openClose;
            ShortSaleSlot = shortSaleSlot;
            DesignatedLocation = designatedLocation;
            ExemptCode = exemptCode;
        }
    }
}
