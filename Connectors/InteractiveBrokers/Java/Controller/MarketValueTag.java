/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.controller;

public enum MarketValueTag {
	NetLiquidationByCurrency,
	CashBalance,
	TotalCashBalance,
	AccruedCash,
	StockMarketValue,
	OptionMarketValue,
	FutureOptionValue,
	FuturesPNL,
	UnrealizedPnL,
	RealizedPnL,
	ExchangeRate,
	FundValue,
	NetDividend,
	MutualFundValue,
	MoneyMarketFundValue,
	CorporateBondValue,
	TBondValue,
	TBillValue,
	WarrantValue,
	FxCashBalance;

	public static MarketValueTag get( int i) {
		return Types.getEnum( i, values() );
	}

	@Override public String toString() {
		switch( this) {
			case NetLiquidationByCurrency: return "Net Liq";
			case StockMarketValue: return "Stocks";
			case OptionMarketValue: return "Options";
			case FutureOptionValue: return "Futures";
		}
		return super.toString().replaceAll("Value", "");
	}
}
