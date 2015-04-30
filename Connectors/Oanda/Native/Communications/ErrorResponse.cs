namespace StockSharp.Oanda.Native.Communications
{
	using Newtonsoft.Json;

	class ErrorResponse
	{
		[JsonProperty("code")]
		public int Code { get; set; }

		[JsonProperty("message")]
		public string Message { get; set; }

		[JsonProperty("moreInfo")]
		public string MoreInfo { get; set; }
	}
}