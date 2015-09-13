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
     * @class Execution
     * @brief Class describing an order's execution.
     * @sa ExecutionFilter, CommissionReport
     */
    public class Execution
    {
        private int orderId;
        private int clientId;
        private string execId;
        private string time;
        private string acctNumber;
        private string exchange;
        private string side;
        private double shares;
        private double price;
        private int permId;
        private int liquidation;
        private int cumQty;
        private double avgPrice;
        private string orderRef;
        private string evRule;
        private double evMultiplier;

        /**
         * @brief The API client's order Id.
         */
        public int OrderId
        {
            get { return orderId; }
            set { orderId = value; }
        }

        /**
         * @brief The API client identifier which placed the order which originated this execution.
         */
        public int ClientId
        {
            get { return clientId; }
            set { clientId = value; }
        }

        /**
         * @brief The execution's identifier.
         */
        public string ExecId
        {
            get { return execId; }
            set { execId = value; }
        }

        /**
         * @brief The execution's server time.
         */
        public string Time
        {
            get { return time; }
            set { time = value; }
        }

        /**
         * @brief The account to which the order was allocated.
         */
        public string AcctNumber
        {
            get { return acctNumber; }
            set { acctNumber = value; }
        }

        /**
         * @brief The exchange where the execution took place.
         */
        public string Exchange
        {
            get { return exchange; }
            set { exchange = value; }
        }

        /**
         * @brief Specifies if the transaction was buy or sale
         * BOT for bought, SLD for sold
         */
        public string Side
        {
            get { return side; }
            set { side = value; }
        }

        /**
         * @brief The number of shares filled.
         */
        public double Shares
        {
            get { return shares; }
            set { shares = value; }
        }

        /**
         * @brief The order's execution price excluding commissions.
         */
        public double Price
        {
            get { return price; }
            set { price = value; }
        }

        /**
         * @brief The TWS order identifier.
         */
        public int PermId
        {
            get { return permId; }
            set { permId = value; }
        }

        /**
         * @brief Identifies the position as one to be liquidated last should the need arise.
         */
        public int Liquidation
        {
            get { return liquidation; }
            set { liquidation = value; }
        }

        /**
         * @brief Cumulative quantity. 
         * Used in regular trades, combo trades and legs of the combo.
         */
        public int CumQty
        {
            get { return cumQty; }
            set { cumQty = value; }
        }

        /**
         * @brief Average price. 
         * Used in regular trades, combo trades and legs of the combo. Includes commissions.
         */
        public double AvgPrice
        {
            get { return avgPrice; }
            set { avgPrice = value; }
        }

        /**
         * @brief Allows API client to add a reference to an order.
         */
        public string OrderRef
        {
            get { return orderRef; }
            set { orderRef = value; }
        }

        /**
         * @brief The Economic Value Rule name and the respective optional argument.
         * The two values should be separated by a colon. For example, aussieBond:YearsToExpiration=3. When the optional argument is not present, the first value will be followed by a colon.
         */
        public string EvRule
        {
            get { return evRule; }
            set { evRule = value; }
        }

        /**
         * @brief Tells you approximately how much the market value of a contract would change if the price were to change by 1.
         * It cannot be used to get market value by multiplying the price by the approximate multiplier.
         */
        public double EvMultiplier
        {
            get { return evMultiplier; }
            set { evMultiplier = value; }
        }

        public Execution()
        {
            orderId = 0;
            clientId = 0;
            shares = 0;
            price = 0;
            permId = 0;
            liquidation = 0;
            cumQty = 0;
            avgPrice = 0;
            evMultiplier = 0;
        }

        public Execution(int orderId, int clientId, String execId, String time,
                          String acctNumber, String exchange, String side, double shares,
                          double price, int permId, int liquidation, int cumQty,
                          double avgPrice, String orderRef, String evRule, double evMultiplier)
        {
            OrderId = orderId;
            ClientId = clientId;
            ExecId = execId;
            Time = time;
            AcctNumber = acctNumber;
            Exchange = exchange;
            Side = side;
            Shares = shares;
            Price = price;
            PermId = permId;
            Liquidation = liquidation;
            CumQty = cumQty;
            AvgPrice = avgPrice;
            OrderRef = orderRef;
            EvRule = evRule;
            EvMultiplier = evMultiplier;
        }

        public override bool Equals(Object p_other)
        {
            bool l_bRetVal = false;

            if (p_other == null)
            {
                l_bRetVal = false;
            }
            else if (this == p_other)
            {
                l_bRetVal = true;
            }
            else
            {
                Execution l_theOther = (Execution)p_other;
                l_bRetVal = String.Compare(ExecId, l_theOther.ExecId, true) == 0;
            }
            return l_bRetVal;
        }
    }
}
