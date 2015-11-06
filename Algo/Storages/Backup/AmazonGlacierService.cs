namespace StockSharp.Algo.Storages.Backup
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Threading;

	using Amazon;
	using Amazon.Runtime;
	using Amazon.S3;

	using Ecng.Common;

	/// <summary>
	/// The data storage service based on Amazon Glacier http://aws.amazon.com/glacier/.
	/// </summary>
	public class AmazonGlacierService : IBackupService
	{
		private readonly AmazonS3Client _client;
		private readonly string _vaultName;

		/// <summary>
		/// Initializes a new instance of the <see cref="AmazonGlacierService"/>.
		/// </summary>
		/// <param name="endpoint">Region address.</param>
		/// <param name="vaultName">Storage name.</param>
		/// <param name="accessKey">Key.</param>
		/// <param name="secretKey">Secret.</param>
		public AmazonGlacierService(RegionEndpoint endpoint, string vaultName, string accessKey, string secretKey)
		{
			if (vaultName.IsEmpty())
				throw new ArgumentNullException(nameof(vaultName));

			_client = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), endpoint);
			_vaultName = vaultName;
		}

		IEnumerable<BackupEntry> IBackupService.Get(BackupEntry parent)
		{
			throw new NotImplementedException();
		}

		void IBackupService.Delete(BackupEntry entry)
		{
			throw new NotImplementedException();
		}

		CancellationTokenSource IBackupService.Download(BackupEntry entry, Stream stream, Action<int> progress)
		{
			throw new NotImplementedException();
		}

		CancellationTokenSource IBackupService.Upload(BackupEntry entry, Stream stream, Action<int> progress)
		{
			throw new NotImplementedException();
		}
	}
}