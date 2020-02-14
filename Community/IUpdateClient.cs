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
		/// Check on availability of the newest version.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="localFiles">Local files info (name and hash).</param>
		/// <returns>List of files actions. Empty array means no any updates.</returns>
		string HasNewVersion(ProductData product, Tuple<string, string>[] localFiles);


		/// <summary>
		/// Get changes.
		/// </summary>
		/// <param name="product">Product.</param>
		/// <param name="localFiles">Local files info (name and hash).</param>
		/// <returns>List of files actions. Empty array means no any updates.</returns>
		Tuple<string, Guid, bool>[] GetChanges(ProductData product, Tuple<string, string>[] localFiles);
	}
}