#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp Algo Trading, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Backup.Algo
File: AmazonGlacierService.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp Algo Trading, LLC
*******************************************************************************************/
#endregion S# License
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