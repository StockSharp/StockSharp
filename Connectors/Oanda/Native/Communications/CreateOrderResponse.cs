#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Oanda.Native.Communications.Oanda
File: CreateOrderResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Oanda.Native.Communications
{
	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class CreateOrderResponse
	{
		[JsonProperty("instrument")]
		public string Instrument { get; set; }

		[JsonProperty("time")]
		public long Time { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("tradeOpened")]
		public TradeData TradeOpened { get; set; }

		[JsonProperty("tradeReduced")]
		public TradeData TradeReduced { get; set; }

		[JsonProperty("orderOpened")]
		public Order OrderOpened { get; set; }
	}
}