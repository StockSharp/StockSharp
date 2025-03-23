namespace StockSharp.Algo.Storages.Remote;

using StockSharp.Algo.Storages;

/// <summary>
/// The client for access to the history server.
/// </summary>
public class RemoteStorageClient : Disposable
{
	private readonly IMessageAdapter _adapter;
	private readonly RemoteStorageCache _cache;
	private readonly int _securityBatchSize;
	private readonly TimeSpan _timeout;

	private readonly SynchronizedDictionary<long, (SyncObject sync, List<Message> messages)> _pendings = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteStorageClient"/>.
	/// </summary>
	/// <param name="adapter">Message adapter.</param>
	/// <param name="cache">Cache.</param>
	/// <param name="securityBatchSize">The new instruments request block size.</param>
	/// <param name="timeout">Timeout.</param>
	public RemoteStorageClient(IMessageAdapter adapter, RemoteStorageCache cache, int securityBatchSize, TimeSpan timeout)
	{
		if (securityBatchSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(securityBatchSize), securityBatchSize, LocalizedStrings.InvalidValue);

		if (timeout <= TimeSpan.Zero)
			throw new ArgumentOutOfRangeException(nameof(timeout), timeout, LocalizedStrings.InvalidValue);

		_adapter = new AutoConnectMessageAdapter(adapter ?? throw new ArgumentNullException(nameof(adapter)));
		_adapter = new ChannelMessageAdapter(_adapter, new InMemoryMessageChannel(new MessageByOrderQueue(), "Adapter In", _adapter.AddErrorLog), new InMemoryMessageChannel(new MessageByOrderQueue(), "Adapter Out", _adapter.AddErrorLog));
		_adapter.NewOutMessage += OnNewOutMessage;

		_cache = cache;
		_securityBatchSize = securityBatchSize;
		_timeout = timeout;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_adapter.NewOutMessage -= OnNewOutMessage;
		_adapter.Dispose();

		base.DisposeManaged();
	}

	private void OnNewOutMessage(Message message)
	{
		if (message.IsBack())
		{
			_adapter.SendInMessage(message);
			return;
		}

		var connError = message is ConnectMessage cm ? cm.Error : null;
		if (connError is not null || message is DisconnectMessage)
		{
			foreach (var (_, (sync, _)) in _pendings.CopyAndClear())
				sync.PulseSignal(connError ?? new InvalidOperationException(LocalizedStrings.UnexpectedDisconnection));

			return;
		}

		long transId;

		if (message is IOriginalTransactionIdMessage responseMsg)
			transId = responseMsg.OriginalTransactionId;
		else if (message is TimeMessage timeMsg && long.TryParse(timeMsg.OriginalTransactionId, out var pingId))
			transId = pingId;
		else
			return;

		if (!_pendings.TryGetValue(transId, out var t))
			return;

		var error = message is SubscriptionResponseMessage r ? r.Error : null;

		if (message is SubscriptionFinishedMessage ||
			message is SubscriptionOnlineMessage ||
			message is TimeMessage ||
			error is not null)
		{
			if (error is null && message is not TimeMessage)
				t.messages.Add(message);

			t.sync.PulseSignal(error);
			_pendings.Remove(transId);
		}
		else
			t.messages.Add(message);
	}

	/// <summary>
	/// Get all available instruments.
	/// </summary>
	public IEnumerable<SecurityId> AvailableSecurities
		=> [.. Do<SecurityMessage>(
				new SecurityLookupMessage { OnlySecurityId = true },
				() => (typeof(SecurityLookupMessage), Extensions.LookupAllCriteriaMessage.ToString()),
				out _).Select(s => s.SecurityId)];

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
		if (securityProvider is null)
			throw new ArgumentNullException(nameof(securityProvider));

		var existingIds = securityProvider.LookupAll().Select(s => s.Id.ToSecurityId()).ToSet();
		
		LookupSecurities(criteria, existingIds, newSecurity, isCancelled, updateProgress);
	}

	private void LookupSecurities(SecurityLookupMessage criteria, ISet<SecurityId> existingIds, Action<SecurityMessage> newSecurity, Func<bool> isCancelled, Action<int, int> updateProgress)
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

		if (criteria.SecurityId != default || criteria.SecurityIds.Length > 0)
		{
			var newSecurityIds = new HashSet<SecurityId>();

			void tryAdd(SecurityId secId)
			{
				if (!existingIds.Contains(secId))
					newSecurityIds.Add(secId);
			}

			if (criteria.SecurityId != default)
				tryAdd(criteria.SecurityId);

			foreach (var secId in criteria.SecurityIds)
				tryAdd(secId);

			if (newSecurityIds.Count > 0)
			{
				criteria = criteria.TypedClone();
				criteria.SecurityId = default;
				criteria.SecurityIds = [.. newSecurityIds];

				var newSecurities = Do<SecurityMessage>(criteria, () => (typeof(SecurityLookupMessage), criteria.ToString()), out _).ToArray();

				updateProgress(0, newSecurities.Length);

				foreach (var security in newSecurities)
					newSecurity(security);

				updateProgress(newSecurities.Length, newSecurities.Length);
			}
		}
		else
		{
			criteria = criteria.TypedClone();
			criteria.OnlySecurityId = true;

			var securities = Do<SecurityMessage>(criteria, () => (typeof(SecurityLookupMessage), criteria.ToString()), out var isFull).ToArray();

			if (isFull)
			{
				var newSecurities = securities
					.Where(s => !existingIds.Contains(s.SecurityId))
					.ToArray();

				updateProgress(0, newSecurities.Length);

				foreach (var security in newSecurities)
					newSecurity(security);

				updateProgress(newSecurities.Length, newSecurities.Length);
			}
			else
			{
				var newSecurityIds = securities
					.Select(s => s.SecurityId)
					.Where(id => !existingIds.Contains(id))
					.ToArray();

				updateProgress(0, newSecurityIds.Length);

				var count = 0;

				foreach (var batch in newSecurityIds.Chunk(_securityBatchSize))
				{
					if (isCancelled())
						break;

					foreach (var security in Do<SecurityMessage>(
						new SecurityLookupMessage { SecurityIds = batch },
						() => (typeof(SecurityLookupMessage), batch.Select(i => i.To<string>()).JoinComma()),
						out _))
					{
						newSecurity(security);
					}

					count += batch.Length;

					updateProgress(count, newSecurityIds.Length);
				}
			}
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
		return [.. securities];
	}

	/// <summary>
	/// To find exchange boards that match the filter <paramref name="criteria" />.
	/// </summary>
	/// <param name="criteria">Message boards lookup for specified criteria.</param>
	/// <returns>Exchange boards.</returns>
	public IEnumerable<BoardMessage> LoadExchangeBoards(BoardLookupMessage criteria)
		=> Do<BoardMessage>(criteria, () => (typeof(BoardLookupMessage), criteria.ToString()), out _);

	/// <summary>
	/// Save securities.
	/// </summary>
	/// <param name="securities">Securities.</param>
	public void SaveSecurities(IEnumerable<SecurityMessage> securities)
		=> Do([.. securities]);

	/// <summary>
	/// Get all available data types.
	/// </summary>
	/// <param name="securityId">Instrument identifier.</param>
	/// <param name="format">Format type.</param>
	/// <returns>Data types.</returns>
	public IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
		=> [.. Do<DataTypeInfoMessage>(new DataTypeLookupMessage
		{
			SecurityId = securityId,
			Format = (int)format,
		}, () => (typeof(DataTypeLookupMessage), securityId, format), out _).Select(t => t.FileDataType).Distinct()];

	/// <summary>
	/// Verify.
	/// </summary>
	public void Verify() => Do<Message>(new TimeMessage { OfflineMode = MessageOfflineModes.Ignore }, () => null, out _);

	/// <summary>
	/// To get all the dates for which market data are recorded.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="format">Storage format.</param>
	/// <returns>Dates.</returns>
	public IEnumerable<DateTime> GetDates(SecurityId securityId, DataType dataType, StorageFormats format)
		=> [.. Do<DataTypeInfoMessage>(new DataTypeLookupMessage
		{
			SecurityId = securityId,
			RequestDataType = dataType,
			Format = (int)format,
			IncludeDates = true,
		}, () => (typeof(DataTypeLookupMessage), securityId, dataType, format), out _).SelectMany(i => i.Dates).OrderBy().Distinct()];

	/// <summary>
	/// To save data in the format of StockSharp storage.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="format">Storage format.</param>
	/// <param name="date">Date.</param>
	/// <param name="stream"></param>
	public void SaveStream(SecurityId securityId, DataType dataType, StorageFormats format, DateTime date, Stream stream)
		=> Do(new RemoteFileCommandMessage
		{
			Command = CommandTypes.Update,
			Scope = CommandScopes.File,
			SecurityId = securityId,
			FileDataType = dataType,
			From = date,
			To = date.AddDays(1),
			Format = (int)format,
			Body = stream.To<byte[]>(),
		});

	/// <summary>
	/// To load data in the format of StockSharp storage.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="format">Storage format.</param>
	/// <param name="date">Date.</param>
	/// <returns>Data in the format of StockSharp storage. If no data exists, <see cref="Stream.Null"/> will be returned.</returns>
	public Stream LoadStream(SecurityId securityId, DataType dataType, StorageFormats format, DateTime date)
		=> Do<RemoteFileMessage>(new RemoteFileCommandMessage
		{
			Command = CommandTypes.Get,
			Scope = CommandScopes.File,
			SecurityId = securityId,
			FileDataType = dataType,
			From = date,
			To = date.AddDays(1),
			Format = (int)format,
		}, () => null, out _).FirstOrDefault()?.Body.To<Stream>() ?? Stream.Null;

	/// <summary>
	/// To remove market data on specified date from the storage.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="format">Storage format.</param>
	/// <param name="date">Date.</param>
	public void Delete(SecurityId securityId, DataType dataType, StorageFormats format, DateTime date)
		=> Do(new RemoteFileCommandMessage
		{
			Command = CommandTypes.Remove,
			Scope = CommandScopes.File,
			SecurityId = securityId,
			FileDataType = dataType,
			Format = (int)format,
			From = date,
			To = date.AddDays(1),
		});

	private void Do(params Message[] messages)
	{
		if (messages is null)
			throw new ArgumentNullException(nameof(messages));

		foreach (var message in messages)
			_adapter.SendInMessage(message);
	}

	private IEnumerable<TResult> Do<TResult>(ITransactionIdMessage request, Func<object> getKey, out bool isFull)
		where TResult : Message//, IOriginalTransactionIdMessage
	{
		if (request is null)	throw new ArgumentNullException(nameof(request));
		if (getKey is null)		throw new ArgumentNullException(nameof(getKey));

		var cache = _cache;
		var key = cache is null ? null : getKey();
		var needCache = key is not null;

		isFull = false;

		if (needCache && cache.TryGet(key, out var cached))
			return cached.Cast<TResult>();

		var str = request.ToString();
		object sync = string.Intern(str);

		lock (sync)
		{
			if (needCache && cache.TryGet(key, out cached))
				return cached.Cast<TResult>();

			var transId = request.TransactionId = _adapter.TransactionIdGenerator.GetNextId();

			var requestSync = new SyncObject();
			var messages = new List<Message>();

			_pendings.Add(transId, (requestSync, messages));

			System.Diagnostics.Debug.WriteLine($"Download: {str}");

			if (!_adapter.SendInMessage((Message)request))
				throw new NotSupportedException(request.ToString());

			if (!requestSync.WaitSignal(_timeout, out var error))
				throw new TimeoutException(request.ToString());

			if (error is not null)
				throw new InvalidOperationException(LocalizedStrings.SomeConnectionFailed, (Exception)error);

			var archive = messages.Count == 1 && messages[0] is SubscriptionFinishedMessage finishedMsg && finishedMsg.Body.Length > 0 ? finishedMsg.Body : [];

			if (archive.Length > 0)
			{
				messages.Clear();

				if (typeof(TResult) == typeof(SecurityMessage))
				{
					messages.AddRange(archive.ExtractSecurities());
					isFull = true;
				}
				else if (typeof(TResult) == typeof(BoardMessage))
				{
					messages.AddRange(archive.ExtractBoards());
					isFull = true;
				}
			}
			else
				messages.AddRange(messages.CopyAndClear().OfType<TResult>());

			if (needCache)
				cache.Set(key, [.. messages]);

			return messages.Cast<TResult>();
		}
	}
}