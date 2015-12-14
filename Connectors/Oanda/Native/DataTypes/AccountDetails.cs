#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Oanda.Native.DataTypes.Oanda
File: AccountDetails.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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