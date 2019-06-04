namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	/// <summary>
	/// In memory implementation of <see cref="IMappingMessageAdapterProvider{SecurityId}"/>.
	/// </summary>
	public class InMemorySecurityMessageAdapterProvider : IMappingMessageAdapterProvider<SecurityId>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InMemorySecurityMessageAdapterProvider"/>.
		/// </summary>
		public InMemorySecurityMessageAdapterProvider()
		{
		}

		private readonly CachedSynchronizedDictionary<SecurityId, IMessageAdapter> _adapters = new CachedSynchronizedDictionary<SecurityId, IMessageAdapter>();

		/// <inheritdoc />
		public virtual IEnumerable<KeyValuePair<SecurityId, IMessageAdapter>> Adapters => _adapters.CachedPairs;

		/// <inheritdoc />
		public event Action Changed;

		/// <inheritdoc />
		public virtual IMessageAdapter TryGetAdapter(SecurityId securityId)
		{
			return _adapters.TryGetValue(securityId);
		}

		/// <inheritdoc />
		public virtual void SetAdapter(SecurityId securityId, IMessageAdapter adapter)
		{
			IMessageAdapter prev;

			lock (_adapters.SyncRoot)
			{
				prev = _adapters.TryGetValue(securityId);
				_adapters[securityId] = adapter;
			}

			if (prev != adapter)
				Changed?.Invoke();
		}

		/// <inheritdoc />
		public virtual bool RemoveAssociation(SecurityId securityId)
		{
			if (!_adapters.Remove(securityId))
				return false;

			Changed?.Invoke();
			return true;
		}
	}
}