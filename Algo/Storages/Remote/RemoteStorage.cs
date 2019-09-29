namespace StockSharp.Algo.Storages.Remote
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Net;

	using StockSharp.Community;
	using StockSharp.Logging;
	using StockSharp.Algo.Storages;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The external market data storage access to which is organized through the WCF network connection (for more details see <see cref="System.ServiceModel"/>).
	/// </summary>
	[ErrorLogging]
	public abstract class RemoteStorage : BaseLogReceiver, IRemoteStorage
	{
		private readonly SynchronizedDictionary<Guid, SynchronizedDictionary<UserPermissions, SynchronizedDictionary<Tuple<string, string, string, DateTime?>, bool>>> _sessions = new SynchronizedDictionary<Guid, SynchronizedDictionary<UserPermissions, SynchronizedDictionary<Tuple<string, string, string, DateTime?>, bool>>>();
		private readonly SynchronizedDictionary<string, Type> _dataTypes = new SynchronizedDictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Initialize <see cref="RemoteStorage"/>.
		/// </summary>
		/// <param name="storageRegistry">Market-data storage.</param>
		/// <param name="securityStorage">Securities meta info storage.</param>
		/// <param name="extendedInfoStorage">Extended info <see cref="Message.ExtensionInfo"/> storage.</param>
		/// <param name="exchangeInfoProvider">The exchange boards provider.</param>
		protected RemoteStorage(IStorageRegistry storageRegistry, ISecurityStorage securityStorage,
			IExtendedInfoStorage extendedInfoStorage, IExchangeInfoProvider exchangeInfoProvider)
		{
			StorageRegistry = storageRegistry ?? throw new ArgumentNullException(nameof(storageRegistry));
			SecurityStorage = securityStorage ?? throw new ArgumentNullException(nameof(securityStorage));
			ExtendedInfoStorage = extendedInfoStorage ?? throw new ArgumentNullException(nameof(extendedInfoStorage));
			ExchangeInfoProvider = exchangeInfoProvider ?? throw new ArgumentNullException(nameof(exchangeInfoProvider));

			AddDataType(typeof(ExecutionMessage));
			AddDataType(typeof(Level1ChangeMessage));
			AddDataType(typeof(QuoteChangeMessage));
			AddDataType(typeof(NewsMessage));

			AddDataType(typeof(TimeFrameCandleMessage));
			AddDataType(typeof(RangeCandleMessage));
			AddDataType(typeof(RenkoCandleMessage));
			AddDataType(typeof(PnFCandleMessage));
			AddDataType(typeof(TickCandleMessage));
			AddDataType(typeof(VolumeCandleMessage));

			AddDataType(typeof(PositionChangeMessage));
		}

		/// <summary>
		/// Market-data storage.
		/// </summary>
		public IStorageRegistry StorageRegistry { get; }

		/// <summary>
		/// Securities meta info storage.
		/// </summary>
		public ISecurityStorage SecurityStorage { get; }

		/// <summary>
		/// Extended info <see cref="Message.ExtensionInfo"/> storage.
		/// </summary>
		public IExtendedInfoStorage ExtendedInfoStorage { get; }

		/// <summary>
		/// The exchange boards provider.
		/// </summary>
		public IExchangeInfoProvider ExchangeInfoProvider { get; }

		private IRemoteAuthorization _authorization = new AnonymousRemoteAuthorization();

		/// <summary>
		/// Authorization module.
		/// </summary>
		public IRemoteAuthorization Authorization
		{
			get => _authorization;
			set => _authorization = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <summary>
		/// The maximum number of instruments which can be requested from the server via the <see cref="IRemoteStorage.GetSecurities"/> method. It equals to 200.
		/// </summary>
		public const int DefaultMaxSecurityCount = 200;

		private int _maxSecurityCount = DefaultMaxSecurityCount;

		/// <summary>
		/// The maximum number of instruments which can be requested from the server via the <see cref="IRemoteStorage.GetSecurities"/> method. The default is <see cref="RemoteStorage.DefaultMaxSecurityCount"/>.
		/// </summary>
		public int MaxSecurityCount
		{
			get => _maxSecurityCount;
			set
			{
				if (value < 1)
					throw new ArgumentOutOfRangeException();

				_maxSecurityCount = value;
			}
		}

		/// <summary>
		/// Event of server restarting.
		/// </summary>
		public event Action Restarting;

		/// <summary>
		/// Event of start downloading.
		/// </summary>
		public event Func<bool> StartDownloading;

		/// <summary>
		/// Event of stop downloading.
		/// </summary>
		public event Action StopDownloading;

		/// <summary>
		/// To add the data type that can pass <see cref="RemoteStorage"/>.
		/// </summary>
		/// <param name="dataType">Data type.</param>
		protected void AddDataType(Type dataType)
		{
			if (dataType == null)
				throw new ArgumentNullException(nameof(dataType));

			_dataTypes.Add(dataType.Name, dataType);
		}

		/// <summary>
		/// To get a list of available market data storages.
		/// </summary>
		/// <returns>Market data storages.</returns>
		protected abstract IEnumerable<IMarketDataDrive> GetDrives();

		private void CheckSession(Guid sessionId, UserPermissions requestedPermission, string securityId = null, string dataType = null, string arg = null, DateTime? date = null)
		{
			var info = _sessions.TryGetValue(sessionId);

			if (info == null)
				throw new UnauthorizedAccessException(LocalizedStrings.Str2080Params.Put(sessionId));

			var dict = info.SafeAdd(requestedPermission);

			var hasPermissions = dict.SafeAdd(Tuple.Create(securityId, dataType, arg, date),
				key => _authorization.HasPermissions(sessionId, requestedPermission, securityId, dataType, arg, date));

			if (!hasPermissions)
				throw new UnauthorizedAccessException(LocalizedStrings.Str2081Params.Put(sessionId, requestedPermission, securityId, dataType, arg, date));
		}

		private Security ToSecurity(string securityId, bool check = true)
		{
			var security = SecurityStorage.LookupById(securityId);

			if (check && security == null)
				throw new InvalidOperationException(LocalizedStrings.Str704Params.Put(securityId));

			return security;
		}

		private IMarketDataStorageDrive GetStorageDrive(string securityId, string dataType, string arg, StorageFormats format)
		{
			var security = ToSecurity(securityId);
			return GetStorage(security, dataType, arg, GetDrives().First(), format).Drive;
		}

		private IMarketDataStorage GetStorage(string securityId, string dataType, string arg, DateTime date, StorageFormats format)
		{
			var security = ToSecurity(securityId);
			return GetDrives().Select(drive => GetStorage(security, dataType, arg, drive, format)).FirstOrDefault(storage => storage.Dates.Contains(date));
		}

		private IMarketDataStorage GetStorage(Security security, string dataType, string arg, IMarketDataDrive drive, StorageFormats format)
		{
			var type = _dataTypes.TryGetValue(dataType);

			if (type == null)
				throw new InvalidOperationException(LocalizedStrings.Str2082Params.Put(dataType));

			return StorageRegistry.GetStorage(security, type, type.StringToMessageArg(arg), drive, format);
		}

		Guid IAuthenticationService.Login(string email, string password)
		{
			return ((IAuthenticationService)this).Login2(Products.Hydra, email, password).Item1;
		}

		Tuple<Guid, long> IAuthenticationService.Login2(Products product, string email, string password)
		{
			return ((IAuthenticationService)this).Login3(product, null, email, password);
		}

		Tuple<Guid, long> IAuthenticationService.Login3(Products product, string version, string email, string password)
		{
			var sessionId = Authorization.ValidateCredentials(email, password.Secure(), NetworkHelper.UserAddress);

			_sessions.Add(sessionId, new SynchronizedDictionary<UserPermissions, SynchronizedDictionary<Tuple<string, string, string, DateTime?>, bool>>());

			this.AddInfoLog(LocalizedStrings.Str2084Params, sessionId, email, product, version);

			return Tuple.Create(sessionId, -1L);
		}

		void IAuthenticationService.Ping(Guid sessionId)
		{
		}

		void IAuthenticationService.Logout(Guid sessionId)
		{
			var info = _sessions.TryGetValue(sessionId);

			if (info != null)
			{
				_sessions.Remove(sessionId);

				this.AddInfoLog(LocalizedStrings.Str2085Params, sessionId);
			}
		}

		long IAuthenticationService.GetId(Guid sessionId)
		{
			this.AddInfoLog("GetId {0}.", sessionId);
			throw new NotSupportedException();
		}

		private static string[] ToIds(IEnumerable<Security> securities)
		{
			return securities.Select(s => s.Id).ToArray();
		}

		string[] IRemoteStorage.LookupSecurityIds(Guid sessionId, SecurityLookupMessage criteria)
		{
			CheckSession(sessionId, UserPermissions.SecurityLookup);
			this.AddInfoLog(LocalizedStrings.Str2086Params, sessionId);

			if (criteria.IsLookupAll())
				return ToIds(SecurityStorage.LookupAll());

			this.AddInfoLog(LocalizedStrings.Str2087Params, sessionId);

			return ToIds(SecurityStorage.Lookup(criteria).Where(s => s.Board != ExchangeBoard.Test));
		}

		SecurityMessage[] IRemoteStorage.GetSecurities(Guid sessionId, string[] securityIds)
		{
			CheckSession(sessionId, UserPermissions.SecurityLookup);
			this.AddInfoLog(LocalizedStrings.Str2088Params, sessionId);

			if (securityIds == null)
				throw new ArgumentNullException(nameof(securityIds));

			if (securityIds.Length > MaxSecurityCount)
				throw new ArgumentOutOfRangeException(nameof(securityIds));

			return securityIds
				.Select(id => ToSecurity(id, false)?.ToMessage())
				.Where(s => s != null)
				.ToArray();
		}

		void IRemoteStorage.SaveSecurities(Guid sessionId, SecurityMessage[] securities)
		{
			CheckSession(sessionId, UserPermissions.EditSecurities);
			this.AddInfoLog(LocalizedStrings.Str2088Params, sessionId);

			if (securities == null)
				throw new ArgumentNullException(nameof(securities));

			if (securities.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(securities));

			if (securities.Length > MaxSecurityCount)
				throw new ArgumentOutOfRangeException(nameof(securities));

			foreach (var message in securities)
			{
				var security = SecurityStorage.LookupById(message.SecurityId);

				if (security == null)
					security = message.ToSecurity(ExchangeInfoProvider);
				else
					security.ApplyChanges(message, ExchangeInfoProvider);

				SecurityStorage.Save(security, false);
			}
		}

		void IRemoteStorage.DeleteSecurities(Guid sessionId, string[] securityIds)
		{
			CheckSession(sessionId, UserPermissions.DeleteSecurities);
			//this.AddInfoLog(LocalizedStrings.Str2088Params, sessionId);

			if (securityIds == null)
				throw new ArgumentNullException(nameof(securityIds));

			if (securityIds.IsEmpty())
				throw new ArgumentOutOfRangeException(nameof(securityIds));

			foreach (var securityId in securityIds)
			{
				SecurityStorage.DeleteById(securityId);
			}
		}

		DateTime[] IRemoteStorage.GetDates(Guid sessionId, string securityId, string dataType, string arg, StorageFormats format)
		{
			CheckSession(sessionId, UserPermissions.Load);
			this.AddInfoLog(LocalizedStrings.Str2089Params, sessionId, securityId, dataType, arg);

			var security = ToSecurity(securityId, false);

			return security == null
					? ArrayHelper.Empty<DateTime>()
			       	: GetDrives()
						.SelectMany(drive => GetStorage(security, dataType, arg, drive, format).Dates)
			       	  	.Distinct()
			       	  	.OrderBy()
			       	  	.ToArray();
		}

		string[] IRemoteStorage.GetAvailableSecurities(Guid sessionId)
		{
			CheckSession(sessionId, UserPermissions.SecurityLookup);
			this.AddInfoLog(LocalizedStrings.Str2088Params, sessionId);

			var generator = new SecurityIdGenerator();

			return GetDrives()
						.SelectMany(drive => drive.AvailableSecurities)
						.Distinct()
						.Select(id => generator.GenerateId(id.SecurityCode, id.BoardCode))
						.ToArray();
		}

		Tuple<string, string>[] IRemoteStorage.GetAvailableDataTypes(Guid sessionId, string securityIdStr, StorageFormats format)
		{
			CheckSession(sessionId, UserPermissions.Load);
			this.AddInfoLog(LocalizedStrings.Str2090Params, sessionId, securityIdStr.IsEmpty(LocalizedStrings.Str1569));

			var securityId = securityIdStr.IsEmpty() ? default : securityIdStr.ToSecurityId();

			return GetDrives()
						.SelectMany(drive => drive.GetAvailableDataTypes(securityId, format))
						.Distinct()
						.Select(t => Tuple.Create(t.MessageType.Name, TraderHelper.CandleArgToFolderName(t.Arg)))
						.ToArray();
		}

		void IRemoteStorage.Save(Guid sessionId, string securityId, string dataType, string arg, DateTime date, StorageFormats format, byte[] data)
		{
			CheckSession(sessionId, UserPermissions.Save, securityId, dataType, arg, date);

			this.AddInfoLog(LocalizedStrings.Str2091Params, sessionId, securityId, dataType, arg, date);

			GetStorageDrive(securityId, dataType, arg, format).SaveStream(date, data.To<Stream>());
		}

		void IRemoteStorage.Delete(Guid sessionId, string securityId, string dataType, string arg, DateTime date, StorageFormats format)
		{
			CheckSession(sessionId, UserPermissions.Delete, securityId, dataType, arg, date);

			this.AddInfoLog(LocalizedStrings.Str2092Params, sessionId, securityId, dataType, arg, date);

			GetStorage(securityId, dataType, arg, date, format).Delete(date);
		}

		Stream IRemoteStorage.LoadStream(Guid sessionId, string securityId, string dataType, string arg, DateTime date, StorageFormats format)
		{
			CheckSession(sessionId, UserPermissions.Load, securityId, dataType, arg, date);

			this.AddInfoLog(LocalizedStrings.Str2093Params, sessionId, securityId, dataType, arg, date);

			var storage = GetStorage(securityId, dataType, arg, date, format);
			return storage == null ? Stream.Null : storage.Drive.LoadStream(date);
		}

		string[] IRemoteStorage.LookupExchanges(Guid sessionId, BoardLookupMessage criteria)
		{
			CheckSession(sessionId, UserPermissions.ExchangeLookup);

			this.AddInfoLog(LocalizedStrings.RemoteStorageLookupExchanges, sessionId, criteria);

			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			var code = criteria.Like;

			var exchanges = ExchangeInfoProvider.Exchanges;

			if (!code.IsEmpty())
				exchanges = exchanges.Where(e => e.Name.ContainsIgnoreCase(code) || e.FullName?.ContainsIgnoreCase(code) == true);

			return exchanges.Select(e => e.Name).ToArray();
		}

		string[] IRemoteStorage.LookupExchangeBoards(Guid sessionId, BoardLookupMessage criteria)
		{
			CheckSession(sessionId, UserPermissions.ExchangeBoardLookup);

			this.AddInfoLog(LocalizedStrings.RemoteStorageLookupExchangeBoards, sessionId, criteria);

			if (criteria == null)
				throw new ArgumentNullException(nameof(criteria));

			return ExchangeInfoProvider.LookupBoards(criteria.Like).Select(b => b.Code).ToArray();
		}

		string[] IRemoteStorage.GetExchanges(Guid sessionId, string[] codes)
		{
			CheckSession(sessionId, UserPermissions.ExchangeLookup);

			if (codes == null)
				throw new ArgumentNullException(nameof(codes));

			this.AddInfoLog(LocalizedStrings.RemoteStorageGetExchanges, sessionId, codes.Join(","));

			return codes
				.Select(ExchangeInfoProvider.GetExchange)
				.Where(e => e != null)
				.Select(e => e.Name)
				.ToArray();
		}

		BoardMessage[] IRemoteStorage.GetExchangeBoards(Guid sessionId, string[] codes)
		{
			CheckSession(sessionId, UserPermissions.ExchangeBoardLookup);

			if (codes == null)
				throw new ArgumentNullException(nameof(codes));

			this.AddInfoLog(LocalizedStrings.RemoteStorageGetExchangeBoards, sessionId, codes.Join(","));

			return codes
				.Select(ExchangeInfoProvider.GetExchangeBoard)
				.Where(b => b != null)
				.Select(b => b.ToMessage())
				.ToArray();
		}

		void IRemoteStorage.SaveExchanges(Guid sessionId, string[] exchanges)
		{
			CheckSession(sessionId, UserPermissions.EditExchanges);

			if (exchanges == null)
				throw new ArgumentNullException(nameof(exchanges));

			this.AddInfoLog(LocalizedStrings.RemoteStorageSaveExchanges, sessionId);

			foreach (var exchange in exchanges)
			{
				ExchangeInfoProvider.GetOrCreateBoard(exchange);
			}
		}

		void IRemoteStorage.SaveExchangeBoards(Guid sessionId, BoardMessage[] boards)
		{
			CheckSession(sessionId, UserPermissions.EditBoards);

			if (boards == null)
				throw new ArgumentNullException(nameof(boards));

			this.AddInfoLog(LocalizedStrings.RemoteStorageSaveExchangeBoards, sessionId);

			foreach (var message in boards)
			{
				var board = ExchangeInfoProvider.GetExchangeBoard(message.Code);

				if (board == null)
					board = message.ToBoard();
				else
					board.ApplyChanges(message);

				ExchangeInfoProvider.Save(board);
			}
		}

		void IRemoteStorage.DeleteExchanges(Guid sessionId, string[] codes)
		{
			CheckSession(sessionId, UserPermissions.DeleteExchanges);

			if (codes == null)
				throw new ArgumentNullException(nameof(codes));

			this.AddInfoLog(LocalizedStrings.RemoteStorageDeleteExchanges, sessionId, codes.Join(","));

			foreach (var code in codes)
			{
				var exchange = ExchangeInfoProvider.GetExchange(code);

				if (exchange == null)
					continue;

				ExchangeInfoProvider.Delete(exchange);
			}
		}

		void IRemoteStorage.DeleteExchangeBoards(Guid sessionId, string[] codes)
		{
			CheckSession(sessionId, UserPermissions.DeleteBoards);

			if (codes == null)
				throw new ArgumentNullException(nameof(codes));

			this.AddInfoLog(LocalizedStrings.RemoteStorageDeleteExchangeBoards, sessionId, codes.Join(","));

			foreach (var code in codes)
			{
				var board = ExchangeInfoProvider.GetExchangeBoard(code);

				if (board == null)
					continue;

				ExchangeInfoProvider.Delete(board);
			}
		}

		private static string GetTypeName(Type type)
		{
			return Converter.GetAlias(type) ?? type.GetTypeName(false);
		}

		Tuple<string, string>[] IRemoteStorage.GetSecurityExtendedFields(Guid sessionId, string storageName)
		{
			CheckSession(sessionId, UserPermissions.SecurityLookup);

			this.AddInfoLog(LocalizedStrings.RemoteStorageGetSecurityExtendedFields, sessionId, storageName);

			return ExtendedInfoStorage.Get(storageName)?.Fields.Select(t => Tuple.Create(t.Item1, GetTypeName(t.Item2))).ToArray();
		}

		void IRemoteStorage.CreateSecurityExtendedFields(Guid sessionId, string storageName, Tuple<string, string>[] fields)
		{
			CheckSession(sessionId, UserPermissions.EditSecurities);

			if (fields == null)
				throw new ArgumentNullException(nameof(fields));

			this.AddInfoLog(LocalizedStrings.RemoteStorageCreateSecurityExtendedFields, sessionId, storageName, fields.Select(t => $"{t.Item1}={t.Item2}").Join(","));

			ExtendedInfoStorage.Create(storageName, fields.Select(t => Tuple.Create(t.Item1, t.Item2.To<Type>())).ToArray());
		}

		void IRemoteStorage.DeleteSecurityExtendedFields(Guid sessionId, string storageName)
		{
			CheckSession(sessionId, UserPermissions.DeleteSecurities);

			this.AddInfoLog(LocalizedStrings.RemoteStorageDeleteSecurityExtendedFields, sessionId, storageName);

			var item = ExtendedInfoStorage.Get(storageName);

			if (item != null)
				ExtendedInfoStorage.Delete(item);
		}

		void IRemoteStorage.AddSecurityExtendedInfo(Guid sessionId, string storageName, string securityId, string[] fieldValues)
		{
			CheckSession(sessionId, UserPermissions.EditSecurities);

			this.AddInfoLog(LocalizedStrings.RemoteStorageAddSecurityExtendedInfo, sessionId, storageName, securityId);

			var storage = ExtendedInfoStorage.Get(storageName);

			if (storage == null)
				return;

			var fields = storage.Fields;

			storage.Add(securityId.ToSecurityId(), fields.Select((f, i) => new KeyValuePair<string, object>(f.Item1, fieldValues[i].To(f.Item2))).ToDictionary());
		}

		void IRemoteStorage.DeleteSecurityExtendedInfo(Guid sessionId, string storageName, string securityId)
		{
			CheckSession(sessionId, UserPermissions.EditSecurities);

			this.AddInfoLog(LocalizedStrings.RemoteStorageDeleteSecurityExtendedInfo, sessionId, storageName, securityId);

			ExtendedInfoStorage.Get(storageName)?.Delete(securityId.ToSecurityId());
		}

		string[] IRemoteStorage.GetSecurityExtendedStorages(Guid sessionId)
		{
			CheckSession(sessionId, UserPermissions.SecurityLookup);

			this.AddInfoLog(LocalizedStrings.RemoteStorageGetSecurityExtendedStorages, sessionId);

			return ExtendedInfoStorage.Storages.Select(s => s.StorageName).ToArray();
		}

		string[] IRemoteStorage.GetExtendedInfoSecurities(Guid sessionId, string storageName)
		{
			CheckSession(sessionId, UserPermissions.SecurityLookup);

			this.AddInfoLog(LocalizedStrings.RemoteStorageGetExtendedInfoSecurities, sessionId, storageName);

			return ExtendedInfoStorage.Get(storageName)?.Securities.Select(id => id.ToStringId()).ToArray();
		}

		string[] IRemoteStorage.GetSecurityExtendedInfo(Guid sessionId, string storageName, string securityId)
		{
			CheckSession(sessionId, UserPermissions.SecurityLookup);

			this.AddInfoLog(LocalizedStrings.RemoteStorageGetSecurityExtendedInfo, sessionId, storageName, securityId);

			var storage = ExtendedInfoStorage.Get(storageName);

			var info = storage?.Load(securityId.ToSecurityId());

			if (info == null)
				return null;

			return storage.Fields.Select(t => info[t.Item1].To<string>()).ToArray();
		}

		Tuple<string, string[]>[] IRemoteStorage.GetAllExtendedInfo(Guid sessionId, string storageName)
		{
			CheckSession(sessionId, UserPermissions.SecurityLookup);

			this.AddInfoLog(LocalizedStrings.RemoteStorageGetAllExtendedInfo, sessionId, storageName);

			var storage = ExtendedInfoStorage.Get(storageName);

			if (storage == null)
				return null;

			var info = storage.Load();

			return info.Select(t => Tuple.Create(t.Item1.ToStringId(), storage.Fields.Select(t1 => t.Item2[t1.Item1].To<string>()).ToArray())).ToArray();
		}

		Tuple<string, string[], UserPermissions>[] IRemoteStorage.GetUsers(Guid sessionId)
		{
			CheckSession(sessionId, UserPermissions.GetUsers);

			this.AddInfoLog(LocalizedStrings.RemoteStorageGetUsers, sessionId);

			return _authorization.AllRemoteUsers.Select(c => Tuple.Create(c.Email, c.IpRestrictions.Cache.Select(a => a.To<string>()).ToArray(), c.Permissions.SyncGet(d => d.Keys.JoinMask()))).ToArray();
		}

		void IRemoteStorage.SaveUser(Guid sessionId, string login, string password, string[] ipAddresses, UserPermissions permissions)
		{
			CheckSession(sessionId, UserPermissions.EditUsers);

			this.AddInfoLog(LocalizedStrings.RemoteStorageSaveUser, sessionId, login, ipAddresses.Join(","), permissions);

			_authorization.SaveRemoteUser(login, password.Secure(), ipAddresses.Select(s => s.To<IPAddress>()).ToArray(), permissions);
		}

		void IRemoteStorage.DeleteUser(Guid sessionId, string login)
		{
			CheckSession(sessionId, UserPermissions.DeleteUsers);

			this.AddInfoLog(LocalizedStrings.RemoteStorageDeleteUser, sessionId, login);

			_authorization.DeleteRemoteUser(login);
		}

		void IRemoteStorage.Restart(Guid sessionId)
		{
			CheckSession(sessionId, UserPermissions.ServerManage);

			this.AddInfoLog(LocalizedStrings.RemoteStorageRestart, sessionId);

			Restarting?.Invoke();
		}

		bool IRemoteStorage.StartDownloading(Guid sessionId)
		{
			CheckSession(sessionId, UserPermissions.ServerManage);

			this.AddInfoLog(LocalizedStrings.RemoteStorageStartDownloading, sessionId);

			return StartDownloading?.Invoke() ?? false;
		}

		void IRemoteStorage.StopDownloading(Guid sessionId)
		{
			CheckSession(sessionId, UserPermissions.ServerManage);

			this.AddInfoLog(LocalizedStrings.RemoteStorageStopDownloading, sessionId);

			StopDownloading?.Invoke();
		}
	}
}