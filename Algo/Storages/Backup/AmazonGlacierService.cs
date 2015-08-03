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
	/// Сервис хранения данных, основанный на Amazon Glacier http://aws.amazon.com/glacier/
	/// </summary>
	public class AmazonGlacierService : IBackupService
	{
		private readonly AmazonS3Client _client;
		private readonly string _vaultName;

		/// <summary>
		/// Создать <see cref="AmazonGlacierService"/>.
		/// </summary>
		/// <param name="endpoint">Адрес региона.</param>
		/// <param name="vaultName">Имя хранилища.</param>
		/// <param name="accessKey">Ключ.</param>
		/// <param name="secretKey">Секрет.</param>
		public AmazonGlacierService(RegionEndpoint endpoint, string vaultName, string accessKey, string secretKey)
		{
			if (vaultName.IsEmpty())
				throw new ArgumentNullException("vaultName");

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