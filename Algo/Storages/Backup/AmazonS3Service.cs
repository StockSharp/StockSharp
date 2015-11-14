namespace StockSharp.Algo.Storages.Backup
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Threading;

	using Amazon;
	using Amazon.Runtime;
	using Amazon.S3;
	using Amazon.S3.Model;

	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// The data storage service based on Amazon S3 http://aws.amazon.com/s3/.
	/// </summary>
	public class AmazonS3Service : IBackupService
	{
		private readonly string _bucket;
		private readonly AmazonS3Client _client;
		private const int _bufferSize = 1024 * 1024 * 10; // 10mb

		/// <summary>
		/// Initializes a new instance of the <see cref="AmazonS3Service"/>.
		/// </summary>
		/// <param name="endpoint">Region address.</param>
		/// <param name="bucket">Storage name.</param>
		/// <param name="accessKey">Key.</param>
		/// <param name="secretKey">Secret.</param>
		public AmazonS3Service(RegionEndpoint endpoint, string bucket, string accessKey, string secretKey)
		{
			if (bucket.IsEmpty())
				throw new ArgumentNullException(nameof(bucket));

			_client = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), endpoint);
			_bucket = bucket;
		}

		IEnumerable<BackupEntry> IBackupService.Get(BackupEntry parent)
		{
			//if (parent != null && !parent.IsDirectory)
			//	throw new ArgumentException("{0} should be directory.".Put(parent.Name), "parent");

			var request = new ListObjectsRequest
			{
				BucketName = _bucket,
				Prefix = parent != null ? GetKey(parent) : null,
			};

			do
			{
				var response = _client.ListObjects(request);

				foreach (var entry in response.S3Objects)
				{
					var be = GetPath(entry.Key);
					be.Size = entry.Size;
					yield return be;
				}

				foreach (var commonPrefix in response.CommonPrefixes)
				{
					yield return new BackupEntry
					{
						Name = commonPrefix,
						Parent = parent,
					};
				}

				if (response.IsTruncated)
					request.Marker = response.NextMarker;
				else
					break;
			}
			while (true);
		}

		void IBackupService.Delete(BackupEntry entry)
		{
			_client.DeleteObject(_bucket, GetKey(entry));
		}

		// TODO make async

		CancellationTokenSource IBackupService.Download(BackupEntry entry, Stream stream, Action<int> progress)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (progress == null)
				throw new ArgumentNullException(nameof(progress));

			var source = new CancellationTokenSource();

			var key = GetKey(entry);

			var request = new GetObjectRequest
			{
				BucketName = _bucket,
				Key = key,
			};

			var bytes = new byte[_bufferSize];
			var readTotal = 0L;

			using (var response = _client.GetObject(request))
			using (var responseStream = response.ResponseStream)
			{
				response.WriteObjectProgressEvent += (s, a) => progress(a.PercentDone);

				while (readTotal < response.ContentLength)
				{
					var read = responseStream.Read(bytes, 0, bytes.Length);

					if (read <= 0)
						throw new InvalidOperationException(LocalizedStrings.NetworkConnectionError.Put(read));

					stream.Write(bytes, 0, read);

					readTotal += read;
				}
			}

			return source;
		}

		CancellationTokenSource IBackupService.Upload(BackupEntry entry, Stream stream, Action<int> progress)
		{
			if (entry == null)
				throw new ArgumentNullException(nameof(entry));

			if (stream == null)
				throw new ArgumentNullException(nameof(stream));

			if (progress == null)
				throw new ArgumentNullException(nameof(progress));

			var key = GetKey(entry);

			var initResponse = _client.InitiateMultipartUpload(new InitiateMultipartUploadRequest
			{
				BucketName = _bucket,
				Key = key,
			});

			var filePosition = 0L;
			var nextProgress = 1;

			var etags = new List<PartETag>();

			var partNum = 1;

			while (filePosition < stream.Length)
			{
				var response = _client.UploadPart(new UploadPartRequest
				{
					BucketName = _bucket,
					UploadId = initResponse.UploadId,
					PartNumber = partNum,
					PartSize = _bufferSize,
					//FilePosition = filePosition,
					InputStream = stream,
					Key = key
				});

				etags.Add(new PartETag(partNum, response.ETag));

				filePosition += _bufferSize;

				var currProgress = (int)(filePosition.Min(stream.Length) * 100 / stream.Length);

				if (currProgress >= nextProgress)
				{
					progress(currProgress);
					nextProgress = currProgress + 1;
				}

				partNum++;
			}

			_client.CompleteMultipartUpload(new CompleteMultipartUploadRequest
			{
				BucketName = _bucket,
				UploadId = initResponse.UploadId,
				Key = key,
				PartETags = etags
			});

			var source = new CancellationTokenSource();

			return source;
		}

		private static string GetKey(BackupEntry entry)
		{
			var key = entry.Name;

			if (key.IsEmpty())
				throw new ArgumentException("entry");

			if (entry.Parent != null)
				key = GetKey(entry.Parent) + "/" + key;

			return key;
		}

		private static BackupEntry GetPath(string key)
		{
			var entities = key.Split('/').Select(p => new BackupEntry { Name = p }).ToArray();

			BackupEntry parent = null;

			foreach (var entity in entities)
			{
				entity.Parent = parent;
				parent = entity;
			}

			return entities.Last();
		}
	}
}