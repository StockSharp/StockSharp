namespace StockSharp.Algo.Storages.Csv;

using System.IO.Compression;

using Ecng.IO;

/// <summary>
/// The interface for presentation in the form of list of trade objects, received from the external storage.
/// </summary>
public interface ICsvEntityList
{
	/// <summary>
	/// Initialize the storage.
	/// </summary>
	/// <param name="errors">Possible errors.</param>
	/// <param name="cancellationToken"><see cref="CancellationToken"/></param>
	ValueTask InitAsync(IList<Exception> errors, CancellationToken cancellationToken);

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
public abstract class CsvEntityList<TKey, TEntity> : SynchronizedList<TEntity>, IStorageEntityList<TEntity>, ICsvEntityList, IDisposable
	where TEntity : class
{
	private readonly CachedSynchronizedDictionary<TKey, TEntity> _items = [];

	private readonly Lock _copySync = new();
	private byte[] _copy;

	private readonly ChannelExecutor _executor;
	private TransactionFileStream _stream;
	private CsvFileWriter _writer;

	/// <summary>
	/// The CSV storage of trading objects.
	/// </summary>
	protected CsvEntityRegistry Registry { get; }

	private IFileSystem FileSystem => Registry.FileSystem;

	/// <inheritdoc />
	public TEntity[] Cache => _items.CachedValues;

	/// <summary>
	/// Initializes a new instance of the <see cref="CsvEntityList{TKey,TEntity}"/>.
	/// </summary>
	/// <param name="registry">The CSV storage of trading objects.</param>
	/// <param name="fileName">CSV file name.</param>
	/// <param name="executor">Sequential operation executor for disk access synchronization.</param>
	protected CsvEntityList(CsvEntityRegistry registry, string fileName, ChannelExecutor executor)
	{
		if (fileName.IsEmpty())
			throw new ArgumentNullException(nameof(fileName));

		Registry = registry ?? throw new ArgumentNullException(nameof(registry));
		_executor = executor ?? throw new ArgumentNullException(nameof(executor));

		FileName = Path.Combine(Registry.Path, fileName);
	}

	/// <summary>
	/// Disposes the resources.
	/// </summary>
	public void Dispose()
	{
		_executor.Add(() =>
		{
			_writer?.Dispose();
			_writer = null;

			_stream?.Dispose();
			_stream = null;
		});

		GC.SuppressFinalize(this);
	}

	private void EnsureStream()
	{
		if (_stream != null)
			return;

		var dir = Path.GetDirectoryName(FileName);
		FileSystem.CreateDirectory(dir);

		_stream = new TransactionFileStream(FileSystem, FileName, FileMode.Append);
		_writer = _stream.CreateCsvWriter(Registry.Encoding);
	}

	private void ResetStream()
	{
		_writer?.Dispose();
		_writer = null;

		_stream?.Dispose();
		_stream = null;
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

		using (_copySync.EnterScope())
			body = _copy;

		if (body is null)
		{
			using (_copySync.EnterScope())
			{
				if (FileSystem.FileExists(FileName))
				{
					using var stream = FileSystem.OpenRead(FileName);
					using var ms = new MemoryStream();
					stream.CopyTo(ms);
					body = ms.ToArray();
				}
				else
					body = [];
			}

			body = body.Compress<GZipStream>();

			using (_copySync.EnterScope())
				_copy ??= body;
		}

		return body;
	}

	private void ResetCopy()
	{
		if (!CreateArchivedCopy)
			return;

		using (_copySync.EnterScope())
			_copy = null;
	}

	#region IStorageEntityList<T>

	TEntity IStorageEntityList<TEntity>.ReadById(object id)
	{
		using (EnterScope())
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
		using (EnterScope())
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
		using (EnterScope())
			return _items.ContainsKey(GetNormalizedKey(item));
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="item">Trade object.</param>
	/// <returns></returns>
	protected override bool OnAdding(TEntity item)
	{
		using (EnterScope())
		{
			if (!_items.TryAdd2(GetNormalizedKey(item), item))
				return false;

			AddCache(item);

			var itemCopy = item;
			_executor.Add(() =>
			{
				EnsureStream();
				ResetCopy();
				Write(_writer, itemCopy);
				_writer.Flush();
				_stream.Commit();
			});
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

		using (EnterScope())
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
		using (EnterScope())
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

		using (EnterScope())
		{
			_items.Clear();
			ClearCache();

			_executor.Add(() =>
			{
				ResetStream();

				var dir = Path.GetDirectoryName(FileName);
				FileSystem.CreateDirectory(dir);

				_stream = new TransactionFileStream(FileSystem, FileName, FileMode.Create);
				_writer = _stream.CreateCsvWriter(Registry.Encoding);
				ResetCopy();
				_writer.Flush();
				_stream.Commit();
			});
		}
	}

	/// <summary>
	/// Write data into storage.
	/// </summary>
	/// <param name="values">Trading objects.</param>
	private void WriteMany(TEntity[] values)
	{
		var valuesCopy = values;
		_executor.Add(() =>
		{
			ResetStream();

			var dir = Path.GetDirectoryName(FileName);
			FileSystem.CreateDirectory(dir);

			_stream = new TransactionFileStream(FileSystem, FileName, FileMode.Create);
			_writer = _stream.CreateCsvWriter(Registry.Encoding);
			ResetCopy();

			foreach (var item in valuesCopy)
				Write(_writer, item);

			_writer.Flush();
			_stream.Commit();
		});
	}

	async ValueTask ICsvEntityList.InitAsync(IList<Exception> errors, CancellationToken cancellationToken)
	{
		if (errors == null)
			throw new ArgumentNullException(nameof(errors));

		if (!FileSystem.FileExists(FileName))
			return;

		var hasDuplicates = false;

		await Do.InvariantAsync(async () =>
		{
			using var stream = FileSystem.OpenRead(FileName);

			var reader = stream.CreateCsvReader(Registry.Encoding);

			var currErrors = 0;

			while (await reader.NextLineAsync(cancellationToken))
			{
				try
				{
					var item = Read(reader);
					var key = GetNormalizedKey(item);

					using (EnterScope())
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
		});

		if (hasDuplicates)
		{
			try
			{
				using (EnterScope())
				{
					using var stream = new TransactionFileStream(FileSystem, FileName, FileMode.Create);
					using var writer = stream.CreateCsvWriter(Registry.Encoding);

					foreach (var item in InnerCollection)
						Write(writer, item);

					writer.Flush();
					stream.Commit();
				}
			}
			catch (Exception ex)
			{
				errors.Add(ex);
			}
		}

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