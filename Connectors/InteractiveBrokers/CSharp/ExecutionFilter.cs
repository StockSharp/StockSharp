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
     * @class ExecutionFilter
     * @brief when requesting executions, a filter can be specified to receive only a subset of them
     * @sa Contract, Execution, CommissionReport
     */
    public class ExecutionFilter
    {
        
        private int clientId;
        private string acctCode;
        private string time;
        private string symbol;
        private string secType;
        private string exchange;
        private string side;

        /**
         * @brief The API client which placed the order
         */
        public int ClientId
        {
            get { return clientId; }
            set { clientId = value; }
        }

        /**
        * @brief The account to which the order was allocated to
        */
        public string AcctCode
        {
            get { return acctCode; }
            set { acctCode = value; }
        }

        /**
         * @brief Time from which the executions will be brough yyyymmdd hh:mm:ss
         * Only those executions reported after the specified time will be returned.
         */
        public string Time
        {
            get { return time; }
            set { time = value; }
        }

        /**
        * @brief The instrument's symbol
        */
        public string Symbol
        {
            get { return symbol; }
            set { symbol = value; }
        }

        /**
         * @brief The Contract's security's type (i.e. STK, OPT...)
         */
        public string SecType
        {
            get { return secType; }
            set { secType = value; }
        }

        /**
         * @brief The exchange at which the execution was produced
         */
        public string Exchange
        {
            get { return exchange; }
            set { exchange = value; }
        }

        /**
        * @brief The Contract's side (Put or Call).
        */
        public string Side
        {
            get { return side; }
            set { side = value; }
        }

        public ExecutionFilter()
        {
            clientId = 0;
        }

        public ExecutionFilter(int clientId, String acctCode, String time,
                String symbol, String secType, String exchange, String side)
        {
            ClientId = clientId;
            AcctCode = acctCode;
            Time = time;
            Symbol = symbol;
            SecType = secType;
            Exchange = exchange;
            Side = side;
        }

        public override bool Equals(Object other)
        {
            bool l_bRetVal = false;

            if (other == null)
            {
                l_bRetVal = false;
            }
            else if (this == other)
            {
                l_bRetVal = true;
            }
            else
            {
                ExecutionFilter l_theOther = (ExecutionFilter)other;
                l_bRetVal = (clientId == l_theOther.clientId &&
                    String.Compare(AcctCode, l_theOther.acctCode, true) == 0 &&
                    String.Compare(time, l_theOther.Time, true) == 0 &&
                    String.Compare(symbol, l_theOther.Symbol, true) == 0 &&
                    String.Compare(secType, l_theOther.SecType, true) == 0 &&
                    String.Compare(exchange, l_theOther.Exchange, true) == 0 &&
                    String.Compare(side, l_theOther.Side, true) == 0);
            }
            return l_bRetVal;
        }
    }
}
