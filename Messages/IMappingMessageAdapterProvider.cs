namespace StockSharp.Messages
{
	using System;
	using System.Collections.Generic;

	/// <summary>
	/// The mapping message adapter's provider interface.
	/// </summary>
	/// <typeparam name="TKey">Type of key.</typeparam>
	public interface IMappingMessageAdapterProvider<TKey>
	{
		/// <summary>
		/// All available adapters.
		/// </summary>
		IEnumerable<KeyValuePair<TKey, IMessageAdapter>> Adapters { get; }

		/// <summary>
		/// Association changed.
		/// </summary>
		event Action Changed;

		/// <summary>
		/// Get adapter by the specified key.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns>The found adapter or <see langword="null"/>.</returns>
		IMessageAdapter TryGetAdapter(TKey key);

		/// <summary>
		/// Make association with adapter.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <param name="adapter">The adapter.</param>
		void SetAdapter(TKey key, IMessageAdapter adapter);

		/// <summary>
		/// Remove association with adapter.
		/// </summary>
		/// <param name="key">Key.</param>
		/// <returns><see langword="true"/> if the association is successfully removed, otherwise, <see langword="false"/>.</returns>
		bool RemoveAssociation(TKey key);
	}
}