#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Responses.Transaq
File: ClientResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Responses
{
	internal class ClientResponse : BaseResponse
	{
		public string Id { get; set; }
		public bool Remove { get; set; }
		public ClientTypes Type { get; set; }
		public string Currency { get; set; }
		//public decimal? MlIntraDay { get; set; }
		//public decimal? MlOverNight { get; set; }
		//public decimal? MlRestrict { get; set; }
		//public decimal? MlCall { get; set; }
		//public decimal? MlClose { get; set; }
		public int MarketId { get; set; }
		public string Union { get; set; }
		public string FortsAcc { get; set; }
	}

	internal enum ClientTypes
	{
		spot,
		leverage,
		mct
	}
}