namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	/// <summary>
	/// Системные статусы заявки.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum OrderStatus : long
	{
		/// <summary>
		/// Транзакция отправлена на сервер.
		/// </summary>
		[EnumMember]SentToServer = 0,

		/// <summary>
		/// Транзакция получена сервером.
		/// </summary>
		[EnumMember]ReceiveByServer = 1,

		/// <summary>
		/// Ошибка отправки транзакции на биржу.
		/// </summary>
		[EnumMember]GateError = 2,

		/// <summary>
		/// Транзакция принята биржей.
		/// </summary>
		[EnumMember]Accepted = 3,

		/// <summary>
		/// Транзакция не принята биржей.
		/// </summary>
		[EnumMember]NotDone = 4,

		/// <summary>
		/// Транзакция не прошла проверку сервера по каким-либо критериям.
		/// Например, проверку на наличие прав у пользователя на отправку транзакции данного типа.
		/// </summary>
		[EnumMember]NotValidated = 5,

		/// <summary>
		/// Транзакция не прошла проверку лимитов сервера.
		/// </summary>
		[EnumMember]NotValidatedLimit = 6,

		/// <summary>
		/// Транзакция клиента, работающего с подтверждением, подтверждена менеджером фирмы.
		/// </summary>
		[EnumMember]AcceptedByManager = 7,

		/// <summary>
		/// Транзакция клиента, работающего с подтверждением, не подтверждена менеджером фирмы.
		/// </summary>
		[EnumMember]NotAcceptedByManager = 8,

		/// <summary>
		/// Транзакция клиента, работающего с подтверждением, снята менеджером фирмы.
		/// </summary>
		[EnumMember]CanceledByManager = 9,

		/// <summary>
		/// Транзакция не поддерживается торговой системой.
		/// </summary>
		[EnumMember]NotSupported = 10,

		/// <summary>
		/// Транзакция не прошла проверку правильности электронной подписи. К примеру, если ключи,
		/// зарегистрированные на сервере, не соответствуют подписи отправленной транзакции.
		/// </summary>
		[EnumMember]NotSigned = 11,

		/// <summary>
		/// Транзакция отправлена на снятие заявки.
		/// </summary>
		[EnumMember]SentToCanceled = 12,

		/// <summary>
		/// Транзакция об успешно снятой заявке.
		/// </summary>
		[EnumMember]Cancelled = 13,

		/// <summary>
		/// Транзакция об успешно исполненной заявке.
		/// </summary>
		[EnumMember]Matched = 14,

		/// <summary>
		/// Транзакция об отклоненной биржей заявке.
		/// </summary>
		[EnumMember]RejectedBySystem = 15,
	}
}