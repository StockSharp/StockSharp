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