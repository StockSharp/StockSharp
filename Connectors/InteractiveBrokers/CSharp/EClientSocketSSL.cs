/* Copyright (C) 2015 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Security.Authentication;
using System.Text;

namespace IBApi
{
    public class EClientSocketSSL : EClientSocket
    {
        public EClientSocketSSL(EWrapper wrapper, EReaderSignal signal) :
            base(wrapper, signal) { }

        protected override Stream createClientStream(string host, int port)
        {
            var rval = new SslStream(base.createClientStream(host, port), false, (o, cert, chain, errors) => true);

            rval.AuthenticateAsClient(host);

            return rval;
        }
    }
}
