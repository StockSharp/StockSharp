namespace StockSharp.Configuration
{
	using System;
	using System.Reflection;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

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

		/// <summary>
		/// <see cref="ICredentialsProvider.Save"/>
		/// </summary>
		/// <param name="provider"><see cref="ICredentialsProvider"/></param>
		/// <param name="credentials"><see cref="ServerCredentials"/></param>
		/// <param name="keepSecret">Save <see cref="ServerCredentials.Password"/> and <see cref="ServerCredentials.Token"/>.</param>
		public static void Save(this ICredentialsProvider provider, ServerCredentials credentials, bool keepSecret)
		{
			if (provider is null)
				throw new ArgumentNullException(nameof(provider));

			if (credentials is null)
				throw new ArgumentNullException(nameof(credentials));

			if (!keepSecret)
			{
				credentials = credentials.Clone();
				credentials.Password = credentials.Token = null;
			}

			provider.Save(credentials);
		}
	}
}