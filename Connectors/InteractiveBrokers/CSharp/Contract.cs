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
     * @class Contract
     * @brief class describing an instrument's definition
     * @sa ContractDetails
     */
    public class Contract
    {
        private int conId;
        private string symbol;
        private string secType;
        private string lastTradeDateOrContractMonth;
        private double strike;
        private string right;
        private string multiplier;
        private string exchange;
        private string currency;
        private string localSymbol;
        private string primaryExch;
        private string tradingClass;
        private bool includeExpired;
        private string secIdType;
        private string secId;
        private string comboLegsDescription;
        private List<ComboLeg> comboLegs;
        private UnderComp underComp;


        /**
        * @brief The unique contract's identifier
        */
        public int ConId
        {
            get { return conId; }
            set { conId = value; }
        }


        /**
         * @brief The underlying's asset symbol
         */
        public string Symbol
        {
            get { return symbol; }
            set { symbol = value; }
        }

        /**
         * @brief The security's type:
         *      STK - stock
         *      OPT - option
         *      FUT - future
         *      IND - index
         *      FOP - future on an option
         *      CASH - forex pair
         *      BAG - combo
         *      WAR - warrant
         */
        public string SecType
        {
            get { return secType; }
            set { secType = value; }
        }

        /**
        * @brief The contract's expiration date (i.e. Options and Futures)
        */
        public string LastTradeDateOrContractMonth
        {
            get { return lastTradeDateOrContractMonth; }
            set { lastTradeDateOrContractMonth = value; }
        }

        /**
         * @brief The option's strike price
         */
        public double Strike
        {
            get { return strike; }
            set { strike = value; }
        }

        /**
         * @brief Either Put or Call (i.e. Options)
         */
        public string Right
        {
            get { return right; }
            set { right = value; }
        }

        /**
         * @brief The instrument's multiplier (i.e. options, futures).
         */
        public string Multiplier
        {
            get { return multiplier; }
            set { multiplier = value; }
        }

        /**
         * @brief The destination exchange.
         */
        public string Exchange
        {
            get { return exchange; }
            set { exchange = value; }
        }

        /**
         * @brief The underlying's cuurrency
         */
        public string Currency
        {
            get { return currency; }
            set { currency = value; }
        }

        /**
         * @brief The contract's symbol within its primary exchange
         */
        public string LocalSymbol
        {
            get { return localSymbol; }
            set { localSymbol = value; }
        }

        /**
         * @brief The contract's primary exchange.
         */
        public string PrimaryExch
        {
            get { return primaryExch; }
            set { primaryExch = value; }
        }

        /**
         * @brief The trading class name for this contract.
         * Available in TWS contract description window as well. For example, GBL Dec '13 future's trading class is "FGBL"
         */
        public string TradingClass
        {
            get { return tradingClass; }
            set { tradingClass = value; }
        }

        /**
        * @brief If set to true, contract details requests and historical data queries can be performed pertaining to expired contracts.
        * Note: Historical data queries on expired contracts are limited to the last year of the contracts life, and are initially only supported for expired futures contracts.
        */
        public bool IncludeExpired
        {
            get { return includeExpired; }
            set { includeExpired = value; }
        }

        /**
         * @brief Security's identifier when querying contract's details or placing orders
         *      SIN - Example: Apple: US0378331005
         *      CUSIP - Example: Apple: 037833100
         *      SEDOL - Consists of 6-AN + check digit. Example: BAE: 0263494
         *      RIC - Consists of exchange-independent RIC Root and a suffix identifying the exchange. Example: AAPL.O for Apple on NASDAQ.
         */
        public string SecIdType
        {
            get { return secIdType; }
            set { secIdType = value; }
        }

        /**
        * @brief Identifier of the security type
        * @sa secIdType
        */
        public string SecId
        {
            get { return secId; }
            set { secId = value; }
        }

         /**
         * @brief Description of the combo legs.
         */
        public string ComboLegsDescription
        {
            get { return comboLegsDescription; }
            set { comboLegsDescription = value; }
        }

        /**
         * @brief The legs of a combined contract definition
         * @sa ComboLeg
         */
        public List<ComboLeg> ComboLegs
        {
            get { return comboLegs; }
            set { comboLegs = value; }
        }

        /**
         * @brief Delta and underlying price for Delta-Neutral combo orders.
         * Underlying (STK or FUT), delta and underlying price goes into this attribute.
         * @sa UnderComp
         */
        public UnderComp UnderComp
        {
            get { return underComp; }
            set { underComp = value; }
        }
    }
}
