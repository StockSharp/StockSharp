namespace StockSharp.Configuration
{
	using System.Reflection;

	using Ecng.Common;

	/// <summary>
	/// Extension class.
	/// </summary>
	public static class Extensions
	{
		/// <summary>
		/// </summary>
		public static long? TryGetProductId()
			=> Assembly
				.GetEntryAssembly()?
				.GetAttribute<ProductIdAttribute>()?
				.ProductId;
	}
}