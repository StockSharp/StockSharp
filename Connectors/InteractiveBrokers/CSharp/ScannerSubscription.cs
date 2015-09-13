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
     * @class ScannerSubscription
     * @brief Defines a market scanner request
     */
    public class ScannerSubscription
    {
        /**
         * @var int numberOfRows
         * @brief The number of rows to be returned for the query
         */
        private int numberOfRows = -1;

        /**
         * @var string instrument
         * @brief The instrument's type for the scan. I.e. STK, FUT.HK, etc.
         */
        private string instrument;

        /**
         * @var string locationCode
         * @brief The request's location (STK.US, STK.US.MAJOR, etc). 
         */
        private string locationCode;

        /**
         * @var string scanCode
         * @brief Same as TWS Market Scanner's "parameters" field, for example: TOP_PERC_GAIN
         */
        private string scanCode;

        /**
         * @var double abovePrice
         * @brief Filters out Contracts which price is below this value
         */
        private double abovePrice = Double.MaxValue;

        /**
         * @var double belowPrice
         * @brief Filters out contracts which price is above this value.
         */
        private double belowPrice = Double.MaxValue;

        /**
         * @var int aboveVolume
         * @brief Filters out Contracts which volume is above this value.
         */
        private int aboveVolume = Int32.MaxValue;

        /**
         * @var int averageOptionVolumeAbove
         * @brief Filters out Contracts which option volume is above this value.
         */
        private int averageOptionVolumeAbove = Int32.MaxValue;

        /**
         * @var double marketCapAbove
         * @brief Filters out Contracts which market cap is above this value.
         */
        private double marketCapAbove = Double.MaxValue;

        /**
         * @var double marketCapBelow
         * @brief Filters out Contracts which market cap is below this value.
         */
        private double marketCapBelow = Double.MaxValue;

        /**
         * @var string moodyRatingAbove
         * @brief Filters out Contracts which Moody's rating is below this value.
         */
        private string moodyRatingAbove;

        /**
         * @var string moodyRatingBelow
         * @brief Filters out Contracts which Moody's rating is above this value.
         */
        private string moodyRatingBelow;

        /**
         * @var string spRatingAbove
         * @brief Filters out Contracts with a S&P rating below this value.
         */
        private string spRatingAbove;

        /**
         * @var string spRatingBelow
         * @brief Filters out Contracts with a S&P rating above this value.
         */
        private string spRatingBelow;

        /**
         * @var string maturityDateAbove
         * @brief Filter out Contracts with a maturity date earlier than this value.
         */
        private string maturityDateAbove;

        /**
         * @var string maturityDateBelow
         * @brief Filter out Contracts with a maturity date older than this value.
         */
        private string maturityDateBelow;

        /**
         * @var double couponRateAbove
         * @brief Filter out Contracts with a coupon rate lower than this value.
         */
        private double couponRateAbove = Double.MaxValue;

        /**
         * @var double couponRateBelow
         * @brief Filter out Contracts with a coupon rate higher than this value.
         */
        private double couponRateBelow = Double.MaxValue;

        /**
         * @var string excludeConvertible
         * @brief Filters out Convertible bonds
         */
        private string excludeConvertible;

        /**
         * @var string scannerSettingPairs
         * @brief For example, a pairing "Annual, true" used on the "top Option Implied Vol % Gainers" scan would return annualized volatilities.
         */
        private string scannerSettingPairs;

        /**
         * @var string stockTypeFilter
         * @brief -
         *      CORP = Corporation
         *      ADR = American Depositary Receipt
         *      ETF = Exchange Traded Fund
         *      REIT = Real Estate Investment Trust
         *      CEF = Closed End Fund
         */
        private string stockTypeFilter;

        public int NumberOfRows
        {
            get { return numberOfRows; }
            set { numberOfRows = value; }
        }
       
        public string Instrument
        {
            get { return instrument; }
            set { instrument = value; }
        }

        public string LocationCode
        {
            get { return locationCode; }
            set { locationCode = value; }
        }

        public string ScanCode
        {
            get { return scanCode; }
            set { scanCode = value; }
        }

        public double AbovePrice
        {
            get { return abovePrice; }
            set { abovePrice = value; }
        }

        public double BelowPrice
        {
            get { return belowPrice; }
            set { belowPrice = value; }
        }

        public int AboveVolume
        {
            get { return aboveVolume; }
            set { aboveVolume = value; }
        }

        public int AverageOptionVolumeAbove
        {
            get { return averageOptionVolumeAbove; }
            set { averageOptionVolumeAbove = value; }
        }

        public double MarketCapAbove
        {
            get { return marketCapAbove; }
            set { marketCapAbove = value; }
        }

        public double MarketCapBelow
        {
            get { return marketCapBelow; }
            set { marketCapBelow = value; }
        }

        public string MoodyRatingAbove
        {
            get { return moodyRatingAbove; }
            set { moodyRatingAbove = value; }
        }

        public string MoodyRatingBelow
        {
            get { return moodyRatingBelow; }
            set { moodyRatingBelow = value; }
        }

        public string SpRatingAbove
        {
            get { return spRatingAbove; }
            set { spRatingAbove = value; }
        }

        public string SpRatingBelow
        {
            get { return spRatingBelow; }
            set { spRatingBelow = value; }
        }

        public string MaturityDateAbove
        {
            get { return maturityDateAbove; }
            set { maturityDateAbove = value; }
        }

        public string MaturityDateBelow
        {
            get { return maturityDateBelow; }
            set { maturityDateBelow = value; }
        }

        public double CouponRateAbove
        {
            get { return couponRateAbove; }
            set { couponRateAbove = value; }
        }

        public double CouponRateBelow
        {
            get { return couponRateBelow; }
            set { couponRateBelow = value; }
        }

        public string ExcludeConvertible
        {
            get { return excludeConvertible; }
            set { excludeConvertible = value; }
        }

        public string ScannerSettingPairs
        {
            get { return scannerSettingPairs; }
            set { scannerSettingPairs = value; }
        }

        public string StockTypeFilter
        {
            get { return stockTypeFilter; }
            set { stockTypeFilter = value; }
        }
    }
}
