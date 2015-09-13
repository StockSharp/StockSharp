/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

//Hint. we could separate the message validation from the Socket client class...
namespace IBApi
{
    public class MessageValidator
    {
        private int serverVersion;

        public int ServerVersion
        {
            get { return serverVersion;  }
            set { serverVersion = value; }
        }

        public MessageValidator(int serverVersion)
        {
            ServerVersion = serverVersion;
        }
    }
}
