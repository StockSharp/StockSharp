namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Extended info <see cref="Message.ExtensionInfo"/> storage.
	/// </summary>
	public interface IExtendedInfoStorageItem
	{
		/// <summary>
		/// Extended fields (names and types).
		/// </summary>
		IEnumerable<Tuple<string, Type>> Fields { get; }

		/// <summary>
		/// Get all security identifiers.
		/// </summary>
		IEnumerable<SecurityId> Securities { get; }

		/// <summary>
		/// Add extended info.
		/// </summary>
		/// <param name="securityId">Security identifier.</param>
		/// <param name="extensionInfo">Extended information.</param>
		void Add(SecurityId securityId, IDictionary<string, object> extensionInfo);

		/// <summary>
		/// Load extended info. 
		/// </summary>
		/// <returns>Extended information.</returns>
		IEnumerable<Tuple<SecurityId, IDictionary<string, object>>> Load();

		/// <summary>
		/// Load extended info. 
		/// </summary>
		/// <returns>Extended information.</returns>
		IDictionary<string, object> Load(SecurityId securityId);

		/// <summary>
		/// Delete extended info.
		/// </summary>
		/// <param name="securityId">Security identifier.</param>
		void Delete(SecurityId securityId);
	}

	/// <summary>
	/// Extended info <see cref="Message.ExtensionInfo"/> storage.
	/// </summary>
	public interface IExtendedInfoStorage
	{
		/// <summary>
		/// Get all extended storage names.
		/// </summary>
		IEnumerable<string> StoragesNames { get; }

		/// <summary>
		/// To get storage for the specified name.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <returns>Storage.</returns>
		IExtendedInfoStorageItem Get(string storageName);

		/// <summary>
		/// To create storage.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="fields">Extended fields (names and types).</param>
		/// <returns>Storage.</returns>
		IExtendedInfoStorageItem Create(string storageName, Tuple<string, Type>[] fields);
	}

	/// <summary>
	/// Extended info <see cref="Message.ExtensionInfo"/> storage, used csv files.
	/// </summary>
	public class CsvExtendedInfoStorage : IExtendedInfoStorage
	{
		private class CsvExtendedInfoStorageItem : IExtendedInfoStorageItem
		{
			private readonly string _fileName;
			private Tuple<string, Type>[] _fields;
			private readonly SyncObject _lock = new SyncObject();
			//private readonly Dictionary<string, Type> _fieldTypes = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
			private readonly Dictionary<SecurityId, Dictionary<string, object>> _cache = new Dictionary<SecurityId, Dictionary<string, object>>();
			private bool _isDirty;

			public CsvExtendedInfoStorageItem(string fileName)
			{
				if (fileName.IsEmpty())
					throw new ArgumentNullException(nameof(fileName));

				_fileName = fileName;
			}

			public CsvExtendedInfoStorageItem(string fileName, Tuple<string, Type>[] fields)
				: this(fileName)
			{
				if (fields == null)
					throw new ArgumentNullException(nameof(fields));

				if (fields.IsEmpty())
					throw new ArgumentOutOfRangeException(nameof(fields));

				_fields = fields;
			}

			public void Init()
			{
				if (File.Exists(_fileName))
				{
					CultureInfo.InvariantCulture.DoInCulture(() =>
					{
						using (var stream = new FileStream(_fileName, FileMode.Open, FileAccess.Read))
						{
							var reader = new FastCsvReader(stream, Encoding.UTF8);

							reader.NextLine();
							reader.Skip();

							var fields = new string[reader.ColumnCount - 1];

							for (var i = 0; i < reader.ColumnCount - 1; i++)
								fields[i] = reader.ReadString();

							reader.NextLine();
							reader.Skip();

							var types = new Type[reader.ColumnCount - 1];

							for (var i = 0; i < reader.ColumnCount - 1; i++)
							{
								types[i] = reader.ReadString().To<Type>();
								//_fieldTypes.Add(fields[i], types[i]);
							}

							if (_fields == null)
							{
								if (fields.Length != types.Length)
									throw new InvalidOperationException($"{fields.Length} != {types.Length}");

								_fields = fields.Select((f, i) => Tuple.Create(f, types[i])).ToArray();
							}

							while (reader.NextLine())
							{
								var secId = reader.ReadString().ToSecurityId();

								var values = new Dictionary<string, object>();

								for (var i = 0; i < fields.Length; i++)
								{
									values[fields[i]] = reader.ReadString().To(types[i]);
								}

								_cache.Add(secId, values);
							}
						}
					});
				}
				else
				{
					if (_fields == null)
						throw new InvalidOperationException();
				}

				ThreadingHelper
					.Timer(() =>
					{
						try
						{
							OnFlush();
						}
						catch (Exception ex)
						{
							ex.LogError();
						}
					})
					.Interval(TimeSpan.FromSeconds(5));
			}

			private void OnFlush()
			{
				lock (_lock)
				{
					if (!_isDirty)
						return;

					_isDirty = false;

					CultureInfo.InvariantCulture.DoInCulture(() =>
					{
						using (var stream = new FileStream(_fileName, FileMode.Create, FileAccess.Write))
						using (var writer = new CsvFileWriter(stream))
						{
							writer.WriteRow(new[] { nameof(SecurityId) }.Concat(_fields.Select(f => f.Item1)));
							writer.WriteRow(new[] { typeof(string) }.Concat(_fields.Select(f => f.Item2)).Select(t => Converter.GetAlias(t) ?? t.GetTypeName(false)));

							foreach (var pair in _cache)
							{
								writer.WriteRow(new[] { pair.Key.ToStringId() }.Concat(_fields.Select(f => pair.Value.TryGetValue(f.Item1)?.To<string>())));
							}
						}
					});
				}
			}

			IEnumerable<Tuple<string, Type>> IExtendedInfoStorageItem.Fields => _fields;

			void IExtendedInfoStorageItem.Add(SecurityId securityId, IDictionary<string, object> extensionInfo)
			{
				lock (_lock)
				{
					var dict = _cache.SafeAdd(securityId);

					foreach (var field in _fields)
					{
						var value = extensionInfo[field.Item1];

						if (value == null)
							continue;

						dict[field.Item1] = value;

						//_fieldTypes.TryAdd(field, value.GetType());

						_isDirty = true;
					}
				}
			}

			IEnumerable<Tuple<SecurityId, IDictionary<string, object>>> IExtendedInfoStorageItem.Load()
			{
				lock (_lock)
				{
					var retVal = new Tuple<SecurityId, IDictionary<string, object>>[_cache.Count];

					var i = 0;
					foreach (var pair in _cache)
					{
						retVal[i] = Tuple.Create(pair.Key, pair.Value.ToDictionary());
						i++;
					}

					return retVal;
				}
			}

			IDictionary<string, object> IExtendedInfoStorageItem.Load(SecurityId securityId)
			{
				lock (_lock)
					return _cache.TryGetValue(securityId)?.ToDictionary();
			}

			void IExtendedInfoStorageItem.Delete(SecurityId securityId)
			{
				lock (_lock)
					_cache.Remove(securityId);
			}

			IEnumerable<SecurityId> IExtendedInfoStorageItem.Securities
			{
				get
				{
					lock (_lock)
						return _cache.Keys.ToArray();
				}
			}
		}

		private readonly CachedSynchronizedDictionary<string, CsvExtendedInfoStorageItem> _items = new CachedSynchronizedDictionary<string, CsvExtendedInfoStorageItem>(StringComparer.InvariantCultureIgnoreCase);
		private readonly string _path;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvExtendedInfoStorage"/>.
		/// </summary>
		/// <param name="path">Path to storage.</param>
		public CsvExtendedInfoStorage(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			_path = path.ToFullPath();
			Directory.CreateDirectory(path);
		}

		IExtendedInfoStorageItem IExtendedInfoStorage.Create(string storageName, Tuple<string, Type>[] fields)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			var fileName = Path.Combine(_path, storageName + ".csv");

			return _items.SafeAdd(storageName, key =>
			{
				var item = new CsvExtendedInfoStorageItem(fileName, fields);
				item.Init();
				return item;
			});
		}

		IExtendedInfoStorageItem IExtendedInfoStorage.Get(string storageName)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			return _items.TryGetValue(storageName);
		}

		IEnumerable<string> IExtendedInfoStorage.StoragesNames => _items.CachedKeys;

		/// <summary>
		/// Initialize the storage.
		/// </summary>
		public void Init()
		{
			foreach (var fileName in Directory.GetFiles(_path, "*.csv"))
			{
				var item = new CsvExtendedInfoStorageItem(fileName);
				_items.Add(Path.GetFileNameWithoutExtension(fileName), item);

				item.Init();
			}
		}
	}
}