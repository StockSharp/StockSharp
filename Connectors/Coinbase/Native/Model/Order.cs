namespace StockSharp.Coinbase.Native.Model
{
	using System;
	using System.Reflection;

	using Newtonsoft.Json;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	class Order
	{
		[JsonProperty("id")]
		public string Id { get; set; }

		[JsonProperty("product_id")]
		public string Product { get; set; }

		[JsonProperty("price")]
		public double? Price { get; set; }

		[JsonProperty("size")]
		public double? Size { get; set; }

		[JsonProperty("side")]
		public string Side { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("time_in_force")]
		public string TimeInForce { get; set; }

		[JsonProperty("created_at")]
		public DateTime CreatedAt { get; set; }

		[JsonProperty("fill_fees")]
		public double? FillFees { get; set; }

		[JsonProperty("filled_size")]
		public double? FilledSize { get; set; }

		[JsonProperty("executed_value")]
		public double? ExecutedValue { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }
	}
}