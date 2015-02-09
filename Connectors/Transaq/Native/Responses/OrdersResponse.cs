namespace StockSharp.Transaq.Native.Responses
{
	using System;
	using System.Collections.Generic;

	using StockSharp.Messages;

	internal class OrdersResponse : BaseResponse
	{
		public IEnumerable<TransaqOrder> Orders { get; set; }
		public IEnumerable<TransaqStopOrder> StopOrders { get; set; }
	}

	internal abstract class TransaqBaseOrder
	{
		public int TransactionId { get; set; }
		public int SecId { get; set; }
		public string Board { get; set; }
		public string SecCode { get; set; }
		public string Client { get; set; }
		public BuySells BuySell { get; set; }
		public DateTime? ExpDate { get; set; }
		public TransaqOrderStatus Status { get; set; }
		public DateTime? AcceptTime { get; set; }
	}

	internal class TransaqOrder : TransaqBaseOrder
	{
		public long OrderNo { get; set; }
		public DateTime? Time { get; set; }
		public long? OriginOrderNo { get; set; }
		public string BrokerRef { get; set; }
		public decimal Value { get; set; }
		public decimal AccruEdint { get; set; }
		public string SettleCode { get; set; }
		public int Balance { get; set; }
		public decimal Price { get; set; }
		public int Quantity { get; set; }
		public int Hidden { get; set; }
		public decimal Yield { get; set; }
		public DateTime? WithdrawTime { get; set; }
		public TransaqAlgoOrderConditionTypes ConditionType { get; set; }
		public decimal? ConditionValue { get; set; }
		public DateTime? ValidAfter { get; set; }
		public DateTime? ValidBefore { get; set; }
		public decimal MaxCommission { get; set; }
		public string Result { get; set; }
	}

	internal class TransaqStopOrder : TransaqBaseOrder
	{
		public long? ActiveOrderNo { get; set; }
		public string Canceller { get; set; }
		public long? AllTradeNo { get; set; }
		public DateTime? ValidBefore { get; set; }
		public string Author { get; set; }
		public long? LinkedOrderNo { get; set; }

		public StopLoss StopLoss { get; set; }
		public TakeProfit TakeProfit { get; set; }
	}

	internal class StopLoss
	{
		public bool UseCredit { get; set; }
		public decimal ActivationPrice { get; set; }
		public DateTime? GuardTime { get; set; }
		public string BrokerRef { get; set; }
		public decimal Quantity { get; set; }
		public decimal? OrderPrice { get; set; }
	}

	internal class TakeProfit : StopLoss
	{
		public decimal? Extremum { get; set; }
		public decimal? Level { get; set; }
		public Unit Correction { get; set; }
		public Unit GuardSpread { get; set; }
	}

	internal enum TransaqOrderStatus
	{
		none,

		/// <summary>
		/// Активная.
		/// </summary>
		active,

		/// <summary>
		/// Снята трейдером (заявка уже попала на рынок и была отменена).
		/// </summary>
		cancelled,

		/// <summary>
		/// Отклонена Брокером.
		/// </summary>
		denied,

		/// <summary>
		/// Прекращена трейдером (условная заявка, которую сняли до наступления условия).
		/// </summary>
		disabled,

		/// <summary>
		/// Время действия истекло.
		/// </summary>
		expired,

		/// <summary>
		/// Не удалось выставить на биржу.
		/// </summary>
		failed,

		/// <summary>
		/// Выставляется на биржу.
		/// </summary>
		forwarding,

		/// <summary>
		/// Статус не известен из-за проблем со связью с биржей.
		/// </summary>
		inactive,

		/// <summary>
		/// Исполнена.
		/// </summary>
		matched,

		/// <summary>
		/// Отклонена контрагентом.
		/// </summary>
		refused,

		/// <summary>
		/// Отклонена биржей.
		/// </summary>
		rejected,

		/// <summary>
		/// Аннулирована биржей.
		/// </summary>
		removed,

		/// <summary>
		/// Не наступило время активации.
		/// </summary>
		wait,

		/// <summary>
		/// Ожидает наступления условия.
		/// </summary>
		watching,

		//only for stoporder

		/// <summary>
		/// Ожидает исполнения связанной заявки.
		/// </summary>
		linkwait,

		/// <summary>
		/// Выполнена (Stop Loss).
		/// </summary>
		sl_executed,

		/// <summary>
		/// Стоп выставляется на биржу (Stop Loss).
		/// </summary>
		sl_forwarding,

		/// <summary>
		/// Стоп ожидает исполнения в защитном периоде.
		/// </summary>
		sl_guardtime,

		/// <summary>
		/// Ожидает исполнения в режиме коррекции (Take Profit).
		/// </summary>
		tp_correction,

		/// <summary>
		/// Стоп ожидает исполнения в защитном режиме после коррекции (Take Profit).
		/// </summary>
		tp_correction_guardtime,

		/// <summary>
		/// Выполнен (Take Profit).
		/// </summary>
		tp_executed,

		/// <summary>
		/// Стоп выставляется на биржу (Take Profit).
		/// </summary>
		tp_forwarding,

		/// <summary>
		/// Стоп ожидает исполнения в защитном периоде (Take Profit).
		/// </summary>
		tp_guardtime,
	}
}