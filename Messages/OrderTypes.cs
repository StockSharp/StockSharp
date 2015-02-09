namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Типы заявок.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum OrderTypes
	{
		/// <summary>
		/// Лимитная.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str1353Key)]
		Limit,

		/// <summary>
		/// Рыночная.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str241Key)]
		Market,

		/// <summary>
		/// Условная (стоп, тейк-профик).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str242Key)]
		Conditional,

		/// <summary>
		/// Заявка на сделку РЕПО.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str243Key)]
		Repo,

		/// <summary>
		/// Заявка на модифицированную сделку РЕПО (РЕПО-М).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str244Key)]
		ExtRepo,

		/// <summary>
		/// Заявка на внебиржевую сделку.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str245Key)]
		Rps,

		///// <summary>
		///// Айсберг-заявка.
		///// </summary>
		//[EnumMember]
		//[EnumDisplayName("Айсберг")]
		//Iceberg,

		/// <summary>
		/// Заявка на исполнение поставочных контрактов (например, опционы).
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str246Key)]
		Execute,
	}
}