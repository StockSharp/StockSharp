#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.BitStamp.Native.BitStamp
File: Balance.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.BitStamp.Native
{
	using Newtonsoft.Json;

	class Balance
	{
		[JsonProperty("usd_balance")]
		public double UsdBalance { get; set; }

		[JsonProperty("btc_balance")]
		public double BtcBalance { get; set; }

		[JsonProperty("usd_reserved")]
		public double UsdReserved { get; set; }

		[JsonProperty("btc_reserved")]
		public double BtcReserved { get; set; }

		[JsonProperty("usd_available")]
		public double UsdAvailable { get; set; }

		[JsonProperty("btc_available")]
		public double BtcAvailable { get; set; }

		[JsonProperty("fee")]
		public double Fee { get; set; }
	}
}