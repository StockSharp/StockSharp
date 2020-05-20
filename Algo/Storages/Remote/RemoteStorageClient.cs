namespace StockSharp.Algo.Storages.Remote
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.ComponentModel;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Algo.Storages.Remote.Messages;

	/// <summary>
	/// The client for access to the history server.
	/// </summary>
	public class RemoteStorageClient : Disposable
	{
		private sealed class RemoteStorageDrive : IMarketDataStorageDrive
		{
			private readonly RemoteStorageClient _parent;
			private readonly SecurityId _securityId;
			private readonly DataType _dataType;
			private readonly StorageFormats _format;

			public RemoteStorageDrive(RemoteStorageClient parent, SecurityId securityId, DataType dataType, StorageFormats format, IMarketDataDrive drive)
			{
				if (securityId.IsDefault())
					throw new ArgumentNullException(nameof(securityId));

				if (dataType == null)
					throw new ArgumentNullException(nameof(dataType));

				// TODO
				//if (drive == null)
				//	throw new ArgumentNullException(nameof(drive));

				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
				_securityId = securityId;
				_dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
				_format = format;
				_drive = drive;
			}

			private readonly IMarketDataDrive _drive;

			IMarketDataDrive IMarketDataStorageDrive.Drive => _drive;

			private IEnumerable<DateTime> _dates;
			private DateTime _prevDatesSync;

			IEnumerable<DateTime> IMarketDataStorageDrive.Dates
			{
				get
				{
					if (_prevDatesSync.IsDefault() || (DateTime.Now - _prevDatesSync).TotalSeconds > 3)
					{
						_dates = _parent.Do<AvailableDataRequestMessage, AvailableDataInfoMessage>(new AvailableDataRequestMessage
						{
							SecurityId = _securityId,
							RequestDataType = _dataType,
							Format = _format,
						}).Select(i => i.Date.UtcDateTime).ToArray();

						_prevDatesSync = DateTime.Now;
					}

					return _dates;
				}
			}

			void IMarketDataStorageDrive.ClearDatesCache()
			{
				//_parent.Invoke(f => f.ClearDatesCache(_parent.SessionId, _security.Id, _dataType, _arg));
			}

			void IMarketDataStorageDrive.Delete(DateTime date)
			{
				_parent.Do(new RemoteFileCommandMessage
				{
					Command = CommandTypes.Remove,
					Scope = CommandScopes.File,
					SecurityId = _securityId,
					FileDataType = _dataType,
					Format = _format,
					StartDate = date,
					EndDate = date,
				});
			}

			void IMarketDataStorageDrive.SaveStream(DateTime date, Stream stream)
			{
				_parent.Do(new RemoteFileCommandMessage
				{
					Command = CommandTypes.Update,
					Scope = CommandScopes.File,
					SecurityId = _securityId,
					FileDataType = _dataType,
					StartDate = date,
					EndDate = date,
					Format = _format,
					Body = stream.To<byte[]>(),
				});
			}

			Stream IMarketDataStorageDrive.LoadStream(DateTime date)
			{
				return _parent.Do<RemoteFileCommandMessage, RemoteFileMessage>(new RemoteFileCommandMessage
				{
					Command = CommandTypes.Get,
					Scope = CommandScopes.File,
					SecurityId = _securityId,
					FileDataType = _dataType,
					StartDate = date,
					EndDate = date,
					Format = _format,
				}).FirstOrDefault()?.Body.To<Stream>() ?? Stream.Null;
			}
		}

		private readonly SynchronizedDictionary<Tuple<SecurityId, DataType, StorageFormats>, RemoteStorageDrive> _remoteStorages = new SynchronizedDictionary<Tuple<SecurityId, DataType, StorageFormats>, RemoteStorageDrive>();

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteStorageClient"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		public RemoteStorageClient(EndPoint address)
		{
			Address = address ?? throw new ArgumentNullException(nameof(address));
			Credentials = new ServerCredentials();
			SecurityBatchSize = 1000;
		}

		/// <summary>
		/// Server address.
		/// </summary>
		public EndPoint Address { get; }

		internal RemoteMarketDataDrive Drive { get; set; }

		/// <summary>
		/// Information about the login and password for access to remote storage.
		/// </summary>
		public ServerCredentials Credentials { get; }

		private int _securityBatchSize;

		/// <summary>
		/// The new instruments request block size. By default it does not exceed 1000 elements.
		/// </summary>
		public int SecurityBatchSize
		{
			get => _securityBatchSize;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException();

				_securityBatchSize = value;
			}
		}

		/// <summary>
		/// Get all available instruments.
		/// </summary>
		public IEnumerable<SecurityId> AvailableSecurities
		{
			get
			{
				return Do<SecurityLookupMessage, SecurityMessage>(new SecurityLookupMessage { OnlySecurityId = true })
					.Select(s => s.SecurityId)
					.ToArray();
			}
		}

		/// <summary>
		/// Download securities by the specified criteria.
		/// </summary>
		/// <param name="criteria">Message security lookup for specified criteria.</param>
		/// <param name="securityProvider">The provider of information about instruments.</param>
		/// <param name="newSecurity">The handler through which a new instrument will be passed.</param>
		/// <param name="isCancelled">The handler which returns an attribute of search cancel.</param>
		/// <param name="updateProgress">The handler through which a progress change will be passed.</param>
		public void LookupSecurities(SecurityLookupMessage criteria, ISecurityProvider securityProvider, Action<SecurityMessage> newSecurity, Func<bool> isCancelled, Action<int, int> updateProgress)
		{
			var existingIds = securityProvider?.LookupAll().Select(s => s.Id.ToSecurityId()).ToHashSet() ?? new HashSet<SecurityId>();
			
			LookupSecurities(criteria, existingIds, newSecurity, isCancelled, updateProgress);
		}

		/// <summary>
		/// Download securities by the specified criteria.
		/// </summary>
		/// <param name="criteria">Message security lookup for specified criteria.</param>
		/// <param name="existingIds">Existing securities.</param>
		/// <param name="newSecurity">The handler through which a new instrument will be passed.</param>
		/// <param name="isCancelled">The handler which returns an attribute of search cancel.</param>
		/// <param name="updateProgress">The handler through which a progress change will be passed.</param>
		public void LookupSecurities(SecurityLookupMessage criteria, ISet<SecurityId> existingIds, Action<SecurityMessage> newSecurity, Func<bool> isCancelled, Action<int, int> updateProgress)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			if (existingIds == null)
				throw new ArgumentNullException(nameof(existingIds));

			if (newSecurity == null)
				throw new ArgumentNullException(nameof(newSecurity));

			if (isCancelled == null)
				throw new ArgumentNullException(nameof(isCancelled));

			if (updateProgress == null)
				throw new ArgumentNullException(nameof(updateProgress));

			criteria = criteria.TypedClone();
			criteria.OnlySecurityId = true;

			var securities = Do<SecurityLookupMessage, SecurityMessage>(criteria).Select(s => s.SecurityId).ToArray();

			var newSecurityIds = securities
				.Where(id => !existingIds.Contains(id))
				.ToArray();

			updateProgress(0, newSecurityIds.Length);

			var count = 0;

			foreach (var b in newSecurityIds.Batch(SecurityBatchSize))
			{
				if (isCancelled())
					break;

				var batch = b.ToArray();

				foreach (var security in Do<SecurityLookupMessage, SecurityMessage>(new SecurityLookupMessage { SecurityIds = batch.ToArray() }))
					newSecurity(security);

				count += batch.Length;

				updateProgress(count, newSecurityIds.Length);
			}
		}

		/// <summary>
		/// To find securities that match the filter <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">Message security lookup for specified criteria.</param>
		/// <returns>Securities.</returns>
		public SecurityMessage[] LoadSecurities(SecurityLookupMessage criteria)
		{
			var securities = new List<SecurityMessage>();
			LookupSecurities(criteria, new HashSet<SecurityId>(), securities.Add, () => false, (i, c) => { });
			return securities.ToArray();
		}

		/// <summary>
		/// To find exchange boards that match the filter <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">Message boards lookup for specified criteria.</param>
		/// <returns>Exchange boards.</returns>
		public IEnumerable<BoardMessage> LoadExchangeBoards(BoardLookupMessage criteria)
		{
			return Do<BoardLookupMessage, BoardMessage>(criteria);
		}

		/// <summary>
		/// Save securities.
		/// </summary>
		/// <param name="securities">Securities.</param>
		public void SaveSecurities(IEnumerable<SecurityMessage> securities)
		{
			Do(securities);
		}

		//private class RemoteExtendedStorage : IRemoteExtendedStorage
		//{
		//	private class SecurityRemoteExtendedStorage : ISecurityRemoteExtendedStorage
		//	{
		//		private readonly RemoteExtendedStorage _parent;
		//		private readonly SecurityId _securityId;
		//		private readonly string _securityIdStr;

		//		public SecurityRemoteExtendedStorage(RemoteExtendedStorage parent, SecurityId securityId)
		//		{
		//			_parent = parent ?? throw new ArgumentNullException(nameof(parent));

		//			_securityId = securityId;
		//			_securityIdStr = securityId.ToStringId();
		//		}

		//		void ISecurityRemoteExtendedStorage.AddSecurityExtendedInfo(object[] fieldValues)
		//		{
		//			_parent._client.Invoke(f => f.AddSecurityExtendedInfo(_parent._client.SessionId, _parent._storageName, _securityIdStr, fieldValues.Select(v => v.To<string>()).ToArray()));
		//		}

		//		void ISecurityRemoteExtendedStorage.DeleteSecurityExtendedInfo()
		//		{
		//			_parent._client.Invoke(f => f.DeleteSecurityExtendedInfo(_parent._client.SessionId, _parent._storageName, _securityIdStr));
		//		}

		//		SecurityId ISecurityRemoteExtendedStorage.SecurityId => _securityId;
		//	}

		//	private readonly RemoteStorageClient _client;
		//	private readonly string _storageName;
			
		//	public RemoteExtendedStorage(RemoteStorageClient client, string storageName)
		//	{
		//		if (storageName.IsEmpty())
		//			throw new ArgumentNullException(nameof(storageName));

		//		_client = client ?? throw new ArgumentNullException(nameof(client));
		//		_storageName = storageName;
		//	}

		//	private Tuple<string, Type>[] _securityExtendedFields;

		//	public Tuple<string, Type>[] Fields
		//	{
		//		get
		//		{
		//			return _securityExtendedFields ?? (_securityExtendedFields = _client
		//				   .Invoke(f => f.GetSecurityExtendedFields(_client.SessionId, _storageName))
		//				   .Select(t => Tuple.Create(t.Item1, t.Item2.To<Type>()))
		//				   .ToArray());
		//		}
		//	}

		//	void IRemoteExtendedStorage.CreateSecurityExtendedFields(Tuple<string, Type>[] fields)
		//	{
		//		if (fields == null)
		//			throw new ArgumentNullException(nameof(fields));

		//		_client.Invoke(f => f.CreateSecurityExtendedFields(_client.SessionId, _storageName, fields.Select(t => Tuple.Create(t.Item1, Converter.GetAlias(t.Item2) ?? t.Item2.GetTypeName(false))).ToArray()));
		//	}

		//	string IRemoteExtendedStorage.StorageName => _storageName;

		//	IEnumerable<SecurityId> IRemoteExtendedStorage.Securities
		//	{
		//		get { return _client.Invoke(f => f.GetExtendedInfoSecurities(_client.SessionId, _storageName)).Select(id => id.ToSecurityId()); }
		//	}

		//	private readonly SynchronizedDictionary<SecurityId, ISecurityRemoteExtendedStorage> _securityStorages = new SynchronizedDictionary<SecurityId, ISecurityRemoteExtendedStorage>();

		//	ISecurityRemoteExtendedStorage IRemoteExtendedStorage.GetSecurityStorage(SecurityId securityId)
		//	{
		//		if (securityId.IsDefault())
		//			throw new ArgumentNullException(nameof(securityId));

		//		return _securityStorages.SafeAdd(securityId, key => new SecurityRemoteExtendedStorage(this, key));
		//	}

		//	Tuple<SecurityId, object[]>[] IRemoteExtendedStorage.GetAllExtendedInfo()
		//	{
		//		var fields = Fields;

		//		if (fields == null)
		//			return null;

		//		return _client
		//			.Invoke(f => f.GetAllExtendedInfo(_client.SessionId, _storageName))
		//				.Select(t => Tuple.Create(t.Item1.ToSecurityId(), t.Item2.Select((v, i) => v.To(fields[i].Item2)).ToArray()))
		//			.ToArray();
		//	}
		//}

		///// <summary>
		///// Get security extended storage names.
		///// </summary>
		///// <returns>Storage names.</returns>
		//public string[] GetSecurityExtendedStorages()
		//{
		//	return Invoke(f => f.GetSecurityExtendedStorages(SessionId));
		//}

		//private readonly SynchronizedDictionary<string, RemoteExtendedStorage> _extendedStorages = new SynchronizedDictionary<string, RemoteExtendedStorage>(StringComparer.InvariantCultureIgnoreCase);

		///// <summary>
		///// Get extended info storage.
		///// </summary>
		///// <param name="storageName">Storage name.</param>
		///// <returns>Extended info storage.</returns>
		//public IRemoteExtendedStorage GetExtendedStorage(string storageName)
		//{
		//	return _extendedStorages.SafeAdd(storageName, key => new RemoteExtendedStorage(this, storageName));
		//}

		/// <summary>
		/// To get a wrapper for access to remote market data.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="format">Format type.</param>
		/// <returns>The wrapper for access to remote market data.</returns>
		public IMarketDataStorageDrive GetRemoteStorage(SecurityId securityId, Type dataType, object arg, StorageFormats format)
		{
			if (securityId == default)
				throw new ArgumentNullException(nameof(securityId));

			var dt = DataType.Create(dataType, arg);

			return _remoteStorages.SafeAdd(Tuple.Create(securityId, dt, format),
				key => new RemoteStorageDrive(this, securityId, dt, format, Drive));
		}

		/// <summary>
		/// Get all available data types.
		/// </summary>
		/// <param name="securityId">Instrument identifier.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Data types.</returns>
		public IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
		{
			//if (securityId.IsDefault())
			//	throw new ArgumentNullException(nameof(securityId));

			return Do<AvailableDataRequestMessage, AvailableDataInfoMessage>(new AvailableDataRequestMessage { SecurityId = securityId, Format = format })
				.Select(t => t.FileDataType).ToArray();
		}

		/// <summary>
		/// Verify.
		/// </summary>
		public void Verify()
		{
			Do(new TimeMessage());
		}

		private void Do<TMessage>(TMessage message)
		{
			//return default;
		}

		private void Do<TMessage>(IEnumerable<TMessage> message)
		{
			//return default;
		}

		private IEnumerable<TResult> Do<TMessage, TResult>(TMessage message)
		{
			return default;
		}
	}
}