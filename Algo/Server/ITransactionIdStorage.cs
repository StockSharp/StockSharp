namespace StockSharp.Algo.Server
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Localization;

	/// <summary>
	/// The interface describing the transaction and request identifiers storage.
	/// </summary>
	public interface ITransactionIdStorage
	{
		/// <summary>
		/// Get session based transaction and request identifiers storage.
		/// </summary>
		/// <param name="sessionId"><see cref="IMessageListenerSession.Id"/> value.</param>
		/// <returns>Session based transaction and request identifiers storage.</returns>
		ISessionTransactionIdStorage Get(string sessionId);
	}

	/// <summary>
	/// The interface describing the session based transaction and request identifiers storage.
	/// </summary>
	public interface ISessionTransactionIdStorage
	{
		/// <summary>
		/// Find request id by the specified transaction id.
		/// </summary>
		/// <param name="transactionId">Transaction ID.</param>
		/// <returns>The request identifier. <see langword="null"/> if the specified id doesn't exist.</returns>
		string TryGetRequestId(long transactionId);

		/// <summary>
		/// Find transaction id by the specified request id.
		/// </summary>
		/// <param name="requestId">The request identifier.</param>
		/// <returns>Transaction ID. <see langword="null"/> if the specified request doesn't exist.</returns>
		long? TryGetTransactionId(string requestId);

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
	public class InMemoryTransactionIdStorage : ITransactionIdStorage
	{
		private class InMemorySessionTransactionIdStorage : ISessionTransactionIdStorage
		{
			private readonly IdGenerator _idGenerator;
			private readonly SynchronizedPairSet<long, string> _requestIdMap = new SynchronizedPairSet<long, string>();

			public InMemorySessionTransactionIdStorage(IdGenerator idGenerator)
			{
				_idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
			}

			string ISessionTransactionIdStorage.TryGetRequestId(long transactionId)
			{
				return _requestIdMap.TryGetValue(transactionId);
			}

			long? ISessionTransactionIdStorage.TryGetTransactionId(string requestId)
			{
				if (_requestIdMap.TryGetKey(requestId, out var transId))
					return transId;

				return null;
			}

			string ISessionTransactionIdStorage.CreateRequestId()
			{
				var transactionId = _idGenerator.GetNextId();
				var requestId = transactionId.To<string>();

				Add(requestId, transactionId);

				return requestId;
			}

			long ISessionTransactionIdStorage.CreateTransactionId(string requestId)
			{
				var transactionId = _idGenerator.GetNextId();

				Add(requestId, transactionId);

				return transactionId;
			}

			private void Add(string requestId, long transactionId)
			{
				lock (_requestIdMap.SyncRoot)
				{
					if (_requestIdMap.ContainsKey(transactionId))
						throw new ArgumentException(LocalizedStrings.Str415Params.Put(transactionId));

					if (_requestIdMap.ContainsValue(requestId))
						throw new ArgumentException(LocalizedStrings.Str415Params.Put(requestId));

					_requestIdMap.Add(transactionId, requestId);
				}
			}

			bool ISessionTransactionIdStorage.RemoveRequestId(string requestId)
			{
				return _requestIdMap.RemoveByValue(requestId);
			}

			bool ISessionTransactionIdStorage.RemoveTransactionId(long transactionId)
			{
				return _requestIdMap.Remove(transactionId);
			}
		}

		private readonly IdGenerator _idGenerator;
		private readonly SynchronizedDictionary<string, ISessionTransactionIdStorage> _sessionStorages = new SynchronizedDictionary<string, ISessionTransactionIdStorage>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemoryTransactionIdStorage"/>.
		/// </summary>
		/// <param name="idGenerator">Transaction id generator.</param>
		public InMemoryTransactionIdStorage(IdGenerator idGenerator)
		{
			_idGenerator = idGenerator ?? throw new ArgumentNullException(nameof(idGenerator));
		}

		ISessionTransactionIdStorage ITransactionIdStorage.Get(string sessionId)
		{
			return _sessionStorages.SafeAdd(sessionId, key => new InMemorySessionTransactionIdStorage(_idGenerator));
		}
	}

	/// <summary>
	/// Plain implementation of <see cref="ITransactionIdStorage"/>.
	/// </summary>
	public class PlainTransactionIdStorage : ITransactionIdStorage
	{
		private class PlainSessionTransactionIdStorage : ISessionTransactionIdStorage
		{
			string ISessionTransactionIdStorage.TryGetRequestId(long transactionId)
			{
				return transactionId.To<string>();
			}

			long? ISessionTransactionIdStorage.TryGetTransactionId(string requestId)
			{
				return requestId.To<long>();
			}

			string ISessionTransactionIdStorage.CreateRequestId()
			{
				return DateTime.UtcNow.Ticks.To<string>();
			}

			long ISessionTransactionIdStorage.CreateTransactionId(string requestId)
			{
				return requestId.To<long>();
			}

			bool ISessionTransactionIdStorage.RemoveRequestId(string requestId)
			{
				return true;
			}

			bool ISessionTransactionIdStorage.RemoveTransactionId(long transactionId)
			{
				return true;
			}
		}

		ISessionTransactionIdStorage ITransactionIdStorage.Get(string sessionId)
		{
			return new PlainSessionTransactionIdStorage();
		}
	}
}