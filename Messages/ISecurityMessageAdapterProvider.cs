namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	/// <summary>
	/// The security based message adapter's provider interface. 
	/// </summary>
	public interface ISecurityMessageAdapterProvider
	{
		/// <summary>
		/// All available adapters.
		/// </summary>
		IEnumerable<KeyValuePair<SecurityId, IMessageAdapter>> Adapters { get; }

		/// <summary>
		/// Association changed.
		/// </summary>
		event Action Changed;

		/// <summary>
		/// Get adapter by security id.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns>The found adapter.</returns>
		IMessageAdapter GetAdapter(SecurityId securityId);

		/// <summary>
		/// Make association adapter and security id.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="adapter">The adapter.</param>
		void SetAdapter(SecurityId securityId, IMessageAdapter adapter);

		/// <summary>
		/// Remove association between security id and adapter.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <returns><see langword="true"/> if the association is successfully removed, otherwise, <see langword="false"/>.</returns>
		bool RemoveAssociation(SecurityId securityId);
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

		private readonly CachedSynchronizedDictionary<SecurityId, IMessageAdapter> _adapters = new CachedSynchronizedDictionary<SecurityId, IMessageAdapter>();

		/// <inheritdoc />
		public virtual IEnumerable<KeyValuePair<SecurityId, IMessageAdapter>> Adapters => _adapters.CachedPairs;

		/// <inheritdoc />
		public event Action Changed;

		/// <inheritdoc />
		public virtual IMessageAdapter GetAdapter(SecurityId securityId)
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