#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.Native.BitStamp
File: Order.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp.Native
{
	using System;
	
	using Ecng.Net;

	using Newtonsoft.Json;

	class Order
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("order_type")]
		public int Type { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("datetime")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Time { get; set; }

		[JsonProperty("amount")]
		public double Amount { get; set; }

		public override string ToString()
		{
			return (new { Time, Id, Type, Price, Amount }).ToString();
		}
	}
}

//{
//  "order_type": 0,
//  "price": "123.87",
//  "datetime": "1380234521",
//  "amount": "3.70000000",
//  "amount_sum": "22.20000000",
//  "id": 7629424
//}