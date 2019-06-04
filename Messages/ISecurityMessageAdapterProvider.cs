namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using Key = System.Tuple<SecurityId, MarketDataTypes?>;

	/// <summary>
	/// The security based message adapter's provider interface.
	/// </summary>
	public interface ISecurityMessageAdapterProvider : IMappingMessageAdapterProvider<Key>
	{
		/// <summary>
		/// Get adapter by the specified security id.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type.</param>
		/// <returns>The found adapter or <see langword="null"/>.</returns>
		IMessageAdapter TryGetAdapter(SecurityId securityId, MarketDataTypes? dataType);

		/// <summary>
		/// Make association with adapter.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Data type.</param>
		/// <param name="adapter">The adapter.</param>
		void SetAdapter(SecurityId securityId, MarketDataTypes? dataType, IMessageAdapter adapter);
	}

	/// <summary>
	/// In memory implementation of <see cref="ISecurityMessageAdapterProvider"/>.
	/// </summary>
	public class InMemorySecurityMessageAdapterProvider : ISecurityMessageAdapterProvider
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="InMemorySecurityMessageAdapterProvider"/>.
		/// </summary>
		public InMemorySecurityMessageAdapterProvider()
		{
		}

		private readonly CachedSynchronizedDictionary<Key, IMessageAdapter> _adapters = new CachedSynchronizedDictionary<Key, IMessageAdapter>();

		/// <inheritdoc />
		public virtual IEnumerable<KeyValuePair<Key, IMessageAdapter>> Adapters => _adapters.CachedPairs;

		/// <inheritdoc />
		public event Action Changed;

		/// <inheritdoc />
		public virtual IMessageAdapter TryGetAdapter(Key key)
		{
			return _adapters.TryGetValue(key) ?? _adapters.TryGetValue(Tuple.Create(key.Item1, (MarketDataTypes?)null));
		}

		/// <inheritdoc />
		public virtual void SetAdapter(Key key, IMessageAdapter adapter)
		{
			IMessageAdapter prev;

			lock (_adapters.SyncRoot)
			{
				prev = _adapters.TryGetValue(key);
				_adapters[key] = adapter;
			}

			if (prev != adapter)
				Changed?.Invoke();
		}

		/// <inheritdoc />
		public virtual bool RemoveAssociation(Key key)
		{
			if (!_adapters.Remove(key))
				return false;

			Changed?.Invoke();
			return true;
		}

		/// <inheritdoc />
		public IMessageAdapter TryGetAdapter(SecurityId securityId, MarketDataTypes? dataType)
		{
			return TryGetAdapter(Tuple.Create(securityId, dataType));
		}

		/// <inheritdoc />
		public void SetAdapter(SecurityId securityId, MarketDataTypes? dataType, IMessageAdapter adapter)
		{
			SetAdapter(Tuple.Create(securityId, dataType), adapter);
		}
	}
}