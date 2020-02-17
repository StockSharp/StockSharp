namespace StockSharp.Community
{
	using System;

	/// <summary>
	/// The interface describing a client for access to the service of work with files and documents.
	/// </summary>
	public interface IFileClient
	{
		/// <summary>
		/// Part size.
		/// </summary>
		int PartSize { get; set; }

		/// <summary>
		/// Use compression.
		/// </summary>
		bool Compression { get; set; }

		/// <summary>
		/// Check hash of downloaded files.
		/// </summary>
		bool CheckDownloadedHash { get; set; }

		/// <summary>
		/// To get the file data.
		/// </summary>
		/// <param name="id">File ID.</param>
		/// <param name="progress">Progress callback.</param>
		/// <param name="cancel">Cancel callback.</param>
		/// <returns>The file data.</returns>
		FileData GetFile(long id, Action<long> progress = null, Func<bool> cancel = null);

		/// <summary>
		/// To get the file data.
		/// </summary>
		/// <param name="id">File ID.</param>
		/// <returns>The file data.</returns>
		FileData GetFileInfo(long id);

		/// <summary>
		/// Download file.
		/// </summary>
		/// <param name="data">The file data.</param>
		/// <param name="progress">Progress callback.</param>
		/// <param name="cancel">Cancel callback.</param>
		/// <returns>If the operation was cancelled by <paramref name="cancel"/>, <see langword="false"/> will return.</returns>
		bool Download(FileData data, Action<long> progress = null, Func<bool> cancel = null);

		/// <summary>
		/// Download file.
		/// </summary>
		/// <param name="data">The file data.</param>
		/// <param name="operationId">Operation ID.</param>
		/// <param name="progress">Progress callback.</param>
		/// <param name="cancel">Cancel callback.</param>
		/// <returns>If the operation was cancelled by <paramref name="cancel"/>, <see langword="false"/> will return.</returns>
		bool DownloadTemp(FileData data, Guid operationId, Action<long> progress = null, Func<bool> cancel = null);

		/// <summary>
		/// To upload the existing file.
		/// </summary>
		/// <param name="data">File data.</param>
		/// <param name="progress">Progress callback.</param>
		/// <param name="cancel">Cancel callback.</param>
		void Update(FileData data, Action<long> progress = null, Func<bool> cancel = null);

		/// <summary>
		/// To upload the file to the site.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <param name="body">File body.</param>
		/// <param name="isPublic">Is the file available for public.</param>
		/// <param name="progress">Progress callback.</param>
		/// <param name="cancel">Cancel callback.</param>
		/// <returns>File data. If the operation was cancelled by <paramref name="cancel"/>, <see langword="null"/> will return.</returns>
		FileData Upload(string fileName, byte[] body, bool isPublic, Action<long> progress = null, Func<bool> cancel = null);

		/// <summary>
		/// To start uploading temp file to the site.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <param name="body">File body.</param>
		/// <param name="progress">Progress callback.</param>
		/// <param name="cancel">Cancel callback.</param>
		/// <returns>File data. If the operation was cancelled by <paramref name="cancel"/>, <see langword="null"/> will return.</returns>
		Guid? UploadTemp(string fileName, byte[] body, Action<long> progress = null, Func<bool> cancel = null);

		/// <summary>
		/// To get a upload size limit.
		/// </summary>
		/// <returns>Upload size limit.</returns>
		long GetUploadLimit();

		/// <summary>
		/// Share file.
		/// </summary>
		/// <param name="id">File ID.</param>
		/// <returns>Public token.</returns>
		string Share(long id);

		/// <summary>
		/// Undo <see cref="Share"/> operation.
		/// </summary>
		/// <param name="id">File ID.</param>
		void UnShare(long id);
	}
}