#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Community.Community
File: FileClient.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Community
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	/// <summary>
	/// The client for access to the service of work with files and documents.
	/// </summary>
	public class FileClient : BaseCommunityClient<IFileService>
	{
		private const int _partSize = 20 * 1024; // 10kb

		private readonly CachedSynchronizedDictionary<long, FileData> _cache = new CachedSynchronizedDictionary<long, FileData>(); 

		/// <summary>
		/// Initializes a new instance of the <see cref="FileClient"/>.
		/// </summary>
		public FileClient()
			: this("https://stocksharp.com/services/fileservice.svc".To<Uri>())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="FileClient"/>.
		/// </summary>
		/// <param name="address">Service address.</param>
		public FileClient(Uri address)
			: base(address, "file")
		{
		}

		/// <summary>
		/// To get the file data.
		/// </summary>
		/// <param name="id">File ID.</param>
		/// <param name="progress">Progress callback.</param>
		/// <param name="cancel">Cancel callback.</param>
		/// <returns>The file data.</returns>
		public FileData GetFile(long id, Action<int> progress = null, Func<bool> cancel = null)
		{
			var data = GetFileInfo(id);
			Download(data, progress, cancel);
			return data;
		}

		/// <summary>
		/// To get the file data.
		/// </summary>
		/// <param name="id">File ID.</param>
		/// <returns>The file data.</returns>
		public FileData GetFileInfo(long id)
		{
			return _cache.SafeAdd(id, key => Invoke(f => f.GetFileInfo(NullableSessionId ?? Guid.Empty, id)));
		}

		/// <summary>
		/// Download file.
		/// </summary>
		/// <param name="data">The file data.</param>
		/// <param name="progress">Progress callback.</param>
		/// <param name="cancel">Cancel callback.</param>
		/// <returns>If the operation was cancelled by <paramref name="cancel"/>, <see langword="false"/> will return.</returns>
		public bool Download(FileData data, Action<int> progress = null, Func<bool> cancel = null)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			if (data.Body != null)
				return true;

			var operationId = Invoke(f => f.BeginDownload(AuthenticationClient.Instance.SessionId, data.Id));

			var body = new List<byte>();

			while (body.Count < data.BodyLength)
			{
				if (cancel?.Invoke() == true)
				{
					Invoke(f => f.FinishDownload(operationId, true));
					return false;
				}

				body.AddRange(Invoke(f => f.ProcessDownload(operationId, body.Count, _partSize)));
				progress?.Invoke(body.Count);
			}

			Invoke(f => f.FinishDownload(operationId, false));
			data.Body = body.ToArray();
			return true;
		}

		/// <summary>
		/// To upload the existing file.
		/// </summary>
		/// <param name="data">File data.</param>
		/// <param name="progress">Progress callback.</param>
		/// <param name="cancel">Cancel callback.</param>
		public void Update(FileData data, Action<int> progress = null, Func<bool> cancel = null)
		{
			if (data == null)
				throw new ArgumentNullException(nameof(data));

			if (data.Body == null)
				throw new ArgumentException(nameof(data));

			if (data.Body.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(data));

			var operationId = Invoke(f => f.BeginUploadExisting(AuthenticationClient.Instance.SessionId, data.Id));
			Upload(operationId, data.Body, progress, cancel);
		}

		private long? Upload(Guid operationId, byte[] body, Action<int> progress, Func<bool> cancel)
		{
			var sentCount = 0;

			foreach (var part in body.Batch(_partSize))
			{
				if (cancel?.Invoke() == true)
				{
					Invoke(f => f.FinishUpload(operationId, true));
					return null;
				}

				var arr = part.ToArray();

				ValidateError(Invoke(f => f.ProcessUpload(operationId, arr)));

				sentCount += arr.Length;
				progress?.Invoke(sentCount);
			}

			var id = Invoke(f => f.FinishUpload(operationId, false));

			if (id < 0)
				ValidateError((byte)-id);

			return id;
		}

		/// <summary>
		/// To upload the file to the site.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <param name="body">File body.</param>
		/// <param name="isPublic">Is the file available for public.</param>
		/// <param name="progress">Progress callback.</param>
		/// <param name="cancel">Cancel callback.</param>
		/// <returns>File data. If the operation was cancelled by <paramref name="cancel"/>, <see langword="null"/> will return.</returns>
		public FileData Upload(string fileName, byte[] body, bool isPublic, Action<int> progress = null, Func<bool> cancel = null)
		{
			if (fileName.IsEmpty())
				throw new ArgumentNullException(nameof(fileName));

			if (body == null)
				throw new ArgumentNullException(nameof(body));

			if (body.Length == 0)
				throw new ArgumentOutOfRangeException(nameof(body));

			var operationId = Invoke(f => f.BeginUpload(AuthenticationClient.Instance.SessionId, fileName, isPublic));

			var id = Upload(operationId, body, progress, cancel);

			if (id == null)
				return null;

			var data = new FileData
			{
				Id = id.Value,
				FileName = fileName,
				Body = body,
				BodyLength = body.Length,
				IsPublic = isPublic,
				CreationDate = DateTime.UtcNow
			};

			_cache.Add(id.Value, data);

			return data;
		}

		/// <summary>
		/// To get a upload size limit.
		/// </summary>
		/// <returns>Upload size limit.</returns>
		public long GetUploadLimit()
		{
			return Invoke(f => f.GetUploadLimit(AuthenticationClient.Instance.SessionId));
		}

		private static void ValidateError(byte errorCode)
		{
			((ErrorCodes)errorCode).ThrowIfError();
		}
	}
}