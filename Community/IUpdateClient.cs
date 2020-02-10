namespace StockSharp.Community
{
	using System;

	/// <summary>
	/// The interface describing a client for access to <see cref="IUpdateService"/>.
	/// </summary>
	public interface IUpdateClient
	{
		/// <summary>
		/// All available products.
		/// </summary>
		ProductData[] Products { get; }

		/// <summary>
		/// Upload new version.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="releaseNotes">Release notes.</param>
		/// <param name="operationIds">List of operation, previously made by <see cref="IFileService.BeginUploadTemp"/>.</param>
		void Upload(ProductData product, string releaseNotes, Guid[] operationIds);

		/// <summary>
		/// Download a new version.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="localFiles">Local files info (name and hash).</param>
		/// <returns>List of files actions. Empty array means no any updates.</returns>
		Tuple<string, Tuple<string, Guid, bool>[]> Download(ProductData product, Tuple<string, string>[] localFiles);
	}
}