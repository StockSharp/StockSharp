namespace StockSharp.Oanda.Native.Communications
{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class CandlesResponse
	{
		[JsonProperty("instrument")]
		public string Instrument { get; set; }

		[JsonProperty("granularity")]
		public string TimeFrame { get; set; }

		[JsonProperty("candles")]
		public IEnumerable<Candle> Candles { get; set; }
	}
}