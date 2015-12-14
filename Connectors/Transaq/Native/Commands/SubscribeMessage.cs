#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Commands.Transaq
File: SubscribeMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Commands
{
	using System.Collections.Generic;

	using StockSharp.Messages;

	internal class SubscribeMessage : BaseCommandMessage
	{
		public SubscribeMessage() : base(ApiCommands.Subscribe)
		{
			AllTrades = new List<SecurityId>();
			Quotations = new List<SecurityId>();
			Quotes = new List<SecurityId>();
		}

		public List<SecurityId> AllTrades { get; private set; }
		public List<SecurityId> Quotations { get; private set; }
		public List<SecurityId> Quotes { get; private set; }
	}
}