namespace StockSharp.Transaq.Native.Responses
{
	internal class MarketOrdResponse : BaseResponse
	{
		public int SecId { get; set; }
		public string SecCode { get; set; }
		public bool Permit { get; set; }
	}
}