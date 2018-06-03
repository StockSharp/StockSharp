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
	using Ecng.Reflection;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Security native identifier storage.
	/// </summary>
	public interface INativeIdStorage
	{
		/// <summary>
		/// The new native security identifier added to storage.
		/// </summary>
		event Action<string, SecurityId, object> Added;

		/// <summary>
		/// Initialize the storage.
		/// </summary>
		/// <returns>Possible errors with storage names. Empty dictionary means initialization without any issues.</returns>
		IDictionary<string, Exception> Init();

		/// <summary>
		/// Get native security identifiers for storage. 
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <returns>Security identifiers.</returns>
		Tuple<SecurityId, object>[] Get(string name);

		/// <summary>
		/// Try add native security identifier to storage.
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <param name="nativeId">Native (internal) trading system security id.</param>
		/// <param name="isPersistable">Save the identifier as a permanent.</param>
		/// <returns><see langword="true"/> if native identifier was added. Otherwise, <see langword="false" />.</returns>
		bool TryAdd(string name, SecurityId securityId, object nativeId, bool isPersistable = true);

		/// <summary>
		/// Try get security identifier by native identifier.
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <param name="nativeId">Native (internal) trading system security id.</param>
		/// <returns>Security identifier.</returns>
		SecurityId? TryGetByNativeId(string name, object nativeId);

		/// <summary>
		/// Try get native security identifier by identifier.
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <returns>Native (internal) trading system security id.</returns>
		object TryGetBySecurityId(string name, SecurityId securityId);
	}

	/// <summary>
	/// CSV security native identifier storage.
	/// </summary>
	public sealed class CsvNativeIdStorage : INativeIdStorage
	{
		private readonly SyncObject _sync = new SyncObject();
		private readonly Dictionary<string, PairSet<SecurityId, object>> _nativeIds = new Dictionary<string, PairSet<SecurityId, object>>(StringComparer.InvariantCultureIgnoreCase);

		private readonly string _path;

		/// <summary>
		/// Initializes a new instance of the <see cref="CsvNativeIdStorage"/>.
		/// </summary>
		/// <param name="path">Path to storage.</param>
		public CsvNativeIdStorage(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			_path = path.ToFullPath();
			_delayAction = new DelayAction(ex => ex.LogError());
		}

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
		public event Action<string, SecurityId, object> Added;

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
		public Tuple<SecurityId, object>[] Get(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			lock (_sync)
			{
				var nativeIds = _nativeIds.TryGetValue(name);

				if (nativeIds == null)
					return ArrayHelper.Empty<Tuple<SecurityId, object>>();

				return nativeIds.Select(p => Tuple.Create(p.Key, p.Value)).ToArray();
			}
		}

		/// <inheritdoc />
		public bool TryAdd(string name, SecurityId securityId, object nativeId, bool isPersistable)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			if (nativeId == null)
				throw new ArgumentNullException(nameof(nativeId));

			lock (_sync)
			{
				var nativeIds = _nativeIds.SafeAdd(name);
				var added = nativeIds.TryAdd(securityId, nativeId);

				if (!added)
					return false;
			}

			if (isPersistable)
				Save(name, securityId, nativeId);

			Added?.Invoke(name, securityId, nativeId);

			return true;
		}

		/// <inheritdoc />
		public SecurityId? TryGetByNativeId(string name, object nativeId)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			lock (_sync)
			{
				var nativeIds = _nativeIds.TryGetValue(name);

				if (nativeIds == null)
					return null;

				if (!nativeIds.TryGetKey(nativeId, out var securityId))
					return null;

				return securityId;
			}
		}

		/// <inheritdoc />
		public object TryGetBySecurityId(string name, SecurityId securityId)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			lock (_sync)
				return _nativeIds.TryGetValue(name)?.TryGetValue(securityId);
		}

		private void Save(string name, SecurityId securityId, object nativeId)
		{
			DelayAction.DefaultGroup.Add(() =>
			{
				var fileName = Path.Combine(_path, name + ".csv");

				var appendHeader = !File.Exists(fileName) || new FileInfo(fileName).Length == 0;

				using (var writer = new CsvFileWriter(new TransactionFileStream(fileName, FileMode.Append)))
				{
					var nativeIdType = nativeId.GetType();
					var typleType = nativeIdType.GetGenericType(typeof(Tuple<,>));

					if (appendHeader)
					{
						if (typleType == null)
						{
							writer.WriteRow(new[]
							{
								"Symbol",
								"Board",
								GetTypeName(nativeIdType),
							});
						}
						else
						{
							dynamic tuple = nativeId;

							writer.WriteRow(new[]
							{
								"Symbol",
								"Board",
								GetTypeName((Type)tuple.Item1.GetType()),
								GetTypeName((Type)tuple.Item2.GetType()),
							});
						}
					}

					if (typleType == null)
					{
						writer.WriteRow(new[]
						{
							securityId.SecurityCode,
							securityId.BoardCode,
							nativeId.ToString()
						});
					}
					else
					{
						dynamic tuple = nativeId;

						writer.WriteRow(new[]
						{
							securityId.SecurityCode,
							securityId.BoardCode,
							(string)tuple.Item1.ToString(),
							(string)tuple.Item2.ToString()
						});
					}
				}
			});
		}

		private static string GetTypeName(Type nativeIdType)
		{
			return Converter.GetAlias(nativeIdType) ?? nativeIdType.GetTypeName(false);
		}

		private void LoadFile(string fileName)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				if (!File.Exists(fileName))
					return;

				var name = Path.GetFileNameWithoutExtension(fileName);

				var pairs = new List<Tuple<SecurityId, object>>();

				using (var stream = new FileStream(fileName, FileMode.Open, FileAccess.Read))
				{
					var reader = new FastCsvReader(stream, Encoding.UTF8);

					reader.NextLine();
					reader.Skip(2);

					var type1 = reader.ReadString().To<Type>();
					var type2 = reader.ReadString().To<Type>();

					var isTuple = type2 != null;

					while (reader.NextLine())
					{
						var securityId = new SecurityId
						{
							SecurityCode = reader.ReadString(),
							BoardCode = reader.ReadString()
						};

						var nativeId = reader.ReadString().To(type1);

						if (isTuple)
						{
							var nativeId2 = reader.ReadString().To(type2);
							nativeId = typeof(Tuple<,>).MakeGenericType(type1, type2).CreateInstance(new[] { nativeId, nativeId2 });
						}
						
						pairs.Add(Tuple.Create(securityId, nativeId));
					}
				}

				lock (_sync)
				{
					var nativeIds = _nativeIds.SafeAdd(name);

					foreach (var tuple in pairs)
					{
						nativeIds.Add(tuple.Item1, tuple.Item2);
					}
				}
			});
        }
	}

	/// <summary>
	/// In memory security native identifier storage.
	/// </summary>
	public class InMemoryNativeIdStorage : INativeIdStorage
	{
		private readonly SynchronizedDictionary<string, PairSet<SecurityId, object>> _nativeIds = new SynchronizedDictionary<string, PairSet<SecurityId, object>>(StringComparer.InvariantCultureIgnoreCase);

		private Action<string, SecurityId, object> _added;

		event Action<string, SecurityId, object> INativeIdStorage.Added
		{
			add => _added += value;
			remove => _added -= value;
		}

		IDictionary<string, Exception> INativeIdStorage.Init()
		{
			return new Dictionary<string, Exception>();
		}

		bool INativeIdStorage.TryAdd(string name, SecurityId securityId, object nativeId, bool isPersistable)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			if (nativeId == null)
				throw new ArgumentNullException(nameof(nativeId));

			lock (_nativeIds.SyncRoot)
			{
				var added = _nativeIds.SafeAdd(name).TryAdd(securityId, nativeId);

				if (!added)
					return false;
			}

			_added?.Invoke(name, securityId, nativeId);

			return true;
		}

		object INativeIdStorage.TryGetBySecurityId(string name, SecurityId securityId)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			lock (_nativeIds.SyncRoot)
				return _nativeIds.TryGetValue(name)?.TryGetValue(securityId);
		}

		SecurityId? INativeIdStorage.TryGetByNativeId(string name, object nativeId)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			var securityId = default(SecurityId);

			lock (_nativeIds.SyncRoot)
			{
				if (_nativeIds.TryGetValue(name)?.TryGetKey(nativeId, out securityId) != true)
					return null;
			}

			return securityId;
		}

		Tuple<SecurityId, object>[] INativeIdStorage.Get(string name)
		{
			if (name.IsEmpty())
				throw new ArgumentNullException(nameof(name));

			lock (_nativeIds.SyncRoot)
				return _nativeIds.TryGetValue(name)?.Select(p => Tuple.Create(p.Key, p.Value)).ToArray() ?? ArrayHelper.Empty<Tuple<SecurityId, object>>();
		}
	}
}