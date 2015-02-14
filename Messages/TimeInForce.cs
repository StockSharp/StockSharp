namespace StockSharp.Messages
{
	using System;
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Время жизни лимитной заявки.
	/// </summary>
	[DataContract]
	[Serializable]
	public enum TimeInForce
	{
		/// <summary>
		/// Поставить в очередь.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str405Key)]
		PutInQueue,

		/// <summary>
		/// Немедленно или отклонить.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str406Key)]
		MatchOrCancel,

		/// <summary>
		/// Снять остаток.
		/// </summary>
		[EnumMember]
		[EnumDisplayNameLoc(LocalizedStrings.Str407Key)]
		CancelBalance,
	}
}