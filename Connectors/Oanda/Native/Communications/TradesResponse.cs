namespace StockSharp.Oanda.Native.Communications
{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class TradesResponse
	{
		[JsonProperty("trades")]
		public IEnumerable<TradeData> Trades { get; set; }
	}
}