/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

import java.util.ArrayList;
import java.util.Collections;
import java.util.Date;
import java.util.HashMap;
import java.util.Map.Entry;
import java.util.StringTokenizer;
import java.util.Vector;

import com.ib.client.CommissionReport;
import com.ib.client.Contract;
import com.ib.client.ContractDetails;
import com.ib.client.EWrapper;
import com.ib.client.Execution;
import com.ib.client.ExecutionFilter;
import com.ib.client.Order;
import com.ib.client.OrderState;
import com.ib.client.ScannerSubscription;
import com.ib.client.TagValue;
import com.ib.client.UnderComp;
import com.ib.controller.ApiConnection.ILogger;
import com.ib.controller.Types.BarSize;
import com.ib.controller.Types.DeepSide;
import com.ib.controller.Types.DeepType;
import com.ib.controller.Types.DurationUnit;
import com.ib.controller.Types.ExerciseType;
import com.ib.controller.Types.FADataType;
import com.ib.controller.Types.FundamentalType;
import com.ib.controller.Types.MktDataType;
import com.ib.controller.Types.NewsType;
import com.ib.controller.Types.WhatToShow;

public class ApiController implements EWrapper {
	private ApiConnection m_client;
	private final ILogger m_outLogger;
	private final ILogger m_inLogger;
	private int m_reqId;	// used for all requests except orders; designed not to conflict with m_orderId
	private int m_orderId;

	private final IConnectionHandler m_connectionHandler;
	private ITradeReportHandler m_tradeReportHandler;
	private IAdvisorHandler m_advisorHandler;
	private IScannerHandler m_scannerHandler;
	private ITimeHandler m_timeHandler;
	private IBulletinHandler m_bulletinHandler;
	private final HashMap<Integer,IInternalHandler> m_contractDetailsMap = new HashMap<Integer,IInternalHandler>();
	private final HashMap<Integer,IOptHandler> m_optionCompMap = new HashMap<Integer,IOptHandler>();
	private final HashMap<Integer,IEfpHandler> m_efpMap = new HashMap<Integer,IEfpHandler>();
	private final HashMap<Integer,ITopMktDataHandler> m_topMktDataMap = new HashMap<Integer,ITopMktDataHandler>();
	private final HashMap<Integer,IDeepMktDataHandler> m_deepMktDataMap = new HashMap<Integer,IDeepMktDataHandler>();
	private final HashMap<Integer, IScannerHandler> m_scannerMap = new HashMap<Integer, IScannerHandler>();
	private final HashMap<Integer, IRealTimeBarHandler> m_realTimeBarMap = new HashMap<Integer, IRealTimeBarHandler>();
	private final HashMap<Integer, IHistoricalDataHandler> m_historicalDataMap = new HashMap<Integer, IHistoricalDataHandler>();
	private final HashMap<Integer, IFundamentalsHandler> m_fundMap = new HashMap<Integer, IFundamentalsHandler>();
	private final HashMap<Integer, IOrderHandler> m_orderHandlers = new HashMap<Integer, IOrderHandler>();
	private final HashMap<Integer,IAccountSummaryHandler> m_acctSummaryHandlers = new HashMap<Integer,IAccountSummaryHandler>();
	private final HashMap<Integer,IMarketValueSummaryHandler> m_mktValSummaryHandlers = new HashMap<Integer,IMarketValueSummaryHandler>();
	private final ConcurrentHashSet<IPositionHandler> m_positionHandlers = new ConcurrentHashSet<IPositionHandler>();
	private final ConcurrentHashSet<IAccountHandler> m_accountHandlers = new ConcurrentHashSet<IAccountHandler>();
	private final ConcurrentHashSet<ILiveOrderHandler> m_liveOrderHandlers = new ConcurrentHashSet<ILiveOrderHandler>();

	// ---------------------------------------- Constructor and Connection handling ----------------------------------------
	public interface IConnectionHandler {
		void connected();
		void disconnected();
		void accountList(ArrayList<String> list);
		void error(Exception e);
		void message(int id, int errorCode, String errorMsg);
		void show(String string);
	}

	public ApiController( IConnectionHandler handler, ILogger inLogger, ILogger outLogger) {
		m_connectionHandler = handler;
		m_client = new ApiConnection( this, inLogger, outLogger);
		m_inLogger = inLogger;
		m_outLogger = outLogger;
	}

	public void connect( String host, int port, int clientId) {
		m_client.eConnect(host, port, clientId);
		sendEOM();
	}

	public void disconnect() {
		m_client.eDisconnect();
		m_connectionHandler.disconnected();
		sendEOM();
	}

	@Override public void managedAccounts(String accounts) {
		ArrayList<String> list = new ArrayList<String>();
		for( StringTokenizer st = new StringTokenizer( accounts, ","); st.hasMoreTokens(); ) {
			list.add( st.nextToken() );
		}
		m_connectionHandler.accountList( list);
		recEOM();
	}

	@Override public void nextValidId(int orderId) {
		m_orderId = orderId;
		m_reqId = m_orderId + 10000000; // let order id's not collide with other request id's
		if (m_connectionHandler != null) {
			m_connectionHandler.connected();
		}
		recEOM();
	}

	@Override public void error(Exception e) {
		m_connectionHandler.error( e);
	}

	@Override public void error(int id, int errorCode, String errorMsg) {
		IOrderHandler handler = m_orderHandlers.get( id);
		if (handler != null) {
			handler.handle( errorCode, errorMsg);
		}

		for (ILiveOrderHandler liveHandler : m_liveOrderHandlers) {
			liveHandler.handle( id, errorCode, errorMsg);
		}

		// "no sec def found" response?
		if (errorCode == 200) {
			IInternalHandler hand = m_contractDetailsMap.remove( id);
			if (hand != null) {
				hand.contractDetailsEnd();
			}
		}

		m_connectionHandler.message( id, errorCode, errorMsg);
		recEOM();
	}

	@Override public void connectionClosed() {
		m_connectionHandler.disconnected();
	}


	// ---------------------------------------- Account and portfolio updates ----------------------------------------
	public interface IAccountHandler {
		public void accountValue(String account, String key, String value, String currency);
		public void accountTime(String timeStamp);
		public void accountDownloadEnd(String account);
		public void updatePortfolio(Position position);
	}

    public void reqAccountUpdates(boolean subscribe, String acctCode, IAccountHandler handler) {
    	m_accountHandlers.add( handler);
    	m_client.reqAccountUpdates(subscribe, acctCode);
		sendEOM();
    }

	@Override public void updateAccountValue(String tag, String value, String currency, String account) {
		if (tag.equals( "Currency") ) { // ignore this, it is useless
			return;
		}

		for( IAccountHandler handler : m_accountHandlers) {
			handler.accountValue( account, tag, value, currency);
		}
		recEOM();
	}

	@Override public void updateAccountTime(String timeStamp) {
		for( IAccountHandler handler : m_accountHandlers) {
			handler.accountTime( timeStamp);
		}
		recEOM();
	}

	@Override public void accountDownloadEnd(String account) {
		for( IAccountHandler handler : m_accountHandlers) {
			handler.accountDownloadEnd( account);
		}
		recEOM();
	}

	@Override public void updatePortfolio(Contract contractIn, int positionIn, double marketPrice, double marketValue, double averageCost, double unrealizedPNL, double realizedPNL, String account) {
		NewContract contract = new NewContract( contractIn);
		contract.exchange( contractIn.m_primaryExch);

		Position position = new Position( contract, account, positionIn, marketPrice, marketValue, averageCost, unrealizedPNL, realizedPNL);
		for( IAccountHandler handler : m_accountHandlers) {
			handler.updatePortfolio( position);
		}
		recEOM();
	}

	// ---------------------------------------- Account Summary handling ----------------------------------------
	public interface IAccountSummaryHandler {
		void accountSummary( String account, AccountSummaryTag tag, String value, String currency);
		void accountSummaryEnd();
	}

	public interface IMarketValueSummaryHandler {
		void marketValueSummary( String account, MarketValueTag tag, String value, String currency);
		void marketValueSummaryEnd();
	}

	/** @param group pass "All" to get data for all accounts */
	public void reqAccountSummary(String group, AccountSummaryTag[] tags, IAccountSummaryHandler handler) {
		StringBuilder sb = new StringBuilder();
		for (AccountSummaryTag tag : tags) {
			if (sb.length() > 0) {
				sb.append( ',');
			}
			sb.append( tag);
		}

		int reqId = m_reqId++;
		m_acctSummaryHandlers.put( reqId, handler);
		m_client.reqAccountSummary( reqId, group, sb.toString() );
		sendEOM();
	}

	public void cancelAccountSummary(IAccountSummaryHandler handler) {
		Integer reqId = getAndRemoveKey( m_acctSummaryHandlers, handler);
		if (reqId != null) {
			m_client.cancelAccountSummary( reqId);
			sendEOM();
		}
	}

	public void reqMarketValueSummary(String group, IMarketValueSummaryHandler handler) {
		int reqId = m_reqId++;
		m_mktValSummaryHandlers.put( reqId, handler);
		m_client.reqAccountSummary( reqId, group, "$LEDGER");
		sendEOM();
	}

	public void cancelMarketValueSummary(IMarketValueSummaryHandler handler) {
		Integer reqId = getAndRemoveKey( m_mktValSummaryHandlers, handler);
		if (reqId != null) {
			m_client.cancelAccountSummary( reqId);
			sendEOM();
		}
	}

	@Override public void accountSummary( int reqId, String account, String tag, String value, String currency) {
		if (tag.equals( "Currency") ) { // ignore this, it is useless
			return;
		}

		IAccountSummaryHandler handler = m_acctSummaryHandlers.get( reqId);
		if (handler != null) {
			handler.accountSummary(account, AccountSummaryTag.valueOf( tag), value, currency);
		}

		IMarketValueSummaryHandler handler2 = m_mktValSummaryHandlers.get( reqId);
		if (handler2 != null) {
			handler2.marketValueSummary(account, MarketValueTag.valueOf( tag), value, currency);
		}

		recEOM();
	}

	@Override public void accountSummaryEnd( int reqId) {
		IAccountSummaryHandler handler = m_acctSummaryHandlers.get( reqId);
		if (handler != null) {
			handler.accountSummaryEnd();
		}

		IMarketValueSummaryHandler handler2 = m_mktValSummaryHandlers.get( reqId);
		if (handler2 != null) {
			handler2.marketValueSummaryEnd();
		}

		recEOM();
	}

	// ---------------------------------------- Position handling ----------------------------------------
	public interface IPositionHandler {
		void position( String account, NewContract contract, int position, double avgCost);
		void positionEnd();
	}

	public void reqPositions( IPositionHandler handler) {
		m_positionHandlers.add( handler);
		m_client.reqPositions();
		sendEOM();
	}

	public void cancelPositions( IPositionHandler handler) {
		m_positionHandlers.remove( handler);
		m_client.cancelPositions();
		sendEOM();
	}

	@Override public void position(String account, Contract contractIn, int pos, double avgCost) {
		NewContract contract = new NewContract( contractIn);
		for (IPositionHandler handler : m_positionHandlers) {
			handler.position( account, contract, pos, avgCost);
		}
		recEOM();
	}

	@Override public void positionEnd() {
		for (IPositionHandler handler : m_positionHandlers) {
			handler.positionEnd();
		}
		recEOM();
	}

	// ---------------------------------------- Contract Details ----------------------------------------
	public interface IContractDetailsHandler {
		void contractDetails(ArrayList<NewContractDetails> list);
	}

	public void reqContractDetails( NewContract contract, final IContractDetailsHandler processor) {
		final ArrayList<NewContractDetails> list = new ArrayList<NewContractDetails>();
		internalReqContractDetails( contract, new IInternalHandler() {
			@Override public void contractDetails(NewContractDetails data) {
				list.add( data);
			}
			@Override public void contractDetailsEnd() {
				processor.contractDetails( list);
			}
		});
		sendEOM();
	}

	private interface IInternalHandler {
		void contractDetails(NewContractDetails data);
		void contractDetailsEnd();
	}

	private void internalReqContractDetails( NewContract contract, IInternalHandler processor) {
		int reqId = m_reqId++;
		m_contractDetailsMap.put( reqId, processor);
		m_client.reqContractDetails(reqId, contract.getContract() );
		sendEOM();
	}

	@Override public void contractDetails(int reqId, ContractDetails contractDetails) {
		IInternalHandler handler = m_contractDetailsMap.get( reqId);
		if (handler != null) {
			handler.contractDetails( new NewContractDetails( contractDetails) );
		}
		else {
			show( "Error: no contract details handler for reqId " + reqId);
		}
		recEOM();
	}

	@Override public void bondContractDetails(int reqId, ContractDetails contractDetails) {
		IInternalHandler handler = m_contractDetailsMap.get( reqId);
		if (handler != null) {
			handler.contractDetails(new NewContractDetails( contractDetails) );
		}
		else {
			show( "Error: no bond contract details handler for reqId " + reqId);
		}
		recEOM();
	}

	@Override public void contractDetailsEnd(int reqId) {
		IInternalHandler handler = m_contractDetailsMap.remove( reqId);
		if (handler != null) {
			handler.contractDetailsEnd();
		}
		else {
			show( "Error: no contract details handler for reqId " + reqId);
		}
		recEOM();
	}

	// ---------------------------------------- Top Market Data handling ----------------------------------------
	public interface ITopMktDataHandler {
		void tickPrice(NewTickType tickType, double price, int canAutoExecute);
		void tickSize(NewTickType tickType, int size);
		void tickString(NewTickType tickType, String value);
		void tickSnapshotEnd();
		void marketDataType(MktDataType marketDataType);
	}

	public interface IEfpHandler extends ITopMktDataHandler {
		void tickEFP(int tickType, double basisPoints, String formattedBasisPoints, double impliedFuture, int holdDays, String futureExpiry, double dividendImpact, double dividendsToExpiry);
	}

	public interface IOptHandler extends ITopMktDataHandler {
		void tickOptionComputation( NewTickType tickType, double impliedVol, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice);
	}

	public static class TopMktDataAdapter implements ITopMktDataHandler {
		@Override public void tickPrice(NewTickType tickType, double price, int canAutoExecute) {
		}
		@Override public void tickSize(NewTickType tickType, int size) {
		}
		@Override public void tickString(NewTickType tickType, String value) {
		}
		@Override public void tickSnapshotEnd() {
		}
		@Override public void marketDataType(MktDataType marketDataType) {
		}
	}

    public void reqTopMktData(NewContract contract, String genericTickList, boolean snapshot, ITopMktDataHandler handler) {
    	int reqId = m_reqId++;
    	m_topMktDataMap.put( reqId, handler);
    	m_client.reqMktData( reqId, contract.getContract(), genericTickList, snapshot, Collections.<TagValue>emptyList() );
		sendEOM();
    }

    public void reqOptionMktData(NewContract contract, String genericTickList, boolean snapshot, IOptHandler handler) {
    	int reqId = m_reqId++;
    	m_topMktDataMap.put( reqId, handler);
    	m_optionCompMap.put( reqId, handler);
    	m_client.reqMktData( reqId, contract.getContract(), genericTickList, snapshot, Collections.<TagValue>emptyList() );
		sendEOM();
    }

    public void reqEfpMktData(NewContract contract, String genericTickList, boolean snapshot, IEfpHandler handler) {
    	int reqId = m_reqId++;
    	m_topMktDataMap.put( reqId, handler);
    	m_efpMap.put( reqId, handler);
    	m_client.reqMktData( reqId, contract.getContract(), genericTickList, snapshot, Collections.<TagValue>emptyList() );
		sendEOM();
    }

    public void cancelTopMktData( ITopMktDataHandler handler) {
    	Integer reqId = getAndRemoveKey( m_topMktDataMap, handler);
    	if (reqId != null) {
    		m_client.cancelMktData( reqId);
    	}
    	else {
    		show( "Error: could not cancel top market data");
    	}
		sendEOM();
    }

    public void cancelOptionMktData( IOptHandler handler) {
    	cancelTopMktData( handler);
    	getAndRemoveKey( m_optionCompMap, handler);
    }

    public void cancelEfpMktData( IEfpHandler handler) {
    	cancelTopMktData( handler);
    	getAndRemoveKey( m_efpMap, handler);
    }

	public void reqMktDataType( MktDataType type) {
		m_client.reqMarketDataType( type.ordinal() );
		sendEOM();
	}

	@Override public void tickPrice(int reqId, int tickType, double price, int canAutoExecute) {
		ITopMktDataHandler handler = m_topMktDataMap.get( reqId);
		if (handler != null) {
			handler.tickPrice( NewTickType.get( tickType), price, canAutoExecute);
		}
		recEOM();
	}

	@Override public void tickGeneric(int reqId, int tickType, double value) {
		ITopMktDataHandler handler = m_topMktDataMap.get( reqId);
		if (handler != null) {
			handler.tickPrice( NewTickType.get( tickType), value, 0);
		}
		recEOM();
	}

	@Override public void tickSize(int reqId, int tickType, int size) {
		ITopMktDataHandler handler = m_topMktDataMap.get( reqId);
		if (handler != null) {
			handler.tickSize( NewTickType.get( tickType), size);
		}
		recEOM();
	}

	@Override public void tickString(int reqId, int tickType, String value) {
		ITopMktDataHandler handler = m_topMktDataMap.get( reqId);
		if (handler != null) {
			handler.tickString( NewTickType.get( tickType), value);
		}
		recEOM();
	}

	@Override public void tickEFP(int reqId, int tickType, double basisPoints, String formattedBasisPoints, double impliedFuture, int holdDays, String futureExpiry, double dividendImpact, double dividendsToExpiry) {
		IEfpHandler handler = m_efpMap.get( reqId);
		if (handler != null) {
			handler.tickEFP( tickType, basisPoints, formattedBasisPoints, impliedFuture, holdDays, futureExpiry, dividendImpact, dividendsToExpiry);
		}
		recEOM();
	}

	@Override public void tickSnapshotEnd(int reqId) {
		ITopMktDataHandler handler = m_topMktDataMap.get( reqId);
		if (handler != null) {
			handler.tickSnapshotEnd();
		}
		recEOM();
	}

	@Override public void marketDataType(int reqId, int marketDataType) {
		ITopMktDataHandler handler = m_topMktDataMap.get( reqId);
		if (handler != null) {
			handler.marketDataType( MktDataType.get( marketDataType) );
		}
		recEOM();
	}


	// ---------------------------------------- Deep Market Data handling ----------------------------------------
	public interface IDeepMktDataHandler {
		void updateMktDepth(int position, String marketMaker, DeepType operation, DeepSide side, double price, int size);
	}

    public void reqDeepMktData( NewContract contract, int numRows, IDeepMktDataHandler handler) {
    	int reqId = m_reqId++;
    	m_deepMktDataMap.put( reqId, handler);
    	Vector<TagValue> mktDepthOptions = new Vector<TagValue>();
    	m_client.reqMktDepth( reqId, contract.getContract(), numRows, mktDepthOptions);
		sendEOM();
    }

    public void cancelDeepMktData( IDeepMktDataHandler handler) {
    	Integer reqId = getAndRemoveKey( m_deepMktDataMap, handler);
    	if (reqId != null) {
    		m_client.cancelMktDepth( reqId);
    		sendEOM();
    	}
    }

	@Override public void updateMktDepth(int reqId, int position, int operation, int side, double price, int size) {
		IDeepMktDataHandler handler = m_deepMktDataMap.get( reqId);
		if (handler != null) {
			handler.updateMktDepth( position, null, DeepType.get( operation), DeepSide.get( side), price, size);
		}
		recEOM();
	}

	@Override public void updateMktDepthL2(int reqId, int position, String marketMaker, int operation, int side, double price, int size) {
		IDeepMktDataHandler handler = m_deepMktDataMap.get( reqId);
		if (handler != null) {
			handler.updateMktDepth( position, marketMaker, DeepType.get( operation), DeepSide.get( side), price, size);
		}
		recEOM();
	}

	// ---------------------------------------- Option computations ----------------------------------------
	public void reqOptionVolatility(NewContract c, double optPrice, double underPrice, IOptHandler handler) {
		int reqId = m_reqId++;
		m_optionCompMap.put( reqId, handler);
		m_client.calculateImpliedVolatility( reqId, c.getContract(), optPrice, underPrice);
		sendEOM();
	}

	public void reqOptionComputation( NewContract c, double vol, double underPrice, IOptHandler handler) {
		int reqId = m_reqId++;
		m_optionCompMap.put( reqId, handler);
		m_client.calculateOptionPrice(reqId, c.getContract(), vol, underPrice);
		sendEOM();
	}

	void cancelOptionComp( IOptHandler handler) {
		Integer reqId = getAndRemoveKey( m_optionCompMap, handler);
		if (reqId != null) {
			m_client.cancelCalculateOptionPrice( reqId);
			sendEOM();
		}
	}

	@Override public void tickOptionComputation(int reqId, int tickType, double impliedVol, double delta, double optPrice, double pvDividend, double gamma, double vega, double theta, double undPrice) {
		IOptHandler handler = m_optionCompMap.get( reqId);
		if (handler != null) {
			handler.tickOptionComputation( NewTickType.get( tickType), impliedVol, delta, optPrice, pvDividend, gamma, vega, theta, undPrice);
		}
		else {
			System.out.println( String.format( "not handled %s %s %s %s %s %s %s %s %s", tickType, impliedVol, delta, optPrice, pvDividend, gamma, vega, theta, undPrice) );
		}
		recEOM();
	}


	// ---------------------------------------- Trade reports ----------------------------------------
	public interface ITradeReportHandler {
		void tradeReport(String tradeKey, NewContract contract, Execution execution);
		void tradeReportEnd();
		void commissionReport(String tradeKey, CommissionReport commissionReport);
	}

    public void reqExecutions( ExecutionFilter filter, ITradeReportHandler handler) {
    	m_tradeReportHandler = handler;
    	m_client.reqExecutions( m_reqId++, filter);
		sendEOM();
    }

	@Override public void execDetails(int reqId, Contract contract, Execution execution) {
		if (m_tradeReportHandler != null) {
			int i = execution.m_execId.lastIndexOf( '.');
			String tradeKey = execution.m_execId.substring( 0, i);
			m_tradeReportHandler.tradeReport( tradeKey, new NewContract( contract), execution);
		}
		recEOM();
	}

	@Override public void execDetailsEnd(int reqId) {
		if (m_tradeReportHandler != null) {
			m_tradeReportHandler.tradeReportEnd();
		}
		recEOM();
	}

	@Override public void commissionReport(CommissionReport commissionReport) {
		if (m_tradeReportHandler != null) {
			int i = commissionReport.m_execId.lastIndexOf( '.');
			String tradeKey = commissionReport.m_execId.substring( 0, i);
			m_tradeReportHandler.commissionReport( tradeKey, commissionReport);
		}
		recEOM();
	}

	// ---------------------------------------- Advisor info ----------------------------------------
	public interface IAdvisorHandler {
		void groups(ArrayList<Group> groups);
		void profiles(ArrayList<Profile> profiles);
		void aliases(ArrayList<Alias> aliases);
	}

	public void reqAdvisorData( FADataType type, IAdvisorHandler handler) {
		m_advisorHandler = handler;
		m_client.requestFA( type.ordinal() );
		sendEOM();
	}

	public void updateGroups( ArrayList<Group> groups) {
		m_client.replaceFA( FADataType.GROUPS.ordinal(), AdvisorUtil.getGroupsXml( groups) );
		sendEOM();
	}

	public void updateProfiles(ArrayList<Profile> profiles) {
		m_client.replaceFA( FADataType.PROFILES.ordinal(), AdvisorUtil.getProfilesXml( profiles) );
		sendEOM();
	}

	@Override public final void receiveFA(int faDataType, String xml) {
		if (m_advisorHandler == null) {
			return;
		}

		FADataType type = FADataType.get( faDataType);

		switch( type) {
			case GROUPS:
				ArrayList<Group> groups = AdvisorUtil.getGroups( xml);
				m_advisorHandler.groups(groups);
				break;

			case PROFILES:
				ArrayList<Profile> profiles = AdvisorUtil.getProfiles( xml);
				m_advisorHandler.profiles(profiles);
				break;

			case ALIASES:
				ArrayList<Alias> aliases = AdvisorUtil.getAliases( xml);
				m_advisorHandler.aliases(aliases);
				break;
		}
		recEOM();
	}

	// ---------------------------------------- Trading and Option Exercise ----------------------------------------
	/** This interface is for receiving events for a specific order placed from the API.
	 *  Compare to ILiveOrderHandler. */
	public interface IOrderHandler {
		void orderState(NewOrderState orderState);
		void orderStatus(OrderStatus status, int filled, int remaining, double avgFillPrice, long permId, int parentId, double lastFillPrice, int clientId, String whyHeld);
		void handle(int errorCode, String errorMsg);
	}

	public void placeOrModifyOrder(NewContract contract, final NewOrder order, final IOrderHandler handler) {
		// when placing new order, assign new order id
		if (order.orderId() == 0) {
			order.orderId( m_orderId++);
			if (handler != null) {
				m_orderHandlers.put( order.orderId(), handler);
			}
		}

		m_client.placeOrder( contract, order);
		sendEOM();
	}

	public void cancelOrder(int orderId) {
		m_client.cancelOrder( orderId);
		sendEOM();
	}

	public void cancelAllOrders() {
		m_client.reqGlobalCancel();
		sendEOM();
	}

	public void exerciseOption( String account, NewContract contract, ExerciseType type, int quantity, boolean override) {
		m_client.exerciseOptions( m_reqId++, contract.getContract(), type.ordinal(), quantity, account, override ? 1 : 0);
		sendEOM();
	}

	public void removeOrderHandler( IOrderHandler handler) {
		getAndRemoveKey(m_orderHandlers, handler);
	}


	// ---------------------------------------- Live order handling ----------------------------------------
	/** This interface is for downloading and receiving events for all live orders.
	 *  Compare to IOrderHandler. */
	public interface ILiveOrderHandler {
		void openOrder(NewContract contract, NewOrder order, NewOrderState orderState);
		void openOrderEnd();
		void orderStatus(int orderId, OrderStatus status, int filled, int remaining, double avgFillPrice, long permId, int parentId, double lastFillPrice, int clientId, String whyHeld);
		void handle(int orderId, int errorCode, String errorMsg);  // add permId?
	}

	public void reqLiveOrders( ILiveOrderHandler handler) {
		m_liveOrderHandlers.add( handler);
		m_client.reqAllOpenOrders();
		sendEOM();
	}

	public void takeTwsOrders( ILiveOrderHandler handler) {
		m_liveOrderHandlers.add( handler);
		m_client.reqOpenOrders();
		sendEOM();
	}

	public void takeFutureTwsOrders( ILiveOrderHandler handler) {
		m_liveOrderHandlers.add( handler);
		m_client.reqAutoOpenOrders( true);
		sendEOM();
	}

	public void removeLiveOrderHandler(ILiveOrderHandler handler) {
		m_liveOrderHandlers.remove( handler);
	}

	@Override public void openOrder(int orderId, Contract contract, Order orderIn, OrderState orderState) {
		NewOrder order = new NewOrder( orderIn);

		IOrderHandler handler = m_orderHandlers.get( orderId);
		if (handler != null) {
			handler.orderState( new NewOrderState( orderState) );
		}

		if (!order.whatIf() ) {
			for (ILiveOrderHandler liveHandler : m_liveOrderHandlers) {
				liveHandler.openOrder( new NewContract( contract), order, new NewOrderState( orderState) );
			}
		}
		recEOM();
	}

	@Override public void openOrderEnd() {
		for (ILiveOrderHandler handler : m_liveOrderHandlers) {
			handler.openOrderEnd();
		}
		recEOM();
	}

	@Override public void orderStatus(int orderId, String status, int filled, int remaining, double avgFillPrice, int permId, int parentId, double lastFillPrice, int clientId, String whyHeld) {
		IOrderHandler handler = m_orderHandlers.get( orderId);
		if (handler != null) {
			handler.orderStatus( OrderStatus.valueOf( status), filled, remaining, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld);
		}

		for (ILiveOrderHandler liveOrderHandler : m_liveOrderHandlers) {
			liveOrderHandler.orderStatus(orderId, OrderStatus.valueOf( status), filled, remaining, avgFillPrice, permId, parentId, lastFillPrice, clientId, whyHeld);
		}
		recEOM();
	}


	// ---------------------------------------- Market Scanners ----------------------------------------
	public interface IScannerHandler {
		void scannerParameters(String xml);
		void scannerData( int rank, NewContractDetails contractDetails, String legsStr);
		void scannerDataEnd();
	}

	public void reqScannerParameters( IScannerHandler handler) {
		m_scannerHandler = handler;
		m_client.reqScannerParameters();
		sendEOM();
	}

	public void reqScannerSubscription( ScannerSubscription sub, IScannerHandler handler) {
		int reqId = m_reqId++;
		m_scannerMap.put( reqId, handler);
		Vector<TagValue> scannerSubscriptionOptions = new Vector<TagValue>();
		m_client.reqScannerSubscription( reqId, sub, scannerSubscriptionOptions);
		sendEOM();
	}

	public void cancelScannerSubscription( IScannerHandler handler) {
		Integer reqId = getAndRemoveKey( m_scannerMap, handler);
		if (reqId != null) {
			m_client.cancelScannerSubscription( reqId);
			sendEOM();
		}
	}

	@Override public void scannerParameters(String xml) {
		m_scannerHandler.scannerParameters( xml);
		recEOM();
	}

	@Override public void scannerData(int reqId, int rank, ContractDetails contractDetails, String distance, String benchmark, String projection, String legsStr) {
		IScannerHandler handler = m_scannerMap.get( reqId);
		if (handler != null) {
			handler.scannerData( rank, new NewContractDetails( contractDetails), legsStr);
		}
		recEOM();
	}

	@Override public void scannerDataEnd(int reqId) {
		IScannerHandler handler = m_scannerMap.get( reqId);
		if (handler != null) {
			handler.scannerDataEnd();
		}
		recEOM();
	}


	// ----------------------------------------- Historical data handling ----------------------------------------
	public interface IHistoricalDataHandler {
		void historicalData(Bar bar, boolean hasGaps);
		void historicalDataEnd();
	}

	/** @param endDateTime format is YYYYMMDD HH:MM:SS [TMZ]
	 *  @param duration is number of durationUnits */
    public void reqHistoricalData( NewContract contract, String endDateTime, int duration, DurationUnit durationUnit, BarSize barSize, WhatToShow whatToShow, boolean rthOnly, IHistoricalDataHandler handler) {
    	int reqId = m_reqId++;
    	m_historicalDataMap.put( reqId, handler);
    	String durationStr = duration + " " + durationUnit.toString().charAt( 0);
    	m_client.reqHistoricalData(reqId, contract.getContract(), endDateTime, durationStr, barSize.toString(), whatToShow.toString(), rthOnly ? 1 : 0, 2, Collections.<TagValue>emptyList() );
		sendEOM();
    }

    public void cancelHistoricalData( IHistoricalDataHandler handler) {
    	Integer reqId = getAndRemoveKey( m_historicalDataMap, handler);
    	if (reqId != null) {
    		m_client.cancelHistoricalData( reqId);
    		sendEOM();
    	}
    }

	@Override public void historicalData(int reqId, String date, double open, double high, double low, double close, int volume, int count, double wap, boolean hasGaps) {
		IHistoricalDataHandler handler = m_historicalDataMap.get( reqId);
		if (handler != null) {
			if (date.startsWith( "finished")) {
				handler.historicalDataEnd();
			}
			else {
				long longDate;
				if (date.length() == 8) {
					int year = Integer.parseInt( date.substring( 0, 4) );
					int month = Integer.parseInt( date.substring( 4, 6) );
					int day = Integer.parseInt( date.substring( 6) );
					longDate = new Date( year - 1900, month - 1, day).getTime() / 1000;
				}
				else {
					longDate = Long.parseLong( date);
				}
				Bar bar = new Bar( longDate, high, low, open, close, wap, volume, count);
				handler.historicalData(bar, hasGaps);
			}
		}
		recEOM();
	}


	//----------------------------------------- Real-time bars --------------------------------------
	public interface IRealTimeBarHandler {
		void realtimeBar(Bar bar); // time is in seconds since epoch
	}

    public void reqRealTimeBars(NewContract contract, WhatToShow whatToShow, boolean rthOnly, IRealTimeBarHandler handler) {
    	int reqId = m_reqId++;
    	m_realTimeBarMap.put( reqId, handler);
    	Vector<TagValue> realTimeBarsOptions = new Vector<TagValue>();
    	m_client.reqRealTimeBars(reqId, contract.getContract(), 0, whatToShow.toString(), rthOnly, realTimeBarsOptions);
		sendEOM();
    }

    public void cancelRealtimeBars( IRealTimeBarHandler handler) {
    	Integer reqId = getAndRemoveKey( m_realTimeBarMap, handler);
    	if (reqId != null) {
    		m_client.cancelRealTimeBars( reqId);
    		sendEOM();
    	}
    }

    @Override public void realtimeBar(int reqId, long time, double open, double high, double low, double close, long volume, double wap, int count) {
    	IRealTimeBarHandler handler = m_realTimeBarMap.get( reqId);
		if (handler != null) {
			Bar bar = new Bar( time, high, low, open, close, wap, volume, count);
			handler.realtimeBar( bar);
		}
		recEOM();
	}


    // ----------------------------------------- Fundamentals handling ----------------------------------------
	public interface IFundamentalsHandler {
		void fundamentals( String str);
	}

    public void reqFundamentals( NewContract contract, FundamentalType reportType, IFundamentalsHandler handler) {
    	int reqId = m_reqId++;
    	m_fundMap.put( reqId, handler);
    	m_client.reqFundamentalData( reqId, contract.getContract(), reportType.getApiString());
		sendEOM();
    }

    @Override public void fundamentalData(int reqId, String data) {
		IFundamentalsHandler handler = m_fundMap.get( reqId);
		if (handler != null) {
			handler.fundamentals( data);
		}
		recEOM();
	}


	// ---------------------------------------- Time handling ----------------------------------------
	public interface ITimeHandler {
		void currentTime(long time);
	}

	public void reqCurrentTime( ITimeHandler handler) {
		m_timeHandler = handler;
		m_client.reqCurrentTime();
		sendEOM();
	}

	@Override public void currentTime(long time) {
		m_timeHandler.currentTime(time);
		recEOM();
	}


	// ---------------------------------------- Bulletins handling ----------------------------------------
	public interface IBulletinHandler {
		void bulletin(int msgId, NewsType newsType, String message, String exchange);
	}

	public void reqBulletins( boolean allMessages, IBulletinHandler handler) {
		m_bulletinHandler = handler;
		m_client.reqNewsBulletins( allMessages);
		sendEOM();
	}

	public void cancelBulletins() {
		m_client.cancelNewsBulletins();
	}

	@Override public void updateNewsBulletin(int msgId, int msgType, String message, String origExchange) {
		m_bulletinHandler.bulletin( msgId, NewsType.get( msgType), message, origExchange);
		recEOM();
	}

	@Override public void verifyMessageAPI( String apiData) {}
	@Override public void verifyCompleted( boolean isSuccessful, String errorText) {}
	@Override public void displayGroupList(int reqId, String groups) {}
	@Override public void displayGroupUpdated(int reqId, String contractInfo) {}

	// ---------------------------------------- other methods ----------------------------------------
	/** Not supported in ApiController. */
	@Override public void deltaNeutralValidation(int reqId, UnderComp underComp) {
		show( "RECEIVED DN VALIDATION");
		recEOM();
	}

	private void sendEOM() {
		m_outLogger.log( "\n");
	}

	private void recEOM() {
		m_inLogger.log( "\n");
	}

	public void show(String string) {
		m_connectionHandler.show( string);
	}

    private static <K,V> K getAndRemoveKey( HashMap<K,V> map, V value) {
    	for (Entry<K,V> entry : map.entrySet() ) {
    		if (entry.getValue() == value) {
    			map.remove( entry.getKey() );
    			return entry.getKey();
    		}
    	}
		return null;
    }

	/** Obsolete, never called. */
	@Override public void error(String str) {
		throw new RuntimeException();
	}
}
