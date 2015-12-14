#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Commands.Transaq
File: NewRpsOrderMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Commands
{
	class NewRpsOrderMessage : NewBaseOrderMessage
	{
		public NewRpsOrderMessage()
			: base(ApiCommands.NewRpsOrder)
		{
		}

		public string CpFirmId { get; set; }
		public string MatchRef { get; set; }
		public string SettleCode { get; set; }
		//public DateTime? SettleDate { get; set; }
		public int? RefundRate { get; set; }
	}
}