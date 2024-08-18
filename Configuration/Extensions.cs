namespace StockSharp.Configuration;

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