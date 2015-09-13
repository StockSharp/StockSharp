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
     * @class ContractDetails
     * @brief extended contract details.
     * @sa Contract
     */
    public class ContractDetails
    {
        private Contract summary;
        private string marketName;
        private double minTick;
        private int priceMagnifier;
        private string orderTypes;
        private string validExchanges;
        private int underConId;
        private string longName;
        private string contractMonth;
        private string industry;
        private string category;
        private string subcategory;
        private string timeZoneId;
        private string tradingHours;
        private string liquidHours;
        private string evRule;
        private double evMultiplier;       
        private List<TagValue> secIdList;
       
        // BOND values
        private string cusip;
        private string ratings;
        private string descAppend;
        private string bondType;
        private string couponType;
        private bool callable = false;
        private bool putable = false;
        private double coupon = 0;
        private bool convertible = false;
        private string maturity;
        private string issueDate;
        private string nextOptionDate;
        private string nextOptionType;
        private bool nextOptionPartial = false;
        private string notes;

        /**
         * @brief A Contract object summarising this product.
         */
        public Contract Summary
        {
            get { return summary; }
            set { summary = value; }
        }

        /**
        * @brief The market name for this product.
        */
        public string MarketName
        {
            get { return marketName; }
            set { marketName = value; }
        }

        /**
        * @brief The minimum allowed price variation.
         * Note that many securities vary their minimum tick size according to their price. This value will only show the smallest of the different minimum tick sizes regardless of the product's price.
        */
        public double MinTick
        {
            get { return minTick; }
            set { minTick = value; }
        }

        /**
        * @brief Allows execution and strike prices to be reported consistently with market data, historical data and the order price, i.e. Z on LIFFE is reported in Index points and not GBP.
        */
        public int PriceMagnifier
        {
            get { return priceMagnifier; }
            set { priceMagnifier = value; }
        }

        /**
        * @brief Supported order types for this product.
        */
        public string OrderTypes
        {
            get { return orderTypes; }
            set { orderTypes = value; }
        }

        /**
        * @brief Exchanges on which this product is traded.
        */
        public string ValidExchanges
        {
            get { return validExchanges; }
            set { validExchanges = value; }
        }

        /**
        * @brief Underlying's contract Id
        */
        public int UnderConId
        {
            get { return underConId; }
            set { underConId = value; }
        }

        /**
        * @brief Descriptive name of the product.
        */
        public string LongName
        {
            get { return longName; }
            set { longName = value; }
        }

        /**
        * @brief Typically the contract month of the underlying for a Future contract.
        */
        public string ContractMonth
        {
            get { return contractMonth; }
            set { contractMonth = value; }
        }

        /**
        * @brief The industry classification of the underlying/product. For example, Financial.
        */
        public string Industry
        {
            get { return industry; }
            set { industry = value; }
        }

        /**
        * @brief The industry category of the underlying. For example, InvestmentSvc.
        */
        public string Category
        {
            get { return category; }
            set { category = value; }
        }

        /**
        * @brief The industry subcategory of the underlying. For example, Brokerage.
        */
        public string Subcategory
        {
            get { return subcategory; }
            set { subcategory = value; }
        }

        /**
        * @brief The ID of the time zone for the trading hours of the product. For example, EST.
        */
        public string TimeZoneId
        {
            get { return timeZoneId; }
            set { timeZoneId = value; }
        }

        /**
        * @brief The trading hours of the product.
         * This value will contain the trading hours of the current day as well as the next's. For example, 20090507:0700-1830,1830-2330;20090508:CLOSED.
        */
        public string TradingHours
        {
            get { return tradingHours; }
            set { tradingHours = value; }
        }

        /**
        * @brief The liquid hours of the product.
         * This value will contain the liquid hours of the current day as well as the next's. For example, 20090507:0700-1830,1830-2330;20090508:CLOSED.
        */
        public string LiquidHours
        {
            get { return liquidHours; }
            set { liquidHours = value; }
        }

        /**
        * @brief Contains the Economic Value Rule name and the respective optional argument.
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

        /**
        * @brief A list of contract identifiers that the customer is allowed to view.
         * CUSIP/ISIN/etc.
        */
        public List<TagValue> SecIdList
        {
            get { return secIdList; }
            set { secIdList = value; }
        }

        /**
        * @brief The nine-character bond CUSIP or the 12-character SEDOL.
         * For Bonds only.
        */
        public string Cusip
        {
            get { return cusip; }
            set { cusip = value; }
        }

        /**
        * @brief Identifies the credit rating of the issuer.
         * For Bonds only. A higher credit rating generally indicates a less risky investment. Bond ratings are from Moody's and S&P respectively.
        */
        public string Ratings
        {
            get { return ratings; }
            set { ratings = value; }
        }

        /**
        * @brief A description string containing further descriptive information about the bond.
         * For Bonds only.
        */
        public string DescAppend
        {
            get { return descAppend; }
            set { descAppend = value; }
        }

        /**
        * @brief The type of bond, such as "CORP."
        */
        public string BondType
        {
            get { return bondType; }
            set { bondType = value; }
        }

        /**
        * @brief The type of bond coupon.
         * For Bonds only.
        */
        public string CouponType
        {
            get { return couponType; }
            set { couponType = value; }
        }

        /**
        * @brief If true, the bond can be called by the issuer under certain conditions.
         * For Bonds only.
        */
        public bool Callable
        {
            get { return callable; }
            set { callable = value; }
        }

        /**
        * @brief Values are True or False. If true, the bond can be sold back to the issuer under certain conditions.
         * For Bonds only.
        */
        public bool Putable
        {
            get { return putable; }
            set { putable = value; }
        }

        /**
        * @brief The interest rate used to calculate the amount you will receive in interest payments over the course of the year.
         * For Bonds only.
        */
        public double Coupon
        {
            get { return coupon; }
            set { coupon = value; }
        }

        /**
        * @brief Values are True or False. If true, the bond can be converted to stock under certain conditions.
         * For Bonds only.
        */
        public bool Convertible
        {
            get { return convertible; }
            set { convertible = value; }
        }

        /**
        * @brief he date on which the issuer must repay the face value of the bond.
         * For Bonds only.
        */
        public string Maturity
        {
            get { return maturity; }
            set { maturity = value; }
        }

        /** 
        * @brief The date the bond was issued. 
         * For Bonds only.
        */
        public string IssueDate
        {
            get { return issueDate; }
            set { issueDate = value; }
        }

        /**
        * @brief Only if bond has embedded options. 
         * Refers to callable bonds and puttable bonds. Available in TWS description window for bonds.
        */
        public string NextOptionDate
        {
            get { return nextOptionDate; }
            set { nextOptionDate = value; }
        }

        /**
        * @brief Type of embedded option.
        * Only if bond has embedded options.
        */
        public string NextOptionType
        {
            get { return nextOptionType; }
            set { nextOptionType = value; }
        }

        /**
       * @brief Only if bond has embedded options.
        * For Bonds only.
       */
        public bool NextOptionPartial
        {
            get { return nextOptionPartial; }
            set { nextOptionPartial = value; }
        }

        /**
        * @brief If populated for the bond in IB's database.
         * For Bonds only.
        */
        public string Notes
        {
            get { return notes; }
            set { notes = value; }
        }

        public ContractDetails()
        {
            summary = new Contract();
            minTick = 0;
            underConId = 0;
            evMultiplier = 0;
        }

        public ContractDetails(Contract summary, String marketName,
                double minTick, String orderTypes, String validExchanges, int underConId, String longName,
                String contractMonth, String industry, String category, String subcategory,
                String timeZoneId, String tradingHours, String liquidHours,
                String evRule, double evMultiplier)
        {
            Summary = summary;
            MarketName = marketName;
            MinTick = minTick;
            OrderTypes = orderTypes;
            ValidExchanges = validExchanges;
            UnderConId = underConId;
            LongName = longName;
            ContractMonth = contractMonth;
            Industry = industry;
            Category = category;
            Subcategory = subcategory;
            TimeZoneId = timeZoneId;
            TradingHours = tradingHours;
            LiquidHours = liquidHours;
            EvRule = evRule;
            EvMultiplier = evMultiplier;
        }
    }
}
