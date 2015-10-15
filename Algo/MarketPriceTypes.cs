namespace StockSharp.Algo
{
	using System.Runtime.Serialization;

	using StockSharp.Localization;

	/// <summary>
	/// The type of market prices.
	/// </summary>
	[DataContract]
	public enum MarketPriceTypes
	{
		/// <summary>
		/// The counter-price (for quick closure of position).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str975Key)]
		[EnumMember]
		Opposite,

		/// <summary>
		/// The concurrent price (for quoting at the edge of spread).
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str976Key)]
		[EnumMember]
		Following,

		/// <summary>
		/// Spread middle.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str500Key)]
		[EnumMember]
		Middle,
	}
}