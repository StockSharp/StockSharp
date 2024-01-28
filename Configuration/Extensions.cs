namespace StockSharp.Configuration;

using Ecng.Common;

/// <summary>
/// Extension class.
/// </summary>
public static class Extensions
{
	/// <summary>
	/// </summary>
	public static long? TryGetProductId()
		=> Paths
			.EntryAssembly?
			.GetAttribute<ProductIdAttribute>()?
			.ProductId;
}