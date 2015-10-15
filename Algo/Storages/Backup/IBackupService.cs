namespace StockSharp.Algo.Storages.Backup
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;

	/// <summary>
	/// The interface describing online data storage service.
	/// </summary>
	public interface IBackupService
	{
		/// <summary>
		/// List of files.
		/// </summary>
		/// <param name="parent">Element.</param>
		/// <returns>File list.</returns>
		IEnumerable<BackupEntry> Get(BackupEntry parent);

		/// <summary>
		/// Delete file from the service.
		/// </summary>
		/// <param name="entry">Element.</param>
		void Delete(BackupEntry entry);

		/// <summary>
		/// Save file.
		/// </summary>
		/// <param name="entry">Element.</param>
		/// <param name="stream">The thread of the open file that will be saved to the service.</param>
		/// <param name="progress">Progress notification.</param>
		/// <returns>Cancellation token.</returns>
		CancellationTokenSource Download(BackupEntry entry, Stream stream, Action<int> progress);

		/// <summary>
		/// Upload file.
		/// </summary>
		/// <param name="entry">Element.</param>
		/// <param name="stream">The thread of the open file into which data from the service will be downloaded.</param>
		/// <param name="progress">Progress notification.</param>
		/// <returns>Cancellation token.</returns>
		CancellationTokenSource Upload(BackupEntry entry, Stream stream, Action<int> progress);
	}
}