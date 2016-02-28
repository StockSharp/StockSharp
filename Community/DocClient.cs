#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: DocClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
			: base(address, "doc")
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