namespace StockSharp.Transaq.Native.Responses
{
	using System;

	internal class NewsHeaderResponse : NewsBodyResponse
	{
		public DateTime? TimeStamp { get; set; }
		public string Source { get; set; }
		public string Title { get; set; }
	}
}