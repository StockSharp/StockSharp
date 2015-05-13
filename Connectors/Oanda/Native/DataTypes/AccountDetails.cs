namespace StockSharp.Oanda.Native.DataTypes
{
	using Newtonsoft.Json;

	class AccountDetails
	{
		[JsonProperty("accountId")]
		public int Id { get; set; }

		[JsonProperty("accountName")]
		public string Name { get; set; }

		[JsonProperty("balance")]
		public double Balance { get; set; }

		[JsonProperty("unrealizedPl")]
		public double UnrealizedPnL { get; set; }

		[JsonProperty("realizedPl")]
		public double RealizedPnL { get; set; }

		[JsonProperty("marginUsed")]
		public double MarginUsed { get; set; }

		[JsonProperty("marginAvail")]
		public double MarginAvailable { get; set; }

		[JsonProperty("openTrades")]
		public int OpenTrades { get; set; }

		[JsonProperty("openOrders")]
		public int OpenOrders { get; set; }

		[JsonProperty("marginRate")]
		public double MarginRate { get; set; }

		[JsonProperty("accountCurrency")]
		public string Currency { get; set; }
	}
}