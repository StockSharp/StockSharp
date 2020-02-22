namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Security;

	/// <summary>
	/// Extensions for <see cref="Community"/>.
	/// </summary>
	public static class Extensions
	{
		private static readonly Dictionary<Products, ProductData> _productsMapping = new Dictionary<Products, ProductData>
		{
			{ Products.Api, new ProductData { Id = 5, Name = "S#.API" } },
			{ Products.Hydra, new ProductData { Id = 8, Name = "S#.Data" } },
			{ Products.Designer, new ProductData { Id = 9, Name = "S#.Designer" } },
			{ Products.Terminal, new ProductData { Id = 10, Name = "S#.Terminal" } },
			{ Products.Shell, new ProductData { Id = 11, Name = "S#.Shell" } },
			{ Products.MatLab, new ProductData { Id = 12, Name = "S#.MatLab" } },
			{ Products.Lci, new ProductData { Id = 13, Name = "S#.Ë×È" } },
			{ Products.Server, new ProductData { Id = 14, Name = "S#.Server" } },
			{ Products.Updater, new ProductData { Id = 16, Name = "S#.Updater" } },
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

		/// <summary>
		/// Get product's public name.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <returns>Public name.</returns>
		public static string GetPublicName(this ProductData product)
		{
			if (product == null)
				throw new ArgumentNullException(nameof(product));

			var name = product.Name.Remove("S#.", true);

			if (name.CompareIgnoreCase("Data"))
				name = "Hydra";

			return name;
		}

		/// <summary>
		/// Get file list and their hashes in the specified folder.
		/// </summary>
		/// <param name="path">Path to the folder.</param>
		/// <returns>File list.</returns>
		public static Tuple<string, string>[] GetLocalFiles(string path)
		{
			var localFiles = Directory.GetFiles(path, "*.*", SearchOption.AllDirectories);
			return localFiles.Select(f =>
			{
				var name = f.Remove(path);

				if (name[0] == '\\')
					name = name.Substring(1);

				return Tuple.Create(name, File.ReadAllBytes(f).Md5());
			}).ToArray();
		}
	}
}