#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Commands.Transaq
File: SubscribeTicksMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Commands
{
	using System.Collections.Generic;

	internal class SubscribeTicksMessage : BaseCommandMessage
	{
		public SubscribeTicksMessage() : base(ApiCommands.SubscribeTicks)
		{
			Items = new List<SubscribeTicksSecurity>();
		}

		public bool Filter { get; set; }
		public List<SubscribeTicksSecurity> Items { get; private set; }
	}

	internal class SubscribeTicksSecurity
	{
		//public string Board { get; set; }
		//public string SecCode { get; set; }
		public int TradeNo { get; set; }
		public int SecId { get; set; }
	}
}