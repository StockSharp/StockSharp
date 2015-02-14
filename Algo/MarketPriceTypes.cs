namespace StockSharp.Algo
{
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// Типы рыночных цен.
	/// </summary>
	[DataContract]
	public enum MarketPriceTypes
	{
		/// <summary>
		/// Встречная цена (для быстрого закрытия позы).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str975Key)]
		[EnumMember]
		Opposite,

		/// <summary>
		/// Попутная цена (для котирования на краю спреда).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str976Key)]
		[EnumMember]
		Following,

		/// <summary>
		/// Середина спреда.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str500Key)]
		[EnumMember]
		Middle,
	}
}