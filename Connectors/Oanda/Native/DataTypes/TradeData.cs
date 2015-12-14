#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Oanda.Native.DataTypes.Oanda
File: TradeData.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	internal class TradeData
	{
		[JsonProperty("id")]
		public int Id { get; set; }

		[JsonProperty("units")]
		public int Units { get; set; }

		[JsonProperty("side")]
		public string Side { get; set; }

		[JsonProperty("instrument")]
		public string Instrument { get; set; }

		[JsonProperty("time")]
		public long Time { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("takeProfit")]
		public double TakeProfit { get; set; }

		[JsonProperty("stopLoss")]
		public double StopLoss { get; set; }

		[JsonProperty("trailingStop")]
		public int TrailingStop { get; set; }

		[JsonProperty("trailingAmount")]
		public double TrailingAmount { get; set; }
	}
}