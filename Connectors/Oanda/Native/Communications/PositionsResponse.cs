namespace StockSharp.Oanda.Native.Communications
{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class PositionsResponse
	{
		[JsonProperty("positions")]
		public IEnumerable<Position> Positions { get; set; }
	}
}