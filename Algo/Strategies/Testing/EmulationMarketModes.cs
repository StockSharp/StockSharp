namespace StockSharp.Algo.Strategies.Testing
{
	using System.ComponentModel.DataAnnotations;

	using StockSharp.Localization;

	/// <summary>
	/// The data type for paper trading.
	/// </summary>
	public enum EmulationMarketDataModes
	{
		/// <summary>
		/// Storage.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StorageKey)]
		Storage,

		/// <summary>
		/// Generated.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.GeneratedKey)]
		Generate,

		/// <summary>
		/// None.
		/// </summary>
		[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.NoneKey)]
		No
	}
}