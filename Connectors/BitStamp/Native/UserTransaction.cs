#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.Native.BitStamp
File: UserTransaction.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp.Native
{
	using System;

	using Ecng.Net;

	using Newtonsoft.Json;

	class UserTransaction
	{
		[JsonProperty("datetime")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Time { get; set; }

		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("type")]
		public int Type { get; set; }

		[JsonProperty("usd")]
		public double UsdAmount { get; set; }

		[JsonProperty("btc")]
		public double BtcAmount { get; set; }

		[JsonProperty("fee")]
		public double Fee { get; set; }

		[JsonProperty("order_id")]
		public long OrderId { get; set; }
	}
}