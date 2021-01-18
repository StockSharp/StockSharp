namespace StockSharp.Algo.Storages.Remote
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.IO;

	using Ecng.Common;
	using Ecng.Collections;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Algo.Storages.Remote.Messages;

	/// <summary>
	/// The client for access to the history server.
	/// </summary>
	public class RemoteStorageClient
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteStorageClient"/>.
		/// </summary>
		/// <param name="adapter">Message adapter.</param>
		public RemoteStorageClient(IMessageAdapter adapter)
		{
			Adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
		}

		/// <summary>
		/// Message adapter.
		/// </summary>
		public IMessageAdapter Adapter { get; }

		private int _securityBatchSize = 1000;

		/// <summary>
		/// The new instruments request block size. By default it does not exceed 1000 elements.
		/// </summary>
		public int SecurityBatchSize
		{
			get => _securityBatchSize;
			set
			{
				if (value <= 0)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

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
				return Do<SecurityMessage>(new SecurityLookupMessage { OnlySecurityId = true })
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
			var existingIds = securityProvider?.LookupAll().Select(s => s.Id.ToSecurityId()).ToSet() ?? new HashSet<SecurityId>();
			
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

			var securities = Do<SecurityMessage>(criteria).Select(s => s.SecurityId).ToArray();

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

				foreach (var security in Do<SecurityMessage>(new SecurityLookupMessage { SecurityIds = batch.ToArray() }))
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
			return Do<BoardMessage>(criteria);
		}

		/// <summary>
		/// Save securities.
		/// </summary>
		/// <param name="securities">Securities.</param>
		public void SaveSecurities(IEnumerable<SecurityMessage> securities)
		{
			Do(securities.ToArray());
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
		/// Get all available data types.
		/// </summary>
		/// <param name="securityId">Instrument identifier.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Data types.</returns>
		public IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
		{
			//if (securityId.IsDefault())
			//	throw new ArgumentNullException(nameof(securityId));

			return Do<AvailableDataInfoMessage>(new AvailableDataRequestMessage { SecurityId = securityId, Format = format })
				.Select(t => t.FileDataType).Distinct().ToArray();
		}

		/// <summary>
		/// Verify.
		/// </summary>
		public void Verify()
		{
			Do(new TimeMessage());
		}

		/// <summary>
		/// To get all the dates for which market data are recorded.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type info.</param>
		/// <param name="format">Storage format.</param>
		/// <returns>Dates.</returns>
		public IEnumerable<DateTime> GetDates(SecurityId securityId, DataType dataType, StorageFormats format)
		{
			return Do<AvailableDataInfoMessage>(new AvailableDataRequestMessage
			{
				SecurityId = securityId,
				RequestDataType = dataType,
				Format = format,
			}).Select(i => i.Date.UtcDateTime).ToArray();
		}

		/// <summary>
		/// To save data in the format of StockSharp storage.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type info.</param>
		/// <param name="format">Storage format.</param>
		/// <param name="date">Date.</param>
		/// <param name="stream"></param>
		public void SaveStream(SecurityId securityId, DataType dataType, StorageFormats format, DateTime date, Stream stream)
		{
			Do(new RemoteFileCommandMessage
			{
				Command = CommandTypes.Update,
				Scope = CommandScopes.File,
				SecurityId = securityId,
				FileDataType = dataType,
				StartDate = date,
				EndDate = date,
				Format = format,
				Body = stream.To<byte[]>(),
			});
		}

		/// <summary>
		/// To load data in the format of StockSharp storage.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type info.</param>
		/// <param name="format">Storage format.</param>
		/// <param name="date">Date.</param>
		/// <returns>Data in the format of StockSharp storage. If no data exists, <see cref="Stream.Null"/> will be returned.</returns>
		public Stream LoadStream(SecurityId securityId, DataType dataType, StorageFormats format, DateTime date)
		{
			return Do<RemoteFileMessage>(new RemoteFileCommandMessage
			{
				Command = CommandTypes.Get,
				Scope = CommandScopes.File,
				SecurityId = securityId,
				FileDataType = dataType,
				StartDate = date,
				EndDate = date,
				Format = format,
			}).FirstOrDefault()?.Body.To<Stream>() ?? Stream.Null;
		}

		/// <summary>
		/// To remove market data on specified date from the storage.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type info.</param>
		/// <param name="format">Storage format.</param>
		/// <param name="date">Date.</param>
		public void Delete(SecurityId securityId, DataType dataType, StorageFormats format, DateTime date)
		{
			Do(new RemoteFileCommandMessage
			{
				Command = CommandTypes.Remove,
				Scope = CommandScopes.File,
				SecurityId = securityId,
				FileDataType = dataType,
				Format = format,
				StartDate = date,
				EndDate = date,
			});
		}

		private void Do(params Message[] messages)
		{
			Adapter.TypedClone().Upload(messages);
		}

		private IEnumerable<TResult> Do<TResult>(Message message)
			where TResult : Message, IOriginalTransactionIdMessage
		{
			return Adapter.TypedClone().Download<TResult>(message);
		}
	}
}