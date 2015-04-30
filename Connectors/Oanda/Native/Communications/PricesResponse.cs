namespace StockSharp.Oanda.Native.Communications
{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class PricesResponse
	{
		[JsonProperty("prices")]
		public long Time { get; set; }

		[JsonProperty("prices")]
		public IEnumerable<Price> Prices { get; set; }
	}
}