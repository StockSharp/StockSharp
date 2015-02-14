namespace StockSharp.Transaq.Native.Responses
{
	internal class ClientLimitsResponse : BaseResponse
	{
		public string Client { get; set; }
		public decimal CBPLimit { get; set; }
		public decimal CBPlused { get; set; }
		public decimal CBPLPlanned { get; set; }
		public decimal FobVarMargin { get; set; }
		public decimal Coverage { get; set; }
		public decimal LiquidityC { get; set; }
		public decimal Profit { get; set; }
		public decimal MoneyCurrent { get; set; }
		public decimal MoneyBlocked { get; set; }
		public decimal MoneyFree { get; set; }
		public decimal OptionsPremium { get; set; }
		public decimal ExchangeFee { get; set; }
		public decimal FortsVarMargin { get; set; }
		public decimal VarMargin { get; set; }
		public decimal PclMargin { get; set; }
		public decimal OptionsVm { get; set; }
		public decimal SpotBuyLimit { get; set; }
		public decimal UsedStopBuyLimit { get; set; }
		public decimal CollatCurrent { get; set; }
		public decimal CollatBlocked { get; set; }
		public decimal CollatFree { get; set; }
	}
}