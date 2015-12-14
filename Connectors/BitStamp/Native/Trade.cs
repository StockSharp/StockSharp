#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.Native.BitStamp
File: Trade.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp.Native
{
	using Newtonsoft.Json;

	class Trade
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("amount")]
		public double Amount { get; set; }

		public override string ToString()
		{
			return (new { Id, Amount, Price }).ToString();
		}
	}
}

//Trade: {
//  "price": 124.4,
//  "amount": 5.44394508,
//  "24vol": 7768.93108898,
//  "id": 1446511
//}