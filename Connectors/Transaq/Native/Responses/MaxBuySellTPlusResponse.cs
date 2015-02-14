namespace StockSharp.Transaq.Native.Responses
{
	using System.Collections.Generic;

	class MaxBuySellTPlusResponse : BaseResponse
	{
		/// <summary>
		/// Код клиента.
		/// </summary>
		public string Client { get; set; }

		/// <summary>
		/// Информация по инструментам.
		/// </summary>
		public IEnumerable<MaxBuySellTPlusSecurity> Securities { get; set; }
	}

	class MaxBuySellTPlusSecurity
	{
		/// <summary>
		/// Id инструмента.
		/// </summary>
		public string SecId { get; set; }

		/// <summary>
		/// Id рынка.
		/// </summary>
		public int Market { get; set; }

		/// <summary>
		/// Обозначение инструмента.
		/// </summary>
		public string SecCode { get; set; }

		/// <summary>
		/// Максимум купить (лот).
		/// </summary>
		public long MaxBuy { get; set; }

		/// <summary>
		/// Максимум продать (лот).
		/// </summary>
		public long MaxSell { get; set; }
	}
}