/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace IBApi
{
    /**
     * @class EClientSocket
     * @brief TWS/Gateway client class
     * This client class contains all the available methods to communicate with IB. Up to eight clients can be connected to a single instance of the TWS/Gateway simultaneously. From herein, the TWS/Gateway will be referred to as the Host.
     */
    public abstract class EClient
    {
        protected int serverVersion;

        protected ETransport socketTransport;

        protected EWrapper wrapper;

        protected bool isConnected;
        protected int clientId;
        protected bool extraAuth;
        protected bool useV100Plus = true;

        internal bool UseV100Plus { get { return useV100Plus; } }

        private string connectOptions = "";
        protected bool allowRedirect = false;

        /**
         * @brief Constructor
         * @param wrapper EWrapper's implementating class instance. Every message being delivered by IB to the API client will be forwarded to the EWrapper's implementating class.
         * @sa EWrapper
         */
        public EClient(EWrapper wrapper)
        {
            this.wrapper = wrapper;
            this.clientId = -1;
            this.extraAuth = false;
            this.isConnected = false;
            this.optionalCapabilities = "";
            this.AsyncEConnect = false;
        }

        public void SetConnectOptions(string connectOptions)
        {
            if (IsConnected())
            {
                wrapper.error(this.clientId, EClientErrors.AlreadyConnected.Code, EClientErrors.AlreadyConnected.Message);

                return;
            }

            this.connectOptions = connectOptions;
        }

        public void DisableUseV100Plus()
        {
            this.useV100Plus = false;
            this.connectOptions = "";
        }

        public EWrapper Wrapper
        {
            get { return wrapper; }
        }

        public bool AllowRedirect { get { return allowRedirect; } set { allowRedirect = value; } }

        /**
         * @brief returns the Host's version. Some of the API functionality might not be available in older Hosts and therefore it is essential to keep the TWS/Gateway as up to date as possible.
         */
        public int ServerVersion
        {
            get { return serverVersion; }
        }

        /**
         * @brief Notifies whether or not the API client is connected to the Host.
         * @returns true if connection has been established, false if it has not.
         */
        public bool IsConnected()
        {
            return isConnected;
        }

        public string ServerTime { get; protected set; }

        /**
         * @brief Establishes a connection to the designated Host.
         * After establishing a connection succesfully, the Host will provide the next valid order id, server's current time, managed accounts and open orders among others depending on the Host version.
         * @param host the Host's IP address. Leave blank for localhost.
         * @param port the Host's port. 7496 by default for the TWS, 4001 by default on the Gateway.
         * @param clientId Every API client program requires a unique id which can be any integer. Note that up to eight clients can be connected simultaneously to a single Host.
         * @sa EWrapper, EWrapper::nextValidId, EWrapper::currentTime
         */

        private static readonly string encodedVersion = Constants.MinVersion.ToString() + (Constants.MaxVersion != Constants.MinVersion ? ".." + Constants.MaxVersion : string.Empty);
        protected Stream tcpStream;

        protected abstract uint prepareBuffer(BinaryWriter paramsList);

        protected void sendConnectRequest()
        {
            try
            {
                if (useV100Plus)
                {
                    var paramsList = new BinaryWriter(new MemoryStream());

                    paramsList.AddParameter("API");

                    var lengthPos = prepareBuffer(paramsList);

                    paramsList.Write(Encoding.ASCII.GetBytes("v" + encodedVersion + (IsEmpty(connectOptions) ? string.Empty : " " + connectOptions)));

                    CloseAndSend(paramsList, lengthPos);
                }
                else
                {
                    List<byte> buf = new List<byte>();

                    buf.AddRange(UTF8Encoding.UTF8.GetBytes(Constants.ClientVersion.ToString()));
                    buf.Add(Constants.EOL);
                    socketTransport.Send(new EMessage(buf.ToArray()));
                }
            }
            catch (IOException)
            {
                wrapper.error(clientId, EClientErrors.CONNECT_FAIL.Code, EClientErrors.CONNECT_FAIL.Message);
                throw;
            }
        }

        public void startApi()
        {
            if (!CheckConnection())
                return;

            const int VERSION = 2;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.StartApi);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(clientId);

            if (serverVersion >= MinServerVer.OPTIONAL_CAPABILITIES)
                paramsList.AddParameter(optionalCapabilities);

            CloseAndSend(paramsList, lengthPos);
        }

        public string optionalCapabilities { get; set; }

        /**
         * @brief Terminates the connection and notifies the EWrapper implementing class.
         * @sa EWrapper::connectionClosed, eDisconnect
         */
        public void Close()
        {
            eDisconnect();
            wrapper.connectionClosed();
        }

        /**
         * @brief Closes the socket connection and terminates its thread.
         */
        public void eDisconnect()
        {
            if (socketTransport == null)
            {
                return;
            }

            isConnected = false;
            serverVersion = 0;
            this.clientId = -1;
            this.extraAuth = false;
            this.optionalCapabilities = "";

            if (tcpStream != null)
                tcpStream.Close();
 
            wrapper.connectionClosed();
        }


        /**
        * @brief Cancels a historical data request.
        * @param reqId the request's identifier.
        * @sa reqHistoricalData
        */
        public void cancelHistoricalData(int reqId)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(24, " It does not support historical data cancelations."))
                return;
            const int VERSION = 1;
            //No server version validation takes place here since minimum is already higher
            SendCancelRequest(OutgoingMessages.CancelOptionPrice, VERSION, reqId, EClientErrors.FAIL_SEND_CANHISTDATA);
        }

        /**
         * @brief Calculate the volatility for an option.
         * Request the calculation of the implied volatility based on hypothetical option and its underlying prices. The calculation will be return in EWrapper's tickOptionComputation callback.
         * @param reqId unique identifier of the request.
         * @param contract the option's contract for which the volatility wants to be calculated.
         * @param optionPrice hypothetical option price.
         * @param underPrice hypothetical option's underlying price.
         * @sa EWrapper::tickOptionComputation, cancelCalculateImpliedVolatility, Contract
         */
        public void calculateImpliedVolatility(int reqId, Contract contract, double optionPrice, double underPrice, List<TagValue> impliedVolatilityOptions)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.REQ_CALC_IMPLIED_VOLAT, " It does not support calculate implied volatility."))
                return;
            if (!Util.StringIsEmpty(contract.TradingClass) && !CheckServerVersion(MinServerVer.TRADING_CLASS, ""))
                return;
            const int version = 3;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.ReqCalcImpliedVolat);
            paramsList.AddParameter(version);
            paramsList.AddParameter(reqId);
            paramsList.AddParameter(contract.ConId);
            paramsList.AddParameter(contract.Symbol);
            paramsList.AddParameter(contract.SecType);
            paramsList.AddParameter(contract.LastTradeDateOrContractMonth);
            paramsList.AddParameter(contract.Strike);
            paramsList.AddParameter(contract.Right);
            paramsList.AddParameter(contract.Multiplier);
            paramsList.AddParameter(contract.Exchange);
            paramsList.AddParameter(contract.PrimaryExch);
            paramsList.AddParameter(contract.Currency);
            paramsList.AddParameter(contract.LocalSymbol);
            if (serverVersion >= MinServerVer.TRADING_CLASS)
                paramsList.AddParameter(contract.TradingClass);
            paramsList.AddParameter(optionPrice);
            paramsList.AddParameter(underPrice);

            if (serverVersion >= MinServerVer.LINKING)
            {
                int tagValuesCount = impliedVolatilityOptions == null ? 0 : impliedVolatilityOptions.Count;
                paramsList.AddParameter(tagValuesCount);
                paramsList.AddParameter(TagValueListToString(impliedVolatilityOptions));
            }

            CloseAndSend(reqId, paramsList, lengthPos, EClientErrors.FAIL_SEND_REQCALCIMPLIEDVOLAT);
        }

        /**
         * @brief Calculates an option's price.
         * Calculates an option's price based on the provided volatility and its underlying's price. The calculation will be return in EWrapper's tickOptionComputation callback.
         * @param reqId request's unique identifier.
         * @param contract the option's contract for which the price wants to be calculated.
         * @param volatility hypothetical volatility.
         * @param underPrice hypothetical underlying's price.
         * @sa EWrapper::tickOptionComputation, cancelCalculateOptionPrice, Contract
         */
        public void calculateOptionPrice(int reqId, Contract contract, double volatility, double underPrice, List<TagValue> optionPriceOptions)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.REQ_CALC_OPTION_PRICE,
                " It does not support calculation price requests."))
                return;
            if (!Util.StringIsEmpty(contract.TradingClass) &&
                !CheckServerVersion(MinServerVer.REQ_CALC_OPTION_PRICE, " It does not support tradingClass parameter in calculateOptionPrice."))
                return;

            const int version = 3;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.ReqCalcOptionPrice);
            paramsList.AddParameter(version);
            paramsList.AddParameter(reqId);
            paramsList.AddParameter(contract.ConId);
            paramsList.AddParameter(contract.Symbol);
            paramsList.AddParameter(contract.SecType);
            paramsList.AddParameter(contract.LastTradeDateOrContractMonth);
            paramsList.AddParameter(contract.Strike);
            paramsList.AddParameter(contract.Right);
            paramsList.AddParameter(contract.Multiplier);
            paramsList.AddParameter(contract.Exchange);
            paramsList.AddParameter(contract.PrimaryExch);
            paramsList.AddParameter(contract.Currency);
            paramsList.AddParameter(contract.LocalSymbol);
            if (serverVersion >= MinServerVer.TRADING_CLASS)
                paramsList.AddParameter(contract.TradingClass);
            paramsList.AddParameter(volatility);
            paramsList.AddParameter(underPrice);

            if (serverVersion >= MinServerVer.LINKING)
            {
                int tagValuesCount = optionPriceOptions == null ? 0 : optionPriceOptions.Count;
                paramsList.AddParameter(tagValuesCount);
                paramsList.AddParameter(TagValueListToString(optionPriceOptions));
            }

            CloseAndSend(reqId, paramsList, lengthPos, EClientErrors.FAIL_SEND_REQCALCOPTIONPRICE);
        }

        /**
         * @brief Cancels the account's summary request.
         * After requesting an account's summary, invoke this function to cancel it.
         * @param reqId the identifier of the previously performed account request
         * @sa reqAccountSummary
         */
        public void cancelAccountSummary(int reqId)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.ACCT_SUMMARY,
                " It does not support account summary cancellation."))
                return;
            SendCancelRequest(OutgoingMessages.CancelAccountSummary, 1, reqId, EClientErrors.FAIL_SEND_CANACCOUNTDATA);
        }

        /**
         * @brief Cancels an option's implied volatility calculation request
         * @param reqId the identifier of the implied volatility's calculation request.
         * @sa calculateImpliedVolatility
         */
        public void cancelCalculateImpliedVolatility(int reqId)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.CANCEL_CALC_IMPLIED_VOLAT,
                " It does not support calculate implied volatility cancellation."))
                return;
            SendCancelRequest(OutgoingMessages.CancelImpliedVolatility, 1, reqId, EClientErrors.FAIL_SEND_CANCALCIMPLIEDVOLAT);
        }

        /**
         * @brief Cancels an option's price calculation request
         * @param reqId the identifier of the option's price's calculation request.
         * @sa calculateOptionPrice
         */
        public void cancelCalculateOptionPrice(int reqId)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.CANCEL_CALC_OPTION_PRICE,
                " It does not support calculate option price cancellation."))
                return;
            SendCancelRequest(OutgoingMessages.CancelOptionPrice, 1, reqId, EClientErrors.FAIL_SEND_CANCALCOPTIONPRICE);
        }

        /**
         * @brief Cancels Fundamental data request
         * @param reqId the request's idenfier.
         * @sa reqFundamentalData
         */
        public void cancelFundamentalData(int reqId)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.FUNDAMENTAL_DATA,
                " It does not support fundamental data requests."))
                return;
            SendCancelRequest(OutgoingMessages.CancelFundamentalData, 1, reqId, EClientErrors.FAIL_SEND_CANFUNDDATA);
        }



        /**
         * @brief Cancels a RT Market Data request
         * @param tickerId request's identifier
         * @sa reqMktData
         */
        public void cancelMktData(int tickerId)
        {
            if (!CheckConnection())
                return;

            SendCancelRequest(OutgoingMessages.CancelMarketData, 1, tickerId, EClientErrors.FAIL_SEND_CANMKT);
        }

        /**
         * @brief Cancel's market depth's request.
         * @param tickerId request's identifier.
         * @sa reqMarketDepth
         */
        public void cancelMktDepth(int tickerId)
        {
            if (!CheckConnection())
                return;

            SendCancelRequest(OutgoingMessages.CancelMarketDepth, 1, tickerId,
                EClientErrors.FAIL_SEND_CANMKTDEPTH);
        }

        /**
         * @brief Cancels IB's news bulletin subscription
         * @sa reqNewsBulletins
         */
        public void cancelNewsBulletin()
        {
            if (!CheckConnection())
                return;
            SendCancelRequest(OutgoingMessages.CancelNewsBulletin, 1,
                EClientErrors.FAIL_SEND_CORDER);
        }

        /**
         * @brief Cancels an active order
         * @param orderId the order's client id
         * @sa placeOrder, reqGlobalCancel
         */
        public void cancelOrder(int orderId)
        {
            if (!CheckConnection())
                return;
            SendCancelRequest(OutgoingMessages.CancelOrder, 1, orderId,
                EClientErrors.FAIL_SEND_CORDER);
        }

        /**
         * @brief Cancels all account's positions request
         * @sa reqPositions
         */
        public void cancelPositions()
        {
            if (!CheckConnection())
                return;

            if (!CheckServerVersion(MinServerVer.ACCT_SUMMARY,
                " It does not support position cancellation."))
                return;

            SendCancelRequest(OutgoingMessages.CancelPositions, 1, EClientErrors.FAIL_SEND_CANPOSITIONS);
        }

        /**
         * @brief Cancels Real Time Bars' subscription
         * @param tickerId the request's identifier.
         * @sa reqRealTimeBars
         */
        public void cancelRealTimeBars(int tickerId)
        {
            if (!CheckConnection())
                return;

            SendCancelRequest(OutgoingMessages.CancelRealTimeBars, 1, tickerId, EClientErrors.FAIL_SEND_CANRTBARS);
        }

        /**
         * @brief Cancels Scanner Subscription
         * @param tickerId the subscription's unique identifier.
         * @sa reqScannerSubscription, ScannerSubscription, reqScannerParameters
         */
        public void cancelScannerSubscription(int tickerId)
        {
            if (!CheckConnection())
                return;

            SendCancelRequest(OutgoingMessages.CancelScannerSubscription, 1, tickerId, EClientErrors.FAIL_SEND_CANSCANNER);
        }

        /**
         * @brief Exercises your options
         * @param tickerId exercise request's identifier
         * @param contract the option Contract to be exercised.
         * @param exerciseAction set to 1 to exercise the option, set to 2 to let the option lapse.
         * @param exerciseQuantity number of contracts to be exercised
         * @param account destination account
         * @param ovrd Specifies whether your setting will override the system's natural action. For example, if your action is "exercise" and the option is not in-the-money, by natural action the option would not exercise. If you have override set to "yes" the natural action would be overridden and the out-of-the money option would be exercised. Set to 1 to override, set to 0 not to.
         */
        public void exerciseOptions(int tickerId, Contract contract, int exerciseAction, int exerciseQuantity, string account, int ovrd)
        {
            //WARN needs to be tested!
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(21, " It does not support options exercise from the API."))
                return;
            if ((!Util.StringIsEmpty(contract.TradingClass) || contract.ConId > 0) &&
                !CheckServerVersion(MinServerVer.TRADING_CLASS, " It does not support conId not tradingClass parameter when exercising options."))
                return;

            int VERSION = 2;

            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.ExerciseOptions);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(tickerId);

            if (serverVersion >= MinServerVer.TRADING_CLASS)
            {
                paramsList.AddParameter(contract.ConId);
            }
            paramsList.AddParameter(contract.Symbol);
            paramsList.AddParameter(contract.SecType);
            paramsList.AddParameter(contract.LastTradeDateOrContractMonth);
            paramsList.AddParameter(contract.Strike);
            paramsList.AddParameter(contract.Right);
            paramsList.AddParameter(contract.Multiplier);
            paramsList.AddParameter(contract.Exchange);
            paramsList.AddParameter(contract.Currency);
            paramsList.AddParameter(contract.LocalSymbol);
            if (serverVersion >= MinServerVer.TRADING_CLASS)
            {
                paramsList.AddParameter(contract.TradingClass);
            }
            paramsList.AddParameter(exerciseAction);
            paramsList.AddParameter(exerciseQuantity);
            paramsList.AddParameter(account);
            paramsList.AddParameter(ovrd);

            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_GENERIC);
        }

        /**
         * @brief Places an order
         * @param id the order's unique identifier. Use a sequential id starting with the id received at the nextValidId method.
         * @param contract the order's contract
         * @param order the order
         * @sa nextValidId, reqAllOpenOrders, reqAutoOpenOrders, reqOpenOrders, cancelOrder, reqGlobalCancel, EWrapper::openOrder, EWrapper::orderStatus, Order, Contract
         */
        public void placeOrder(int id, Contract contract, Order order)
        {
            if (!CheckConnection())
                return;

            if (!VerifyOrder(order, id, StringsAreEqual(Constants.BagSecType, contract.SecType)))
                return;
            if (!VerifyOrderContract(contract, id))
                return;

            int MsgVersion = (serverVersion < MinServerVer.NOT_HELD) ? 27 : 45;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);


            paramsList.AddParameter(OutgoingMessages.PlaceOrder);
            paramsList.AddParameter(MsgVersion);
            paramsList.AddParameter(id);

            if (serverVersion >= MinServerVer.PLACE_ORDER_CONID)
            {
                paramsList.AddParameter(contract.ConId);
            }
            paramsList.AddParameter(contract.Symbol);
            paramsList.AddParameter(contract.SecType);
            paramsList.AddParameter(contract.LastTradeDateOrContractMonth);
            paramsList.AddParameter(contract.Strike);
            paramsList.AddParameter(contract.Right);
            if (serverVersion >= 15)
            {
                paramsList.AddParameter(contract.Multiplier);
            }
            paramsList.AddParameter(contract.Exchange);
            if (serverVersion >= 14)
            {
                paramsList.AddParameter(contract.PrimaryExch);
            }
            paramsList.AddParameter(contract.Currency);
            if (serverVersion >= 2)
            {
                paramsList.AddParameter(contract.LocalSymbol);
            }
            if (serverVersion >= MinServerVer.TRADING_CLASS)
            {
                paramsList.AddParameter(contract.TradingClass);
            }
            if (serverVersion >= MinServerVer.SEC_ID_TYPE)
            {
                paramsList.AddParameter(contract.SecIdType);
                paramsList.AddParameter(contract.SecId);
            }

            // paramsList.AddParameter main order fields
            paramsList.AddParameter(order.Action);

            if (ServerVersion >= MinServerVer.FRACTIONAL_POSITIONS)
                paramsList.AddParameter(order.TotalQuantity);
            else
                paramsList.AddParameter((int)order.TotalQuantity);

            paramsList.AddParameter(order.OrderType);
            if (serverVersion < MinServerVer.ORDER_COMBO_LEGS_PRICE)
            {
                paramsList.AddParameter(order.LmtPrice == Double.MaxValue ? 0 : order.LmtPrice);
            }
            else
            {
                paramsList.AddParameterMax(order.LmtPrice);
            }
            if (serverVersion < MinServerVer.TRAILING_PERCENT)
            {
                paramsList.AddParameter(order.AuxPrice == Double.MaxValue ? 0 : order.AuxPrice);
            }
            else
            {
                paramsList.AddParameterMax(order.AuxPrice);
            }

            // paramsList.AddParameter extended order fields
            paramsList.AddParameter(order.Tif);
            paramsList.AddParameter(order.OcaGroup);
            paramsList.AddParameter(order.Account);
            paramsList.AddParameter(order.OpenClose);
            paramsList.AddParameter(order.Origin);
            paramsList.AddParameter(order.OrderRef);
            paramsList.AddParameter(order.Transmit);
            if (serverVersion >= 4)
            {
                paramsList.AddParameter(order.ParentId);
            }

            if (serverVersion >= 5)
            {
                paramsList.AddParameter(order.BlockOrder);
                paramsList.AddParameter(order.SweepToFill);
                paramsList.AddParameter(order.DisplaySize);
                paramsList.AddParameter(order.TriggerMethod);
                if (serverVersion < 38)
                {
                    // will never happen
                    paramsList.AddParameter(/* order.ignoreRth */ false);
                }
                else
                {
                    paramsList.AddParameter(order.OutsideRth);
                }
            }

            if (serverVersion >= 7)
            {
                paramsList.AddParameter(order.Hidden);
            }

            // paramsList.AddParameter combo legs for BAG requests
            bool isBag = StringsAreEqual(Constants.BagSecType, contract.SecType);
            if (serverVersion >= 8 && isBag)
            {
                if (contract.ComboLegs == null)
                {
                    paramsList.AddParameter(0);
                }
                else
                {
                    paramsList.AddParameter(contract.ComboLegs.Count);

                    ComboLeg comboLeg;
                    for (int i = 0; i < contract.ComboLegs.Count; i++)
                    {
                        comboLeg = (ComboLeg)contract.ComboLegs[i];
                        paramsList.AddParameter(comboLeg.ConId);
                        paramsList.AddParameter(comboLeg.Ratio);
                        paramsList.AddParameter(comboLeg.Action);
                        paramsList.AddParameter(comboLeg.Exchange);
                        paramsList.AddParameter(comboLeg.OpenClose);

                        if (serverVersion >= MinServerVer.SSHORT_COMBO_LEGS)
                        {
                            paramsList.AddParameter(comboLeg.ShortSaleSlot);
                            paramsList.AddParameter(comboLeg.DesignatedLocation);
                        }
                        if (serverVersion >= MinServerVer.SSHORTX_OLD)
                        {
                            paramsList.AddParameter(comboLeg.ExemptCode);
                        }
                    }
                }
            }

            // add order combo legs for BAG requests
            if (serverVersion >= MinServerVer.ORDER_COMBO_LEGS_PRICE && isBag)
            {
                if (order.OrderComboLegs == null)
                {
                    paramsList.AddParameter(0);
                }
                else
                {
                    paramsList.AddParameter(order.OrderComboLegs.Count);

                    for (int i = 0; i < order.OrderComboLegs.Count; i++)
                    {
                        OrderComboLeg orderComboLeg = order.OrderComboLegs[i];
                        paramsList.AddParameterMax(orderComboLeg.Price);
                    }
                }
            }

            if (serverVersion >= MinServerVer.SMART_COMBO_ROUTING_PARAMS && isBag)
            {
                List<TagValue> smartComboRoutingParams = order.SmartComboRoutingParams;
                int smartComboRoutingParamsCount = smartComboRoutingParams == null ? 0 : smartComboRoutingParams.Count;
                paramsList.AddParameter(smartComboRoutingParamsCount);
                if (smartComboRoutingParamsCount > 0)
                {
                    for (int i = 0; i < smartComboRoutingParamsCount; ++i)
                    {
                        TagValue tagValue = smartComboRoutingParams[i];
                        paramsList.AddParameter(tagValue.Tag);
                        paramsList.AddParameter(tagValue.Value);
                    }
                }
            }

            if (serverVersion >= 9)
            {
                // paramsList.AddParameter deprecated sharesAllocation field
                paramsList.AddParameter("");
            }

            if (serverVersion >= 10)
            {
                paramsList.AddParameter(order.DiscretionaryAmt);
            }

            if (serverVersion >= 11)
            {
                paramsList.AddParameter(order.GoodAfterTime);
            }

            if (serverVersion >= 12)
            {
                paramsList.AddParameter(order.GoodTillDate);
            }

            if (serverVersion >= 13)
            {
                paramsList.AddParameter(order.FaGroup);
                paramsList.AddParameter(order.FaMethod);
                paramsList.AddParameter(order.FaPercentage);
                paramsList.AddParameter(order.FaProfile);
            }
            if (serverVersion >= 18)
            { // institutional short sale slot fields.
                paramsList.AddParameter(order.ShortSaleSlot);      // 0 only for retail, 1 or 2 only for institution.
                paramsList.AddParameter(order.DesignatedLocation); // only populate when order.shortSaleSlot = 2.
            }
            if (serverVersion >= MinServerVer.SSHORTX_OLD)
            {
                paramsList.AddParameter(order.ExemptCode);
            }
            if (serverVersion >= 19)
            {
                paramsList.AddParameter(order.OcaType);
                if (serverVersion < 38)
                {
                    // will never happen
                    paramsList.AddParameter( /* order.rthOnly */ false);
                }
                paramsList.AddParameter(order.Rule80A);
                paramsList.AddParameter(order.SettlingFirm);
                paramsList.AddParameter(order.AllOrNone);
                paramsList.AddParameterMax(order.MinQty);
                paramsList.AddParameterMax(order.PercentOffset);
                paramsList.AddParameter(order.ETradeOnly);
                paramsList.AddParameter(order.FirmQuoteOnly);
                paramsList.AddParameterMax(order.NbboPriceCap);
                paramsList.AddParameterMax(order.AuctionStrategy);
                paramsList.AddParameterMax(order.StartingPrice);
                paramsList.AddParameterMax(order.StockRefPrice);
                paramsList.AddParameterMax(order.Delta);
                // Volatility orders had specific watermark price attribs in server version 26
                double lower = (serverVersion == 26 && order.OrderType.Equals("VOL"))
                     ? Double.MaxValue
                     : order.StockRangeLower;
                double upper = (serverVersion == 26 && order.OrderType.Equals("VOL"))
                     ? Double.MaxValue
                     : order.StockRangeUpper;
                paramsList.AddParameterMax(lower);
                paramsList.AddParameterMax(upper);
            }

            if (serverVersion >= 22)
            {
                paramsList.AddParameter(order.OverridePercentageConstraints);
            }

            if (serverVersion >= 26)
            { // Volatility orders
                paramsList.AddParameterMax(order.Volatility);
                paramsList.AddParameterMax(order.VolatilityType);
                if (serverVersion < 28)
                {
                    bool isDeltaNeutralTypeMKT = (String.Compare("MKT", order.DeltaNeutralOrderType, true) == 0);
                    paramsList.AddParameter(isDeltaNeutralTypeMKT);
                }
                else
                {
                    paramsList.AddParameter(order.DeltaNeutralOrderType);
                    paramsList.AddParameterMax(order.DeltaNeutralAuxPrice);

                    if (serverVersion >= MinServerVer.DELTA_NEUTRAL_CONID && !IsEmpty(order.DeltaNeutralOrderType))
                    {
                        paramsList.AddParameter(order.DeltaNeutralConId);
                        paramsList.AddParameter(order.DeltaNeutralSettlingFirm);
                        paramsList.AddParameter(order.DeltaNeutralClearingAccount);
                        paramsList.AddParameter(order.DeltaNeutralClearingIntent);
                    }

                    if (serverVersion >= MinServerVer.DELTA_NEUTRAL_OPEN_CLOSE && !IsEmpty(order.DeltaNeutralOrderType))
                    {
                        paramsList.AddParameter(order.DeltaNeutralOpenClose);
                        paramsList.AddParameter(order.DeltaNeutralShortSale);
                        paramsList.AddParameter(order.DeltaNeutralShortSaleSlot);
                        paramsList.AddParameter(order.DeltaNeutralDesignatedLocation);
                    }
                }
                paramsList.AddParameter(order.ContinuousUpdate);
                if (serverVersion == 26)
                {
                    // Volatility orders had specific watermark price attribs in server version 26
                    double lower = order.OrderType.Equals("VOL") ? order.StockRangeLower : Double.MaxValue;
                    double upper = order.OrderType.Equals("VOL") ? order.StockRangeUpper : Double.MaxValue;
                    paramsList.AddParameterMax(lower);
                    paramsList.AddParameterMax(upper);
                }
                paramsList.AddParameterMax(order.ReferencePriceType);
            }

            if (serverVersion >= 30)
            { // TRAIL_STOP_LIMIT stop price
                paramsList.AddParameterMax(order.TrailStopPrice);
            }

            if (serverVersion >= MinServerVer.TRAILING_PERCENT)
            {
                paramsList.AddParameterMax(order.TrailingPercent);
            }

            if (serverVersion >= MinServerVer.SCALE_ORDERS)
            {
                if (serverVersion >= MinServerVer.SCALE_ORDERS2)
                {
                    paramsList.AddParameterMax(order.ScaleInitLevelSize);
                    paramsList.AddParameterMax(order.ScaleSubsLevelSize);
                }
                else
                {
                    paramsList.AddParameter("");
                    paramsList.AddParameterMax(order.ScaleInitLevelSize);

                }
                paramsList.AddParameterMax(order.ScalePriceIncrement);
            }

            if (serverVersion >= MinServerVer.SCALE_ORDERS3 && order.ScalePriceIncrement > 0.0 && order.ScalePriceIncrement != Double.MaxValue)
            {
                paramsList.AddParameterMax(order.ScalePriceAdjustValue);
                paramsList.AddParameterMax(order.ScalePriceAdjustInterval);
                paramsList.AddParameterMax(order.ScaleProfitOffset);
                paramsList.AddParameter(order.ScaleAutoReset);
                paramsList.AddParameterMax(order.ScaleInitPosition);
                paramsList.AddParameterMax(order.ScaleInitFillQty);
                paramsList.AddParameter(order.ScaleRandomPercent);
            }

            if (serverVersion >= MinServerVer.SCALE_TABLE)
            {
                paramsList.AddParameter(order.ScaleTable);
                paramsList.AddParameter(order.ActiveStartTime);
                paramsList.AddParameter(order.ActiveStopTime);
            }

            if (serverVersion >= MinServerVer.HEDGE_ORDERS)
            {
                paramsList.AddParameter(order.HedgeType);
                if (!IsEmpty(order.HedgeType))
                {
                    paramsList.AddParameter(order.HedgeParam);
                }
            }

            if (serverVersion >= MinServerVer.OPT_OUT_SMART_ROUTING)
            {
                paramsList.AddParameter(order.OptOutSmartRouting);
            }

            if (serverVersion >= MinServerVer.PTA_ORDERS)
            {
                paramsList.AddParameter(order.ClearingAccount);
                paramsList.AddParameter(order.ClearingIntent);
            }

            if (serverVersion >= MinServerVer.NOT_HELD)
            {
                paramsList.AddParameter(order.NotHeld);
            }

            if (serverVersion >= MinServerVer.UNDER_COMP)
            {
                if (contract.UnderComp != null)
                {
                    UnderComp underComp = contract.UnderComp;
                    paramsList.AddParameter(true);
                    paramsList.AddParameter(underComp.ConId);
                    paramsList.AddParameter(underComp.Delta);
                    paramsList.AddParameter(underComp.Price);
                }
                else
                {
                    paramsList.AddParameter(false);
                }
            }

            if (serverVersion >= MinServerVer.ALGO_ORDERS)
            {
                paramsList.AddParameter(order.AlgoStrategy);
                if (!IsEmpty(order.AlgoStrategy))
                {
                    List<TagValue> algoParams = order.AlgoParams;
                    int algoParamsCount = algoParams == null ? 0 : algoParams.Count;
                    paramsList.AddParameter(algoParamsCount);
                    if (algoParamsCount > 0)
                    {
                        for (int i = 0; i < algoParamsCount; ++i)
                        {
                            TagValue tagValue = (TagValue)algoParams[i];
                            paramsList.AddParameter(tagValue.Tag);
                            paramsList.AddParameter(tagValue.Value);
                        }
                    }
                }
            }

            if (serverVersion >= MinServerVer.ALGO_ID)
            {
                paramsList.AddParameter(order.AlgoId);
            }

            if (serverVersion >= MinServerVer.WHAT_IF_ORDERS)
            {
                paramsList.AddParameter(order.WhatIf);
            }

            if (serverVersion >= MinServerVer.LINKING)
            {
                //int orderOptionsCount = order.OrderMiscOptions == null ? 0 : order.OrderMiscOptions.Count;
                //paramsList.AddParameter(orderOptionsCount);
                paramsList.AddParameter(TagValueListToString(order.OrderMiscOptions));
            }

            if (serverVersion >= MinServerVer.ORDER_SOLICITED)
            {
                paramsList.AddParameter(order.Solicited);
            }

            if (serverVersion >= MinServerVer.RANDOMIZE_SIZE_AND_PRICE)
            {
                paramsList.AddParameter(order.RandomizeSize);
                paramsList.AddParameter(order.RandomizePrice);
            }

            CloseAndSend(id, paramsList, lengthPos, EClientErrors.FAIL_SEND_ORDER);
        }

        //WARN: Have not tested this yet!
        /**
         * @brief Replaces Financial Advisor's settings
         * A Financial Advisor can define three different configurations: 
         *    1. Groups: offer traders a way to create a group of accounts and apply a single allocation method to all accounts in the group.
         *    2. Profiles: let you allocate shares on an account-by-account basis using a predefined calculation value.
         *    3. Account Aliases: let you easily identify the accounts by meaningful names rather than account numbers.
         * More information at https://www.interactivebrokers.com/en/?f=%2Fen%2Fsoftware%2Fpdfhighlights%2FPDF-AdvisorAllocations.php%3Fib_entity%3Dllc
         * @param faDataType the configuration to change. Set to 1, 2 or 3 as defined above.
         * @param xml the xml-formatted configuration string
         * @sa requestFA 
         */
        public void replaceFA(int faDataType, string xml)
        {
            if (!CheckConnection())
                return;

            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.ReplaceFA);
            paramsList.AddParameter(1);
            paramsList.AddParameter(faDataType);
            paramsList.AddParameter(xml);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_FA_REPLACE);
        }

        /**
         * @brief Requests the FA configuration
         * A Financial Advisor can define three different configurations: 
         *      1. Groups: offer traders a way to create a group of accounts and apply a single allocation method to all accounts in the group.
         *      2. Profiles: let you allocate shares on an account-by-account basis using a predefined calculation value.
         *      3. Account Aliases: let you easily identify the accounts by meaningful names rather than account numbers.
         * More information at https://www.interactivebrokers.com/en/?f=%2Fen%2Fsoftware%2Fpdfhighlights%2FPDF-AdvisorAllocations.php%3Fib_entity%3Dllc
         * @param faDataType the configuration to change. Set to 1, 2 or 3 as defined above.
         * @sa replaceFA 
         */
        public void requestFA(int faDataType)
        {
            if (!CheckConnection())
                return;
            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestFA);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(faDataType);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_FA_REQUEST);
        }

        /**
         * @brief Requests a specific account's summary.
         * This method will subscribe to the account summary as presented in the TWS' Account Summary tab. The data is returned at EWrapper::accountSummary
         * @param reqId the unique request idntifier.
         * @param group set to "All" to return account summary data for all accounts, or set to a specific Advisor Account Group name that has already been created in TWS Global Configuration.
         * @params tags a comma separated list with the desired tags:
         *      - AccountType
         *      - NetLiquidation,
         *      - TotalCashValue — Total cash including futures pnl
         *      - SettledCash — For cash accounts, this is the same as TotalCashValue
         *      - AccruedCash — Net accrued interest
         *      - BuyingPower — The maximum amount of marginable US stocks the account can buy
         *      - EquityWithLoanValue — Cash + stocks + bonds + mutual funds
         *      - PreviousEquityWithLoanValue,
         *      - GrossPositionValue — The sum of the absolute value of all stock and equity option positions
         *      - RegTEquity,
         *      - RegTMargin,
         *      - SMA — Special Memorandum Account
         *      - InitMarginReq,
         *      - MaintMarginReq,
         *      - AvailableFunds,
         *      - ExcessLiquidity,
         *      - Cushion — Excess liquidity as a percentage of net liquidation value
         *      - FullInitMarginReq,
         *      - FullMaintMarginReq,
         *      - FullAvailableFunds,
         *      - FullExcessLiquidity,
         *      - LookAheadNextChange — Time when look-ahead values take effect
         *      - LookAheadInitMarginReq,
         *      - LookAheadMaintMarginReq,
         *      - LookAheadAvailableFunds,
         *      - LookAheadExcessLiquidity,
         *      - HighestSeverity — A measure of how close the account is to liquidation
         *      - DayTradesRemaining — The Number of Open/Close trades a user could put on before Pattern Day Trading is detected. A value of "-1" means that the user can put on unlimited day trades.
         *      - Leverage — GrossPositionValue / NetLiquidation
         * @sa cancelAccountSummary, EWrapper::accountSummary, EWrapper::accountSummaryEnd
         */
        public void reqAccountSummary(int reqId, string group, string tags)
        {
            int VERSION = 1;
            if (!CheckConnection())
                return;

            if (!CheckServerVersion(reqId, MinServerVer.ACCT_SUMMARY,
                " It does not support account summary requests."))
                return;

            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestAccountSummary);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(reqId);
            paramsList.AddParameter(group);
            paramsList.AddParameter(tags);
            CloseAndSend(reqId, paramsList, lengthPos, EClientErrors.FAIL_SEND_REQACCOUNTDATA);
        }

        /**
         * @brief Subscribes to an specific account's information and portfolio
         * Through this method, a single account's subscription can be started/stopped. As a result from the subscription, the account's information, portfolio and last update time will be received at EWrapper::updateAccountValue, EWrapper::updateAccountPortfolio, EWrapper::updateAccountTime respectively.
         * Only one account can be subscribed at a time. A second subscription request for another account when the previous one is still active will cause the first one to be canceled in favour of the second one. Consider user reqPositions if you want to retrieve all your accounts' portfolios directly.
         * @param subscribe set to true to start the subscription and to false to stop it.
         * @param acctCode the account id (i.e. U123456) for which the information is requested.
         * @sa reqPositions, EWrapper::updateAccountValue, EWrapper::updateAccountPortfolio, EWrapper::updateAccountTime
         */
        public void reqAccountUpdates(bool subscribe, string acctCode)
        {
            int VERSION = 2;
            if (!CheckConnection())
                return;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestAccountData);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(subscribe);
            if (serverVersion >= 9)
                paramsList.AddParameter(acctCode);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_REQACCOUNTDATA);
        }

        /**
         * @brief Requests all open orders submitted by any API client as well as those directly placed in the TWS. The existing orders will be received via the openOrder and orderStatus events.
         * @sa reqAutoOpenOrders, reqOpenOrders, EWrapper::openOrder, EWrapper::orderStatus
         */
        public void reqAllOpenOrders()
        {
            int VERSION = 1;
            if (!CheckConnection())
                return;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestAllOpenOrders);
            paramsList.AddParameter(VERSION);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_OORDER);
        }

        /**
         * @brief Requests all order placed on the TWS directly.
         * Only the orders created after this request has been made will be returned.
         * @param autoBind if set to true, the newly created orders will be implicitely associated with this client.
         * @sa reqAllOpenOrders, reqOpenOrders, cancelOrder, reqGlobalCancel, EWrapper::openOrder, EWrapper::orderStatus
         */
        public void reqAutoOpenOrders(bool autoBind)
        {
            int VERSION = 1;
            if (!CheckConnection())
                return;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestAutoOpenOrders);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(autoBind);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_OORDER);
        }

        /**
         * @brief Requests contract information.
         * This method will provide all the contracts matching the contract provided. It can also be used to retrieve complete options and futures chains. This information will be returned at EWrapper:contractDetails
         * @param reqId the unique request identifier.
         * @param contract the contract used as sample to query the available contracts. Typically, it will contain the Contract::Symbol, Contract::Currency, Contract::SecType, Contract::Exchange
         * @sa EWrapper::contractDetails
         */
        public void reqContractDetails(int reqId, Contract contract)
        {
            if (!CheckConnection())
                return;

            if (!IsEmpty(contract.SecIdType) || !IsEmpty(contract.SecId))
            {
                if (!CheckServerVersion(reqId, MinServerVer.SEC_ID_TYPE, " It does not support secIdType not secId attributes"))
                    return;
            }

            if (!IsEmpty(contract.TradingClass))
            {
                if (!CheckServerVersion(reqId, MinServerVer.TRADING_CLASS, " It does not support the TradingClass parameter when requesting contract details."))
                    return;
            }

            if (!IsEmpty(contract.PrimaryExch) && !CheckServerVersion(reqId, MinServerVer.LINKING,
                " It does not support PrimaryExch parameter when requesting contract details."))
                return;


            int VERSION = 8;

            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestContractData);
            paramsList.AddParameter(VERSION);//version
            if (serverVersion >= MinServerVer.CONTRACT_DATA_CHAIN)
            {
                paramsList.AddParameter(reqId);
            }
            if (serverVersion >= MinServerVer.CONTRACT_CONID)
            {
                paramsList.AddParameter(contract.ConId);
            }
            paramsList.AddParameter(contract.Symbol);
            paramsList.AddParameter(contract.SecType);
            paramsList.AddParameter(contract.LastTradeDateOrContractMonth);
            paramsList.AddParameter(contract.Strike);
            paramsList.AddParameter(contract.Right);
            if (serverVersion >= 15)
            {
                paramsList.AddParameter(contract.Multiplier);
            }

            if (serverVersion >= MinServerVer.PRIMARYEXCH)
            {
                paramsList.AddParameter(contract.Exchange);
                paramsList.AddParameter(contract.PrimaryExch);
            }
            else if (serverVersion >= MinServerVer.LINKING)
            {
                if (!IsEmpty(contract.PrimaryExch) && (contract.Exchange == "BEST" || contract.Exchange == "SMART"))
                {
                    paramsList.AddParameter(contract.Exchange + ":" + contract.PrimaryExch);
                }
                else
                {
                    paramsList.AddParameter(contract.Exchange);
                }
            }
            
            paramsList.AddParameter(contract.Currency);
            paramsList.AddParameter(contract.LocalSymbol);
            if (serverVersion >= MinServerVer.TRADING_CLASS)
            {
                paramsList.AddParameter(contract.TradingClass);
            }
            if (serverVersion >= 31)
            {
                paramsList.AddParameter(contract.IncludeExpired);
            }
            if (serverVersion >= MinServerVer.SEC_ID_TYPE)
            {
                paramsList.AddParameter(contract.SecIdType);
                paramsList.AddParameter(contract.SecId);
            }
            CloseAndSend(reqId, paramsList, lengthPos, EClientErrors.FAIL_SEND_REQCONTRACT);
        }

        /**
         * @brief Requests the server's current time.
         * @sa EWrapper::currentTime
         */
        public void reqCurrentTime()
        {
            int VERSION = 1;
            if (!CheckConnection())
                return;

            if (!CheckServerVersion(MinServerVer.CURRENT_TIME, " It does not support current time requests."))
                return;

            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestCurrentTime);
            paramsList.AddParameter(VERSION);//version
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_REQCURRTIME);
        }

        /**
         * @brief Requests all the day's executions matching the filter.
         * Only the current day's executions can be retrieved. Along with the executions, the CommissionReport will also be returned. The execution details will arrive at EWrapper:execDetails
         * @param reqId the request's unique identifier.
         * @param filter the filter criteria used to determine which execution reports are returned.
         * @sa EWrapper::execDetails, EWrapper::commissionReport, ExecutionFilter
         */
        public void reqExecutions(int reqId, ExecutionFilter filter)
        {
            if (!CheckConnection())
                return;

            int VERSION = 3;

            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestExecutions);
            paramsList.AddParameter(VERSION);//version

            if (serverVersion >= MinServerVer.EXECUTION_DATA_CHAIN)
            {
                paramsList.AddParameter(reqId);
            }

            //Send the execution rpt filter data
            if (serverVersion >= 9)
            {
                paramsList.AddParameter(filter.ClientId);
                paramsList.AddParameter(filter.AcctCode);

                // Note that the valid format for time is "yyyymmdd-hh:mm:ss"
                paramsList.AddParameter(filter.Time);
                paramsList.AddParameter(filter.Symbol);
                paramsList.AddParameter(filter.SecType);
                paramsList.AddParameter(filter.Exchange);
                paramsList.AddParameter(filter.Side);
            }
            CloseAndSend(reqId, paramsList, lengthPos, EClientErrors.FAIL_SEND_EXEC);
        }

        /**
         * @brief Requests the contract's Reuters' global fundamental data.
         * Reuters funalmental data will be returned at EWrapper::fundamentalData
         * @param reqId the request's unique identifier.
         * @param contract the contract's description for which the data will be returned.
         * @param reportType there are three available report types: 
         *      - ReportSnapshot: Company overview
         *      - ReportsFinSummary: Financial summary
                - ReportRatios:	Financial ratios
                - ReportsFinStatements:	Financial statements
                - RESC: Analyst estimates
                - CalendarReport: Company calendar
         * @sa EWrapper::fundamentalData
         */
        public void reqFundamentalData(int reqId, Contract contract, String reportType, List<TagValue> fundamentalDataOptions)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(reqId, MinServerVer.FUNDAMENTAL_DATA, " It does not support Fundamental Data requests."))
                return;
            if (!IsEmpty(contract.TradingClass) || contract.ConId > 0 || !IsEmpty(contract.Multiplier))
            {
                if (!CheckServerVersion(reqId, MinServerVer.TRADING_CLASS, ""))
                    return;
            }

            const int VERSION = 3;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestFundamentalData);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(reqId);
            if (serverVersion >= MinServerVer.TRADING_CLASS)
            {
                //WARN: why are we checking the trading class and multiplier above never send them?
                paramsList.AddParameter(contract.ConId);
            }
            paramsList.AddParameter(contract.Symbol);
            paramsList.AddParameter(contract.SecType);
            paramsList.AddParameter(contract.Exchange);
            paramsList.AddParameter(contract.PrimaryExch);
            paramsList.AddParameter(contract.Currency);
            paramsList.AddParameter(contract.LocalSymbol);
            paramsList.AddParameter(reportType);

            if (serverVersion >= MinServerVer.LINKING)
            {
                int tagValuesCount = fundamentalDataOptions == null ? 0 : fundamentalDataOptions.Count;
                paramsList.AddParameter(tagValuesCount);
                paramsList.AddParameter(TagValueListToString(fundamentalDataOptions));
            }

            CloseAndSend(reqId, paramsList, lengthPos, EClientErrors.FAIL_SEND_REQFUNDDATA);
        }

        /**
         * @brief Cancels all the active orders.
         * This method will cancel ALL open orders included those placed directly via the TWS.
         * @sa cancelOrder
         */
        public void reqGlobalCancel()
        {
            if (!CheckConnection())
                return;

            if (!CheckServerVersion(MinServerVer.REQ_GLOBAL_CANCEL, "It does not support global cancel requests."))
                return;

            const int VERSION = 1;

            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestGlobalCancel);
            paramsList.AddParameter(VERSION);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_REQGLOBALCANCEL);
        }

        /**
         * @brief Requests contracts' historical data.
         * When requesting historical data, a finishing time and date is required along with a duration string. For example, having: 
         *      - endDateTime: 20130701 23:59:59 GMT
         *      - durationStr: 3 D
         * will return three days of data counting backwards from July 1st 2013 at 23:59:59 GMT resulting in all the available bars of the last three days until the date and time specified. It is possible to specify a timezone optionally. The resulting bars will be returned in EWrapper::historicalData
         * @param tickerId the request's unique identifier.
         * @param contract the contract for which we want to retrieve the data.
         * @param endDateTime request's ending time with format yyyyMMdd HH:mm:ss {TMZ}
         * @param durationString the amount of time for which the data needs to be retrieved:
         *      - " S (seconds)
         *      - " D (days)
         *      - " W (weeks)
         *      - " M (months)
         *      - " Y (years)
         * @param barSizeSetting the size of the bar:
         *      - 1 sec
         *      - 5 secs
         *      - 15 secs
         *      - 30 secs
         *      - 1 min
         *      - 2 mins
         *      - 3 mins
         *      - 5 mins
         *      - 15 mins
         *      - 30 mins
         *      - 1 hour
         *      - 1 day
         * @param whatToShow the kind of information being retrieved:
         *      - TRADES
         *      - MIDPOINT
         *      - BID
         *      - ASK
         *      - BID_ASK
         *      - HISTORICAL_VOLATILITY
         *      - OPTION_IMPLIED_VOLATILITY
         * @param useRTH set to 0 to obtain the data which was also generated ourside of the Regular Trading Hours, set to 1 to obtain only the RTH data
         * @param formatDate set to 1 to obtain the bars' time as yyyyMMdd HH:mm:ss, set to 2 to obtain it like system time format in seconds
         * @sa EWrapper::historicalData
         */
        public void reqHistoricalData(int tickerId, Contract contract, string endDateTime,
            string durationString, string barSizeSetting, string whatToShow, int useRTH, int formatDate, List<TagValue> chartOptions)
        {
            if (!CheckConnection())
                return;

            if (!CheckServerVersion(tickerId, 16))
                return;

            if (!IsEmpty(contract.TradingClass) || contract.ConId > 0)
            {
                if (!CheckServerVersion(tickerId, MinServerVer.TRADING_CLASS, " It does not support conId nor trading class parameters when requesting historical data."))
                    return;
            }

            const int VERSION = 6;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestHistoricalData);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(tickerId);
            if (serverVersion >= MinServerVer.TRADING_CLASS)
                paramsList.AddParameter(contract.ConId);
            paramsList.AddParameter(contract.Symbol);
            paramsList.AddParameter(contract.SecType);
            paramsList.AddParameter(contract.LastTradeDateOrContractMonth);
            paramsList.AddParameter(contract.Strike);
            paramsList.AddParameter(contract.Right);
            paramsList.AddParameter(contract.Multiplier);
            paramsList.AddParameter(contract.Exchange);
            paramsList.AddParameter(contract.PrimaryExch);
            paramsList.AddParameter(contract.Currency);
            paramsList.AddParameter(contract.LocalSymbol);
            if (serverVersion >= MinServerVer.TRADING_CLASS)
            {
                paramsList.AddParameter(contract.TradingClass);
            }

            paramsList.AddParameter(contract.IncludeExpired ? 1 : 0);


            paramsList.AddParameter(endDateTime);
            paramsList.AddParameter(barSizeSetting);

            paramsList.AddParameter(durationString);
            paramsList.AddParameter(useRTH);
            paramsList.AddParameter(whatToShow);

            paramsList.AddParameter(formatDate);

            if (StringsAreEqual(Constants.BagSecType, contract.SecType))
            {
                if (contract.ComboLegs == null)
                {
                    paramsList.AddParameter(0);
                }
                else
                {
                    paramsList.AddParameter(contract.ComboLegs.Count);

                    ComboLeg comboLeg;
                    for (int i = 0; i < contract.ComboLegs.Count; i++)
                    {
                        comboLeg = (ComboLeg)contract.ComboLegs[i];
                        paramsList.AddParameter(comboLeg.ConId);
                        paramsList.AddParameter(comboLeg.Ratio);
                        paramsList.AddParameter(comboLeg.Action);
                        paramsList.AddParameter(comboLeg.Exchange);
                    }
                }
            }

            if (serverVersion >= MinServerVer.LINKING)
            {
                paramsList.AddParameter(TagValueListToString(chartOptions));
            }

            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_REQHISTDATA);
        }

        /**
         * @brief Requests the next valid order id.
         * @param numIds deprecate
         * @sa EWrapper::nextValidId
         */
        public void reqIds(int numIds)
        {
            if (!CheckConnection())
                return;
            const int VERSION = 1;

            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestIds);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(numIds);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_GENERIC);
        }

        /**
         * @brief Requests the accounts to which the logged user has access to.
         * @sa EWrapper::managedAccounts
         */
        public void reqManagedAccts()
        {
            if (!CheckConnection())
                return;
            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestManagedAccounts);
            paramsList.AddParameter(VERSION);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_GENERIC);
        }

        /**
         * @brief Requests real time market data.
         * This function will return the product's market data. It is important to notice that only real time data can be delivered via the API.
         * @param tickerId the request's identifier
         * @param contract the Contract for which the data is being requested
         * @param genericTickList comma separated ids of the available generic ticks:
         *      - 100 	Option Volume (currently for stocks)
         *      - 101 	Option Open Interest (currently for stocks) 
         *      - 104 	Historical Volatility (currently for stocks)
         *      - 106 	Option Implied Volatility (currently for stocks)
         *      - 162 	Index Future Premium 
         *      - 165 	Miscellaneous Stats 
         *      - 221 	Mark Price (used in TWS P&L computations) 
         *      - 225 	Auction values (volume, price and imbalance) 
         *      - 233 	RTVolume - contains the last trade price, last trade size, last trade time, total volume, VWAP, and single trade flag.
         *      - 236 	Shortable
         *      - 256 	Inventory 	 
         *      - 258 	Fundamental Ratios 
         *      - 411 	Realtime Historical Volatility 
         *      - 456 	IBDividends
         * @param snapshot when set to true, it will provide a single snapshot of the available data. Set to false if you want to receive continuous updates.
         * @sa cancelMktData, EWrapper::tickPrice, EWrapper::tickSize, EWrapper::tickString, EWrapper::tickEFP, EWrapper::tickGeneric, EWrapper::tickOption, EWrapper::tickSnapshotEnd
         */
        public void reqMktData(int tickerId, Contract contract, string genericTickList, bool snapshot, List<TagValue> mktDataOptions)
        {
            if (!CheckConnection())
                return;

            if (snapshot && !CheckServerVersion(tickerId, MinServerVer.SNAPSHOT_MKT_DATA,
                "It does not support snapshot market data requests."))
                return;

            if (contract.UnderComp != null && !CheckServerVersion(tickerId, MinServerVer.UNDER_COMP,
                " It does not support delta-neutral orders"))
                return;


            if (contract.ConId > 0 && !CheckServerVersion(tickerId, MinServerVer.CONTRACT_CONID,
                " It does not support ConId parameter"))
                return;

            if (!Util.StringIsEmpty(contract.TradingClass) && !CheckServerVersion(tickerId, MinServerVer.TRADING_CLASS,
                " It does not support trading class parameter in reqMktData."))
                return;

            int version = 11;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestMarketData);
            paramsList.AddParameter(version);
            paramsList.AddParameter(tickerId);
            if (serverVersion >= MinServerVer.CONTRACT_CONID)
                paramsList.AddParameter(contract.ConId);
            paramsList.AddParameter(contract.Symbol);
            paramsList.AddParameter(contract.SecType);
            paramsList.AddParameter(contract.LastTradeDateOrContractMonth);
            paramsList.AddParameter(contract.Strike);
            paramsList.AddParameter(contract.Right);
            if (serverVersion >= 15)
                paramsList.AddParameter(contract.Multiplier);
            paramsList.AddParameter(contract.Exchange);

            if (serverVersion >= 14)
                paramsList.AddParameter(contract.PrimaryExch);
            paramsList.AddParameter(contract.Currency);
            if (serverVersion >= 2)
                paramsList.AddParameter(contract.LocalSymbol);
            if (serverVersion >= MinServerVer.TRADING_CLASS)
                paramsList.AddParameter(contract.TradingClass);
            if (serverVersion >= 8 && Constants.BagSecType.Equals(contract.SecType))
            {
                if (contract.ComboLegs == null)
                {
                    paramsList.AddParameter(0);
                }
                else
                {
                    paramsList.AddParameter(contract.ComboLegs.Count);
                    for (int i = 0; i < contract.ComboLegs.Count; i++)
                    {
                        ComboLeg leg = contract.ComboLegs[i];
                        paramsList.AddParameter(leg.ConId);
                        paramsList.AddParameter(leg.Ratio);
                        paramsList.AddParameter(leg.Action);
                        paramsList.AddParameter(leg.Exchange);
                    }
                }
            }

            if (serverVersion >= MinServerVer.UNDER_COMP)
            {
                if (contract.UnderComp != null)
                {
                    paramsList.AddParameter(true);
                    paramsList.AddParameter(contract.UnderComp.ConId);
                    paramsList.AddParameter(contract.UnderComp.Delta);
                    paramsList.AddParameter(contract.UnderComp.Price);
                }
                else
                {
                    paramsList.AddParameter(false);
                }
            }
            if (serverVersion >= 31)
            {
                paramsList.AddParameter(genericTickList);
            }
            if (serverVersion >= MinServerVer.SNAPSHOT_MKT_DATA)
            {
                paramsList.AddParameter(snapshot);
            }
            if (serverVersion >= MinServerVer.LINKING)
            {
                paramsList.AddParameter(TagValueListToString(mktDataOptions));
            }

            CloseAndSend(tickerId, paramsList, lengthPos, EClientErrors.FAIL_SEND_REQMKT);
        }

        /**
         * @brief indicates the TWS to switch to "frozen" market data.
         * The API can receive frozen market data from Trader Workstation. Frozen market data is the last data recorded in our system. During normal trading hours, the API receives real-time market data. If you use this function, you are telling TWS to automatically switch to frozen market data after the close. Then, before the opening of the next trading day, market data will automatically switch back to real-time market data.
         * @param marketDataType set to 1 for real time streaming, set to 2 for frozen market data.
         */
        public void reqMarketDataType(int marketDataType)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.REQ_MARKET_DATA_TYPE, " It does not support market data type requests."))
                return;
            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestMarketDataType);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(marketDataType);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_REQMARKETDATATYPE);
        }

        /**
         * @brief Requests the contract's market depth (order book).
         * @param tickerId the request's identifier
         * @param contract the Contract for which the depth is being requested
         * @param numRows the number of rows on each side of the order book
         * @sa cancelMktDepth, EWrapper::updateMktDepth, EWrapper::updateMktDepthL2
         */
        public void reqMarketDepth(int tickerId, Contract contract, int numRows, List<TagValue> mktDepthOptions)
        {
            if (!CheckConnection())
                return;

            if (!IsEmpty(contract.TradingClass) || contract.ConId > 0)
            {
                if (!CheckServerVersion(tickerId, MinServerVer.TRADING_CLASS, " It does not support ConId nor TradingClass parameters in reqMktDepth."))
                    return;
            }

            const int VERSION = 5;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestMarketDepth);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(tickerId);

            // paramsList.AddParameter contract fields
            if (serverVersion >= MinServerVer.TRADING_CLASS)
            {
                paramsList.AddParameter(contract.ConId);
            }
            paramsList.AddParameter(contract.Symbol);
            paramsList.AddParameter(contract.SecType);
            paramsList.AddParameter(contract.LastTradeDateOrContractMonth);
            paramsList.AddParameter(contract.Strike);
            paramsList.AddParameter(contract.Right);
            if (serverVersion >= 15)
            {
                paramsList.AddParameter(contract.Multiplier);
            }
            paramsList.AddParameter(contract.Exchange);
            paramsList.AddParameter(contract.Currency);
            paramsList.AddParameter(contract.LocalSymbol);
            if (serverVersion >= MinServerVer.TRADING_CLASS)
            {
                paramsList.AddParameter(contract.TradingClass);
            }
            if (serverVersion >= 19)
            {
                paramsList.AddParameter(numRows);
            }
            if (serverVersion >= MinServerVer.LINKING)
            {
                //int tagValuesCount = mktDepthOptions == null ? 0 : mktDepthOptions.Count;
                //paramsList.AddParameter(tagValuesCount);
                paramsList.AddParameter(TagValueListToString(mktDepthOptions));
            }
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_REQMKTDEPTH);
        }

        /**
         * @brief Subscribes to IB's News Bulletins
         * @param allMessages if set to true, will return all the existing bulletins for the current day, set to false to receive only the new bulletins.
         * @sa cancelNewsBulletins, EWrapper::updateNewsBulletins
         */
        public void reqNewsBulletins(bool allMessages)
        {
            if (!CheckConnection())
                return;

            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestNewsBulletins);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(allMessages);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_GENERIC);
        }

        /**
         * @brief Requests all open orders places by this specific API client (identified by the API client id)
         * @sa reqAllOpenOrders, reqAutoOpenOrders, placeOrder, cancelOrder, reqGlobalCancel, EWrapper::openOrder, EWrapper::orderStatus
         */
        public void reqOpenOrders()
        {
            int VERSION = 1;
            if (!CheckConnection())
                return;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestOpenOrders);
            paramsList.AddParameter(VERSION);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_OORDER);
        }

        /**
         * @brief Requests all positions from all accounts
         * @sa cancelPositions, EWrapper::position, EWrapper::positionEnd
         */
        public void reqPositions()
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.ACCT_SUMMARY, " It does not support position requests."))
                return;

            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestPositions);
            paramsList.AddParameter(VERSION);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_REQPOSITIONS);
        }

        /**
         * @brief Requests real time bars
         * Currently, only 5 seconds bars are provided. This request ius suject to the same pacing as any historical data request: no more than 60 API queries in more than 600 seconds
         * @param tickerId the request's unique identifier.
         * @param contract the Contract for which the depth is being requested
         * @param barSize currently being ignored
         * @param whatToShow the nature of the data being retrieved:
         *      - TRADES
         *      - MIDPOINT
         *      - BID
         *      - ASK
         * @param useRTH set to 0 to obtain the data which was also generated ourside of the Regular Trading Hours, set to 1 to obtain only the RTH data
         * @sa cancelRealTimeBars, EWrapper::realTimeBar
         */
        public void reqRealTimeBars(int tickerId, Contract contract, int barSize, string whatToShow, bool useRTH, List<TagValue> realTimeBarsOptions)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(tickerId, MinServerVer.REAL_TIME_BARS, " It does not support real time bars."))
                return;

            if (!IsEmpty(contract.TradingClass) || contract.ConId > 0)
            {
                if (!CheckServerVersion(tickerId, MinServerVer.TRADING_CLASS, " It does not support ConId nor TradingClass parameters in reqRealTimeBars."))
                    return;
            }

            const int VERSION = 3;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestRealTimeBars);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(tickerId);

            // paramsList.AddParameter contract fields
            if (serverVersion >= MinServerVer.TRADING_CLASS)
            {
                paramsList.AddParameter(contract.ConId);
            }
            paramsList.AddParameter(contract.Symbol);
            paramsList.AddParameter(contract.SecType);
            paramsList.AddParameter(contract.LastTradeDateOrContractMonth);
            paramsList.AddParameter(contract.Strike);
            paramsList.AddParameter(contract.Right);
            paramsList.AddParameter(contract.Multiplier);
            paramsList.AddParameter(contract.Exchange);
            paramsList.AddParameter(contract.PrimaryExch);
            paramsList.AddParameter(contract.Currency);
            paramsList.AddParameter(contract.LocalSymbol);
            if (serverVersion >= MinServerVer.TRADING_CLASS)
            {
                paramsList.AddParameter(contract.TradingClass);
            }
            paramsList.AddParameter(barSize);  // this parameter is not currently used
            paramsList.AddParameter(whatToShow);
            paramsList.AddParameter(useRTH);
            if (serverVersion >= MinServerVer.LINKING)
            {
                paramsList.AddParameter(TagValueListToString(realTimeBarsOptions));
            }
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_REQRTBARS);
        }

        /**
         * @brief Requests all possible parameters which can be used for a scanner subscription
         * @sa reqScannerSubscription
         */
        public void reqScannerParameters()
        {
            if (!CheckConnection())
                return;
            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestScannerParameters);
            paramsList.AddParameter(VERSION);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_REQSCANNERPARAMETERS);
        }

        /**
         * @brief Starts a subscription to market scan results based on the provided parameters.
         * @param reqId the request's identifier
         * @param subscription summary of the scanner subscription including its filters.
         * @sa reqScannerParameters, ScannerSubscription, EWrapper::scannerData
         */
        public void reqScannerSubscription(int reqId, ScannerSubscription subscription, List<TagValue> scannerSubscriptionOptions)
        {
            if (!CheckConnection())
                return;
            const int VERSION = 4;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.RequestScannerSubscription);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(reqId);
            paramsList.AddParameterMax(subscription.NumberOfRows);
            paramsList.AddParameter(subscription.Instrument);
            paramsList.AddParameter(subscription.LocationCode);
            paramsList.AddParameter(subscription.ScanCode);
            paramsList.AddParameterMax(subscription.AbovePrice);
            paramsList.AddParameterMax(subscription.BelowPrice);
            paramsList.AddParameterMax(subscription.AboveVolume);
            paramsList.AddParameterMax(subscription.MarketCapAbove);
            paramsList.AddParameterMax(subscription.MarketCapBelow);
            paramsList.AddParameter(subscription.MoodyRatingAbove);
            paramsList.AddParameter(subscription.MoodyRatingBelow);
            paramsList.AddParameter(subscription.SpRatingAbove);
            paramsList.AddParameter(subscription.SpRatingBelow);
            paramsList.AddParameter(subscription.MaturityDateAbove);
            paramsList.AddParameter(subscription.MaturityDateBelow);
            paramsList.AddParameterMax(subscription.CouponRateAbove);
            paramsList.AddParameterMax(subscription.CouponRateBelow);
            paramsList.AddParameter(subscription.ExcludeConvertible);
            if (serverVersion >= 25)
            {
                paramsList.AddParameterMax(subscription.AverageOptionVolumeAbove);
                paramsList.AddParameter(subscription.ScannerSettingPairs);
            }
            if (serverVersion >= 27)
            {
                paramsList.AddParameter(subscription.StockTypeFilter);
            }

            if (serverVersion >= MinServerVer.LINKING)
            {
                //int tagValuesCount = scannerSubscriptionOptions == null ? 0 : scannerSubscriptionOptions.Count;
                //paramsList.AddParameter(tagValuesCount);
                paramsList.AddParameter(TagValueListToString(scannerSubscriptionOptions));
            }

            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_REQSCANNER);
        }

        /**
         * @brief Changes the TWS/GW log level.
         * Valid values are:\n
         * 1 = SYSTEM\n
         * 2 = ERROR\n
         * 3 = WARNING\n
         * 4 = INFORMATION\n
         * 5 = DETAIL\n
         */
        public void setServerLogLevel(int logLevel)
        {
            if (!CheckConnection())
                return;
            const int VERSION = 1;

            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.ChangeServerLog);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(logLevel);

            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_SERVER_LOG_LEVEL);
        }

        public void verifyRequest(string apiName, string apiVersion)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.LINKING, " It does not support verification request."))
                return;
            if (!extraAuth)
            {
                ReportError(IncomingMessage.NotValid, EClientErrors.FAIL_SEND_VERIFYMESSAGE, " Intent to authenticate needs to be expressed during initial connect request.");
                return;
            }

            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.VerifyRequest);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(apiName);
            paramsList.AddParameter(apiVersion);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_VERIFYREQUEST);
        }

        public void verifyMessage(string apiData)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.LINKING, " It does not support verification message sending."))
                return;
            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.VerifyMessage);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(apiData);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_VERIFYMESSAGE);
        }

        public void verifyAndAuthRequest(string apiName, string apiVersion, string opaqueIsvKey)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.LINKING_AUTH, " It does not support verification request."))
                return;
            if (!extraAuth)
            {
                ReportError(IncomingMessage.NotValid, EClientErrors.FAIL_SEND_VERIFYANDAUTHMESSAGE, " Intent to authenticate needs to be expressed during initial connect request.");
                return;
            }

            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);
            paramsList.AddParameter(OutgoingMessages.VerifyAndAuthRequest);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(apiName);
            paramsList.AddParameter(apiVersion);
            paramsList.AddParameter(opaqueIsvKey);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_VERIFYANDAUTHREQUEST);
        }

        public void verifyAndAuthMessage(string apiData, string xyzResponse)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.LINKING_AUTH, " It does not support verification message sending."))
                return;
            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);
            paramsList.AddParameter(OutgoingMessages.VerifyAndAuthMessage);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(apiData);
            paramsList.AddParameter(xyzResponse);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_VERIFYANDAUTHMESSAGE);
        }

        public void queryDisplayGroups(int requestId)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.LINKING, " It does not support queryDisplayGroups request."))
                return;
            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.QueryDisplayGroups);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(requestId);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_QUERYDISPLAYGROUPS);
        }

        public void subscribeToGroupEvents(int requestId, int groupId)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.LINKING, " It does not support subscribeToGroupEvents request."))
                return;
            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.SubscribeToGroupEvents);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(requestId);
            paramsList.AddParameter(groupId);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_SUBSCRIBETOGROUPEVENTS);
        }

        public void updateDisplayGroup(int requestId, string contractInfo)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.LINKING, " It does not support updateDisplayGroup request."))
                return;
            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.UpdateDisplayGroup);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(requestId);
            paramsList.AddParameter(contractInfo);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_UPDATEDISPLAYGROUP);
        }

        public void unsubscribeFromGroupEvents(int requestId)
        {
            if (!CheckConnection())
                return;
            if (!CheckServerVersion(MinServerVer.LINKING, " It does not support unsubscribeFromGroupEvents request."))
                return;
            const int VERSION = 1;
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(OutgoingMessages.UnsubscribeFromGroupEvents);
            paramsList.AddParameter(VERSION);
            paramsList.AddParameter(requestId);
            CloseAndSend(paramsList, lengthPos, EClientErrors.FAIL_SEND_UNSUBSCRIBEFROMGROUPEVENTS);
        }

        protected bool CheckServerVersion(int requiredVersion)
        {
            return CheckServerVersion(requiredVersion, "");
        }

        protected bool CheckServerVersion(int requestId, int requiredVersion)
        {
            return CheckServerVersion(requestId, requiredVersion, "");
        }

        protected bool CheckServerVersion(int requiredVersion, string updatetail)
        {
            return CheckServerVersion(IncomingMessage.NotValid, requiredVersion, updatetail);
        }

        protected bool CheckServerVersion(int tickerId, int requiredVersion, string updatetail)
        {
            if (serverVersion < requiredVersion)
            {
                ReportUpdateTWS(tickerId, updatetail);
                return false;
            }
            return true;
        }

        protected void CloseAndSend(BinaryWriter paramsList, uint lengthPos, CodeMsgPair error)
        {
            CloseAndSend(IncomingMessage.NotValid, paramsList, lengthPos, error);
        }

        protected void CloseAndSend(int reqId, BinaryWriter paramsList, uint lengthPos, CodeMsgPair error)
        {
            try
            {
                lock (this)
                {
                    CloseAndSend(paramsList, lengthPos);
                }
            }
            catch (Exception)
            {
                wrapper.error(reqId, error.Code, error.Message);
                Close();
            }
        }

        protected abstract void CloseAndSend(BinaryWriter request, uint lengthPos);

        protected bool CheckConnection()
        {
            if (!isConnected)
            {
                wrapper.error(IncomingMessage.NotValid, EClientErrors.NOT_CONNECTED.Code, EClientErrors.NOT_CONNECTED.Message);
                return false;
            }

            return true;
        }

        protected void ReportError(int reqId, CodeMsgPair error, string tail)
        {
            ReportError(reqId, error.Code, error.Message + tail);
        }

        protected void ReportUpdateTWS(int reqId, string tail)
        {
            ReportError(reqId, EClientErrors.UPDATE_TWS.Code, EClientErrors.UPDATE_TWS.Message + tail);
        }

        protected void ReportUpdateTWS(string tail)
        {
            ReportError(IncomingMessage.NotValid, EClientErrors.UPDATE_TWS.Code, EClientErrors.UPDATE_TWS.Message + tail);
        }

        protected void ReportError(int reqId, int code, string message)
        {
            wrapper.error(reqId, code, message);
        }

        protected void SendCancelRequest(OutgoingMessages msgType, int version, int reqId, CodeMsgPair errorMessage)
        {
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(msgType);
            paramsList.AddParameter(version);
            paramsList.AddParameter(reqId);
            try
            {
                lock (this)
                {
                    CloseAndSend(paramsList, lengthPos);
                }
            }
            catch (Exception)
            {
                wrapper.error(reqId, errorMessage.Code, errorMessage.Message);
                Close();
            }
        }

        protected void SendCancelRequest(OutgoingMessages msgType, int version, CodeMsgPair errorMessage)
        {
            var paramsList = new BinaryWriter(new MemoryStream());
            var lengthPos = prepareBuffer(paramsList);

            paramsList.AddParameter(msgType);
            paramsList.AddParameter(version);
            try
            {
                lock (this)
                {
                    CloseAndSend(paramsList, lengthPos);
                }
            }
            catch (Exception)
            {
                wrapper.error(IncomingMessage.NotValid, errorMessage.Code, errorMessage.Message);
                Close();
            }
        }

        protected bool VerifyOrderContract(Contract contract, int id)
        {
            if (serverVersion < MinServerVer.SSHORT_COMBO_LEGS)
            {
                if (contract.ComboLegs.Count > 0)
                {
                    ComboLeg comboLeg;
                    for (int i = 0; i < contract.ComboLegs.Count; ++i)
                    {
                        comboLeg = (ComboLeg)contract.ComboLegs[i];
                        if (comboLeg.ShortSaleSlot != 0 ||
                            !IsEmpty(comboLeg.DesignatedLocation))
                        {
                            ReportError(id, EClientErrors.UPDATE_TWS,
                                "  It does not support SSHORT flag for combo legs.");
                            return false;
                        }
                    }
                }
            }

            if (serverVersion < MinServerVer.UNDER_COMP)
            {
                if (contract.UnderComp != null)
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support delta-neutral orders.");
                    return false;
                }
            }

            if (serverVersion < MinServerVer.PLACE_ORDER_CONID)
            {
                if (contract.ConId > 0)
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support conId parameter.");
                    return false;
                }
            }

            if (serverVersion < MinServerVer.SEC_ID_TYPE)
            {
                if (!IsEmpty(contract.SecIdType) || !IsEmpty(contract.SecId))
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support secIdType and secId parameters.");
                    return false;
                }
            }
            if (serverVersion < MinServerVer.SSHORTX)
            {
                if (contract.ComboLegs.Count > 0)
                {
                    ComboLeg comboLeg;
                    for (int i = 0; i < contract.ComboLegs.Count; ++i)
                    {
                        comboLeg = (ComboLeg)contract.ComboLegs[i];
                        if (comboLeg.ExemptCode != -1)
                        {
                            ReportError(id, EClientErrors.UPDATE_TWS,
                                "  It does not support exemptCode parameter.");
                            return false;
                        }
                    }
                }
            }
            if (serverVersion < MinServerVer.TRADING_CLASS)
            {
                if (!IsEmpty(contract.TradingClass))
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support tradingClass parameters in placeOrder.");
                    return false;
                }
            }
            return true;
        }

        protected bool VerifyOrder(Order order, int id, bool isBagOrder)
        {
            if (serverVersion < MinServerVer.SCALE_ORDERS)
            {
                if (order.ScaleInitLevelSize != Int32.MaxValue ||
                    order.ScalePriceIncrement != Double.MaxValue)
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support Scale orders.");
                    return false;
                }
            }
            if (serverVersion < MinServerVer.WHAT_IF_ORDERS)
            {
                if (order.WhatIf)
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support what-if orders.");
                    return false;
                }
            }

            if (serverVersion < MinServerVer.SCALE_ORDERS2)
            {
                if (order.ScaleSubsLevelSize != Int32.MaxValue)
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support Subsequent Level Size for Scale orders.");
                    return false;
                }
            }

            if (serverVersion < MinServerVer.ALGO_ORDERS)
            {
                if (!IsEmpty(order.AlgoStrategy))
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support algo orders.");
                    return false;
                }
            }

            if (serverVersion < MinServerVer.NOT_HELD)
            {
                if (order.NotHeld)
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support notHeld parameter.");
                    return false;
                }
            }

            if (serverVersion < MinServerVer.SSHORTX)
            {
                if (order.ExemptCode != -1)
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support exemptCode parameter.");
                    return false;
                }
            }



            if (serverVersion < MinServerVer.HEDGE_ORDERS)
            {
                if (!IsEmpty(order.HedgeType))
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support hedge orders.");
                    return false;
                }
            }

            if (serverVersion < MinServerVer.OPT_OUT_SMART_ROUTING)
            {
                if (order.OptOutSmartRouting)
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support optOutSmartRouting parameter.");
                    return false;
                }
            }

            if (serverVersion < MinServerVer.DELTA_NEUTRAL_CONID)
            {
                if (order.DeltaNeutralConId > 0
                        || !IsEmpty(order.DeltaNeutralSettlingFirm)
                        || !IsEmpty(order.DeltaNeutralClearingAccount)
                        || !IsEmpty(order.DeltaNeutralClearingIntent))
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support deltaNeutral parameters: ConId, SettlingFirm, ClearingAccount, ClearingIntent");
                    return false;
                }
            }

            if (serverVersion < MinServerVer.DELTA_NEUTRAL_OPEN_CLOSE)
            {
                if (!IsEmpty(order.DeltaNeutralOpenClose)
                        || order.DeltaNeutralShortSale
                        || order.DeltaNeutralShortSaleSlot > 0
                        || !IsEmpty(order.DeltaNeutralDesignatedLocation)
                        )
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support deltaNeutral parameters: OpenClose, ShortSale, ShortSaleSlot, DesignatedLocation");
                    return false;
                }
            }

            if (serverVersion < MinServerVer.SCALE_ORDERS3)
            {
                if (order.ScalePriceIncrement > 0 && order.ScalePriceIncrement != Double.MaxValue)
                {
                    if (order.ScalePriceAdjustValue != Double.MaxValue ||
                        order.ScalePriceAdjustInterval != Int32.MaxValue ||
                        order.ScaleProfitOffset != Double.MaxValue ||
                        order.ScaleAutoReset ||
                        order.ScaleInitPosition != Int32.MaxValue ||
                        order.ScaleInitFillQty != Int32.MaxValue ||
                        order.ScaleRandomPercent)
                    {
                        ReportError(id, EClientErrors.UPDATE_TWS,
                            "  It does not support Scale order parameters: PriceAdjustValue, PriceAdjustInterval, " +
                            "ProfitOffset, AutoReset, InitPosition, InitFillQty and RandomPercent");
                        return false;
                    }
                }
            }

            if (serverVersion < MinServerVer.ORDER_COMBO_LEGS_PRICE && isBagOrder)
            {
                if (order.OrderComboLegs.Count > 0)
                {
                    OrderComboLeg orderComboLeg;
                    for (int i = 0; i < order.OrderComboLegs.Count; ++i)
                    {
                        orderComboLeg = (OrderComboLeg)order.OrderComboLegs[i];
                        if (orderComboLeg.Price != Double.MaxValue)
                        {
                            ReportError(id, EClientErrors.UPDATE_TWS,
                                "  It does not support per-leg prices for order combo legs.");
                            return false;
                        }
                    }
                }
            }

            if (serverVersion < MinServerVer.TRAILING_PERCENT)
            {
                if (order.TrailingPercent != Double.MaxValue)
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support trailing percent parameter.");
                    return false;
                }
            }

            if (serverVersion < MinServerVer.ALGO_ID && !IsEmpty(order.AlgoId))
            {
                ReportError(id, EClientErrors.UPDATE_TWS, " It does not support algoId parameter");

                return false;
            }

            if (serverVersion < MinServerVer.SCALE_TABLE)
            {
                if (!IsEmpty(order.ScaleTable) || !IsEmpty(order.ActiveStartTime) || !IsEmpty(order.ActiveStopTime))
                {
                    ReportError(id, EClientErrors.UPDATE_TWS,
                        "  It does not support scaleTable, activeStartTime nor activeStopTime parameters.");
                    return false;
                }
            }

            return true;
        }

        private bool IsEmpty(string str)
        {
            return Util.StringIsEmpty(str);
        }

        private bool StringsAreEqual(string a, string b)
        {
            return String.Compare(a, b, true) == 0;
        }

        private string TagValueListToString(List<TagValue> tagValues)
        {
            StringBuilder tagValuesStr = new StringBuilder();
            int tagValuesCount = tagValues == null ? 0 : tagValues.Count;

            for (int i = 0; i < tagValuesCount; i++)
            {
                TagValue tagValue = tagValues[i];
                tagValuesStr.Append(tagValue.Tag).Append("=").Append(tagValue.Value).Append(";");
            }
            return tagValuesStr.ToString();
        }

        public int ReadInt()
        {
            return IPAddress.NetworkToHostOrder(new BinaryReader(tcpStream).ReadInt32());
        }

        public byte[] ReadByteArray(int msgSize)
        {
            var buf = new byte[msgSize];

            return buf.Take(tcpStream.Read(buf, 0, msgSize)).ToArray();
        }

        public bool AsyncEConnect { get; set; }
    }
}
