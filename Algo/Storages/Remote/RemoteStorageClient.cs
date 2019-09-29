namespace StockSharp.Algo.Storages.Remote
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;
	using System.Security;
	using System.ServiceModel;
	using System.ServiceModel.Description;

	using Ecng.Collections;
	using Ecng.Common;

	using MoreLinq;

	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Community;
	using StockSharp.Messages;

	/// <summary>
	/// The client for access to the history server <see cref="IRemoteStorage"/>.
	/// </summary>
	public class RemoteStorageClient : BaseCommunityClient<IRemoteStorage>
	{
		private readonly bool _streaming;

		private sealed class RemoteStorageDrive : IMarketDataStorageDrive
		{
			private readonly RemoteStorageClient _parent;
			private readonly string _securityId;
			private readonly string _dataType;
			private readonly string _arg;
			private readonly StorageFormats _format;

			public RemoteStorageDrive(RemoteStorageClient parent, string securityId, Type dataType, object arg, StorageFormats format, IMarketDataDrive drive)
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
				_dataType = dataType.Name;
				_arg = arg.To<string>();
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
						_dates = _parent
						       .Invoke(f => f.GetDates(_parent.SessionId, _securityId, _dataType, _arg, _format))
						       .Select(d => d.UtcKind())
						       .ToArray();

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
				_parent.Invoke(f => f.Delete(_parent.SessionId, _securityId, _dataType, _arg, date.ChangeKind(), _format));
			}

			void IMarketDataStorageDrive.SaveStream(DateTime date, Stream stream)
			{
				// mika
				// WCF streaming do not support several output parameters
				// http://stackoverflow.com/questions/1339857/wcf-using-streaming-with-message-contracts
				//_parent._factory.Value.Invoke(f => f.SaveStream(_parent._sessionId, _security.Id, _dataType, _arg, date, rawData));
				var rawData = stream.To<byte[]>();
				_parent.Invoke(f => f.Save(_parent.SessionId, _securityId, _dataType, _arg, date.ChangeKind(), _format, rawData));
			}

			Stream IMarketDataStorageDrive.LoadStream(DateTime date)
			{
				var stream = _parent.Invoke(f => f.LoadStream(_parent.SessionId, _securityId, _dataType, _arg, date.ChangeKind(), _format));

				var memStream = new MemoryStream();
				stream.CopyTo(memStream);
				memStream.Position = 0;

				return memStream.Length == 0 ? Stream.Null : memStream;
			}
		}

		/// <summary>
		/// Default address.
		/// </summary>
		public static readonly Uri DefaultUrl = "net.tcp://localhost:8000".To<Uri>();

		private readonly SynchronizedDictionary<Tuple<SecurityId, Type, object, StorageFormats>, RemoteStorageDrive> _remoteStorages = new SynchronizedDictionary<Tuple<SecurityId, Type, object, StorageFormats>, RemoteStorageDrive>();

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteStorageClient"/>.
		/// </summary>
		public RemoteStorageClient()
			: this(DefaultUrl)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteStorageClient"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		/// <param name="streaming">Data transfer via WCF Streaming.</param>
		public RemoteStorageClient(Uri address, bool streaming = true)
			: base(address, "remoteStorage")
		{
			_streaming = streaming;
			Credentials = new ServerCredentials();
			SecurityBatchSize = 1000;
		}

		internal RemoteMarketDataDrive Drive { get; set; }

		/// <summary>
		/// Information about the login and password for access to remote storage.
		/// </summary>
		public ServerCredentials Credentials { get; }

		private Guid _sessionId;

		/// <inheritdoc />
		protected override Guid SessionId => _sessionId;

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
				return Invoke(f => f.GetAvailableSecurities(SessionId))
					.Select(id => id.ToSecurityId())
					.ToArray();
			}
		}

		/// <inheritdoc />
		protected override ChannelFactory<IRemoteStorage> CreateChannel()
		{
			var f = new ChannelFactory<IRemoteStorage>(new NetTcpBinding(SecurityMode.None)
			{
				TransferMode = _streaming ? TransferMode.StreamedResponse : TransferMode.Buffered,
				OpenTimeout = TimeSpan.FromMinutes(5),
				SendTimeout = TimeSpan.FromMinutes(40),
				ReceiveTimeout = TimeSpan.FromMinutes(40),
				MaxReceivedMessageSize = int.MaxValue,
				ReaderQuotas =
				{
					MaxArrayLength = int.MaxValue,
					MaxBytesPerRead = int.MaxValue
				},
				MaxBufferSize = int.MaxValue,
				MaxBufferPoolSize = int.MaxValue
			}, new EndpointAddress(Address));

			foreach (var op in f.Endpoint.Contract.Operations)
			{
				if (op.Behaviors[typeof(DataContractSerializerOperationBehavior)] is DataContractSerializerOperationBehavior dataContractBehavior)
					dataContractBehavior.MaxItemsInObjectGraph = int.MaxValue;
			}

			return f;
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
			var existingIds = securityProvider?.LookupAll().Select(s => s.Id).ToHashSet(StringComparer.InvariantCultureIgnoreCase) ?? new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
			
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
		public void LookupSecurities(SecurityLookupMessage criteria, ISet<string> existingIds, Action<SecurityMessage> newSecurity, Func<bool> isCancelled, Action<int, int> updateProgress)
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

			var ids = Invoke(f => f.LookupSecurityIds(SessionId, criteria));

			var newSecurityIds = ids
				.Where(id => !existingIds.Contains(id))
				.ToArray();

			updateProgress(0, newSecurityIds.Length);

			var count = 0;

			foreach (var b in newSecurityIds.Batch(RemoteStorage.DefaultMaxSecurityCount))
			{
				if (isCancelled())
					break;

				var batch = b.ToArray();

				foreach (var security in Invoke(f => f.GetSecurities(SessionId, batch)))
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
			LookupSecurities(criteria, new HashSet<string>(), securities.Add, () => false, (i, c) => { });
			return securities.ToArray();
		}

		/// <summary>
		/// To find exchanges that match the filter <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">Message boards lookup for specified criteria.</param>
		/// <returns>Exchanges.</returns>
		public string[] LoadExchanges(BoardLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var codes = Invoke(f => f.LookupExchanges(SessionId, criteria));

			return Invoke(f => f.GetExchanges(SessionId, codes));
		}

		/// <summary>
		/// To find exchange boards that match the filter <paramref name="criteria" />.
		/// </summary>
		/// <param name="criteria">Message boards lookup for specified criteria.</param>
		/// <returns>Exchange boards.</returns>
		public BoardMessage[] LoadExchangeBoards(BoardLookupMessage criteria)
		{
			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var codes = Invoke(f => f.LookupExchangeBoards(SessionId, criteria));

			return Invoke(f => f.GetExchangeBoards(SessionId, codes));
		}

		/// <summary>
		/// Save securities.
		/// </summary>
		/// <param name="securities">Securities.</param>
		public void SaveSecurities(Security[] securities)
		{
			SaveSecurities(securities.Select(s => s.ToMessage()).ToArray());
		}

		/// <summary>
		/// Save securities.
		/// </summary>
		/// <param name="securities">Securities.</param>
		public void SaveSecurities(SecurityMessage[] securities)
		{
			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			if (securities.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(securities));

			Invoke(f => f.SaveSecurities(SessionId, securities));
		}

		/// <summary>
		/// Delete securities.
		/// </summary>
		/// <param name="securities">Securities.</param>
		public void DeleteSecurities(Security[] securities)
		{
			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			if (securities.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(securities));

			Invoke(f => f.DeleteSecurities(SessionId, securities.Select(s => s.Id).ToArray()));
		}

		/// <summary>
		/// Save exchanges.
		/// </summary>
		/// <param name="exchanges">Exchanges.</param>
		public void SaveExchanges(Exchange[] exchanges)
		{
			if (exchanges == null)
				throw new ArgumentNullException(nameof(exchanges));

			if (exchanges.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(exchanges));

			Invoke(f => f.SaveExchanges(SessionId, exchanges.Select(e => e.Name).ToArray()));
		}

		/// <summary>
		/// Delete exchanges.
		/// </summary>
		/// <param name="exchanges">Exchanges.</param>
		public void DeleteExchanges(Exchange[] exchanges)
		{
			if (exchanges == null)
				throw new ArgumentNullException(nameof(exchanges));

			if (exchanges.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(exchanges));

			Invoke(f => f.DeleteExchanges(SessionId, exchanges.Select(e => e.Name).ToArray()));
		}

		/// <summary>
		/// Save exchange boards.
		/// </summary>
		/// <param name="boards">Boards.</param>
		public void SaveExchangeBoards(ExchangeBoard[] boards)
		{
			var messages = boards.Select(b => b.ToMessage()).ToArray();

			if (messages.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(boards));

			Invoke(f => f.SaveExchangeBoards(SessionId, messages));
		}

		/// <summary>
		/// Delete exchange boards.
		/// </summary>
		/// <param name="boards">Boards.</param>
		public void DeleteExchangeBoards(ExchangeBoard[] boards)
		{
			if (boards == null)
				throw new ArgumentNullException(nameof(boards));

			if (boards.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(boards));

			Invoke(f => f.DeleteExchangeBoards(SessionId, boards.Select(e => e.Code).ToArray()));
		}

		private class RemoteExtendedStorage : IRemoteExtendedStorage
		{
			private class SecurityRemoteExtendedStorage : ISecurityRemoteExtendedStorage
			{
				private readonly RemoteExtendedStorage _parent;
				private readonly SecurityId _securityId;
				private readonly string _securityIdStr;

				public SecurityRemoteExtendedStorage(RemoteExtendedStorage parent, SecurityId securityId)
				{
					_parent = parent ?? throw new ArgumentNullException(nameof(parent));

					_securityId = securityId;
					_securityIdStr = securityId.ToStringId();
				}

				void ISecurityRemoteExtendedStorage.AddSecurityExtendedInfo(object[] fieldValues)
				{
					_parent._client.Invoke(f => f.AddSecurityExtendedInfo(_parent._client.SessionId, _parent._storageName, _securityIdStr, fieldValues.Select(v => v.To<string>()).ToArray()));
				}

				void ISecurityRemoteExtendedStorage.DeleteSecurityExtendedInfo()
				{
					_parent._client.Invoke(f => f.DeleteSecurityExtendedInfo(_parent._client.SessionId, _parent._storageName, _securityIdStr));
				}

				SecurityId ISecurityRemoteExtendedStorage.SecurityId => _securityId;
			}

			private readonly RemoteStorageClient _client;
			private readonly string _storageName;
			
			public RemoteExtendedStorage(RemoteStorageClient client, string storageName)
			{
				if (storageName.IsEmpty())
					throw new ArgumentNullException(nameof(storageName));

				_client = client ?? throw new ArgumentNullException(nameof(client));
				_storageName = storageName;
			}

			private Tuple<string, Type>[] _securityExtendedFields;

			public Tuple<string, Type>[] Fields
			{
				get
				{
					return _securityExtendedFields ?? (_securityExtendedFields = _client
						   .Invoke(f => f.GetSecurityExtendedFields(_client.SessionId, _storageName))
						   .Select(t => Tuple.Create(t.Item1, t.Item2.To<Type>()))
						   .ToArray());
				}
			}

			void IRemoteExtendedStorage.CreateSecurityExtendedFields(Tuple<string, Type>[] fields)
			{
				if (fields == null)
					throw new ArgumentNullException(nameof(fields));

				_client.Invoke(f => f.CreateSecurityExtendedFields(_client.SessionId, _storageName, fields.Select(t => Tuple.Create(t.Item1, Converter.GetAlias(t.Item2) ?? t.Item2.GetTypeName(false))).ToArray()));
			}

			string IRemoteExtendedStorage.StorageName => _storageName;

			IEnumerable<SecurityId> IRemoteExtendedStorage.Securities
			{
				get { return _client.Invoke(f => f.GetExtendedInfoSecurities(_client.SessionId, _storageName)).Select(id => id.ToSecurityId()); }
			}

			private readonly SynchronizedDictionary<SecurityId, ISecurityRemoteExtendedStorage> _securityStorages = new SynchronizedDictionary<SecurityId, ISecurityRemoteExtendedStorage>();

			ISecurityRemoteExtendedStorage IRemoteExtendedStorage.GetSecurityStorage(SecurityId securityId)
			{
				if (securityId.IsDefault())
					throw new ArgumentNullException(nameof(securityId));

				return _securityStorages.SafeAdd(securityId, key => new SecurityRemoteExtendedStorage(this, key));
			}

			Tuple<SecurityId, object[]>[] IRemoteExtendedStorage.GetAllExtendedInfo()
			{
				var fields = Fields;

				if (fields == null)
					return null;

				return _client
					.Invoke(f => f.GetAllExtendedInfo(_client.SessionId, _storageName))
						.Select(t => Tuple.Create(t.Item1.ToSecurityId(), t.Item2.Select((v, i) => v.To(fields[i].Item2)).ToArray()))
					.ToArray();
			}
		}

		/// <summary>
		/// Get security extended storage names.
		/// </summary>
		/// <returns>Storage names.</returns>
		public string[] GetSecurityExtendedStorages()
		{
			return Invoke(f => f.GetSecurityExtendedStorages(SessionId));
		}

		private readonly SynchronizedDictionary<string, RemoteExtendedStorage> _extendedStorages = new SynchronizedDictionary<string, RemoteExtendedStorage>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Get extended info storage.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <returns>Extended info storage.</returns>
		public IRemoteExtendedStorage GetExtendedStorage(string storageName)
		{
			return _extendedStorages.SafeAdd(storageName, key => new RemoteExtendedStorage(this, storageName));
		}

		/// <summary>
		/// Get users.
		/// </summary>
		/// <returns>Users info.</returns>
		public Tuple<string, IPAddress[], UserPermissions>[] GetUsers()
		{
			return Invoke(f => f.GetUsers(SessionId))
				.Select(t => Tuple.Create(t.Item1, t.Item2.Select(s => s.To<IPAddress>()).ToArray(), t.Item3))
				.ToArray();
		}

		/// <summary>
		/// Save user info.
		/// </summary>
		/// <param name="login">Login.</param>
		/// <param name="password">Password.</param>
		/// <param name="ipAddresses">IP address list.</param>
		/// <param name="permissions">Permissions.</param>
		public void SaveUser(string login, SecureString password, IPAddress[] ipAddresses, UserPermissions permissions)
		{
			Invoke(f => f.SaveUser(SessionId, login, password.UnSecure(), ipAddresses.Select(i => i.To<string>()).ToArray(), permissions));
		}

		/// <summary>
		/// Delete existing user.
		/// </summary>
		/// <param name="login">Login.</param>
		public void DeleteUser(string login)
		{
			Invoke(f => f.DeleteUser(SessionId, login));
		}

		/// <summary>
		/// Restart server.
		/// </summary>
		public void Restart()
		{
			Invoke(f => f.Restart(SessionId));
		}

		/// <summary>
		/// Start downloading.
		/// </summary>
		/// <returns><see langword="true"/>, if downloading was start, otherwise, <see langword="false"/>.</returns>
		public bool StartDownloading()
		{
			return Invoke(f => f.StartDownloading(SessionId));
		}

		/// <summary>
		/// Stop downloading.
		/// </summary>
		public void StopDownloading()
		{
			Invoke(f => f.StopDownloading(SessionId));
		}

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
			if (securityId.IsDefault())
				throw new ArgumentNullException(nameof(securityId));

			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			return _remoteStorages.SafeAdd(Tuple.Create(securityId, dataType, arg, format),
				key => new RemoteStorageDrive(this, securityId.ToStringId(), dataType, arg, format, Drive));
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

			return Invoke(f => f.GetAvailableDataTypes(SessionId, securityId.ToStringId(nullIfEmpty: true), format))
				.Select(t =>
				{
					var messageType = typeof(CandleMessage).To<string>().Replace(typeof(CandleMessage).Name, t.Item1).To<Type>();
					return DataType.Create(messageType, messageType.StringToMessageArg(t.Item2));
				})
				.ToArray();
		}

		/// <summary>
		/// To log in.
		/// </summary>
		public void Login()
		{
			_sessionId = base.Invoke(f => f.Login(Credentials.Email, Credentials.Password.UnSecure()));
		}

		/// <inheritdoc />
		protected override TResult Invoke<TResult>(Func<IRemoteStorage, TResult> handler)
		{
			if (SessionId == default)
				Login();

			try
			{
				return base.Invoke(handler);
			}
			catch (FaultException<ExceptionDetail> ex)
			{
				if (ex.Detail.Type != typeof(UnauthorizedAccessException).FullName)
					throw;

				Login();
				return base.Invoke(handler);
			}
		}

		/// <inheritdoc />
		protected override void DisposeManaged()
		{
			if (SessionId != default)
			{
				Invoke(f => f.Logout(SessionId));
				_sessionId = default;
			}

			base.DisposeManaged();
		}
	}
}