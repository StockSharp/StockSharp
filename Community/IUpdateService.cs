namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	/// <summary>
	/// The interface describing the updates service.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/updateservice.svc")]
	public interface IUpdateService
	{
		/// <summary>
		/// Get all products.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>All available products.</returns>
		[OperationContract]
		ProductData[] GetProducts(Guid sessionId);

		/// <summary>
		/// Check on availability of the newest version.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="productId">Product ID.</param>
		/// <param name="localFiles">Local files info (name and hash).</param>
		/// <returns>Check result.</returns>
		[OperationContract]
		bool HasNewVersion(Guid sessionId, long productId, Tuple<string, string>[] localFiles);

		/// <summary>
		/// Get changes.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="productId">Product ID.</param>
		/// <param name="localFiles">Local files info (name and hash).</param>
		/// <returns>List of files actions. Empty array means no any updates.</returns>
		[OperationContract]
		Tuple<string, Guid, bool>[] GetChanges(Guid sessionId, long productId, Tuple<string, string>[] localFiles);
	}
}