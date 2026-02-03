namespace StockSharp.Algo.PositionManagement;

/// <summary>
/// Position modification algorithms.
/// </summary>
public enum PositionModifyAlgorithms
{
	/// <summary>
	/// Change position using market order.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MarketOrdersKey,
		Description = LocalizedStrings.PosModifyMarketOrdersKey)]
	MarketOrder,

	/// <summary>
	/// Change position using the VWAP algorithm.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.VWAPKey,
		Description = LocalizedStrings.PosModifyVWAPKey)]
	VWAP,

	/// <summary>
	/// Change position using the TWAP algorithm.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TWAPKey,
		Description = LocalizedStrings.PosModifyTWAPKey)]
	TWAP,

	/// <summary>
	/// Change position using the Iceberg algorithm.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.IcebergKey,
		Description = LocalizedStrings.PosModifyIcebergKey)]
	Iceberg,

	/// <summary>
	/// Change position using quoting-based accumulation.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.QuotingKey,
		Description = LocalizedStrings.PosModifyQuotingKey)]
	Quoting,
}
