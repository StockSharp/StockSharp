#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Commands.Transaq
File: NewCondOrderMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Commands
{
	using System;

	internal class NewCondOrderMessage : NewOrderMessage
	{
		public NewCondOrderMessage()
		{
			Id = ApiCommands.NewCondOrder;
		}

		public TransaqAlgoOrderConditionTypes CondType { get; set; }
		public decimal CondValue { get; set; }
		
		public TransaqAlgoOrderValidTypes ValidAfterType { get; set; }
		public DateTime? ValidAfter { get; set; }
		
		public TransaqAlgoOrderValidTypes ValidBeforeType { get; set; }
		public DateTime? ValidBefore { get; set; }
	}
}