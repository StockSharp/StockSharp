#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Responses.Transaq
File: SecuritiesResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	internal class SecuritiesResponse : BaseResponse
	{
		public IEnumerable<TransaqSecurity> Securities { get; internal set; }
	}

	internal class TransaqSecurity : Pit
	{
		public int SecId { get; set; }
		public bool Active { get; set; }
		public string ShortName { get; set; }
		public bool OpMaskUseCredit { get; set; }
		public bool OpMaskByMarket { get; set; }
		public bool OpMaskNoSplit { get; set; }
		public bool OpMaskImmorCancel { get; set; }
		public bool OpMaskCancelBalance { get; set; }
		public string Type { get; set; }
		public string TimeZone { get; set; }
	}

	//internal enum TransaqSecurityTypes
	//{
	//	SHARE,
	//	BOND,
	//	FUT,
	//	OPT,
	//	GKO,
	//	FOB,
	//	IDX,
	//	QUOTES,
	//	CURRENCY,
	//	ETS_CURRENCY,
	//	ADR,
	//	NYSE,
	//	METAl,
	//	OIL
	//}
}