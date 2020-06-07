#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Storages.Algo
File: ISecurityStorage.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The interface for access to the storage of information on instruments.
	/// </summary>
	public interface ISecurityStorage : ISecurityProvider
	{
		/// <summary>
		/// Save security.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <param name="forced">Forced update.</param>
		void Save(Security security, bool forced);

		/// <summary>
		/// Delete security.
		/// </summary>
		/// <param name="security">Security.</param>
		void Delete(Security security);

		/// <summary>
		/// To delete instruments by the criterion.
		/// </summary>
		/// <param name="criteria">The criterion.</param>
		void DeleteBy(SecurityLookupMessage criteria);
	}

	/// <summary>
	/// In memory implementation of <see cref="ISecurityStorage"/>.
	/// </summary>
	public class InMemorySecurityStorage : ISecurityStorage
	{
		private readonly ISecurityProvider _underlying;
		private readonly SynchronizedDictionary<string, Security> _inner = new SynchronizedDictionary<string, Security>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemorySecurityStorage"/>.
		/// </summary>
		public InMemorySecurityStorage()
			: this(new CollectionSecurityProvider())
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InMemorySecurityStorage"/>.
		/// </summary>
		/// <param name="underlying">Underlying provider.</param>
		public InMemorySecurityStorage(ISecurityProvider underlying)
		{
			_underlying = underlying ?? throw new ArgumentNullException(nameof(underlying));
		}

		SyncObject ISecurityProvider.SyncRoot => _inner.SyncRoot;

		int ISecurityProvider.Count => _underlying.Count + _inner.Count;

		/// <inheritdoc />
		public event Action<IEnumerable<Security>> Added;

		/// <inheritdoc />
		public event Action<IEnumerable<Security>> Removed;

		/// <inheritdoc />
		public event Action Cleared;

		/// <inheritdoc />
		public void Delete(Security security)
		{
			if (security is null)
				throw new ArgumentNullException(nameof(security));

			if (_inner.Remove(security.Id))
				Removed?.Invoke(new[] { security });
		}

		/// <inheritdoc />
		public void DeleteBy(SecurityLookupMessage criteria)
		{
			if (criteria.IsLookupAll())
			{
				_inner.Clear();
				Cleared?.Invoke();
				return;
			}

			Security[] toDelete;

			lock (_inner.SyncRoot)
			{
				toDelete = _inner.Values.Filter(criteria).ToArray();

				foreach (var security in toDelete)
					_inner.Remove(security.Id);
			}

			Removed?.Invoke(toDelete);
		}

		/// <inheritdoc />
		public IEnumerable<Security> Lookup(SecurityLookupMessage criteria)
		{
			return _inner.SyncGet(d => d.Values.Filter(criteria).ToArray()).Concat(_underlying.Lookup(criteria)).Distinct();
		}

		/// <inheritdoc />
		public void Save(Security security, bool forced)
		{
			if (security is null)
				throw new ArgumentNullException(nameof(security));

			if (_inner.TryAdd(security.Id, security))
				Added?.Invoke(new[] { security });
		}
	}
}