namespace StockSharp.Transaq.Native.Responses
{
	internal class ClientResponse : BaseResponse
	{
		public string Id { get; set; }
		public bool Remove { get; set; }
		public ClientTypes Type { get; set; }
		public string Currency { get; set; }
		public decimal? MlIntraDay { get; set; }
		public decimal? MlOverNight { get; set; }
		public decimal? MlRestrict { get; set; }
		public decimal? MlCall { get; set; }
		public decimal? MlClose { get; set; }
	}

	internal enum ClientTypes
	{
		spot,
		leverage,
		margin_level
	}
}