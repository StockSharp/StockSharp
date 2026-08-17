namespace StockSharp.Messages;

/// <summary>
/// Tracks who holds a subscription that is shared: one upstream subscription per key, opened by the
/// first holder to ask for it and given up after the last one lets go.
/// </summary>
/// <remarks>
/// This is bookkeeping only - it neither builds nor rewrites messages. What identifies a holder is
/// left to the caller: a transaction id where subscribers are numbered, or a client session paired
/// with the transaction it subscribed under where a dropped connection has to release everything it
/// held at once. The registry is the only place holders are recorded, so nothing else has to keep a
/// second list of them and risk the two disagreeing.
/// </remarks>
/// <typeparam name="TKey">What makes two requests the same subscription.</typeparam>
/// <typeparam name="THolder">What identifies one holder.</typeparam>
/// <typeparam name="THolderData">What the caller keeps about each holder - what it asked for.</typeparam>
/// <typeparam name="TPayload">What the caller keeps about the shared subscription itself.</typeparam>
public class SharedSubscriptionRegistry<TKey, THolder, THolderData, TPayload>
{
	/// <summary>
	/// One shared subscription and everyone currently holding it.
	/// </summary>
	public sealed class Entry
	{
		internal Entry(TKey key, Func<CachedSynchronizedDictionary<THolder, THolderData>, TPayload> createPayload)
		{
			Key = key;

			// The payload is handed the holders so it can expose them as its own. Building its own
			// would put the same people in two collections that then drift apart.
			Payload = createPayload(Holders);
		}

		/// <summary>
		/// Everyone holding it, and what each of them asked for. Cached, because the fan-out path
		/// walks this on every message that arrives for the subscription.
		/// </summary>
		public CachedSynchronizedDictionary<THolder, THolderData> Holders { get; } = [];

		/// <summary>What the caller keeps about this subscription.</summary>
		public TPayload Payload { get; }

		/// <summary>What makes two requests this subscription.</summary>
		internal TKey Key { get; }

		/// <summary>Whether it can still be found by its key, or only by its holders.</summary>
		internal bool IsKeyed { get; set; } = true;
	}

	private readonly Lock _sync = new();
	private readonly Dictionary<TKey, Entry> _byKey = [];
	private readonly Dictionary<THolder, Entry> _byHolder = [];

	/// <summary>
	/// Records that <paramref name="holder"/> holds the subscription named by <paramref name="key"/>,
	/// creating it if it is not there yet.
	/// </summary>
	/// <param name="key">What makes two requests the same subscription.</param>
	/// <param name="holder">The holder taking it.</param>
	/// <param name="data">What this holder asked for.</param>
	/// <param name="createPayload">
	/// Builds what to keep about the subscription, for a new one, from the holders it will have.
	/// </param>
	/// <param name="isOpener">
	/// <see langword="true"/> when this holder created the subscription and the caller has to open it
	/// upstream.
	/// </param>
	/// <returns>
	/// The entry, or <see langword="null"/> when this holder already holds a subscription and is
	/// asking twice.
	/// </returns>
	public Entry Add(TKey key, THolder holder, THolderData data, Func<CachedSynchronizedDictionary<THolder, THolderData>, TPayload> createPayload, out bool isOpener)
	{
		if (createPayload is null)
			throw new ArgumentNullException(nameof(createPayload));

		using (_sync.EnterScope())
		{
			isOpener = false;

			if (_byHolder.ContainsKey(holder))
				return null;

			if (!_byKey.TryGetValue(key, out var entry))
			{
				_byKey[key] = entry = new(key, createPayload);
				isOpener = true;
			}

			entry.Holders.Add(holder, data);
			_byHolder[holder] = entry;

			return entry;
		}
	}

	/// <summary>
	/// Records that <paramref name="holder"/> holds the subscription named by <paramref name="key"/>,
	/// for a caller that keeps nothing about each holder beyond the fact that it is one.
	/// </summary>
	/// <param name="key">What makes two requests the same subscription.</param>
	/// <param name="holder">The holder taking it.</param>
	/// <param name="createPayload">
	/// Builds what to keep about the subscription, for a new one, from the holders it will have.
	/// </param>
	/// <param name="isOpener">
	/// <see langword="true"/> when this holder created the subscription and the caller has to open it
	/// upstream.
	/// </param>
	/// <returns>
	/// The entry, or <see langword="null"/> when this holder already holds a subscription and is
	/// asking twice.
	/// </returns>
	public Entry Add(TKey key, THolder holder, Func<CachedSynchronizedDictionary<THolder, THolderData>, TPayload> createPayload, out bool isOpener)
		=> Add(key, holder, default, createPayload, out isOpener);

	/// <summary>
	/// Finds a shared subscription by what makes it that subscription.
	/// </summary>
	/// <param name="key">What makes two requests the same subscription.</param>
	/// <param name="entry">The entry, when there is one.</param>
	/// <returns><see langword="true"/> when it exists.</returns>
	public bool TryGetByKey(TKey key, out Entry entry)
	{
		using (_sync.EnterScope())
			return _byKey.TryGetValue(key, out entry);
	}

	/// <summary>
	/// Finds the shared subscription a holder holds.
	/// </summary>
	/// <param name="holder">The holder.</param>
	/// <param name="entry">The entry, when it holds one.</param>
	/// <returns><see langword="true"/> when it holds one.</returns>
	public bool TryGetByHolder(THolder holder, out Entry entry)
	{
		using (_sync.EnterScope())
			return _byHolder.TryGetValue(holder, out entry);
	}

	/// <summary>
	/// Drops one holder.
	/// </summary>
	/// <param name="holder">The holder letting go.</param>
	/// <param name="entry">The subscription it was holding, whether or not it was the last.</param>
	/// <returns>
	/// <see langword="true"/> when it was the last holder, so the caller has to give the subscription
	/// up upstream.
	/// </returns>
	public bool Remove(THolder holder, out Entry entry)
	{
		using (_sync.EnterScope())
		{
			if (!_byHolder.Remove(holder, out entry))
				return false;

			entry.Holders.Remove(holder);

			if (entry.Holders.Count > 0)
				return false;

			Unkey(entry);

			return true;
		}
	}

	/// <summary>
	/// Drops every holder the caller recognises as its own - a client session that went away without
	/// unsubscribing holds everything it took until this is called.
	/// </summary>
	/// <param name="match">Recognises the holders to drop.</param>
	/// <returns>The subscriptions that lost their last holder.</returns>
	public IReadOnlyCollection<Entry> RemoveWhere(Func<THolder, bool> match)
	{
		if (match is null)
			throw new ArgumentNullException(nameof(match));

		using (_sync.EnterScope())
		{
			var released = new List<Entry>();

			foreach (var holder in _byHolder.Keys.Where(match).ToArray())
			{
				if (!_byHolder.Remove(holder, out var entry))
					continue;

				entry.Holders.Remove(holder);

				if (entry.Holders.Count > 0)
					continue;

				Unkey(entry);
				released.Add(entry);
			}

			return released;
		}
	}

	/// <summary>
	/// Gives up a whole subscription at once, holders and all, for one that turned out never to have
	/// been opened.
	/// </summary>
	/// <param name="entry">The subscription to give up.</param>
	public void Drop(Entry entry)
	{
		if (entry is null)
			throw new ArgumentNullException(nameof(entry));

		using (_sync.EnterScope())
		{
			Unkey(entry);

			foreach (var holder in entry.Holders.CachedKeys)
				_byHolder.Remove(holder);

			entry.Holders.Clear();
		}
	}

	/// <summary>
	/// Takes a subscription out of the key index while leaving its holders addressable, for one that
	/// stopped and must not be joined again although those who held it are still being answered.
	/// </summary>
	/// <param name="entry">The subscription.</param>
	public void Unkey(Entry entry)
	{
		if (entry is null)
			throw new ArgumentNullException(nameof(entry));

		using (_sync.EnterScope())
		{
			if (!entry.IsKeyed)
				return;

			entry.IsKeyed = false;
			_byKey.Remove(entry.Key);
		}
	}

	/// <summary>
	/// Everything currently tracked.
	/// </summary>
	/// <returns>The entries.</returns>
	public IReadOnlyCollection<Entry> Entries()
	{
		using (_sync.EnterScope())
			return [.. _byKey.Values];
	}

	/// <summary>
	/// Forgets everything.
	/// </summary>
	public void Clear()
	{
		using (_sync.EnterScope())
		{
			_byKey.Clear();
			_byHolder.Clear();
		}
	}
}
