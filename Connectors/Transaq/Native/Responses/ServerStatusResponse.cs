namespace StockSharp.Transaq.Native.Responses
{
	internal class ServerStatusResponse : BaseResponse
	{
		public string Connected { get; set; }
		public string Recover { get; set; }
		public string TimeZone { get; set; }
	}
}