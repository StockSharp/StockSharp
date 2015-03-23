namespace StockSharp.Messages
{
    using System;
    using System.Runtime.Serialization;
    using Ecng.Serialization;

	/// <summary>
    /// Информация, необходимая для создания РПС-заявки.
    /// </summary>
    [Serializable]
    [System.Runtime.Serialization.DataContract]
	public class RpsOrderInfo
    {
        /// <summary>
        /// Создать <see cref="RpsOrderInfo"/>.
        /// </summary>
        public RpsOrderInfo()
        {
        }

		/// <summary>
		/// Код организации – партнера по внебиржевой сделке.
		/// </summary>
		[DataMember]
		public string Partner { get; set; }

		/// <summary>
		/// Дата исполнения внебиржевой сделки.
		/// </summary>
		[DataMember]
		[Nullable]
		public DateTimeOffset? SettleDate { get; set; }

		/// <summary>
		/// Ссылка, которая связывает две сделки РЕПО или РПС.
		/// Сделка может быть заключена только между контрагентами, указавшими одинаковое значение этого параметра в своих заявках.
		/// Параметр представляет собой произвольный набор количеством до 10 символов (допускаются цифры и буквы).
		/// Необязательный параметр.
		/// </summary>
		[DataMember]
		public string MatchRef { get; set; }

		/// <summary>
		/// Код расчетов при исполнении внебиржевых заявок.
		/// </summary>
		[DataMember]
		public string SettleCode { get; set; }

		/// <summary>
		/// Лицо, от имени которого и за чей счет регистрируется сделка (параметр внебиржевой сделки).
		/// </summary>
		[DataMember]
		public string ForAccount { get; set; }

		/// <summary>
		/// Код валюты расчетов по внебиржевой сделки в формате ISO 4217. Параметр внебиржевой сделки.
		/// </summary>
		[DataMember]
		public CurrencyTypes CurrencyType { get; set; }
    }
}
