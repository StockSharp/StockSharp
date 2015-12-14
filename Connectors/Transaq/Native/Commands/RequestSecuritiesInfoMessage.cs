#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Commands.Transaq
File: RequestSecuritiesInfoMessage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Commands
{
	internal class RequestSecuritiesInfoMessage : BaseCommandMessage
	{
		public RequestSecuritiesInfoMessage() : base(ApiCommands.GetSecuritiesInfo)
		{
		}

		public int Market { get; set; }
		public string SecCode { get; set; }
	}
}