/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;


import java.io.DataInputStream;
import java.io.FilterInputStream;
import java.io.FilterOutputStream;
import java.io.IOException;
import java.io.InputStream;
import java.io.OutputStream;
import java.lang.reflect.Field;
import java.net.Socket;

import com.ib.client.AnyWrapper;
import com.ib.client.Builder;
import com.ib.client.EClientErrors;
import com.ib.client.EClientSocket;
import com.ib.client.EReader;
import com.ib.client.TagValue;
import com.ib.controller.Types.AlgoStrategy;
import com.ib.controller.Types.HedgeType;
import com.ib.controller.Types.SecType;

// NOTE: TWS 936 SERVER_VERSION is 67.

public class ApiConnection extends EClientSocket {
	public interface ILogger {
		void log(String valueOf);
	}

	public static final char EOL = 0;
	public static final char LOG_EOL = '_';

	private final ILogger m_inLogger;
	private final ILogger m_outLogger;

	public ApiConnection(AnyWrapper wrapper, ILogger inLogger, ILogger outLogger) {
		super( wrapper);
		m_inLogger = inLogger;
		m_outLogger = outLogger;
	}

	@Override public synchronized void eConnect(Socket socket, int clientId) throws IOException {
		super.eConnect(socket, clientId);

		// replace the output stream with one that logs all data to m_outLogger
		if (isConnected()) {
			try {
				Field realOsField = FilterOutputStream.class.getDeclaredField( "out");
				realOsField.setAccessible( true);
				OutputStream realOs = (OutputStream)realOsField.get( m_dos);
				realOsField.set( m_dos, new MyOS( realOs) );
			}
			catch( Exception e) {
				e.printStackTrace();
			}
		}
	}

	/** Replace the input stream with ont that logs all data to m_inLogger. */
	@Override public EReader createReader(EClientSocket socket, DataInputStream dis) {
		try {
			Field realIsField = FilterInputStream.class.getDeclaredField( "in");
			realIsField.setAccessible( true);
			InputStream realIs = (InputStream)realIsField.get( dis);
			realIsField.set( dis, new MyIS( realIs) );
		}
		catch( Exception e) {
			e.printStackTrace();
		}
		return super.createReader(socket, dis);
	}

	public synchronized void placeOrder(NewContract contract, NewOrder order) {
		// not connected?
		if( !isConnected() ) {
            notConnected();
			return;
		}

		// ApiController requires TWS 932 or higher; this limitation could be removed if needed
		if (serverVersion() < 66) {
            error( EClientErrors.NO_VALID_ID, EClientErrors.UPDATE_TWS, "ApiController requires TWS build 932 or higher to place orders.");
            return;
		}

		Builder b = new Builder();

		int VERSION = 43;

		// send place order msg
		try {
			b.send( PLACE_ORDER);
			b.send( VERSION);
			b.send( order.orderId() );
			b.send( contract.conid() );
			b.send( contract.symbol());
			b.send( contract.secType() );
			b.send( contract.expiry());
			b.send( contract.strike());
			b.send( contract.right().getApiString() );
			b.send( contract.multiplier() );
			b.send( contract.exchange() );
			b.send( contract.primaryExch() );
			b.send( contract.currency() );
			b.send( contract.localSymbol() );
            if (m_serverVersion >= MIN_SERVER_VER_TRADING_CLASS) {
                b.send(contract.tradingClass() );
            }
			b.send( contract.secIdType() );
			b.send( contract.secId() );
			b.send( order.action() );
			b.send( order.totalQuantity() );
			b.send( order.orderType() );
			b.send( order.lmtPrice() );
			b.send( order.auxPrice() );
			b.send( order.tif() );
			b.send( order.ocaGroup() );
			b.send( order.account() );
			b.send( ""); // open/close
			b.send( ""); // origin
			b.send( order.orderRef() );
			b.send( order.transmit() );
			b.send( order.parentId() );
			b.send( order.blockOrder() );
			b.send( order.sweepToFill() );
			b.send( order.displaySize() );
			b.send( order.triggerMethod() );
			b.send( order.outsideRth() );
			b.send( order.hidden() );

			// send combo legs for BAG orders
			if(contract.secType() == SecType.BAG) {
				b.send( contract.comboLegs().size());

				for (NewComboLeg leg : contract.comboLegs() ) {
					b.send( leg.conid() );
					b.send( leg.ratio() );
					b.send( leg.action().getApiString() );
					b.send( leg.exchange() );
					b.send( leg.openClose().getApiString() );
					b.send( leg.shortSaleSlot() );
					b.send( leg.designatedLocation() );
					b.send( leg.exemptCode() );
				}

				b.send( order.orderComboLegs().size());
				for (Double orderComboLeg : order.orderComboLegs() ) {
					b.send( orderComboLeg);
				}

				b.send( order.smartComboRoutingParams().size() );
				for (TagValue tagValue : order.smartComboRoutingParams() ) {
					b.send( tagValue.m_tag);
					b.send( tagValue.m_value);
				}
			}

			b.send( ""); // obsolete field
			b.send( order.discretionaryAmt() );
			b.send( order.goodAfterTime() );
			b.send( order.goodTillDate() );
			b.send( order.faGroup());
			b.send( order.faMethod() );
			b.send( order.faPercentage() );
			b.send( order.faProfile());
			b.send( 0); // short sale slot
			b.send( ""); // designatedLocation
			b.send( ""); // exemptCode
			b.send( order.ocaType() );
			b.send( order.rule80A() );
			b.send( ""); // settlingFirm
			b.send( order.allOrNone() );
			b.send( order.minQty() );
			b.send( order.percentOffset() );
			b.send( order.eTradeOnly() );
			b.send( order.firmQuoteOnly() );
			b.send( order.nbboPriceCap() );
			b.send( order.auctionStrategy() );
			b.send( order.startingPrice() );
			b.send( order.stockRefPrice() );
			b.send( order.delta() );
			b.send( order.stockRangeLower() );
			b.send( order.stockRangeUpper() );
			b.send( order.overridePercentageConstraints() );
			b.send( order.volatility() );
			b.send( order.volatilityType() );
			b.send( order.deltaNeutralOrderType() );
			b.send( order.deltaNeutralAuxPrice() );

			if (order.deltaNeutralOrderType() != OrderType.None) {
				b.send( order.deltaNeutralConId() );
				b.send( ""); //deltaNeutralSettlingFirm
				b.send( ""); //deltaNeutralClearingAccount
				b.send( ""); //deltaNeutralClearingIntent
				b.send( ""); //deltaNeutralOpenClose
                b.send( ""); //deltaNeutralShortSale
                b.send( ""); //deltaNeutralShortSaleSlot
                b.send( ""); //deltaNeutralDesignatedLocation
			}
			
			b.send( order.continuousUpdate() );
			b.send( order.referencePriceType() );
			b.send( order.trailStopPrice() );
			b.send( order.trailingPercent() );
			b.send( order.scaleInitLevelSize() );
			b.send( order.scaleSubsLevelSize() );
			b.send( order.scalePriceIncrement() );

			if (order.scalePriceIncrement() != 0 && order.scalePriceIncrement() != Double.MAX_VALUE) {
				b.send( order.scalePriceAdjustValue() );
				b.send( order.scalePriceAdjustInterval() );
				b.send( order.scaleProfitOffset() );
				b.send( order.scaleAutoReset() );
				b.send( order.scaleInitPosition() );
				b.send( order.scaleInitFillQty() );
				b.send( order.scaleRandomPercent() );
			}

			if (m_serverVersion >= MIN_SERVER_VER_SCALE_TABLE) {
				b.send( order.scaleTable() );
				b.send( ""); // active start time
				b.send( ""); // active stop time
			}

	        b.send( order.hedgeType() );
			if (order.hedgeType() != HedgeType.None) {
				b.send( order.hedgeParam() );
			}

			b.send( order.optOutSmartRouting() );
			b.send( "");//clearingAccount
			b.send( "");//clearingIntent
			b.send( order.notHeld() );

			b.send( contract.underComp() != null);
			if (contract.underComp() != null) {
				b.send( contract.underComp().conid() );
				b.send( contract.underComp().delta() );
				b.send( contract.underComp().price() );
			}

			b.send( order.algoStrategy() );
			if( order.algoStrategy() != AlgoStrategy.None) {
				b.send( order.algoParams().size() );
				for( TagValue tagValue : order.algoParams() ) {
					b.send( tagValue.m_tag);
					b.send( tagValue.m_value);
				}
			}

	        if (m_serverVersion >= MIN_SERVER_VER_ALGO_ID) {
	        	b.send( order.algoId() );
	        }

			b.send( order.whatIf() );
			
			// send orderMiscOptions stub
	        if(m_serverVersion >= MIN_SERVER_VER_LINKING) {	     
	            b.send( "" );
	        }

			m_dos.write( b.getBytes() );
		}
		catch( Exception e) {
			e.printStackTrace();
			error( order.orderId(), 512, "Order sending error - " + e);
			close();
		}
	}

    /** An output stream that forks all writes to the output logger. */
    private class MyOS extends OutputStream {
    	final OutputStream m_os;

    	MyOS( OutputStream os) {
    		m_os = os;
    	}

    	@Override public void write(byte[] b) throws IOException {
    		m_os.write( b);
    		log( new String( b) );
    	}

    	@Override public synchronized void write(byte[] b, int off, int len) throws IOException {
    		m_os.write(b, off, len);
    		log( new String( b, off, len) );
    	}

    	@Override public synchronized void write(int b) throws IOException {
    		m_os.write(b);
    		log( String.valueOf( (char)b) );
    	}

    	@Override public void flush() throws IOException {
    		m_os.flush();
    	}

    	@Override public void close() throws IOException {
    		m_os.close();
    		m_outLogger.log( "<output stream closed>");
    	}

    	void log( String str) {
    		m_outLogger.log( str.replace( EOL, LOG_EOL) );
    	}
    }

    /** An input stream that forks all reads to the input logger. */
    private class MyIS extends InputStream {
    	InputStream m_is;

    	MyIS( InputStream is) {
    		m_is = is;
    	}

		@Override public int read() throws IOException {
			int c = m_is.read();
			log( String.valueOf( (char)c) );
			return c;
		}

		@Override public int read(byte[] b) throws IOException {
			int n = m_is.read(b);
			log( new String( b, 0, n) );
			return n;
		}

		@Override public int read(byte[] b, int off, int len) throws IOException {
			int n = m_is.read(b, off, len);
			log( new String( b, 0, n) );
			return n;
		}

		@Override public void close() throws IOException {
			super.close();
			log( "<input stream closed>");
		}

    	void log( String str) {
    		m_inLogger.log( str.replace( EOL, LOG_EOL) );
    	}
    }
}
