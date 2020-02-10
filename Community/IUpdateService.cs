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
		/// Upload new version.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="productId">Product ID.</param>
		/// <param name="releaseNotes">Release notes.</param>
		/// <param name="operationIds">List of operation, previously made by <see cref="IFileService.BeginUploadTemp"/>.</param>
		[OperationContract]
		void Upload(Guid sessionId, long productId, string releaseNotes, Guid[] operationIds);

		/// <summary>
		/// Download a new version.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="productId">Product ID.</param>
		/// <param name="localFiles">Local files info (name and hash).</param>
		/// <returns>List of files actions. Empty array means no any updates.</returns>
		[OperationContract]
		Tuple<string, Tuple<string, Guid, bool>[]> Download(Guid sessionId, long productId, Tuple<string, string>[] localFiles);
	}
}