namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Linq;

	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// The supplier of information on instruments, getting data from the collection.
	/// </summary>
	public class CollectionSecurityProvider : ISecurityProvider
	{
		private readonly SynchronizedDictionary<SecurityId, Security> _inner = new SynchronizedDictionary<SecurityId, Security>();

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionSecurityProvider"/>.
		/// </summary>
		public CollectionSecurityProvider()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CollectionSecurityProvider"/>.
		/// </summary>
		/// <param name="securities">The instruments collection.</param>
		public CollectionSecurityProvider(IEnumerable<Security> securities)
			=> AddRange(securities);

		/// <inheritdoc />
		public int Count => _inner.Count;

		private Action<IEnumerable<Security>> _added;

		event Action<IEnumerable<Security>> ISecurityProvider.Added
		{
			add => _added += value;
			remove => _added -= value;
		}

		private Action<IEnumerable<Security>> _removed;

		event Action<IEnumerable<Security>> ISecurityProvider.Removed
		{
			add => _removed += value;
			remove => _removed -= value;
		}

		private Action _cleared;

		event Action ISecurityProvider.Cleared
		{
			add => _cleared += value;
			remove => _cleared -= value;
		}

		/// <inheritdoc />
		public Security LookupById(SecurityId id) => _inner.TryGetValue(id);

		/// <inheritdoc />
		public IEnumerable<Security> Lookup(SecurityLookupMessage criteria) => _inner.SyncGet(d => d.Values.Filter(criteria));

		/// <summary>
		/// Add security.
		/// </summary>
		/// <param name="security">Security.</param>
		public void Add(Security security)
		{
			if (_inner.TryAdd(security.ToSecurityId(), security))
				_added?.Invoke(new[] { security });
		}

		/// <summary>
		/// Remove security.
		/// </summary>
		/// <param name="security">Security.</param>
		/// <returns>Check result.</returns>
		public bool Remove(Security security)
		{
			if (security is null)
				throw new ArgumentNullException(nameof(security));

			RemoveRange(new[] { security });
			return true;
		}

		/// <summary>
		/// Add securities.
		/// </summary>
		/// <param name="securities">Securities.</param>
		public void AddRange(IEnumerable<Security> securities)
		{
			if (securities is null)
				throw new ArgumentNullException(nameof(securities));

			var added = new HashSet<Security>(securities);

			foreach (var security in securities)
			{
				if (!_inner.TryAdd(security.ToSecurityId(), security))
					added.Remove(security);
			}

			_added?.Invoke(added);
		}

		/// <summary>
		/// Remove securities.
		/// </summary>
		/// <param name="securities">Securities.</param>
		public void RemoveRange(IEnumerable<Security> securities)
		{
			if (securities is null)
				throw new ArgumentNullException(nameof(securities));

			var removed = new HashSet<Security>(securities);

			foreach (var security in securities)
			{
				if (!_inner.Remove(security.ToSecurityId()))
					removed.Remove(security);
			}

			_removed?.Invoke(removed);
		}

		/// <summary>
		/// Clear.
		/// </summary>
		public void Clear()
		{
			_inner.Clear();
			_cleared?.Invoke();
		}
	}
}