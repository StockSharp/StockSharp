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
	using System.Linq;
	using System.ServiceModel;
	using System.ServiceModel.Description;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	/// <summary>
	/// The client for access to the service of work with files and documents.
	/// </summary>
	public class FileClient : BaseCommunityClient<IFileService>
	{
		private readonly CachedSynchronizedDictionary<long, FileData> _cache = new CachedSynchronizedDictionary<long, FileData>(); 

		/// <summary>
		/// Initializes a new instance of the <see cref="FileClient"/>.
		/// </summary>
		public FileClient()
			: this("http://stocksharp.com/services/fileservice.svc".To<Uri>())
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
		/// Create WCF channel.
		/// </summary>
		/// <returns>WCF channel.</returns>
		protected override ChannelFactory<IFileService> CreateChannel()
		{
			var f = new ChannelFactory<IFileService>(new WSHttpBinding(SecurityMode.None)
			{
				OpenTimeout = TimeSpan.FromMinutes(5),
				SendTimeout = TimeSpan.FromMinutes(10),
				ReceiveTimeout = TimeSpan.FromMinutes(10),
				MaxReceivedMessageSize = int.MaxValue,
				ReaderQuotas =
				{
					MaxArrayLength = int.MaxValue,
					MaxBytesPerRead = int.MaxValue
				},
				MaxBufferPoolSize = int.MaxValue,
			}, new EndpointAddress(Address));

			foreach (var op in f.Endpoint.Contract.Operations)
			{
				var dataContractBehavior = op.Behaviors[typeof(DataContractSerializerOperationBehavior)] as DataContractSerializerOperationBehavior;

				if (dataContractBehavior != null)
					dataContractBehavior.MaxItemsInObjectGraph = int.MaxValue;
			}

			return f;
		}

		/// <summary>
		/// To get the file data.
		/// </summary>
		/// <param name="id">File ID.</param>
		/// <returns>The file data.</returns>
		public FileData GetFile(long id)
		{
			return _cache.SafeAdd(id, key => Invoke(f => f.GetFile(AuthenticationClient.Instance.TryGetSession ?? Guid.Empty, id)));
		}

		/// <summary>
		/// To upload the file to the site.
		/// </summary>
		/// <param name="fileName">File name.</param>
		/// <param name="body">File body.</param>
		/// <param name="isPublic">Is the file available for public.</param>
		/// <param name="progress">Progress callback.</param>
		/// <returns>File ID.</returns>
		public long Upload(string fileName, byte[] body, bool isPublic, Action<int> progress = null)
		{
			var operationId = Invoke(f => f.BeginUpload(AuthenticationClient.Instance.SessionId, fileName, isPublic));

			const int partSize = 20 * 1024; // 10kb

			var sentCount = 0;

			foreach (var part in body.Batch(partSize))
			{
				var arr = part.ToArray();

				ValidateError(Invoke(f => f.ProcessUpload(operationId, arr)));

				sentCount += arr.Length;
				progress?.Invoke(sentCount);
			}

			var id = Invoke(f => f.FinishUpload(operationId, false));

			if (id < 0)
				ValidateError((byte)-id);

			_cache.Add(id, new FileData
			{
				Id = id,
				FileName = fileName,
				Body = body,
				IsPublic = isPublic
			});

			return id;
		}

		private static void ValidateError(byte errorCode)
		{
			((ErrorCodes)errorCode).ThrowIfError();
		}
	}
}