#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Commands.Transaq
File: RequestHistoryDataMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestHistoryDataMessage : BaseCommandMessage
	{
		public RequestHistoryDataMessage() : base(ApiCommands.GetHistoryData)
		{
		}

		//public string Board { get; set; }
		//public string SecCode { get; set; }
		public int SecId { get; set; }
		public int Period { get; set; }
		public long Count { get; set; }
		public bool Reset { get; set; }
	}
}