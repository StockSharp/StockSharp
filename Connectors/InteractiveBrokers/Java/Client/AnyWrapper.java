/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

package com.ib.client;

public interface AnyWrapper {
    void error( Exception e);
    void error( String str);
    void error(int id, int errorCode, String errorMsg);
    void connectionClosed();
}

