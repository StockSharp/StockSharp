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

	using StockSharp.Messages;

	/// <summary>
	/// Extended info <see cref="Message.ExtensionInfo"/> storage.
	/// </summary>
	public interface IExtendedInfoStorageItem
	{
		/// <summary>
		/// Names of extended security fields.
		/// </summary>
		string[] Fields { get; }

		/// <summary>
		/// Add extended info.
		/// </summary>
		/// <param name="securityId">Security identifier.</param>
		/// <param name="extensionInfo">Extended information.</param>
		void Add(SecurityId securityId, IDictionary<object, object> extensionInfo);

		/// <summary>
		/// Load extended info. 
		/// </summary>
		/// <returns>Extended information.</returns>
		IEnumerable<Tuple<SecurityId, IDictionary<object, object>>> Load();
	}

	/// <summary>
	/// Extended info <see cref="Message.ExtensionInfo"/> storage.
	/// </summary>
	public interface IExtendedInfoStorage
	{
		/// <summary>
		/// To get and initialize storage for the specified name.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="fields">Names of extended security fields.</param>
		/// <returns>Storage.</returns>
		IExtendedInfoStorageItem Get(string storageName, string[] fields);
	}

	/// <summary>
	/// Extended info <see cref="Message.ExtensionInfo"/> storage, used csv files.
	/// </summary>
	public class CsvExtendedInfoStorage : IExtendedInfoStorage
	{
		private class CsvExtendedInfoStorageItem : IExtendedInfoStorageItem
		{
			private readonly string _fileName;
			private readonly string[] _fields;
			private readonly SyncObject _lock = new SyncObject();
			private readonly Dictionary<string, Type> _fieldTypes = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
			private readonly Dictionary<SecurityId, Dictionary<object, object>> _cache = new Dictionary<SecurityId, Dictionary<object, object>>();
			private bool _isDirty;

			public CsvExtendedInfoStorageItem(string fileName, string[] fields)
			{
				if (fileName.IsEmpty())
					throw new ArgumentNullException(nameof(fileName));

				if (fields == null)
					throw new ArgumentNullException(nameof(fields));

				if (fields.IsEmpty())
					throw new ArgumentOutOfRangeException(nameof(fields));

				_fileName = fileName;
				_fields = fields;

				ThreadingHelper
					.Timer(OnFlush)
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
							writer.WriteRow(new[] { nameof(SecurityId) }.Concat(_fields));
							writer.WriteRow(new[] { typeof(string) }.Concat(_fields.Select(f => _fieldTypes.TryGetValue(f) ?? typeof(string))).Select(t => Converter.GetAlias(t) ?? t.GetTypeName(false)));

							foreach (var pair in _cache)
							{
								writer.WriteRow(new[] { pair.Key.ToStringId() }.Concat(_fields.Select(f => pair.Value.TryGetValue(f)?.To<string>())));
							}
						}
					});
				}
			}

			string[] IExtendedInfoStorageItem.Fields => _fields;

			void IExtendedInfoStorageItem.Add(SecurityId securityId, IDictionary<object, object> extensionInfo)
			{
				lock (_lock)
				{
					var dict = _cache.SafeAdd(securityId);

					foreach (var field in _fields)
					{
						var value = extensionInfo[field];

						if (value == null)
							continue;

						dict[field] = value;

						_fieldTypes.TryAdd(field, value.GetType());

						_isDirty = true;
					}
				}
			}

			IEnumerable<Tuple<SecurityId, IDictionary<object, object>>> IExtendedInfoStorageItem.Load()
			{
				lock (_lock)
				{
					var retVal = new Tuple<SecurityId, IDictionary<object, object>>[_cache.Count];

					var i = 0;
					foreach (var pair in _cache)
					{
						retVal[i] = Tuple.Create(pair.Key, pair.Value.ToDictionary());
						i++;
					}

					return retVal;
				}
			}

			public void Init()
			{
				if (!File.Exists(_fileName))
					return;

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
							_fieldTypes.Add(fields[i], types[i]);
						}

						var idGenerator = new SecurityIdGenerator();

						while (reader.NextLine())
						{
							var secId = idGenerator.Split(reader.ReadString());

							var values = new Dictionary<object, object>();

							for (var i = 0; i < fields.Length; i++)
							{
								values[fields[i]] = reader.ReadString().To(types[i]);
							}

							_cache.Add(secId, values);
						}
					}
				});
			}
		}

		private readonly SynchronizedDictionary<string, CsvExtendedInfoStorageItem> _items = new SynchronizedDictionary<string, CsvExtendedInfoStorageItem>(StringComparer.InvariantCultureIgnoreCase);
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

		/// <summary>
		/// To get and initialize storage for the specified name.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="fields">Names of extended security fields.</param>
		/// <returns>Storage.</returns>
		public IExtendedInfoStorageItem Get(string storageName, string[] fields)
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
	}
}