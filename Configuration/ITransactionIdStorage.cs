namespace StockSharp.Configuration;

/// <summary>
/// The interface describing the transaction and request identifiers storage.
/// </summary>
public interface ITransactionIdStorage
{
	/// <summary>
	/// Get session based transaction and request identifiers storage.
	/// </summary>
	/// <param name="sessionId">Session identifier.</param>
	/// <param name="persistable">Reuse session identifier.</param>
	/// <returns>Session based transaction and request identifiers storage.</returns>
	ISessionTransactionIdStorage Get(string sessionId, bool persistable);
}

/// <summary>
/// The interface describing the session based transaction and request identifiers storage.
/// </summary>
public interface ISessionTransactionIdStorage
{
	/// <summary>
	/// Try find request id by the specified transaction id.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <param name="requestId">The found request identifier when method returns <see langword="true"/>; otherwise default.</param>
	/// <returns><see langword="true"/> if request id was found for the specified transaction; otherwise, <see langword="false"/>.</returns>
	bool TryGetRequestId(long transactionId, out string requestId);

	/// <summary>
	/// Try find transaction id by the specified request id.
	/// </summary>
	/// <param name="requestId">The request identifier.</param>
	/// <param name="transactionId">The found transaction id when method returns <see langword="true"/>; otherwise default.</param>
	/// <returns><see langword="true"/> if transaction id was found for the specified request; otherwise, <see langword="false"/>.</returns>
	bool TryGetTransactionId(string requestId, out long transactionId);

	/// <summary>
	/// Create request identifier.
	/// </summary>
	/// <returns>The request identifier.</returns>
	string CreateRequestId();

	/// <summary>
	/// Create association.
	/// </summary>
	/// <param name="requestId">The request identifier.</param>
	/// <returns>Transaction ID.</returns>
	long CreateTransactionId(string requestId);

	/// <summary>
	/// Delete association.
	/// </summary>
	/// <param name="requestId">The request identifier.</param>
	/// <returns><see langword="true"/> if association was removed successfully, otherwise, returns <see langword="false"/>.</returns>
	bool RemoveRequestId(string requestId);

	/// <summary>
	/// Delete association.
	/// </summary>
	/// <param name="transactionId">Transaction ID.</param>
	/// <returns><see langword="true"/> if association was removed successfully, otherwise, returns <see langword="false"/>.</returns>
	bool RemoveTransactionId(long transactionId);
}

/// <summary>
/// In memory implementation of <see cref="ITransactionIdStorage"/>.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="InMemoryTransactionIdStorage"/>.
/// </remarks>
/// <param name="idGenerator">Transaction id generator.</param>
public class InMemoryTransactionIdStorage(IdGenerator idGenerator) : ITransactionIdStorage
{
	private class InMemorySessionTransactionIdStorage(IdGenerator idGenerator) : ISessionTransactionIdStorage
	{
		private readonly IdGenerator _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
		private readonly SynchronizedPairSet<long, string> _requestIdMap = [];

		bool ISessionTransactionIdStorage.TryGetRequestId(long transactionId, out string requestId)
			=> _requestIdMap.TryGetValue(transactionId, out requestId);

		bool ISessionTransactionIdStorage.TryGetTransactionId(string requestId, out long transactionId)
		{
			if (!requestId.IsEmpty())
			{
				if (_requestIdMap.TryGetKey(requestId, out var transId))
				{
					transactionId = transId;
					return true;
				}
			}

			transactionId = default;
			return false;
		}

		string ISessionTransactionIdStorage.CreateRequestId()
		{
			var transactionId = _idGenerator.GetNextId();
			var requestId = Do.Invariant(transactionId.To<string>);

			Add(requestId, transactionId);

			return requestId;
		}

		long ISessionTransactionIdStorage.CreateTransactionId(string requestId)
		{
			if (requestId.IsEmpty())
				throw new ArgumentNullException(nameof(requestId));

			var transactionId = _idGenerator.GetNextId();

			Add(requestId, transactionId);

			return transactionId;
		}

		private void Add(string requestId, long transactionId)
		{
			lock (_requestIdMap.SyncRoot)
			{
				if (_requestIdMap.ContainsKey(transactionId))
					throw new ArgumentException(LocalizedStrings.HasDuplicates.Put(transactionId));

				if (_requestIdMap.ContainsValue(requestId))
					throw new ArgumentException(LocalizedStrings.HasDuplicates.Put(requestId));

				_requestIdMap.Add(transactionId, requestId);
			}
		}

		bool ISessionTransactionIdStorage.RemoveRequestId(string requestId)
			=> _requestIdMap.RemoveByValue(requestId);

		bool ISessionTransactionIdStorage.RemoveTransactionId(long transactionId)
			=> _requestIdMap.Remove(transactionId);
	}

	private readonly IdGenerator _idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
	private readonly SynchronizedDictionary<string, ISessionTransactionIdStorage> _item = [];

	ISessionTransactionIdStorage ITransactionIdStorage.Get(string sessionId, bool persistable)
		=> persistable ? _item.SafeAdd(sessionId, key => new InMemorySessionTransactionIdStorage(_idGenerator)) : new InMemorySessionTransactionIdStorage(_idGenerator);
}

/// <summary>
/// Plain implementation of <see cref="ITransactionIdStorage"/>.
/// </summary>
public class PlainTransactionIdStorage : ITransactionIdStorage
{
	private class PlainSessionTransactionIdStorage : ISessionTransactionIdStorage
	{
		bool ISessionTransactionIdStorage.TryGetRequestId(long transactionId, out string requestId)
		{
			requestId = Do.Invariant(transactionId.To<string>);
			return true;
		}

		string ISessionTransactionIdStorage.CreateRequestId()
			=> Do.Invariant(DateTime.UtcNow.Ticks.To<string>);

		bool ISessionTransactionIdStorage.RemoveRequestId(string requestId)
			=> true;

		bool ISessionTransactionIdStorage.TryGetTransactionId(string requestId, out long transactionId)
		{
			if (!requestId.IsEmpty())
			{
				var parsed = 0L;
				if (Do.Invariant(() => long.TryParse(requestId, out parsed)))
				{
					transactionId = parsed;
					return true;
				}
			}

			transactionId = default;
			return false;
		}

		long ISessionTransactionIdStorage.CreateTransactionId(string requestId)
		{
			if (requestId.IsEmpty())
				throw new ArgumentNullException(nameof(requestId));

			return Do.Invariant(requestId.To<long>);
		}

		bool ISessionTransactionIdStorage.RemoveTransactionId(long transactionId)
			=> true;
	}

	ISessionTransactionIdStorage ITransactionIdStorage.Get(string sessionId, bool persistable)
		=> new PlainSessionTransactionIdStorage();
}