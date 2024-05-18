namespace StockSharp.Bitalong.Native.Model
{
	using System;
	using System.Reflection;

	using Ecng.Serialization;

	using Newtonsoft.Json;

	[Obfuscation(Feature = "renaming", ApplyToMembers = true)]
	class Order
	{
		[JsonProperty("orderNumber")]
		public long Id { get; set; }

		[JsonProperty("type")]
		public string Type { get; set; }

		[JsonProperty("amount")]
		public double Amount { get; set; }

		[JsonProperty("total")]
		public double Total { get; set; }

		[JsonProperty("initialRate")]
		public double InitialRate { get; set; }

		[JsonProperty("initialAmount")]
		public double InitialAmount { get; set; }

		[JsonProperty("filledAmount")]
		public double FilledAmount { get; set; }

		[JsonProperty("currencyPair")]
		public string CurrencyPair { get; set; }

		[JsonProperty("timestamp")]
		[JsonConverter(typeof(JsonDateTimeConverter))]
		public DateTime Timestamp { get; set; }

		[JsonProperty("status")]
		public string Status { get; set; }
	}
}