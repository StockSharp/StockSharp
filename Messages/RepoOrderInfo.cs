namespace StockSharp.Messages
{
    using System;
    using System.Runtime.Serialization;
    using Ecng.Serialization;

	/// <summary>
    /// Информация, необходимая для создания РЕПО-заявки.
    /// </summary>
    [Serializable]
    [System.Runtime.Serialization.DataContract]
	public class RepoOrderInfo
    {
        /// <summary>
        /// Создать <see cref="RepoOrderInfo"/>.
        /// </summary>
        public RepoOrderInfo()
        {
            Partner = null;
            Term = null;
            RefundRate = null;
            Rate = null;
            BlockSecurities = false;
            MatchRef = null;
            SettleCode = null;
            SettleDate = null;
            SecondPrice = null;
            StartDiscount = null;
            LowerDiscount = null;
            UpperDiscount = null;
			Value = null;
        }

		/// <summary>
		/// Код организации – партнера по внебиржевой сделке.
		/// </summary>
		[DataMember]
		[Nullable]
		public string Partner { get; set; }

		/// <summary>
		/// Срок РЕПО. Параметр сделок РЕПО-М.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? Term { get; set; }

		/// <summary>
		/// Ставка РЕПО, в процентах. 
		/// </summary>
		[DataMember]
		[Nullable]
		public int? Rate { get; set; }

		/// <summary>
		/// Признак блокировки бумаг на время операции РЕПО («YES», «NO»).
		/// </summary>
		[DataMember]
		[Nullable]
		public bool? BlockSecurities { get; set; }

		/// <summary>
		/// Ставка фиксированного возмещения, выплачиваемого в случае неисполнения второй части РЕПО, в процентах.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? RefundRate { get; set; }

		/// <summary>
		/// Ссылка, которая связывает две сделки РЕПО или РПС.
		/// Сделка может быть заключена только между контрагентами, указавшими одинаковое значение этого параметра в своих заявках.
		/// Параметр представляет собой произвольный набор количеством до 10 символов (допускаются цифры и буквы).
		/// Необязательный параметр.
		/// </summary>
		[DataMember]
		[Nullable]
		public string MatchRef { get; set; }
        
		/// <summary>
		/// Код расчетов при исполнении внебиржевых заявок.
		/// </summary>
		[DataMember]
		[Nullable]
		public string SettleCode { get; set; }

		/// <summary>
		/// Цена второй части РЕПО. 
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? SecondPrice { get; set; }

		/// <summary>
		/// Дата исполнения внебиржевой сделки.
		/// </summary>
		[DataMember]
		[Nullable]
		public DateTimeOffset? SettleDate { get; set; }

		/// <summary>
		/// Начальное значение дисконта в заявке на сделку РЕПО-М.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? StartDiscount { get; set; }

		/// <summary>
		/// Нижнее предельное значение дисконта в заявке на сделку РЕПО-М.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? LowerDiscount { get; set; }

		/// <summary>
		/// Верхнее предельное значение дисконта в заявке на сделку РЕПО-М.
		/// </summary>
		[DataMember]
		[Nullable]
		public int? UpperDiscount { get; set; }

		/// <summary>
		/// Объем сделки РЕПО-М в рублях.
		/// </summary>
		[DataMember]
		[Nullable]
		public decimal? Value { get; set; }
    }
}
