namespace StockSharp.Oanda.Native.Communications
{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class TransactionsResponse
	{
		[JsonProperty("transactions")]
		public IEnumerable<Transaction> Transactions { get; set; }
	}
}