namespace StockSharp.Algo.Storages.Remote
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.IO;
	using System.IO.Compression;
	using System.Text;

	using Ecng.Common;
	using Ecng.Collections;
	using Ecng.IO;

	using StockSharp.Algo.Storages;
	using StockSharp.Algo.Storages.Csv;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Localization;

	/// <summary>
	/// The client for access to the history server.
	/// </summary>
	public class RemoteStorageClient
	{
		private readonly IMessageAdapter _adapter;
		private readonly RemoteStorageCache _cache;
		private readonly int _securityBatchSize;

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteStorageClient"/>.
		/// </summary>
		/// <param name="adapter">Message adapter.</param>
		/// <param name="cache">Cache.</param>
		/// <param name="securityBatchSize">The new instruments request block size.</param>
		public RemoteStorageClient(IMessageAdapter adapter, RemoteStorageCache cache, int securityBatchSize)
		{
			if (securityBatchSize <= 0)
				throw new ArgumentOutOfRangeException(nameof(securityBatchSize), securityBatchSize, LocalizedStrings.InvalidValue);

			_adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
			_cache = cache;
			_securityBatchSize = securityBatchSize;
		}

		/// <summary>
		/// Get all available instruments.
		/// </summary>
		public IEnumerable<SecurityId> AvailableSecurities
			=>	Do<SecurityMessage>(
					new SecurityLookupMessage { OnlySecurityId = true },
					() => (typeof(SecurityLookupMessage), Extensions.LookupAllCriteriaMessage.ToString()),
					out _).Select(s => s.SecurityId)
			.ToArray();

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
				return;
			}

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
			=> Do<BoardMessage>(criteria, () => (typeof(BoardLookupMessage), criteria.ToString()), out _);

		/// <summary>
		/// Save securities.
		/// </summary>
		/// <param name="securities">Securities.</param>
		public void SaveSecurities(IEnumerable<SecurityMessage> securities)
			=> Do(securities.ToArray());

		/// <summary>
		/// Get all available data types.
		/// </summary>
		/// <param name="securityId">Instrument identifier.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Data types.</returns>
		public IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
			=> Do<AvailableDataInfoMessage>(new AvailableDataRequestMessage
			{
				SecurityId = securityId,
				Format = (int)format,
			}, () => (typeof(AvailableDataRequestMessage), securityId, format), out _).Select(t => t.FileDataType).Distinct().ToArray();

		/// <summary>
		/// Verify.
		/// </summary>
		public void Verify()
			=> Do(new TimeMessage());

		/// <summary>
		/// To get all the dates for which market data are recorded.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type info.</param>
		/// <param name="format">Storage format.</param>
		/// <returns>Dates.</returns>
		public IEnumerable<DateTime> GetDates(SecurityId securityId, DataType dataType, StorageFormats format)
			=> Do<AvailableDataInfoMessage>(new AvailableDataRequestMessage
			{
				SecurityId = securityId,
				RequestDataType = dataType,
				Format = (int)format,
				IncludeDates = true,
			}, () => (typeof(AvailableDataRequestMessage), securityId, dataType, format), out _).Select(i => i.Date.UtcDateTime).ToArray();

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
			=> CreateAdapter().Upload(messages);

		private IEnumerable<TResult> Do<TResult>(Message request, Func<object> getKey, out bool isFull)
			where TResult : Message, IOriginalTransactionIdMessage
		{
			if (request is null)	throw new ArgumentNullException(nameof(request));
			if (getKey is null)		throw new ArgumentNullException(nameof(getKey));

			var cache = _cache;
			var key = cache is null ? null : getKey();
			var needCache = key is not null;

			isFull = false;

			if (needCache && cache.TryGet(key, out var messages))
				return messages.Cast<TResult>();

			object sync = string.Intern(request.ToString());

			lock (sync)
			{
				var result = CreateAdapter().Download<TResult>(request, out var archive);

				if (archive.Length > 0)
				{
					var secList = typeof(TResult) == typeof(SecurityMessage) ? new List<SecurityMessage>() : null;
					var boardList = typeof(TResult) == typeof(BoardMessage) ? new List<BoardMessage>() : null;

					if (secList is not null || boardList is not null)
					{
						var encoding = Encoding.UTF8;
						var reader = archive.Uncompress<GZipStream>().To<Stream>().CreateCsvReader(encoding);

						while (reader.NextLine())
						{
							if (secList is not null)
							{
								var security = reader.ReadSecurity();

								if (security.SecurityId.IsAllSecurity())
									continue;

								secList.Add(security);
							}
							else
								boardList.Add(reader.ReadBoard(encoding));
						}

						isFull = true;

						if (secList is not null)
							result = secList.To<IEnumerable<TResult>>();
						else if (boardList is not null)
							result = boardList.To<IEnumerable<TResult>>();
					}
				}

				if (needCache)
					cache.Set(key, result.Cast<Message>().ToArray());

				return result;
			}
		}

		private IMessageAdapter CreateAdapter() => _adapter.TypedClone();
	}
}