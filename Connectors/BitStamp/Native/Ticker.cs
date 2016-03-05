#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.Native.BitStamp
File: Ticker.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp.Native
{
	using System;

	using Ecng.Net;

	using Newtonsoft.Json;

	class Ticker
	{
		public double Bid { get; set; }

		public double Ask { get; set; }

		public double Last { get; set; }

		public double High { get; set; }

		public double Low { get; set; }

		public double Volume { get; set; }

		public double VWAP { get; set; }

		[JsonProperty(PropertyName = "timestamp")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Time { get; set; }

		public override string ToString()
		{
			return $"[Ticker: Bid={Bid}, Ask={Ask}, Last={Last}, High={High}, Low={Low}, Volume={Volume}]";
		}
	}
}