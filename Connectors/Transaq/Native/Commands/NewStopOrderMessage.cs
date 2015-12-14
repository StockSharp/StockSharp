#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Commands.Transaq
File: NewStopOrderMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Commands
{
	using System;

	internal class NewStopOrderMessage : NewBaseOrderMessage
	{
		public NewStopOrderMessage() : base(ApiCommands.NewStopOrder)
		{
		}

		public string LinkedOrderNo { get; set; }
		public DateTime? ValidFor { get; set; }
		public DateTime? ExpDate { get; set; }

		public NewStopOrderElement StopLoss { get; set; }
		public NewStopOrderElement TakeProfit { get; set; }
	}

	internal class NewStopOrderElement
	{
		public decimal? ActivationPrice { get; set; }
		public string OrderPrice { get; set; }
		//public bool IsOrderPriceInPercents { get; set; }
		public bool? ByMarket { get; set; }
		public string Quantity { get; set; }
		//public bool IsQuantityInPercents { get; set; }
		public bool? UseCredit { get; set; }
		public int? GuardTime { get; set; }
		public string BrokerRef { get; set; }
		public string Correction { get; set; }
		//public bool IsCorrectionInPercents { get; set; }
		public string Spread { get; set; }
		//public bool IsSpreadInPercents { get; set; }
	}
}