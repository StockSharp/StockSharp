namespace StockSharp.Oanda.Native.Communications
{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class OrdersResponse
	{
		[JsonProperty("orders")]
		public IEnumerable<Order> Orders { get; set; }
	}
}