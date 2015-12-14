#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Responses.Transaq
File: SecInfoResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Responses
{
	using System;

	internal class SecInfoResponse : BaseResponse
	{
		public int SecId { get; set; }
		public int Market { get; set; }
		public string SecCode { get; set; }
		public string SecName { get; set; }
		public string PName { get; set; }
		public DateTime? MatDate { get; set; }
		public decimal? ClearingPrice { get; set; }
		public decimal? MinPrice { get; set; }
		public decimal? MaxPrice { get; set; }
		public decimal? BuyDeposit { get; set; }
		public decimal? SellDeposit { get; set; }
		public decimal? BgoC { get; set; }
		public decimal? BgoNC { get; set; }
		public decimal? BgoBuy { get; set; }
		public decimal? Accruedint { get; set; }
		public decimal? CouponValue { get; set; }
		public DateTime? CouponDate { get; set; }
		public int CouponPeriod { get; set; }
		public decimal? FaceValue { get; set; }
		public SecInfoPutCalls? PutCall { get; set; }
		public SecInfoOptTypes? OptType { get; set; }
		public int? LotVolume { get; set; }
	}

	internal enum SecInfoPutCalls
	{
		C,
		P
	}

	internal enum SecInfoOptTypes
	{
		M,
		P
	}
}