namespace StockSharp.Algo.Storages.Csv;

using System.IO.Compression;

using Ecng.IO;

/// <summary>
/// The interface for presentation in the form of list of trade objects, received from the external storage.
/// </summary>
public interface ICsvEntityList
{
	/// <summary>
	/// The time delayed action.
	/// </summary>
	DelayAction DelayAction { get; set; }

	/// <summary>
	/// Initialize the storage.
	/// </summary>
	/// <param name="errors">Possible errors.</param>
	void Init(IList<Exception> errors);

	/// <summary>
	/// CSV file name.
	/// </summary>
	string FileName { get; }

	/// <summary>
	/// Create archived copy.
	/// </summary>
	bool CreateArchivedCopy { get; set; }

	/// <summary>
	/// Get archived copy body.
	/// </summary>
	/// <returns>File body.</returns>
	byte[] GetCopy();
    }

/// <summary>
/// List of trade objects, received from the CSV storage.
/// </summary>
/// <typeparam name="TKey">Key type.</typeparam>
/// <typeparam name="TEntity">Entity type.</typeparam>
public abstract class CsvEntityList<TKey, TEntity> : SynchronizedList<TEntity>, IStorageEntityList<TEntity>, ICsvEntityList
	where TEntity : class
{
	private readonly CachedSynchronizedDictionary<TKey, TEntity> _items = [];

	private readonly SyncObject _copySync = new();
	private byte[] _copy;

	/// <summary>
	/// The CSV storage of trading objects.
	/// </summary>
	protected CsvEntityRegistry Registry { get; }

	/// <inheritdoc />
	public TEntity[] Cache => _items.CachedValues;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvEntityList{TKey,TEntity}"/>.
	/// </summary>
	/// <param name="registry">The CSV storage of trading objects.</param>
	/// <param name="fileName">CSV file name.</param>
	protected CsvEntityList(CsvEntityRegistry registry, string fileName)
	{
		if (fileName == null)
			throw new ArgumentNullException(nameof(fileName));

		Registry = registry ?? throw new ArgumentNullException(nameof(registry));

		FileName = Path.Combine(Registry.Path, fileName);
	}

	/// <inheritdoc />
	public string FileName { get; }

	/// <inheritdoc />
	public bool CreateArchivedCopy { get; set; }

	/// <inheritdoc />
	public byte[] GetCopy()
	{
		if (!CreateArchivedCopy)
			throw new NotSupportedException();

		byte[] body;

		lock (_copySync)
			body = _copy;

		if (body is null)
		{
			lock (_copySync)
			{
				if (File.Exists(FileName))
					body = File.ReadAllBytes(FileName);
				else
					body = [];
			}

			body = body.Compress<GZipStream>();

			lock (_copySync)
				_copy ??= body;
		}

		return body;
	}

	private void ResetCopy()
	{
		if (!CreateArchivedCopy)
			return;

		lock (_copySync)
			_copy = null;
	}

	#region IStorageEntityList<T>

	private DelayAction.IGroup<CsvFileWriter> _delayActionGroup;
	private DelayAction _delayAction;

	/// <inheritdoc cref="ICsvEntityList" />
	public DelayAction DelayAction
	{
		get => _delayAction;
		set
		{
			if (_delayAction == value)
				return;

			if (_delayAction != null)
			{
				_delayAction.DeleteGroup(_delayActionGroup);
				_delayActionGroup = null;
			}

			_delayAction = value;

			if (_delayAction != null)
			{
				_delayActionGroup = _delayAction.CreateGroup(() =>
				{
					var stream = new TransactionFileStream(FileName, FileMode.OpenOrCreate);
					stream.Seek(0, SeekOrigin.End);
					return stream.CreateCsvWriter(Registry.Encoding);
				});
			}
		}
	}

	/// <inheritdoc />
	void IStorageEntityList<TEntity>.WaitFlush()
	{
		_delayActionGroup?.WaitFlush(false);
	}

	TEntity IStorageEntityList<TEntity>.ReadById(object id)
	{
		lock (SyncRoot)
			return _items.TryGetValue(NormalizedKey(id));
	}

	private TKey GetNormalizedKey(TEntity entity)
	{
		return NormalizedKey(GetKey(entity));
	}

	private static readonly bool _isSecId = typeof(TKey) == typeof(SecurityId);

	private static TKey NormalizedKey(object key)
	{
		if (key is string str)
		{
			str = str.ToLowerInvariant();

			if (_isSecId)
			{
				// backward compatibility when SecurityList accept as a key string
				key = str.ToSecurityId();
			}
			else
				key = str;
		}

		return (TKey)key;
	}

	/// <inheritdoc />
	public void Save(TEntity entity)
	{
		Save(entity, false);
	}

	/// <summary>
	/// Save object into storage.
	/// </summary>
	/// <param name="entity">Trade object.</param>
	/// <param name="forced">Forced update.</param>
	public virtual void Save(TEntity entity, bool forced)
	{
		lock (SyncRoot)
		{
			var item = _items.TryGetValue(GetNormalizedKey(entity));

			if (item == null)
			{
				Add(entity);
				return;
			}
			else if (IsChanged(entity, forced))
				UpdateCache(entity);
			else
				return;

			WriteMany([.. _items.Values]);
		}
	}

	#endregion

	/// <summary>
	/// Is <paramref name="entity"/> changed.
	/// </summary>
	/// <param name="entity">Trade object.</param>
	/// <param name="forced">Forced update.</param>
	/// <returns>Is changed.</returns>
	protected virtual bool IsChanged(TEntity entity, bool forced)
	{
		return true;
	}

	/// <summary>
	/// Get key from trade object.
	/// </summary>
	/// <param name="item">Trade object.</param>
	/// <returns>The key.</returns>
	protected abstract TKey GetKey(TEntity item);

	/// <summary>
	/// Write data into CSV.
	/// </summary>
	/// <param name="writer">CSV writer.</param>
	/// <param name="data">Trade object.</param>
	protected abstract void Write(CsvFileWriter writer, TEntity data);

	/// <summary>
	/// Read data from CSV.
	/// </summary>
	/// <param name="reader">CSV reader.</param>
	/// <returns>Trade object.</returns>
	protected abstract TEntity Read(FastCsvReader reader);

	/// <summary>
	///
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public override bool Contains(TEntity item)
	{
		lock (SyncRoot)
			return _items.ContainsKey(GetNormalizedKey(item));
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="item">Trade object.</param>
	/// <returns></returns>
	protected override bool OnAdding(TEntity item)
	{
		lock (SyncRoot)
		{
			if (!_items.TryAdd2(GetNormalizedKey(item), item))
				return false;

			AddCache(item);

			_delayActionGroup.Add((writer, data) =>
			{
				ResetCopy();
				Write(writer, data);
			}, item);
		}

		return base.OnAdding(item);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="item">Trade object.</param>
	protected override void OnRemoved(TEntity item)
	{
		base.OnRemoved(item);

		lock (SyncRoot)
		{
			_items.Remove(GetNormalizedKey(item));
			RemoveCache(item);

			WriteMany([.. _items.Values]);
		}
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="items"></param>
	protected void OnRemovedRange(IEnumerable<TEntity> items)
	{
		lock (SyncRoot)
		{
			foreach (var item in items)
			{
				_items.Remove(GetNormalizedKey(item));
				RemoveCache(item);
			}

			WriteMany([.. _items.Values]);
		}
	}

	/// <summary>
	///
	/// </summary>
	protected override void OnCleared()
	{
		base.OnCleared();

		lock (SyncRoot)
		{
			_items.Clear();
			ClearCache();

			_delayActionGroup.Add(writer =>
			{
				ResetCopy();
				writer.Truncate();
			});
		}
	}

	/// <summary>
	/// Write data into storage.
	/// </summary>
	/// <param name="values">Trading objects.</param>
	private void WriteMany(TEntity[] values)
	{
		_delayActionGroup.Add((writer, state) =>
		{
			ResetCopy();

			writer.Truncate();

			foreach (var item in state)
				Write(writer, item);
		}, values, compareStates: (v1, v2) =>
		{
			if (v1 == null)
				return v2 == null;

			if (v2 == null)
				return false;

			if (v1.Length != v2.Length)
				return false;

			return v1.SequenceEqual(v2);
		});
	}

	void ICsvEntityList.Init(IList<Exception> errors)
	{
		if (errors == null)
			throw new ArgumentNullException(nameof(errors));

		if (!File.Exists(FileName))
			return;

		Do.Invariant(() =>
		{
			using var stream = new FileStream(FileName, FileMode.OpenOrCreate, FileAccess.ReadWrite);

			var reader = stream.CreateCsvReader(Registry.Encoding);

			var hasDuplicates = false;
			var currErrors = 0;

			while (reader.NextLine())
			{
				try
				{
					var item = Read(reader);
					var key = GetNormalizedKey(item);

					lock (SyncRoot)
					{
						if (_items.TryAdd2(key, item))
						{
							InnerCollection.Add(item);
							AddCache(item);
						}
						else
							hasDuplicates = true;
					}

					currErrors = 0;
				}
				catch (Exception ex)
				{
					if (errors.Count < 100)
						errors.Add(ex);

					currErrors++;

					if (currErrors >= 1000)
						break;
				}
			}

			if (!hasDuplicates)
				return;

			try
			{
				lock (SyncRoot)
				{
					stream.SetLength(0);

					using var writer = stream.CreateCsvWriter(Registry.Encoding);

					foreach (var item in InnerCollection)
						Write(writer, item);
				}
			}
			catch (Exception ex)
			{
				errors.Add(ex);
			}
		});

		InnerCollection.ForEach(OnAdded);
	}

	/// <summary>
	/// Clear cache.
	/// </summary>
	protected virtual void ClearCache()
	{
	}

	/// <summary>
	/// Add item to cache.
	/// </summary>
	/// <param name="item">New item.</param>
	protected virtual void AddCache(TEntity item)
	{
	}

	/// <summary>
	/// Update item in cache.
	/// </summary>
	/// <param name="item">Item.</param>
	protected virtual void UpdateCache(TEntity item)
	{
	}

	/// <summary>
	/// Remove item from cache.
	/// </summary>
	/// <param name="item">Item.</param>
	protected virtual void RemoveCache(TEntity item)
	{
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return FileName;
	}
}