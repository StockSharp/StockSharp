namespace StockSharp.Community
{
	using System;

	/// <summary>
	/// The client for access to <see cref="IDocService"/>.
	/// </summary>
	public class DocClient : BaseCommunityClient<IDocService>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="DocClient"/>.
		/// </summary>
		public DocClient()
			: this(new Uri("http://stocksharp.com/services/docservice.svc"))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DocClient"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		public DocClient(Uri address)
			: base(address, "doc", true)
		{
		}

		/// <summary>
		/// To download the new version description.
		/// </summary>
		/// <param name="product">Product type.</param>
		/// <param name="version">New version.</param>
		/// <param name="description">New version description.</param>
		public void PostNewVersion(Products product, string version, string description)
		{
			Invoke(f => f.PostNewVersion(SessionId, product, version, description));
		}
	}
}