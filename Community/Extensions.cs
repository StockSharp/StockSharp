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
		private static readonly Dictionary<Products, ProductInfoMessage> _productsMapping = new Dictionary<Products, ProductInfoMessage>
		{
			{ Products.Api, new ProductInfoMessage { Id = 5, Name = "S#.API" } },
			{ Products.Hydra, new ProductInfoMessage { Id = 8, Name = "S#.Data", PackageId = "Hydra" } },
			{ Products.Designer, new ProductInfoMessage { Id = 9, Name = "S#.Designer", PackageId = "Designer" } },
			{ Products.Terminal, new ProductInfoMessage { Id = 10, Name = "S#.Terminal", PackageId = "Terminal" } },
			{ Products.Shell, new ProductInfoMessage { Id = 11, Name = "S#.Shell", PackageId = "Shell" } },
			{ Products.MatLab, new ProductInfoMessage { Id = 12, Name = "S#.MatLab", PackageId = "MatLab" } },
			{ Products.Lci, new ProductInfoMessage { Id = 13, Name = "S#.Ë×È", PackageId = "Lci" } },
			{ Products.Server, new ProductInfoMessage { Id = 14, Name = "S#.Server", PackageId = "Server" } },
			{ Products.Installer, new ProductInfoMessage { Id = 16, Name = "S#.Installer" } },
		};

		/// <summary>
		/// Convert <see cref="ProductInfoMessage"/> to <see cref="Products"/> value.
		/// </summary>
		/// <param name="product"><see cref="ProductInfoMessage"/> value.</param>
		/// <returns><see cref="Products"/> value.</returns>
		public static Products ToEnum(this ProductInfoMessage product)
		{
			if (product == null)
				throw new ArgumentNullException(nameof(product));

			return _productsMapping.First(p => p.Value.Id == product.Id).Key;
		}

		/// <summary>
		/// Convert <see cref="Products"/> to <see cref="ProductInfoMessage"/> value.
		/// </summary>
		/// <param name="product"><see cref="Products"/> value.</param>
		/// <returns><see cref="ProductInfoMessage"/> value.</returns>
		public static ProductInfoMessage FromEnum(this Products product)
		{
			return _productsMapping[product];
		}

		/// <summary>
		/// Get product's public name.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <returns>Public name.</returns>
		public static string GetPublicName(this ProductInfoMessage product)
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

				return Tuple.Create(name, File.ReadAllBytes(f).Hash());
			}).ToArray();
		}

		/// <summary>
		/// Get hash for the specified input.
		/// </summary>
		/// <param name="input">Input.</param>
		/// <returns>File hash.</returns>
		public static string Hash(this byte[] input) => input.Md5();
	}
}