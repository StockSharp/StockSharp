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
		FileData GetFile(Guid sessionId, long id);

		/// <summary>
		/// To upload the file to the site.
		/// </summary>
		/// <param name="sessionId">Session ID.</param>
		/// <param name="fileName">File name.</param>
		/// <param name="body">File body.</param>
		/// <param name="isPublic">Is the file available for public.</param>
		/// <returns>File ID.</returns>
		[OperationContract]
		long Upload(Guid sessionId, string fileName, byte[] body, bool isPublic);
	}
}