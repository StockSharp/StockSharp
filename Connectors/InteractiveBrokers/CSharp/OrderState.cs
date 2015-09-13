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
     * @class OrderState
     * @brief Provides an active order's current state
     * @sa Order
     */
    public class OrderState
    {
        private string status;
        private string initMargin;
        private string maintMargin;
        private string equityWithLoan;
        private double commission;
        private double minCommission;
        private double maxCommission;
        private string commissionCurrency;
        private string warningText;

        /**
         * @brief The order's current status
         */
        public string Status
        {
            get { return status; }
            set { status = value; }
        }

        /**
         * @brief The order's impact on the account's initial margin.
         */
        public string InitMargin
        {
            get { return initMargin; }
            set { initMargin = value; }
        }

        /**
        * @brief The order's impact on the account's maintenance margin
        */
        public string MaintMargin
        {
            get { return maintMargin; }
            set { maintMargin = value; }
        }

        /**
        * @brief Shows the impact the order would have on the account's equity with loan
        */
        public string EquityWithLoan
        {
            get { return equityWithLoan; }
            set { equityWithLoan = value; }
        }

        /**
          * @brief The order's generated commission.
          */
        public double Commission
        {
            get { return commission; }
            set { commission = value; }
        }

        /**
        * @brief The execution's minimum commission.
        */
        public double MinCommission
        {
            get { return minCommission; }
            set { minCommission = value; }
        }

        /**
        * @brief The executions maximum commission.
        */
        public double MaxCommission
        {
            get { return maxCommission; }
            set { maxCommission = value;  }
        }

        /**
         * @brief The generated commission currency
         * @sa CommissionReport
         */
        public string CommissionCurrency
        {
            get { return commissionCurrency; }
            set { commissionCurrency = value; }
        }

        /**
         * @brief If the order is warranted, a descriptive message will be provided.
         */
        public string WarningText
        {
            get { return warningText; }
            set { warningText = value;  }
        }

        public OrderState()
        {
            Status = null;
            InitMargin = null;
            MaintMargin = null;
            EquityWithLoan = null;
            Commission = 0.0;
            MinCommission = 0.0;
            MaxCommission = 0.0;
            CommissionCurrency = null;
            WarningText = null;
        }

        public OrderState(string status, string initMargin, string maintMargin,
                string equityWithLoan, double commission, double minCommission,
                double maxCommission, string commissionCurrency, string warningText)
        {

            InitMargin = initMargin;
            MaintMargin = maintMargin;
            EquityWithLoan = equityWithLoan;
            Commission = commission;
            MinCommission = minCommission;
            MaxCommission = maxCommission;
            CommissionCurrency = commissionCurrency;
            WarningText = warningText;
        }

        public override bool Equals(Object other)
        {

            if (this == other)
                return true;

            if (other == null)
                return false;

            OrderState state = (OrderState)other;

            if (commission != state.commission ||
                minCommission != state.minCommission ||
                maxCommission != state.maxCommission)
            {
                return false;
            }

            if (Util.StringCompare(status, state.status) != 0 ||
                Util.StringCompare(initMargin, state.initMargin) != 0 ||
                Util.StringCompare(maintMargin, state.maintMargin) != 0 ||
                Util.StringCompare(equityWithLoan, state.equityWithLoan) != 0 ||
                Util.StringCompare(commissionCurrency, state.commissionCurrency) != 0)
            {
                return false;
            }

            return true;
        }
    }
}
