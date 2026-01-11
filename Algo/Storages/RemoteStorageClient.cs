namespace StockSharp.Algo.Storages;

/// <summary>
/// The client for access to the history server.
/// </summary>
public class RemoteStorageClient : Disposable
{
	private readonly IMessageAdapter _adapter;
	private readonly int _securityBatchSize;

	private bool _isConnected;

	/// <summary>
	/// Initializes a new instance of the <see cref="RemoteStorageClient"/>.
	/// </summary>
	/// <param name="adapter">Message adapter.</param>
	/// <param name="securityBatchSize">The new instruments request block size.</param>
	public RemoteStorageClient(IMessageAdapter adapter, int securityBatchSize)
	{
		if (securityBatchSize <= 0)
			throw new ArgumentOutOfRangeException(nameof(securityBatchSize), securityBatchSize, LocalizedStrings.InvalidValue);

		_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));

		_securityBatchSize = securityBatchSize;
	}

	/// <inheritdoc />
	protected override void DisposeManaged()
	{
		_adapter.Dispose();
		base.DisposeManaged();
	}

	/// <summary>
	/// Get all available instruments as async stream.
	/// </summary>
	/// <returns>Available instruments.</returns>
	public IAsyncEnumerable<SecurityId> GetAvailableSecuritiesAsync()
	{
		async IAsyncEnumerable<SecurityId> Impl([EnumeratorCancellation]CancellationToken cancellationToken = default)
		{
			var (msgs, _) = await DoAsync<SecurityMessage>(new SecurityLookupMessage { OnlySecurityId = true }, cancellationToken);

			foreach (var s in msgs)
				yield return s.SecurityId;
		}

		return Impl();
	}

	/// <summary>
	/// Download securities by the specified criteria.
	/// </summary>
	/// <param name="criteria">Message security lookup for specified criteria.</param>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <returns>The sequence of found instruments.</returns>
	public IAsyncEnumerable<SecurityMessage> LookupSecuritiesAsync(SecurityLookupMessage criteria, ISecurityProvider securityProvider)
	{
		if (criteria == null)
			throw new ArgumentNullException(nameof(criteria));

		if (securityProvider is null)
			throw new ArgumentNullException(nameof(securityProvider));

		async IAsyncEnumerable<SecurityMessage> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var existingIds = new HashSet<SecurityId>();

			await foreach (var s in securityProvider.LookupAllAsync().WithEnforcedCancellation(cancellationToken))
			{
				existingIds.Add(s.Id.ToSecurityId());
			}

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

					var (newSecurities, _) = await DoAsync<SecurityMessage>(criteria, cancellationToken);

					foreach (var security in newSecurities)
						yield return security;
				}
			}
			else
			{
				criteria = criteria.TypedClone();
				criteria.OnlySecurityId = true;

				var (securities, isFull) = await DoAsync<SecurityMessage>(criteria, cancellationToken);

				if (isFull)
				{
					var newSecurities = securities
						.Where(s => !existingIds.Contains(s.SecurityId))
						.ToArray();

					foreach (var security in newSecurities)
						yield return security;
				}
				else
				{
					var newSecurityIds = securities
						.Select(s => s.SecurityId)
						.Where(id => !existingIds.Contains(id))
						.ToArray();

					var count = 0;

					foreach (var batch in newSecurityIds.Chunk(_securityBatchSize))
					{
						var (batchRes, _) = await DoAsync<SecurityMessage>(new SecurityLookupMessage { SecurityIds = batch }, cancellationToken);

						foreach (var security in batchRes)
							yield return security;

						count += batch.Length;
					}
				}
			}
		}
		
		return Impl();
	}

	/// <summary>
	/// Save securities.
	/// </summary>
	/// <param name="securities">Securities.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public async ValueTask SaveSecuritiesAsync(IEnumerable<SecurityMessage> securities, CancellationToken cancellationToken)
	{
		if (securities is null)
			throw new ArgumentNullException(nameof(securities));

		foreach (var message in securities)
			await _adapter.SendInMessageAsync(message, cancellationToken);
	}

	/// <summary>
	/// Download boards by the specified criteria.
	/// </summary>
	/// <param name="criteria">Message board lookup for specified criteria.</param>
	/// <returns>The sequence of found boards.</returns>
	public IAsyncEnumerable<BoardMessage> LookupBoardsAsync(BoardLookupMessage criteria)
	{
		if (criteria is null)
			throw new ArgumentNullException(nameof(criteria));

		async IAsyncEnumerable<BoardMessage> Impl([EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var (boards, _) = await DoAsync<BoardMessage>(criteria, cancellationToken);

			foreach (var board in boards)
				yield return board;
		}

		return Impl();
	}

	/// <summary>
	/// Get all available data types.
	/// </summary>
	/// <param name="securityId">Instrument identifier.</param>
	/// <param name="format">Format type.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Data types.</returns>
	public async ValueTask<IEnumerable<DataType>> GetAvailableDataTypesAsync(SecurityId securityId, StorageFormats format, CancellationToken cancellationToken)
	{
		var (msgs, _) = await DoAsync<DataTypeInfoMessage>(new DataTypeLookupMessage
		{
			SecurityId = securityId,
			Format = (int)format,
		}, cancellationToken);

		return [.. msgs.Select(m => m.FileDataType).Distinct()];
	}

	/// <summary>
	/// Verify connection to the server.
	/// Connects and then disconnects to test connectivity.
	/// </summary>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public async ValueTask VerifyAsync(CancellationToken cancellationToken)
	{
		await _adapter.ConnectAsync(cancellationToken);
		await _adapter.DisconnectAsync(cancellationToken);
	}

	/// <summary>
	/// To get all the dates for which market data are recorded.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="format">Storage format.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Dates.</returns>
	public async ValueTask<IEnumerable<DateTime>> GetDatesAsync(SecurityId securityId, DataType dataType, StorageFormats format, CancellationToken cancellationToken)
	{
		var (msgs, _) = await DoAsync<DataTypeInfoMessage>(new DataTypeLookupMessage
		{
			SecurityId = securityId,
			RequestDataType = dataType,
			Format = (int)format,
			IncludeDates = true,
		}, cancellationToken);

		return [.. msgs.SelectMany(i => i.Dates).OrderBy().Distinct()];
	}

	/// <summary>
	/// To save data in the format of StockSharp storage.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="format">Storage format.</param>
	/// <param name="date">Date.</param>
	/// <param name="stream"></param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public ValueTask SaveStreamAsync(SecurityId securityId, DataType dataType, StorageFormats format, DateTime date, Stream stream, CancellationToken cancellationToken)
	{
		if (stream is null)
			throw new ArgumentNullException(nameof(stream));

		return _adapter.SendInMessageAsync(new RemoteFileCommandMessage
		{
			Command = CommandTypes.Update,
			Scope = CommandScopes.File,
			SecurityId = securityId,
			FileDataType = dataType,
			From = date,
			To = date.AddDays(1),
			Format = (int)format,
			Body = stream.To<byte[]>(),
		}, cancellationToken);
	}

	/// <summary>
	/// To load data in the format of StockSharp storage.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="format">Storage format.</param>
	/// <param name="date">Date.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns>Data in the format of StockSharp storage. If no data exists, <see cref="Stream.Null"/> will be returned.</returns>
	public async ValueTask<Stream> LoadStreamAsync(SecurityId securityId, DataType dataType, StorageFormats format, DateTime date, CancellationToken cancellationToken)
	{
		var (results, _) = await DoAsync<RemoteFileMessage>(new RemoteFileCommandMessage
		{
			Command = CommandTypes.Get,
			Scope = CommandScopes.File,
			SecurityId = securityId,
			FileDataType = dataType,
			From = date,
			To = date.AddDays(1),
			Format = (int)format,
		}, cancellationToken);

		return results.FirstOrDefault()?.Body.To<Stream>() ?? Stream.Null;
	}

	/// <summary>
	/// To remove market data on specified date from the storage.
	/// </summary>
	/// <param name="securityId">Security ID.</param>
	/// <param name="dataType">Data type info.</param>
	/// <param name="format">Storage format.</param>
	/// <param name="date">Date.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	/// <returns><see cref="ValueTask"/></returns>
	public ValueTask DeleteAsync(SecurityId securityId, DataType dataType, StorageFormats format, DateTime date, CancellationToken cancellationToken)
	{
		return _adapter.SendInMessageAsync(new RemoteFileCommandMessage
		{
			Command = CommandTypes.Remove,
			Scope = CommandScopes.File,
			SecurityId = securityId,
			FileDataType = dataType,
			Format = (int)format,
			From = date,
			To = date.AddDays(1),
		}, cancellationToken);
	}

	private async ValueTask<(TResult[] results, bool isFull)> DoAsync<TResult>(ITransactionIdMessage request, CancellationToken cancellationToken = default)
		where TResult : Message
	{
		if (request is null)
			throw new ArgumentNullException(nameof(request));

		// if request is not a subscription message - just send and return empty
		if (request is not ISubscriptionMessage)
		{
			await _adapter.SendInMessageAsync((Message)request, cancellationToken);
			return ([], false);
		}

		// create subscription from request
		var subscrMsg = ((ISubscriptionMessage)request).TypedClone();

		// ensure adapter is connected
		if (!_isConnected)
		{
			await _adapter.ConnectAsync(cancellationToken);
			_isConnected = true;
		}

		var result = new List<Message>();
		var isFull = false;

		try
		{
			await foreach (var msg in _adapter.SubscribeAsync<Message>(subscrMsg, cancellationToken))
			{
				if (msg is SubscriptionFinishedMessage finishedMsg)
				{
					if (finishedMsg.Body.Length > 0)
					{
						result.Clear();

						if (typeof(TResult) == typeof(SecurityMessage))
						{
							await foreach (var item in finishedMsg.Body.ExtractSecuritiesAsync().WithEnforcedCancellation(cancellationToken))
								result.Add(item);

							isFull = true;
						}
						else if (typeof(TResult) == typeof(BoardMessage))
						{
							await foreach (var item in finishedMsg.Body.ExtractBoardsAsync().WithEnforcedCancellation(cancellationToken))
								result.Add(item);

							isFull = true;
						}
					}

					break;
				}
				else if (msg is SubscriptionOnlineMessage || msg is TimeMessage)
				{
					break;
				}
				else
				{
					if (msg is not TimeMessage)
						result.Add(msg);
				}
			}
		}
		catch (Exception ex) when (ex is not OperationCanceledException)
		{
			throw new InvalidOperationException(LocalizedStrings.SomeConnectionFailed, ex);
		}

		return (result.OfType<TResult>().ToArray(), isFull);
	}
}