/* Copyright (C) 2013 Interactive Brokers LLC. All rights reserved.  This code is subject to the terms
 * and conditions of the IB API Non-Commercial License or the IB API Commercial License, as applicable. */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.IO;

namespace IBApi
{
    public static class IBParamsList
    {
        public static void AddParameter(this BinaryWriter source, OutgoingMessages msgId)
        {
            AddParameter(source, (int)msgId);
        }

        public static void AddParameter(this BinaryWriter source, int value)
        {
            AddParameter(source, value.ToString(CultureInfo.InvariantCulture));
        }

        public static void AddParameter(this BinaryWriter source, double value)
        {
            AddParameter(source, value.ToString(CultureInfo.InvariantCulture));
        }

        public static void AddParameter(this BinaryWriter source, bool value)
        {
            if (value)
                AddParameter(source, "1");
            else
                AddParameter(source, "0");

        }

        public static void AddParameter(this BinaryWriter source, string value)
        {
            if (value != null)
                source.Write(UTF8Encoding.UTF8.GetBytes(value));
            source.Write(Constants.EOL);
        }

        public static void AddParameterMax(this BinaryWriter source, double value)
        {
            if (value == Double.MaxValue)
                source.Write(Constants.EOL);
            else
                source.AddParameter(value);

        }

        public static void AddParameterMax(this BinaryWriter source, int value)
        {
            if (value == Int32.MaxValue)
                source.Write(Constants.EOL);
            else
                source.AddParameter(value);
        }

    }
}
