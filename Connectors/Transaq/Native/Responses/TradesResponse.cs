#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Responses.Transaq
File: TradesResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Responses
{
	using System;
	using System.Collections.Generic;

	internal class TradesResponse : BaseResponse
	{
		public IEnumerable<TransaqMyTrade> Trades { get; internal set; }
	}

	internal class TransaqMyTrade : Tick
	{
		public long OrderNo { get; set; }
		public string Client { get; set; }
		public DateTime Time { get; set; }
		public string BrokerRef { get; set; }
		public decimal Value { get; set; }
		public decimal? Commission { get; set; }
		public decimal Yield { get; set; }
		public decimal? AccrueEdint { get; set; }
		public TradeTypes TradeType { get; set; }
		public string SettleCode { get; set; }
		public long CurrentPos { get; set; }
	}

	internal enum TradeTypes
	{
		T,
		N,
		R,
		P
	}
}