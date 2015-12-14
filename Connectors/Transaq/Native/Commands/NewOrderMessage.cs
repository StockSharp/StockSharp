#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Commands.Transaq
File: NewOrderMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Commands
{
	using System;

	using StockSharp.Transaq.Native.Responses;

	abstract class NewBaseOrderMessage : BaseCommandMessage
	{
		protected NewBaseOrderMessage(string commandId)
			: base(commandId)
		{
		}

		public int SecId { get; set; }
		//public string Board { get; set; }
		//public string SecCode { get; set; }
		public string Client { get; set; }
		public decimal Price { get; set; }
		public int Quantity { get; set; }
		public BuySells BuySell { get; set; }
		public string BrokerRef { get; set; }
	}

	internal class NewOrderMessage : NewBaseOrderMessage
	{
		public NewOrderMessage() : base(ApiCommands.NewOrder)
		{
		}

		public int Hidden { get; set; }
		public bool ByMarket { get; set; }
		public bool UseCredit { get; set; }
		public bool NoSplit { get; set; }
		public DateTime? ExpDate { get; set; }
		public NewOrderUnfilleds Unfilled { get; set; }
	}

	internal enum NewOrderUnfilleds
	{
		PutInQueue,
		CancelBalance,
		ImmOrCancel
	}
}