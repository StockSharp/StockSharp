namespace StockSharp.Community
{
	using System;
	using System.ServiceModel;

	/// <summary>
	/// The interface describing the service to work with files and documents.
	/// </summary>
	[ServiceContract(Namespace = "http://stocksharp.com/services/fileservice.svc")]
	public interface IFileService
	{
		/// <summary>
		/// To upload the file to the site .
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="fileName">File name.</param>
		/// <param name="body">File body.</param>
		/// <returns>Uploaded file link.</returns>
		[OperationContract]
		string Upload(Guid sessionId, string fileName, byte[] body);
	}
}