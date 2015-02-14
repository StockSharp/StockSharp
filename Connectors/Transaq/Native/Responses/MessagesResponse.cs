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