namespace StockSharp.BitStamp.Native
{
	using System;
	
	using Ecng.Net;

	using Newtonsoft.Json;

	class Order
	{
		[JsonProperty("id")]
		public long Id { get; set; }

		[JsonProperty("order_type")]
		public int Type { get; set; }

		[JsonProperty("price")]
		public double Price { get; set; }

		[JsonProperty("datetime")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Time { get; set; }

		[JsonProperty("amount")]
		public double Amount { get; set; }

		public override string ToString()
		{
			return (new { Time, Id, Type, Price, Amount }).ToString();
		}
	}
}

//{
//  "order_type": 0,
//  "price": "123.87",
//  "datetime": "1380234521",
//  "amount": "3.70000000",
//  "amount_sum": "22.20000000",
//  "id": 7629424
//}