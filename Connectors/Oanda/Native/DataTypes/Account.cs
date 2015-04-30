namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class Account
	{
		[JsonProperty("accountId")]
		public int Id { get; set; }

		[JsonProperty("accountName")]
		public string Name { get; set; }

		[JsonProperty("accountCurrency")]
		public string Currency { get; set; }

		[JsonProperty("marginRate")]
		public double MarginRate { get; set; }
	}
}