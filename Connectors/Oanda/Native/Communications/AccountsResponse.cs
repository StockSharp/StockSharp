namespace StockSharp.Oanda.Native.Communications
{
	using System.Collections.Generic;

	using Newtonsoft.Json;

	using StockSharp.Oanda.Native.DataTypes;

	class AccountsResponse
	{
		[JsonProperty("accounts")]
		public IEnumerable<Account> Accounts { get; set; }
	}
}