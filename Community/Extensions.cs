namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	/// <summary>
	/// Extensions.
	/// </summary>
	public static class Extensions
	{
		private static readonly Dictionary<Products, ProductData> _productsMapping = new Dictionary<Products, ProductData>
		{
			{ Products.Api, new ProductData { Id = 5, Name = "S#.API" } },
			{ Products.Designer, new ProductData { Id = 9, Name = "S#.Designer" } },
			{ Products.Hydra, new ProductData { Id = 8, Name = "S#.Data" } },
			{ Products.Terminal, new ProductData { Id = 10, Name = "S#.Terminal" } },
			{ Products.Server, new ProductData { Id = 14, Name = "S#.Server" } },
			{ Products.Studio, new ProductData { Id = 7, Name = "S#.Studio" } },
		};

		/// <summary>
		/// Convert <see cref="ProductData"/> to <see cref="Products"/> value.
		/// </summary>
		/// <param name="product"><see cref="ProductData"/> value.</param>
		/// <returns><see cref="Products"/> value.</returns>
		public static Products ToEnum(this ProductData product)
		{
			if (product == null)
				throw new ArgumentNullException(nameof(product));

			return _productsMapping.First(p => p.Value.Id == product.Id).Key;
		}

		/// <summary>
		/// Convert <see cref="Products"/> to <see cref="ProductData"/> value.
		/// </summary>
		/// <param name="product"><see cref="Products"/> value.</param>
		/// <returns><see cref="ProductData"/> value.</returns>
		public static ProductData FromEnum(this Products product)
		{
			return _productsMapping[product];
		}
	}
}