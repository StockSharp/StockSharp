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
     * @class Order
     * @brief The order's description.
     * @sa Contract, OrderComboLeg, OrderState
     */
    public class Order
    {
        public static int CUSTOMER = 0;
        public static int FIRM = 1;
        public static char OPT_UNKNOWN = '?';
        public static char OPT_BROKER_DEALER = 'b';
        public static char OPT_CUSTOMER = 'c';
        public static char OPT_FIRM = 'f';
        public static char OPT_ISEMM = 'm';
        public static char OPT_FARMM = 'n';
        public static char OPT_SPECIALIST = 'y';
        public static int AUCTION_MATCH = 1;
        public static int AUCTION_IMPROVEMENT = 2;
        public static int AUCTION_TRANSPARENT = 3;
        public static string EMPTY_STR = "";

        // main order fields
        private int orderId;
        private int clientId;
        private int permId;
        private string action;
        private double totalQuantity;
        private string orderType;
        private double lmtPrice;
        private double auxPrice;
        // extended order fields
        // "Time in Force" - DAY, GTC, etc.
        private string tif;
        //GTC orders
        private string activeStartTime;
        private string activeStopTime;
        // one cancels all group name
        private string ocaGroup;
        // 1 = CANCEL_WITH_BLOCK, 2 = REDUCE_WITH_BLOCK, 3 = REDUCE_NON_BLOCK
        private int ocaType;
        private string orderRef;
        // if false, order will be created but not transmited
        private bool transmit;
        // Parent order Id, to associate Auto STP or TRAIL orders with the original order.
        private int parentId;
        private bool blockOrder;
        private bool sweepToFill;
        private int displaySize;
        // 0=Default, 1=Double_Bid_Ask, 2=Last, 3=Double_Last, 4=Bid_Ask, 7=Last_or_Bid_Ask, 8=Mid-point
        private int triggerMethod;
        private bool outsideRth;
        private bool hidden;
        // FORMAT: 20060505 08:00:00 {time zone}
        private string goodAfterTime;
        // FORMAT: 20060505 08:00:00 {time zone}
        private string goodTillDate;
        private bool overridePercentageConstraints;
        // Individual = 'I', Agency = 'A', AgentOtherMember = 'W', IndividualPTIA = 'J', AgencyPTIA = 'U', AgentOtherMemberPTIA = 'M', IndividualPT = 'K', AgencyPT = 'Y', AgentOtherMemberPT = 'N'
        private string rule80A;
        private bool allOrNone;
        private int minQty;
        // REL orders only
        private double percentOffset;
        // for TRAILLIMIT orders only
        private double trailStopPrice;
        private double trailingPercent;
        // Financial advisors only 
        private string faGroup;
        private string faProfile;
        private string faMethod;
        private string faPercentage;
        // Institutional orders only
        // O=Open, C=Close
        private string openClose;
        // 0=Customer, 1=Firm
        private int origin;
        // 1 if you hold the shares, 2 if they will be delivered from elsewhere.  Only for Action="SSHORT
        private int shortSaleSlot;
        // set when slot=2 only.
        private string designatedLocation;
        private int exemptCode;
        // SMART routing only
        private double discretionaryAmt;
        private bool eTradeOnly;
        private bool firmQuoteOnly;
        private double nbboPriceCap;
        private bool optOutSmartRouting;
        // BOX or VOL ORDERS ONLY
        // 1=AUCTION_MATCH, 2=AUCTION_IMPROVEMENT, 3=AUCTION_TRANSPARENT
        private int auctionStrategy;
        // BOX ORDERS ONLY
        private double startingPrice;
        private double stockRefPrice;
        private double delta;
        // pegged to stock or VOL orders
        private double stockRangeLower;
        private double stockRangeUpper;
        // VOLATILITY ORDERS ONLY
        private double volatility;
        // 1=daily, 2=annual
        private int volatilityType;
        private int continuousUpdate;
        // 1=Average, 2 = BidOrAsk
        private int referencePriceType;
        private string deltaNeutralOrderType;
        private double deltaNeutralAuxPrice;
        private int deltaNeutralConId;
        private string deltaNeutralSettlingFirm;

        private string deltaNeutralClearingAccount;
        private string deltaNeutralClearingIntent;
        private string deltaNeutralOpenClose;
        private bool deltaNeutralShortSale;
        private int deltaNeutralShortSaleSlot;
        private string deltaNeutralDesignatedLocation;
        // COMBO ORDERS ONLY
        // EFP orders only
        private double basisPoints;
        // EFP orders only
        private int basisPointsType;
        // SCALE ORDERS ONLY
        private int scaleInitLevelSize;
        private int scaleSubsLevelSize;
        private double scalePriceIncrement;
        private double scalePriceAdjustValue;
        private int scalePriceAdjustInterval;
        private double scaleProfitOffset;
        private bool scaleAutoReset;
        private int scaleInitPosition;
        private int scaleInitFillQty;
        private bool scaleRandomPercent;
        private string scaleTable;
        // HEDGE ORDERS ONLY
        // 'D' - delta, 'B' - beta, 'F' - FX, 'P' - pair
        private string hedgeType;
        // beta value for beta hedge, ratio for pair hedge
        private string hedgeParam;
        // Clearing info
        private string account; // IB account
        // True beneficiary of the order
        private string clearingAccount;
        // "" (Default), "IB", "Away", "PTA" (PostTrade)
        private string clearingIntent;
        // ALGO ORDERS ONLY
        private string algoStrategy;
        private List<TagValue> algoParams;
        // What-if
        private bool whatIf;
        //algoId
        private string algoId;
        // Not Held
        private bool notHeld;
        private string settlingFirm;
        // Smart combo routing params
        private List<TagValue> smartComboRoutingParams;
        // order combo legs
        private List<OrderComboLeg> orderComboLegs = new List<OrderComboLeg>();
        private List<TagValue> orderMiscOptions = new List<TagValue>();
        private bool solicited;

        /**
         * @brief The API client's order id.
         */
        public int OrderId
        {
            get { return orderId; }
            set { orderId = value; }
        }

        public bool Solicited
        {
            get { return solicited; }
            set { solicited = value; }
        }

        /**
         * @brief The API client id which placed the order.
         */
        public int ClientId
        {
            get { return clientId; }
            set { clientId = value; }
        }

        /**
         * @brief The Host order identifier.
         */
        public int PermId
        {
            get { return permId; }
            set { permId = value; }
        }

        /**
         * @brief Identifies the side.
         * Possible values are BUY, SELL, SSHORT
         */
        public string Action
        {
            get { return action; }
            set { action = value; }
        }

        /**
         * @brief The number of positions being bought/sold.
         */
        public double TotalQuantity
        {
            get { return totalQuantity; }
            set { totalQuantity = value; }
        }

        /**
         * @brief The order's type.
         * Available Orders are at https://www.interactivebrokers.com/en/software/api/apiguide/tables/supported_order_types.htm 
         */
        public string OrderType
        {
            get { return orderType; }
            set { orderType = value; }
        }

        /**
         * @brief The LIMIT price.
         * Used for limit, stop-limit and relative orders. In all other cases specify zero. For relative orders with no limit price, also specify zero.
         */
        public double LmtPrice
        {
            get { return lmtPrice; }
            set { lmtPrice = value; }
        }

        /**
         * @brief Generic field to contain the stop price for STP LMT orders, trailing amount, etc.
         */
        public double AuxPrice
        {
            get { return auxPrice; }
            set { auxPrice = value; }
        }

        /**
          * @brief The time in force.
         * Valid values are: \n
         *      DAY - Valid for the day only.\n
         *      GTC - Good until canceled. The order will continue to work within the system and in the marketplace until it executes or is canceled. GTC orders will be automatically be cancelled under the following conditions:
         *          \t\t If a corporate action on a security results in a stock split (forward or reverse), exchange for shares, or distribution of shares.
         *          \t\t If you do not log into your IB account for 90 days.\n
         *          \t\t At the end of the calendar quarter following the current quarter. For example, an order placed during the third quarter of 2011 will be canceled at the end of the first quarter of 2012. If the last day is a non-trading day, the cancellation will occur at the close of the final trading day of that quarter. For example, if the last day of the quarter is Sunday, the orders will be cancelled on the preceding Friday.\n
         *          \t\t Orders that are modified will be assigned a new “Auto Expire” date consistent with the end of the calendar quarter following the current quarter.\n
         *          \t\t Orders submitted to IB that remain in force for more than one day will not be reduced for dividends. To allow adjustment to your order price on ex-dividend date, consider using a Good-Til-Date/Time (GTD) or Good-after-Time/Date (GAT) order type, or a combination of the two.\n
         *      IOC - Immediate or Cancel. Any portion that is not filled as soon as it becomes available in the market is canceled.\n
         *      GTD. - Good until Date. It will remain working within the system and in the marketplace until it executes or until the close of the market on the date specified\n
         *      OPG - Use OPG to send a market-on-open (MOO) or limit-on-open (LOO) order.\n
         *      FOK - If the entire Fill-or-Kill order does not execute as soon as it becomes available, the entire order is canceled.\n
         *      DTC - Day until Canceled \n
          */
        public string Tif
        {
            get { return tif; }
            set { tif = value; }
        }


        /**
         * @brief One-Cancels-All group identifier.
         */
        public string OcaGroup
        {
            get { return ocaGroup; }
            set { ocaGroup = value; }
        }

        /**
         * @brief Tells how to handle remaining orders in an OCA group when one order or part of an order executes.
         * Valid values are:\n
         *      \t\t 1 = Cancel all remaining orders with block.\n
         *      \t\t 2 = Remaining orders are proportionately reduced in size with block.\n
         *      \t\t 3 = Remaining orders are proportionately reduced in size with no block.\n
         * If you use a value "with block" gives your order has overfill protection. This means that only one order in the group will be routed at a time to remove the possibility of an overfill.
         */
        public int OcaType
        {
            get { return ocaType; }
            set { ocaType = value; }
        }

        /**
         * @brief The order reference.
         * Intended for institutional customers only, although all customers may use it to identify the API client that sent the order when multiple API clients are running.
         */
        public string OrderRef
        {
            get { return orderRef; }
            set { orderRef = value; }
        }

        /**
         * @brief Specifies whether the order will be transmitted by TWS. If set to false, the order will be created at TWS but will not be sent.
         */
        public bool Transmit
        {
            get { return transmit; }
            set { transmit = value; }
        }

        /**
         * @brief The order ID of the parent order, used for bracket and auto trailing stop orders.
         */
        public int ParentId
        {
            get { return parentId; }
            set { parentId = value; }
        }

        /**
         * @brief If set to true, specifies that the order is an ISE Block order.
         */
        public bool BlockOrder
        {
            get { return blockOrder; }
            set { blockOrder = value; }
        }

        /**
         * @brief If set to true, specifies that the order is a Sweep-to-Fill order.
         */
        public bool SweepToFill
        {
            get { return sweepToFill; }
            set { sweepToFill = value; }
        }

        /**
         * @brief The publicly disclosed order size, used when placing Iceberg orders.
         */
        public int DisplaySize
        {
            get { return displaySize; }
            set { displaySize = value; }
        }

        /**
         * @brief Specifies how Simulated Stop, Stop-Limit and Trailing Stop orders are triggered.
         * Valid values are:\n
         *  0 - The default value. The "double bid/ask" function will be used for orders for OTC stocks and US options. All other orders will used the "last" function.\n
         *  1 - use "double bid/ask" function, where stop orders are triggered based on two consecutive bid or ask prices.\n
         *  2 - "last" function, where stop orders are triggered based on the last price.\n
         *  3 double last function.\n
         *  4 bid/ask function.\n
         *  7 last or bid/ask function.\n
         *  8 mid-point function.\n
         */
        public int TriggerMethod
        {
            get { return triggerMethod; }
            set { triggerMethod = value; }
        }

        /**
         * @brief If set to true, allows orders to also trigger or fill outside of regular trading hours.
         */
        public bool OutsideRth
        {
            get { return outsideRth; }
            set { outsideRth = value; }
        }

        /**
         * @brief If set to true, the order will not be visible when viewing the market depth. 
         * This option only applies to orders routed to the ISLAND exchange.
         */
        public bool Hidden
        {
            get { return hidden; }
            set { hidden = value; }
        }

        /**
         * @brief Specifies the date and time after which the order will be active.
         * Format: yyyymmdd hh:mm:ss {optional Timezone}
         */
        public string GoodAfterTime
        {
            get { return goodAfterTime; }
            set { goodAfterTime = value; }
        }

        /**
         * @brief The date and time until the order will be active.
         * You must enter GTD as the time in force to use this string. The trade's "Good Till Date," format "YYYYMMDD hh:mm:ss (optional time zone)"
         */
        public string GoodTillDate
        {
            get { return goodTillDate; }
            set { goodTillDate = value; }
        }

        /**
         * @brief Overrides TWS constraints.
         * Precautionary constraints are defined on the TWS Presets page, and help ensure tha tyour price and size order values are reasonable. Orders sent from the API are also validated against these safety constraints, and may be rejected if any constraint is violated. To override validation, set this parameter’s value to True.
         * 
         */
        public bool OverridePercentageConstraints
        {
            get { return overridePercentageConstraints; }
            set { overridePercentageConstraints = value; }
        }

        /**
         * @brief -
         * Individual = 'I'\n
         * Agency = 'A'\n
         * AgentOtherMember = 'W'\n
         * IndividualPTIA = 'J'\n
         * AgencyPTIA = 'U'\n
         * AgentOtherMemberPTIA = 'M'\n
         * IndividualPT = 'K'\n
         * AgencyPT = 'Y'\n
         * AgentOtherMemberPT = 'N'\n
         */
        public string Rule80A
        {
            get { return rule80A; }
            set { rule80A = value; }
        }

        /**
         * @brief Indicates whether or not all the order has to be filled on a single execution.
         */
        public bool AllOrNone
        {
            get { return allOrNone; }
            set { allOrNone = value; }
        }

        /**
         * @brief Identifies a minimum quantity order type.
         */
        public int MinQty
        {
            get { return minQty; }
            set { minQty = value; }
        }

        /**
         * @brief The percent offset amount for relative orders.
         */
        public double PercentOffset
        {
            get { return percentOffset; }
            set { percentOffset = value; }
        }

        /**
         * @brief Trail stop price for TRAILIMIT orders.
         */
        public double TrailStopPrice
        {
            get { return trailStopPrice; }
            set { trailStopPrice = value; }
        }

        /**
         * @brief Specifies the trailing amount of a trailing stop order as a percentage.
         * Observe the following guidelines when using the trailingPercent field:\n
         *    - This field is mutually exclusive with the existing trailing amount. That is, the API client can send one or the other but not both.\n
         *    - This field is read AFTER the stop price (barrier price) as follows: deltaNeutralAuxPrice stopPrice, trailingPercent, scale order attributes\n
         *    - The field will also be sent to the API in the openOrder message if the API client version is >= 56. It is sent after the stopPrice field as follows: stopPrice, trailingPct, basisPoint\n
         */
        public double TrailingPercent
        {
            get { return trailingPercent; }
            set { trailingPercent = value; }
        }

        /**
         * @brief The Financial Advisor group the trade will be allocated to.
         * Use an empty string if not applicable.
         */
        public string FaGroup
        {
            get { return faGroup; }
            set { faGroup = value; }
        }

        /**
         * @brief The Financial Advisor allocation profile the trade will be allocated to.
         * Use an empty string if not applicable.
         */
        public string FaProfile
        {
            get { return faProfile; }
            set { faProfile = value; }
        }

        /**
         * @brief The Financial Advisor allocation method the trade will be allocated to.
         * Use an empty string if not applicable.
         */
        public string FaMethod
        {
            get { return faMethod; }
            set { faMethod = value; }
        }

        /**
         * @brief The Financial Advisor percentage concerning the trade's allocation.
         * Use an empty string if not applicable.
         */
        public string FaPercentage
        {
            get { return faPercentage; }
            set { faPercentage = value; }
        }


        /**
         * @brief For institutional customers only.
         * Available for institutional clients to determine if this order is to open or close a position. Valid values are O (open), C (close).
         */
        public string OpenClose
        {
            get { return openClose; }
            set { openClose = value; }
        }


        /**
         * @brief The order's origin. 
         * Same as TWS "Origin" column. Identifies the type of customer from which the order originated. Valid values are 0 (customer), 1 (firm).
         */
        public int Origin
        {
            get { return origin; }
            set { origin = value; }
        }

        /**
         * @brief -
         * For institutions only. Valid values are: 1 (broker holds shares) or 2 (shares come from elsewhere).
         */
        public int ShortSaleSlot
        {
            get { return shortSaleSlot; }
            set { shortSaleSlot = value; }
        }

        /**
         * @brief Used only when shortSaleSlot is 2.
         * For institutions only. Indicates the location where the shares to short come from. Used only when short 
         * sale slot is set to 2 (which means that the shares to short are held elsewhere and not with IB).
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

        /**
          * @brief The amount off the limit price allowed for discretionary orders.
          */
        public double DiscretionaryAmt
        {
            get { return discretionaryAmt; }
            set { discretionaryAmt = value; }
        }

        /**
         * @brief Trade with electronic quotes.
         */
        public bool ETradeOnly
        {
            get { return eTradeOnly; }
            set { eTradeOnly = value; }
        }

        /**
         * @brief Trade with firm quotes.
         */
        public bool FirmQuoteOnly
        {
            get { return firmQuoteOnly; }
            set { firmQuoteOnly = value; }
        }

        /**
         * @brief Maximum smart order distance from the NBBO.
         */
        public double NbboPriceCap
        {
            get { return nbboPriceCap; }
            set { nbboPriceCap = value; }
        }

        /**
         * @brief Use to opt out of default SmartRouting for orders routed directly to ASX.
         * This attribute defaults to false unless explicitly set to true. When set to false, orders routed directly to ASX will NOT use SmartRouting. When set to true, orders routed directly to ASX orders WILL use SmartRouting.
         */
        public bool OptOutSmartRouting
        {
            get { return optOutSmartRouting; }
            set { optOutSmartRouting = value; }
        }

        /**
         * @brief - 
         * For BOX orders only. Values include:
         *      1 - match \n
         *      2 - improvement \n
         *      3 - transparent \n
         */
        public int AuctionStrategy
        {
            get { return auctionStrategy; }
            set { auctionStrategy = value; }
        }

        /**
         * @brief The auction's starting price.
         * For BOX orders only.
         */
        public double StartingPrice
        {
            get { return startingPrice; }
            set { startingPrice = value; }
        }

        /**
         * @brief The stock's reference price.
         * The reference price is used for VOL orders to compute the limit price sent to an exchange (whether or not Continuous Update is selected), and for price range monitoring.
         */
        public double StockRefPrice
        {
            get { return stockRefPrice; }
            set { stockRefPrice = value; }
        }

        /**
         * @brief The stock's Delta.
         * For orders on BOX only.
         */
        public double Delta
        {
            get { return delta; }
            set { delta = value; }
        }

        /**
          * @brief The lower value for the acceptable underlying stock price range.
          * For price improvement option orders on BOX and VOL orders with dynamic management.
          */
        public double StockRangeLower
        {
            get { return stockRangeLower; }
            set { stockRangeLower = value; }
        }

        /**
         * @brief The upper value for the acceptable underlying stock price range.
         * For price improvement option orders on BOX and VOL orders with dynamic management.
         */
        public double StockRangeUpper
        {
            get { return stockRangeUpper; }
            set { stockRangeUpper = value; }
        }


        /**
         * @brief The option price in volatility, as calculated by TWS' Option Analytics.
         * This value is expressed as a percent and is used to calculate the limit price sent to the exchange.
         */
        public double Volatility
        {
            get { return volatility; }
            set { volatility = value; }
        }

        /**
         * @brief
         * Values include:\n
         *      1 - Daily Volatility
         *      2 - Annual Volatility
         */
        public int VolatilityType
        {
            get { return volatilityType; }
            set { volatilityType = value; }
        }

        /**
         * @brief Specifies whether TWS will automatically update the limit price of the order as the underlying price moves.
         * VOL orders only.
         */
        public int ContinuousUpdate
        {
            get { return continuousUpdate; }
            set { continuousUpdate = value; }
        }

        /**
         * @brief Specifies how you want TWS to calculate the limit price for options, and for stock range price monitoring.
         * VOL orders only. Valid values include: \n
         *      1 - Average of NBBO \n
         *      2 - NBB or the NBO depending on the action and right. \n
         */
        public int ReferencePriceType
        {
            get { return referencePriceType; }
            set { referencePriceType = value; }
        }

        /**
         * @brief Enter an order type to instruct TWS to submit a delta neutral trade on full or partial execution of the VOL order.
         * VOL orders only. For no hedge delta order to be sent, specify NONE.
         */
        public string DeltaNeutralOrderType
        {
            get { return deltaNeutralOrderType; }
            set { deltaNeutralOrderType = value; }
        }

        /**
         * @brief Use this field to enter a value if the value in the deltaNeutralOrderType field is an order type that requires an Aux price, such as a REL order. 
         * VOL orders only.
         */
        public double DeltaNeutralAuxPrice
        {
            get { return deltaNeutralAuxPrice; }
            set { deltaNeutralAuxPrice = value; }
        }

        /**
         * @brief - 
         */
        public int DeltaNeutralConId
        {
            get { return deltaNeutralConId; }
            set { deltaNeutralConId = value; }
        }

        /**
         * @brief -
         */
        public string DeltaNeutralSettlingFirm
        {
            get { return deltaNeutralSettlingFirm; }
            set { deltaNeutralSettlingFirm = value; }
        }

        /**
         * @brief -
         */
        public string DeltaNeutralClearingAccount
        {
            get { return deltaNeutralClearingAccount; }
            set { deltaNeutralClearingAccount = value; }
        }

        /**
         * @brief -
         */
        public string DeltaNeutralClearingIntent
        {
            get { return deltaNeutralClearingIntent; }
            set { deltaNeutralClearingIntent = value; }
        }

        /**
         * @brief Specifies whether the order is an Open or a Close order and is used when the hedge involves a CFD and and the order is clearing away.
         */
        public string DeltaNeutralOpenClose
        {
            get { return deltaNeutralOpenClose; }
            set { deltaNeutralOpenClose = value; }
        }

        /**
         * @brief Used when the hedge involves a stock and indicates whether or not it is sold short.
         */
        public bool DeltaNeutralShortSale
        {
            get { return deltaNeutralShortSale; }
            set { deltaNeutralShortSale = value; }
        }

        /**
         * @brief -
         * Has a value of 1 (the clearing broker holds shares) or 2 (delivered from a third party). If you use 2, then you must specify a deltaNeutralDesignatedLocation.
         */
        public int DeltaNeutralShortSaleSlot
        {
            get { return deltaNeutralShortSaleSlot; }
            set { deltaNeutralShortSaleSlot = value; }
        }

        /**
         * @brief -
         * Used only when deltaNeutralShortSaleSlot = 2.
         */
        public string DeltaNeutralDesignatedLocation
        {
            get { return deltaNeutralDesignatedLocation; }
            set { deltaNeutralDesignatedLocation = value; }
        }

        /**
         * @brief -
         * For EFP orders only.
         */
        public double BasisPoints
        {
            get { return basisPoints; }
            set { basisPoints = value; }
        }

        /**
         * @brief -
         * For EFP orders only.
         */
        public int BasisPointsType
        {
            get { return basisPointsType; }
            set { basisPointsType = value; }
        }

        /**
         * @brief Defines the size of the first, or initial, order component.
         * For Scale orders only.
         */
        public int ScaleInitLevelSize
        {
            get { return scaleInitLevelSize; }
            set { scaleInitLevelSize = value; }
        }

        /**
         * @brief Defines the order size of the subsequent scale order components.
         * For Scale orders only. Used in conjunction with scaleInitLevelSize().
         */
        public int ScaleSubsLevelSize
        {
            get { return scaleSubsLevelSize; }
            set { scaleSubsLevelSize = value; }
        }

        /**
         * @brief Defines the price increment between scale components.
         * For Scale orders only. This value is compulsory.
         */
        public double ScalePriceIncrement
        {
            get { return scalePriceIncrement; }
            set { scalePriceIncrement = value; }
        }

        /**
         * @brief -
         * For extended Scale orders.
         */
        public double ScalePriceAdjustValue
        {
            get { return scalePriceAdjustValue; }
            set { scalePriceAdjustValue = value; }
        }

        /**
         * @brief -
         * For extended Scale orders.
         */
        public int ScalePriceAdjustInterval
        {
            get { return scalePriceAdjustInterval; }
            set { scalePriceAdjustInterval = value; }
        }

        /**
         * @brief -
         * For extended scale orders.
         */
        public double ScaleProfitOffset
        {
            get { return scaleProfitOffset; }
            set { scaleProfitOffset = value; }
        }

        /**
         * @brief -
         * For extended scale orders.
         */
        public bool ScaleAutoReset
        {
            get { return scaleAutoReset; }
            set { scaleAutoReset = value; }
        }

        /**
         * @brief -
         * For extended scale orders.
         */
        public int ScaleInitPosition
        {
            get { return scaleInitPosition; }
            set { scaleInitPosition = value; }
        }

        /**
          * @brief -
          * For extended scale orders.
          */
        public int ScaleInitFillQty
        {
            get { return scaleInitFillQty; }
            set { scaleInitFillQty = value; }
        }

        /**
         * @brief -
         * For extended scale orders.
         */
        public bool ScaleRandomPercent
        {
            get { return scaleRandomPercent; }
            set { scaleRandomPercent = value; }
        }

        /**
         * @brief For hedge orders.
         * Possible values include:\n
         *      D - delta \n
         *      B - beta \n
         *      F - FX \n
         *      P - Pair \n
         */
        public string HedgeType
        {
            get { return hedgeType; }
            set { hedgeType = value; }
        }

        /**
         * @brief -
         * Beta = x for Beta hedge orders, ratio = y for Pair hedge order
         */
        public string HedgeParam
        {
            get { return hedgeParam; }
            set { hedgeParam = value; }
        }

        /**
         * @brief The account the trade will be allocated to.
         */
        public string Account
        {
            get { return account; }
            set { account = value; }
        }

        /**
         * @brief -
         * Institutions only. Indicates the firm which will settle the trade.
         */
        public string SettlingFirm
        {
            get { return settlingFirm; }
            set { settlingFirm = value; }
        }

        /**
         * @brief Specifies the true beneficiary of the order.
         * For IBExecution customers. This value is required for FUT/FOP orders for reporting to the exchange.
         */
        public string ClearingAccount
        {
            get { return clearingAccount; }
            set { clearingAccount = value; }
        }

        /**
        * @brief For exeuction-only clients to know where do they want their shares to be cleared at.
         * Valid values are: IB, Away, and PTA (post trade allocation).
        */
        public string ClearingIntent
        {
            get { return clearingIntent; }
            set { clearingIntent = value; }
        }

        /**
         * @brief The algorithm strategy.
         * As of API verion 9.6, the following algorithms are supported:\n
         *      ArrivalPx - Arrival Price \n
         *      DarkIce - Dark Ice \n
         *      PctVol - Percentage of Volume \n
         *      Twap - TWAP (Time Weighted Average Price) \n
         *      Vwap - VWAP (Volume Weighted Average Price) \n
         * For more information about IB's API algorithms, refer to https://www.interactivebrokers.com/en/software/api/apiguide/tables/ibalgo_parameters.htm
        */
        public string AlgoStrategy
        {
            get { return algoStrategy; }
            set { algoStrategy = value; }
        }

        /**
        * @brief The list of parameters for the IB algorithm.
         * For more information about IB's API algorithms, refer to https://www.interactivebrokers.com/en/software/api/apiguide/tables/ibalgo_parameters.htm
        */
        public List<TagValue> AlgoParams
        {
            get { return algoParams; }
            set { algoParams = value; }
        }

        /**
        * @brief Allows to retrieve the commissions and margin information.
         * When placing an order with this attribute set to true, the order will not be placed as such. Instead it will used to request the commissions and margin information that would result from this order.
        */
        public bool WhatIf
        {
            get { return whatIf; }
            set { whatIf = value; }
        }

        public string AlgoId { get { return algoId; } set { algoId = value; } }

        /**
        * @brief Orders routed to IBDARK are tagged as “post only” and are held in IB's order book, where incoming SmartRouted orders from other IB customers are eligible to trade against them.
         * For IBDARK orders only.
        */
        public bool NotHeld
        {
            get { return notHeld; }
            set { notHeld = value; }
        }

        /**
         * @brief Parameters for combo routing.
         * For more information, refer to https://www.interactivebrokers.com/en/software/api/apiguide/tables/smart_combo_routing.htm   
         */
        public List<TagValue> SmartComboRoutingParams
        {
            get { return smartComboRoutingParams; }
            set { smartComboRoutingParams = value; }
        }

        /**
        * @brief The attributes for all legs within a combo order.
        */
        public List<OrderComboLeg> OrderComboLegs
        {
            get { return orderComboLegs; }
            set { orderComboLegs = value; }
        }

        public List<TagValue> OrderMiscOptions
        {
            get { return orderMiscOptions; }
            set { orderMiscOptions = value; }
        }

        /*
         * @brief for GTC orders.
         */
        public string ActiveStartTime
        {
            get { return activeStartTime; }
            set { activeStartTime = value; }
        }

        /*
        * @brief for GTC orders.
        */
        public string ActiveStopTime
        {
            get { return activeStopTime; }
            set { activeStopTime = value; }
        }

        /*
         * @brief Used for scale orders.
         */
        public string ScaleTable
        {
            get { return scaleTable; }
            set { scaleTable = value; }
        }

        public Order()
        {
            lmtPrice = Double.MaxValue;
            auxPrice = Double.MaxValue;
            activeStartTime = EMPTY_STR;
            activeStopTime = EMPTY_STR;
            outsideRth = false;
            openClose = "O";
            origin = CUSTOMER;
            transmit = true;
            designatedLocation = EMPTY_STR;
            exemptCode = -1;
            minQty = Int32.MaxValue;
            percentOffset = Double.MaxValue;
            nbboPriceCap = Double.MaxValue;
            optOutSmartRouting = false;
            startingPrice = Double.MaxValue;
            stockRefPrice = Double.MaxValue;
            delta = Double.MaxValue;
            stockRangeLower = Double.MaxValue;
            stockRangeUpper = Double.MaxValue;
            volatility = Double.MaxValue;
            volatilityType = Int32.MaxValue;
            deltaNeutralOrderType = EMPTY_STR;
            deltaNeutralAuxPrice = Double.MaxValue;
            deltaNeutralConId = 0;
            deltaNeutralSettlingFirm = EMPTY_STR;
            deltaNeutralClearingAccount = EMPTY_STR;
            deltaNeutralClearingIntent = EMPTY_STR;
            deltaNeutralOpenClose = EMPTY_STR;
            deltaNeutralShortSale = false;
            deltaNeutralShortSaleSlot = 0;
            deltaNeutralDesignatedLocation = EMPTY_STR;
            referencePriceType = Int32.MaxValue;
            trailStopPrice = Double.MaxValue;
            trailingPercent = Double.MaxValue;
            basisPoints = Double.MaxValue;
            basisPointsType = Int32.MaxValue;
            scaleInitLevelSize = Int32.MaxValue;
            scaleSubsLevelSize = Int32.MaxValue;
            scalePriceIncrement = Double.MaxValue;
            scalePriceAdjustValue = Double.MaxValue;
            scalePriceAdjustInterval = Int32.MaxValue;
            scaleProfitOffset = Double.MaxValue;
            scaleAutoReset = false;
            scaleInitPosition = Int32.MaxValue;
            scaleInitFillQty = Int32.MaxValue;
            scaleRandomPercent = false;
            scaleTable = EMPTY_STR;
            whatIf = false;
            notHeld = false;
        }

        public override bool Equals(Object p_other)
        {

            if (this == p_other)
                return true;

            if (p_other == null)
                return false;

            Order l_theOther = (Order)p_other;

            if (PermId == l_theOther.PermId)
            {
                return true;
            }

            if (OrderId != l_theOther.OrderId ||
                ClientId != l_theOther.ClientId ||
                TotalQuantity != l_theOther.TotalQuantity ||
                LmtPrice != l_theOther.LmtPrice ||
                AuxPrice != l_theOther.AuxPrice ||
                OcaType != l_theOther.OcaType ||
                Transmit != l_theOther.Transmit ||
                ParentId != l_theOther.ParentId ||
                BlockOrder != l_theOther.BlockOrder ||
                SweepToFill != l_theOther.SweepToFill ||
                DisplaySize != l_theOther.DisplaySize ||
                TriggerMethod != l_theOther.TriggerMethod ||
                OutsideRth != l_theOther.OutsideRth ||
                Hidden != l_theOther.Hidden ||
                OverridePercentageConstraints != l_theOther.OverridePercentageConstraints ||
                AllOrNone != l_theOther.AllOrNone ||
                MinQty != l_theOther.MinQty ||
                PercentOffset != l_theOther.PercentOffset ||
                TrailStopPrice != l_theOther.TrailStopPrice ||
                TrailingPercent != l_theOther.TrailingPercent ||
                Origin != l_theOther.Origin ||
                ShortSaleSlot != l_theOther.ShortSaleSlot ||
                DiscretionaryAmt != l_theOther.DiscretionaryAmt ||
                ETradeOnly != l_theOther.ETradeOnly ||
                FirmQuoteOnly != l_theOther.FirmQuoteOnly ||
                NbboPriceCap != l_theOther.NbboPriceCap ||
                OptOutSmartRouting != l_theOther.OptOutSmartRouting ||
                AuctionStrategy != l_theOther.AuctionStrategy ||
                StartingPrice != l_theOther.StartingPrice ||
                StockRefPrice != l_theOther.StockRefPrice ||
                Delta != l_theOther.Delta ||
                StockRangeLower != l_theOther.StockRangeLower ||
                StockRangeUpper != l_theOther.StockRangeUpper ||
                Volatility != l_theOther.Volatility ||
                VolatilityType != l_theOther.VolatilityType ||
                ContinuousUpdate != l_theOther.ContinuousUpdate ||
                ReferencePriceType != l_theOther.ReferencePriceType ||
                DeltaNeutralAuxPrice != l_theOther.DeltaNeutralAuxPrice ||
                DeltaNeutralConId != l_theOther.DeltaNeutralConId ||
                DeltaNeutralShortSale != l_theOther.DeltaNeutralShortSale ||
                DeltaNeutralShortSaleSlot != l_theOther.DeltaNeutralShortSaleSlot ||
                BasisPoints != l_theOther.BasisPoints ||
                BasisPointsType != l_theOther.BasisPointsType ||
                ScaleInitLevelSize != l_theOther.ScaleInitLevelSize ||
                ScaleSubsLevelSize != l_theOther.ScaleSubsLevelSize ||
                ScalePriceIncrement != l_theOther.ScalePriceIncrement ||
                ScalePriceAdjustValue != l_theOther.ScalePriceAdjustValue ||
                ScalePriceAdjustInterval != l_theOther.ScalePriceAdjustInterval ||
                ScaleProfitOffset != l_theOther.ScaleProfitOffset ||
                ScaleAutoReset != l_theOther.ScaleAutoReset ||
                ScaleInitPosition != l_theOther.ScaleInitPosition ||
                ScaleInitFillQty != l_theOther.ScaleInitFillQty ||
                ScaleRandomPercent != l_theOther.ScaleRandomPercent ||
                WhatIf != l_theOther.WhatIf ||
                NotHeld != l_theOther.NotHeld ||
                ExemptCode != l_theOther.ExemptCode ||
                RandomizePrice != l_theOther.RandomizePrice ||
                RandomizeSize != l_theOther.RandomizeSize ||
                Solicited != l_theOther.Solicited)
            {
                return false;
            }

            if (Util.StringCompare(Action, l_theOther.Action) != 0 ||
                Util.StringCompare(OrderType, l_theOther.OrderType) != 0 ||
                Util.StringCompare(Tif, l_theOther.Tif) != 0 ||
                Util.StringCompare(ActiveStartTime, l_theOther.ActiveStartTime) != 0 ||
                Util.StringCompare(ActiveStopTime, l_theOther.ActiveStopTime) != 0 ||
                Util.StringCompare(OcaGroup, l_theOther.OcaGroup) != 0 ||
                Util.StringCompare(OrderRef, l_theOther.OrderRef) != 0 ||
                Util.StringCompare(GoodAfterTime, l_theOther.GoodAfterTime) != 0 ||
                Util.StringCompare(GoodTillDate, l_theOther.GoodTillDate) != 0 ||
                Util.StringCompare(Rule80A, l_theOther.Rule80A) != 0 ||
                Util.StringCompare(FaGroup, l_theOther.FaGroup) != 0 ||
                Util.StringCompare(FaProfile, l_theOther.FaProfile) != 0 ||
                Util.StringCompare(FaMethod, l_theOther.FaMethod) != 0 ||
                Util.StringCompare(FaPercentage, l_theOther.FaPercentage) != 0 ||
                Util.StringCompare(OpenClose, l_theOther.OpenClose) != 0 ||
                Util.StringCompare(DesignatedLocation, l_theOther.DesignatedLocation) != 0 ||
                Util.StringCompare(DeltaNeutralOrderType, l_theOther.DeltaNeutralOrderType) != 0 ||
                Util.StringCompare(DeltaNeutralSettlingFirm, l_theOther.DeltaNeutralSettlingFirm) != 0 ||
                Util.StringCompare(DeltaNeutralClearingAccount, l_theOther.DeltaNeutralClearingAccount) != 0 ||
                Util.StringCompare(DeltaNeutralClearingIntent, l_theOther.DeltaNeutralClearingIntent) != 0 ||
                Util.StringCompare(DeltaNeutralOpenClose, l_theOther.DeltaNeutralOpenClose) != 0 ||
                Util.StringCompare(DeltaNeutralDesignatedLocation, l_theOther.DeltaNeutralDesignatedLocation) != 0 ||
                Util.StringCompare(HedgeType, l_theOther.HedgeType) != 0 ||
                Util.StringCompare(HedgeParam, l_theOther.HedgeParam) != 0 ||
                Util.StringCompare(Account, l_theOther.Account) != 0 ||
                Util.StringCompare(SettlingFirm, l_theOther.SettlingFirm) != 0 ||
                Util.StringCompare(ClearingAccount, l_theOther.ClearingAccount) != 0 ||
                Util.StringCompare(ClearingIntent, l_theOther.ClearingIntent) != 0 ||
                Util.StringCompare(AlgoStrategy, l_theOther.AlgoStrategy) != 0 ||
                Util.StringCompare(AlgoId, l_theOther.AlgoId) != 0 ||
                Util.StringCompare(ScaleTable, l_theOther.ScaleTable) != 0)
            {
                return false;
            }

            if (!Util.VectorEqualsUnordered(AlgoParams, l_theOther.AlgoParams))
            {
                return false;
            }

            if (!Util.VectorEqualsUnordered(SmartComboRoutingParams, l_theOther.SmartComboRoutingParams))
            {
                return false;
            }

            // compare order combo legs
            if (!Util.VectorEqualsUnordered(OrderComboLegs, l_theOther.OrderComboLegs))
            {
                return false;
            }

            return true;
        }

        public bool RandomizeSize { get; set; }
        public bool RandomizePrice { get; set; }
    }
}
