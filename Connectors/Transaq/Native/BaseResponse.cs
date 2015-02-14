namespace StockSharp.Transaq.Native
{
	using System;

	internal class BaseResponse
	{
		public BaseResponse()
		{
			IsSuccess = true;
		}

		public bool IsSuccess { get; set; }
		public string Text { get; set; }
		public Exception Exception { get; set; }
		public int Diff { get; set; }
		public long TransactionId { get; set; }
	}
}