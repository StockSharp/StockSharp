namespace StockSharp.Oanda.Native.Communications
{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class InstrumentsResponse
	{
		[JsonProperty("instruments")]
		public IEnumerable<Instrument> Instruments { get; set; }
	}
}