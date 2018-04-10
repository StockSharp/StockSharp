#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: IFileService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
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
		/// To get the file data.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="id">File ID.</param>
		/// <returns>The file data.</returns>
		[OperationContract]
		FileData GetFileInfo(Guid sessionId, long id);

		/// <summary>
		/// To start downloading the file.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="id">File ID.</param>
		/// <returns>Operation ID.</returns>
		[OperationContract]
		Guid BeginDownload(Guid sessionId, long id);

		/// <summary>
		/// Download part of file.
		/// </summary>
		/// <param name="operationId">Operation ID, received from <see cref="BeginDownload"/>.</param>
		/// <param name="startIndex">The zero-based byte offset in file.</param>
		/// <param name="count">The maximum number of bytes to be read.</param>
		/// <returns>The part of file.</returns>
		[OperationContract]
		byte[] ProcessDownload(Guid operationId, int startIndex, int count);

		/// <summary>
		/// To finish downloading the file.
		/// </summary>
		/// <param name="operationId">Operation ID, received from <see cref="BeginDownload"/>.</param>
		/// <param name="isCancel">Cancel the operation.</param>
		[OperationContract]
		void FinishDownload(Guid operationId, bool isCancel);

		/// <summary>
		/// To start uploading the file to the site.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="fileName">File name.</param>
		/// <param name="isPublic">Is the file available for public.</param>
		/// <returns>Operation ID.</returns>
		[OperationContract]
		Guid BeginUpload(Guid sessionId, string fileName, bool isPublic);

		/// <summary>
		/// To start uploading the file to the site.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="id">File ID.</param>
		/// <returns>Operation ID.</returns>
		[OperationContract]
		Guid BeginUploadExisting(Guid sessionId, long id);

		/// <summary>
		/// Upload part of file.
		/// </summary>
		/// <param name="operationId">Operation ID, received from <see cref="BeginUpload"/> or <see cref="BeginUploadExisting"/>.</param>
		/// <param name="bodyPart">The part of file.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte ProcessUpload(Guid operationId, byte[] bodyPart);

		/// <summary>
		/// To finish uploading the file.
		/// </summary>
		/// <param name="operationId">Operation ID, received from <see cref="BeginUpload"/> or <see cref="BeginUploadExisting"/>.</param>
		/// <param name="isCancel">Cancel the operation.</param>
		/// <returns>File ID.</returns>
		[OperationContract]
		long FinishUpload(Guid operationId, bool isCancel);

		/// <summary>
		/// To delete the file.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="id">File ID.</param>
		/// <returns>The execution result code.</returns>
		[OperationContract]
		byte Delete(Guid sessionId, long id);

		/// <summary>
		/// To get a upload size limit.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <returns>Upload size limit.</returns>
		[OperationContract]
		long GetUploadLimit(Guid sessionId);
	}
}