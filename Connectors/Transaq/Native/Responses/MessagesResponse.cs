#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Transaq.Native.Responses.Transaq
File: MessagesResponse.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Transaq.Native.Responses
{
	using System;
	using System.Collections.Generic;

	class MessagesResponse : BaseResponse
	{
		public IEnumerable<TransaqMessage> Messages { get; internal set; }
	}

	class TransaqMessage
	{
		public DateTime? Date { get; set; }
		public bool Urgent { get; set; }
		public string From { get; set; }
		public string Text { get; set; }
	}
}