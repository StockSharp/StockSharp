namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.IO;
	using System.Linq;
	using System.Text;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Security native identifier storage.
	/// </summary>
	public interface INativeIdStorage
	{
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
		/// <param name="nativeId">Native security identifier.</param>
		/// <returns><see langword="true"/> if native identifier was added. Otherwise, <see langword="false" />.</returns>
		bool TryAdd(string name, SecurityId securityId, object nativeId);

		/// <summary>
		/// Try get security identifier by native identifier.
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <param name="nativeId">Native security identifier.</param>
		/// <returns>Security identifier.</returns>
		SecurityId? TryGetByNativeId(string name, object nativeId);

		/// <summary>
		/// Try get native security identifier by identifier.
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <returns>Native security identifier.</returns>
		object TryGetBySecurityId(string name, SecurityId securityId);
	}

	/// <summary>
	/// Csv security native identifier storage.
	/// </summary>
	public sealed class CsvNativeIdStorage : INativeIdStorage
	{
		private readonly SynchronizedDictionary<string, PairSet<SecurityId, object>> _nativeIds = new SynchronizedDictionary<string, PairSet<SecurityId, object>>();

		private readonly string _path;

		/// <summary>
		/// Create <see cref="CsvNativeIdStorage"/>.
		/// </summary>
		/// <param name="path">Path to storage.</param>
		public CsvNativeIdStorage(string path)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			_path = path.ToFullPath();

			if (!Directory.Exists(_path))
				Directory.CreateDirectory(_path);

			Load();
		}

		/// <summary>
		/// Get native security identifiers for storage. 
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <returns>Security identifiers.</returns>
		public Tuple<SecurityId, object>[] Get(string name)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var nativeIds = _nativeIds.TryGetValue(name);

			if (nativeIds == null)
				return ArrayHelper.Empty<Tuple<SecurityId, object>>();

			return nativeIds.Select(p => Tuple.Create(p.Key, p.Value)).ToArray();
		}

		/// <summary>
		/// Try add native security identifier to storage.
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <param name="nativeId">Native security identifier.</param>
		/// <returns><see langword="true"/> if native identifier was added. Otherwise, <see langword="false" />.</returns>
		public bool TryAdd(string name, SecurityId securityId, object nativeId)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			if (nativeId == null)
				throw new ArgumentNullException(nameof(nativeId));

			var nativeIds = _nativeIds.SafeAdd(name);
			var added = nativeIds.TryAdd(securityId, nativeId);

			if (!added)
				return false;

			Save(name, nativeIds);

			return true;
		}

		/// <summary>
		/// Try get security identifier by native identifier.
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <param name="nativeId">Native security identifier.</param>
		/// <returns>Security identifier.</returns>
		public SecurityId? TryGetByNativeId(string name, object nativeId)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var nativeIds = _nativeIds.TryGetValue(name);

			if (nativeIds == null)
				return null;

			SecurityId securityId;

			if (!nativeIds.TryGetKey(nativeId, out securityId))
				return null;

			return securityId;
		}

		/// <summary>
		/// Try get native security identifier by identifier.
		/// </summary>
		/// <param name="name">Storage name.</param>
		/// <param name="securityId">Security identifier.</param>
		/// <returns>Native security identifier.</returns>
		public object TryGetBySecurityId(string name, SecurityId securityId)
		{
			if (name == null)
				throw new ArgumentNullException(nameof(name));

			var nativeIds = _nativeIds.TryGetValue(name);

			if (nativeIds == null)
				return null;

			return nativeIds.TryGetValue(securityId);
		}

		private void Save(string name, IEnumerable<KeyValuePair<SecurityId, object>> values)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				var fileName = Path.Combine(_path, name + ".csv");

				try
				{
					using (var stream = new FileStream(fileName, FileMode.OpenOrCreate))
					using (var writer = new CsvFileWriter(stream))
					{
						foreach (var item in values)
						{
							var securityId = item.Key;
							var nativeId = item.Value;

							writer.WriteRow(new[]
							{
								securityId.SecurityCode,
								securityId.BoardCode,
								nativeId.GetType().GetTypeName(false),
								nativeId.ToString()
							});
						}
					}
				}
				catch (Exception excp)
				{
					excp.LogError("Save native storage to {0} error.".Put(fileName));
				}
			});
		}

		private void Load()
		{
			var files = Directory.GetFiles(_path, "*.csv");

			foreach (var fileName in files)
				LoadFile(fileName);
		}

		private void LoadFile(string fileName)
		{
			CultureInfo.InvariantCulture.DoInCulture(() =>
			{
				try
				{
					var name = Path.GetFileNameWithoutExtension(fileName);
					var nativeIds = _nativeIds.SafeAdd(name);

					using (var stream = new FileStream(fileName, FileMode.OpenOrCreate))
					{
						var reader = new FastCsvReader(stream, Encoding.UTF8);

						while (reader.NextLine())
						{
							var securityId = new SecurityId
							{
								SecurityCode = reader.ReadString(),
								BoardCode = reader.ReadString()
							};

							var type = reader.ReadString().To<Type>();
							var nativeId = reader.ReadString().To(type);

							nativeIds.Add(securityId, nativeId);
						}
					}
				}
				catch (Exception excp)
				{
					excp.LogError("Load native storage from {0} error.".Put(fileName));
				}
			});
        }
	}
}