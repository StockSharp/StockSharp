#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Responses.Transaq
File: PitsResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	class PitsResponse : BaseResponse
	{
		public IEnumerable<Pit> Pits { get; internal set; }
	}

	internal class Pit
	{
		public string SecCode { get; set; }
		public string Board { get; set; }
		public string Market { get; set; }
		public int Decimals { get; set; }
		public decimal MinStep { get; set; }
		public int LotSize { get; set; }
		public decimal PointCost { get; set; }
	}
}
