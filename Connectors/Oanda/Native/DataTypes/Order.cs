#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Oanda.Native.DataTypes.Oanda
File: Order.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class Order
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("side")]
		public string Side { get; set; }

		[JsonProperty("instrument")]
		public string Instrument { get; set; }

		[JsonProperty("units")]
		public int Units { get; set; }

		[JsonProperty("time")]
		public long Time { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("stopLoss")]
		public double? StopLoss { get; set; }

		[JsonProperty("takeProfit")]
		public double? TakeProfit { get; set; }

		[JsonProperty("expiry")]
		public long? Expiry { get; set; }

		[JsonProperty("upperBound")]
		public double? UpperBound { get; set; }

		[JsonProperty("lowerBound")]
		public double? LowerBound { get; set; }

		[JsonProperty("trailingStop")]
		public int? TrailingStop { get; set; }
	}
}