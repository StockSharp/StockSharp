namespace StockSharp.Community
{
	using System;

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

		private ProductData[] _products;

		/// <inheritdoc />
		public ProductData[] Products => _products ?? (_products = Invoke(f => f.GetProducts(SessionId)));

		/// <inheritdoc />
		public void Upload(ProductData product, string releaseNotes, Guid[] operationIds)
		{
			if (product == null)
				throw new ArgumentNullException(nameof(product));

			if (operationIds == null)
				throw new ArgumentNullException(nameof(operationIds));

			if (operationIds.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(operationIds));

			Invoke(f => f.Upload(SessionId, product.Id, releaseNotes, operationIds));
		}

		/// <inheritdoc />
		public Tuple<string, Tuple<string, Guid, bool>[]> Download(ProductData product, Tuple<string, string>[] localFiles)
		{
			if (product == null)
				throw new ArgumentNullException(nameof(product));

			if (localFiles == null)
				throw new ArgumentNullException(nameof(localFiles));

			return Invoke(f => f.Download(SessionId, product.Id, localFiles));
		}
	}
}