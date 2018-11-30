namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Security identifier mapping.
	/// </summary>
	public struct SecurityIdMapping
	{
		/// <summary>
		/// StockSharp format.
		/// </summary>
		public SecurityId StockSharpId { get; set; }

		/// <summary>
		/// Adapter format.
		/// </summary>
		public SecurityId AdapterId { get; set; }

		/// <summary>
		/// Cast <see cref="KeyValuePair{T1,T2}"/> object to the type <see cref="SecurityIdMapping"/>.
		/// </summary>
		/// <param name="pair"><see cref="KeyValuePair{T1,T2}"/> value.</param>
		/// <returns><see cref="SecurityIdMapping"/> value.</returns>
		public static implicit operator SecurityIdMapping(KeyValuePair<SecurityId, SecurityId> pair)
		{
			return new SecurityIdMapping
			{
				StockSharpId = pair.Key,
				AdapterId = pair.Value
			};
		}

		/// <summary>
		/// Cast object from <see cref="SecurityIdMapping"/> to <see cref="KeyValuePair{T1,T2}"/>.
		/// </summary>
		/// <param name="mapping"><see cref="SecurityIdMapping"/> value.</param>
		/// <returns><see cref="KeyValuePair{T1,T2}"/> value.</returns>
		public static explicit operator KeyValuePair<SecurityId, SecurityId>(SecurityIdMapping mapping)
		{
			return new KeyValuePair<SecurityId, SecurityId>(mapping.StockSharpId, mapping.AdapterId);
		}

		/// <inheritdoc />
		public override string ToString()
		{
			return $"{StockSharpId}<->{AdapterId}";
		}
	}

	/// <summary>
	/// Security identifier mappings storage.
	/// </summary>
	public interface ISecurityMappingStorage
	{
		/// <summary>
		/// The new native security identifier added to storage.
		/// </summary>
		event Action<string, SecurityIdMapping> Changed;

		/// <summary>
		/// Initialize the storage.
		/// </summary>
		/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
		IDictionary<string, Exception> Init();

		/// <summary>
		/// Get storage names.
		/// </summary>
		/// <returns>Storage names.</returns>
		IEnumerable<string> GetStorageNames();

		/// <summary>
		/// Get security identifier mappings for storage. 
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <returns>Security identifiers mapping.</returns>
		IEnumerable<SecurityIdMapping> Get(string name);

		/// <summary>
		/// Add security identifier mapping.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="mapping">Security identifier mapping.</param>
		/// <returns><see langword="true"/> if security mapping was added. If was changed, <see langword="false" />.</returns>
		bool Add(string storageName, SecurityIdMapping mapping);

		/// <summary>
		/// Remove security mapping.
		/// </summary>
		/// <param name="storageName">Storage name.</param>
		/// <param name="stockSharpId">StockSharp format.</param>
		/// <returns><see langword="true"/> if mapping was added. Otherwise, <see langword="false" />.</returns>
		bool Remove(string storageName, SecurityId stockSharpId);
	}

	/// <summary>
	/// In memory security identifier mappings storage.
	/// </summary>
	public class InMemorySecurityMappingStorage : ISecurityMappingStorage
	{
		private readonly SynchronizedDictionary<string, PairSet<SecurityId, SecurityId>> _mappings = new SynchronizedDictionary<string, PairSet<SecurityId, SecurityId>>(StringComparer.InvariantCultureIgnoreCase);

		private event Action<string, SecurityIdMapping> _changed;

		event Action<string, SecurityIdMapping> ISecurityMappingStorage.Changed
		{
			add => _changed += value;
			remove => _changed -= value;
		}

		IDictionary<string, Exception> ISecurityMappingStorage.Init()
		{
			return new Dictionary<string, Exception>();
		}

		IEnumerable<string> ISecurityMappingStorage.GetStorageNames()
		{
			lock (_mappings.SyncRoot)
				return _mappings.Keys.ToArray();
		}

		IEnumerable<SecurityIdMapping> ISecurityMappingStorage.Get(string name)
		{
			if (CollectionHelper.IsEmpty(name))
				throw new ArgumentNullException(nameof(name));

			lock (_mappings.SyncRoot)
				return _mappings.TryGetValue(name)?.Select(p => (SecurityIdMapping)p).ToArray() ?? ArrayHelper.Empty<SecurityIdMapping>();
		}

		bool ISecurityMappingStorage.Add(string storageName, SecurityIdMapping mapping)
		{
			if (CollectionHelper.IsEmpty(storageName))
				throw new ArgumentNullException(nameof(storageName));

			if (mapping.IsDefault())
				throw new ArgumentNullException(nameof(mapping));

			var added = false;

			lock (_mappings.SyncRoot)
			{
				var mappings = _mappings.SafeAdd(storageName);

				var stockSharpId = mapping.StockSharpId;

				if (mappings.ContainsKey(stockSharpId))
				{
					mappings.Remove(stockSharpId);
				}
				else
					added = true;

				mappings.Add(stockSharpId, mapping.AdapterId);
			}

			_changed?.Invoke(storageName, mapping);

			return added;
		}

		bool ISecurityMappingStorage.Remove(string storageName, SecurityId stockSharpId)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (stockSharpId.IsDefault())
				throw new ArgumentNullException(nameof(storageName));

			lock (_mappings.SyncRoot)
			{
				var mappings = _mappings.TryGetValue(storageName);

				if (mappings == null)
					return false;

				var removed = mappings.Remove(stockSharpId);

				if (!removed)
					return false;
			}

			_changed?.Invoke(storageName, new SecurityIdMapping { StockSharpId = stockSharpId });

			return true;
		}
	}

	/// <summary>
	/// CSV security identifier mappings storage.
	/// </summary>
	public sealed class CsvSecurityMappingStorage : ISecurityMappingStorage
	{
		private readonly SynchronizedDictionary<string, PairSet<SecurityId, SecurityId>> _mappings = new SynchronizedDictionary<string, PairSet<SecurityId, SecurityId>>(StringComparer.InvariantCultureIgnoreCase);

		private readonly string _path;

		private DelayAction _delayAction;

		/// <summary>
		/// The time delayed action.
		/// </summary>
		public DelayAction DelayAction
		{
			get => _delayAction;
			set => _delayAction = value ?? throw new ArgumentNullException(nameof(value));
		}

		/// <inheritdoc />
		public event Action<string, SecurityIdMapping> Changed;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvSecurityMappingStorage"/>.
		/// </summary>
		/// <param name="path">Path to storage.</param>
		public CsvSecurityMappingStorage(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			_path = path.ToFullPath();
			_delayAction = new DelayAction(ex => ex.LogError());
		}

		/// <inheritdoc />
		public IDictionary<string, Exception> Init()
		{
			if (!Directory.Exists(_path))
				Directory.CreateDirectory(_path);

			var files = Directory.GetFiles(_path, "*.csv");

			var errors = new Dictionary<string, Exception>();

			foreach (var fileName in files)
			{
				try
				{
					LoadFile(fileName);
				}
				catch (Exception ex)
				{
					errors.Add(fileName, ex);
				}
			}

			return errors;
		}

		/// <inheritdoc />
		public IEnumerable<string> GetStorageNames()
		{
			lock (_mappings.SyncRoot)
				return _mappings.Keys.ToArray();
		}

		/// <inheritdoc />
		public IEnumerable<SecurityIdMapping> Get(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			lock (_mappings.SyncRoot)
				return _mappings.TryGetValue(name)?.Select(p => (SecurityIdMapping)p).ToArray() ?? ArrayHelper.Empty<SecurityIdMapping>();
		}

		/// <inheritdoc />
		public bool Add(string storageName, SecurityIdMapping mapping)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (mapping.IsDefault())
				throw new ArgumentNullException(nameof(mapping));

			PairSet<SecurityId, SecurityId> mappings;
			var added = false;

			lock (_mappings.SyncRoot)
			{
				mappings = _mappings.SafeAdd(storageName);

				var stockSharpId = mapping.StockSharpId;

				if (mappings.ContainsKey(stockSharpId))
				{
					mappings.Remove(stockSharpId);
				}
				else
					added = true;

				mappings.Add(stockSharpId, mapping.AdapterId);
			}

			if (!added)
			{
				SecurityIdMapping[] items;

				lock (_mappings.SyncRoot)
					items = mappings.Select(p => (SecurityIdMapping)p).ToArray();

				Save(storageName, true, items);
			}
			else
				Save(storageName, false, new[] { mapping });

			Changed?.Invoke(storageName, mapping);

			return added;
		}

		/// <inheritdoc />
		public bool Remove(string storageName, SecurityId stockSharpId)
		{
			if (storageName.IsEmpty())
				throw new ArgumentNullException(nameof(storageName));

			if (stockSharpId.IsDefault())
				throw new ArgumentNullException(nameof(stockSharpId));

			PairSet<SecurityId, SecurityId> mappings;

			lock (_mappings.SyncRoot)
			{
				mappings = _mappings.TryGetValue(storageName);

				if (mappings == null)
					return false;

				var removed = mappings.Remove(stockSharpId);

				if (!removed)
					return false;
			}

			SecurityIdMapping[] items;

			lock (_mappings.SyncRoot)
				items = mappings.Select(p => (SecurityIdMapping)p).ToArray();

			Save(storageName, true, items);

			Changed?.Invoke(storageName, new SecurityIdMapping { StockSharpId = stockSharpId });

			return true;
		}

		private void LoadFile(string fileName)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				if (!File.Exists(fileName))
					return;

				var name = Path.GetFileNameWithoutExtension(fileName);

				var pairs = new List<Tuple<SecurityId, SecurityId>>();

				using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
				{
					var reader = new FastCsvReader(stream, Encoding.UTF8);

					reader.NextLine();

					while (reader.NextLine())
					{
						var securityId = new SecurityId
						{
							SecurityCode = reader.ReadString(),
							BoardCode = reader.ReadString()
						};
						var adapterId = new SecurityId
						{
							SecurityCode = reader.ReadString(),
							BoardCode = reader.ReadString()
						};

						pairs.Add(Tuple.Create(securityId, adapterId));
					}
				}

				lock (_mappings.SyncRoot)
				{
					var mappings = _mappings.SafeAdd(name);

					foreach (var tuple in pairs)
						mappings.Add(tuple.Item1, tuple.Item2);
				}
			});
		}

		private void Save(string name, bool overwrite, IEnumerable<SecurityIdMapping> mappings)
		{
			DelayAction.DefaultGroup.Add(() =>
			{
				var fileName = Path.Combine(_path, name + ".csv");

				var appendHeader = overwrite || !File.Exists(fileName);
				var mode = overwrite ? FileMode.Create : FileMode.Append;

				using (var writer = new CsvFileWriter(new TransactionFileStream(fileName, mode)))
				{
					if (appendHeader)
						writer.WriteRow(new[] { "SecurityCode", "BoardCode", "AdapterCode", "AdapterBoard" });

					foreach (var mapping in mappings)
					{
						writer.WriteRow(new[]
						{
							mapping.StockSharpId.SecurityCode,
							mapping.StockSharpId.BoardCode,
							mapping.AdapterId.SecurityCode,
							mapping.AdapterId.BoardCode
						});
					}
				}
			});
		}
	}
}