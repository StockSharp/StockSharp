namespace StockSharp.Configuration
{
	using System;

	/// <summary>
	/// Product id attribute.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, Inherited = false, AllowMultiple = false)]
	public class ProductIdAttribute : Attribute
	{
		/// <summary>
		/// Product id.
		/// </summary>
		public long ProductId { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="ProductIdAttribute"/>.
		/// </summary>
		/// <param name="productId"><see cref="ProductId"/></param>
		public ProductIdAttribute(long productId)
		{
			ProductId = productId;
		}
	}
}