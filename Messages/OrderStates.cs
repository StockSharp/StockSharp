namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Состояния заявки.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum OrderStates
	{
		/// <summary>
		/// Не отправлена в торговую систему.
		/// </summary>
		/// <remarks>
		/// Первоначальное значение заявки, когда еще программа не отправила транзакцию в торговую систему.
		/// </remarks>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str237Key)]
		None,

		/// <summary>
		/// Заявка принята биржей и активна.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str238Key)]
		Active,

		/// <summary>
		/// Заявка больше не активна на бирже (была полностью удовлетворена или снята из программы).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str239Key)]
		Done,

		/// <summary>
		/// Заявка не принята торговой системой.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str152Key)]
		Failed,

		/// <summary>
		/// Заявка ожидает от биржи подтверждение регистрации.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str238Key)]
		Pending,
	}
}