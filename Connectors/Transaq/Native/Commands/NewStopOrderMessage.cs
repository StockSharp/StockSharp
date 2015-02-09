namespace StockSharp.Transaq.Native.Commands
{
	using System;

	internal class NewStopOrderMessage : NewBaseOrderMessage
	{
		public NewStopOrderMessage() : base(ApiCommands.NewStopOrder)
		{
		}

		public string LinkedOrderNo { get; set; }
		public DateTime? ValidFor { get; set; }
		public DateTime? ExpDate { get; set; }

		public NewStopOrderElement StopLoss { get; set; }
		public NewStopOrderElement TakeProfit { get; set; }
	}

	internal class NewStopOrderElement
	{
		public decimal? ActivationPrice { get; set; }
		public string OrderPrice { get; set; }
		//public bool IsOrderPriceInPercents { get; set; }
		public bool? ByMarket { get; set; }
		public string Quantity { get; set; }
		//public bool IsQuantityInPercents { get; set; }
		public string UseCredit { get; set; }
		public int? GuardTime { get; set; }
		public string BrokerRef { get; set; }
		public string Correction { get; set; }
		//public bool IsCorrectionInPercents { get; set; }
		public string Spread { get; set; }
		//public bool IsSpreadInPercents { get; set; }
	}
}