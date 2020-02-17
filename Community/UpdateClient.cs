namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	/// <summary>
	/// The client for access to <see cref="IUpdateService"/>.
	/// </summary>
	public class UpdateClient : BaseCommunityClient<IUpdateService>, IUpdateClient
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="UpdateClient"/>.
		/// </summary>
		public UpdateClient()
			: this(new Uri("https://stocksharp.com/services/updateservice.svc"))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="UpdateClient"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		public UpdateClient(Uri address)
			: base(address, "update")
		{
		}

		/// <inheritdoc />
		protected override ChannelFactory<IUpdateService> CreateChannel()
		{
			var f = new ChannelFactory<IUpdateService>(new WSHttpBinding(SecurityMode.Transport)
			{
				MaxReceivedMessageSize = int.MaxValue,
				ReaderQuotas =
				{
					MaxArrayLength = int.MaxValue,
					MaxBytesPerRead = int.MaxValue
				},
				MaxBufferPoolSize = int.MaxValue
			}, new EndpointAddress(Address));

			return f;
		}

		private ProductData[] _products;

		/// <inheritdoc />
		public ProductData[] Products => _products ?? (_products = Invoke(f => f.GetProducts(SessionId)));

		/// <inheritdoc />
		public bool HasNewVersion(ProductData product, Tuple<string, string>[] localFiles)
		{
			if (product == null)
				throw new ArgumentNullException(nameof(product));

			if (localFiles == null)
				throw new ArgumentNullException(nameof(localFiles));

			return Invoke(f => f.HasNewVersion(SessionId, product.Id, localFiles));
		}

		/// <inheritdoc />
		public Tuple<string, Guid, bool, string, long>[] GetChanges(ProductData product, Tuple<string, string>[] localFiles)
		{
			if (product == null)
				throw new ArgumentNullException(nameof(product));

			if (localFiles == null)
				throw new ArgumentNullException(nameof(localFiles));

			return Invoke(f => f.GetChanges(SessionId, product.Id, localFiles));
		}
	}
}