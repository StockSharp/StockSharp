namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// Implementation of <see cref="ISnapshotStorage{TKey}"/>.
	/// </summary>
	/// <typeparam name="TKey">Type of key value.</typeparam>
	/// <typeparam name="TMessage">Message type.</typeparam>
	public class SnapshotStorage<TKey, TMessage> : ISnapshotStorage<TKey>
		where TMessage : Message
	{
		private readonly string _path;

		private readonly AllocationArray<TKey> _dirtyKeys = new AllocationArray<TKey>(10);
		private readonly SynchronizedDictionary<TKey, TMessage> _snapshots = new SynchronizedDictionary<TKey, TMessage>();
		private readonly Dictionary<TKey, long> _offsets = new Dictionary<TKey, long>();
		private readonly ISnapshotSerializer<TKey, TMessage> _serializer;

		/// <summary>
		/// Initializes a new instance of the <see cref="SnapshotStorage{TKey,TMessage}"/>.
		/// </summary>
		/// <param name="path">Path to storage.</param>
		/// <param name="serializer">Serializer.</param>
		public SnapshotStorage(string path, ISnapshotSerializer<TKey, TMessage> serializer)
		{
			if (path == null)
				throw new ArgumentNullException(nameof(path));

			_path = path.ToFullPath();
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		}

		void ISnapshotStorage.Init()
		{
			if (!Directory.Exists(_path))
				Directory.CreateDirectory(_path);

			var fileName = Path.Combine(_path, _serializer.FileName);

			Version version;
			const int versionBytes = 2;
			long currOffset = versionBytes;

			if (File.Exists(fileName))
			{
				using (var stream = File.OpenRead(fileName))
				{
					version = new Version(stream.ReadByte(), stream.ReadByte());

					if (version.Major >= 2)
					{
						while (stream.Position < stream.Length)
						{
							var size = stream.Read<int>();

							var buffer = new byte[size];
							stream.ReadBytes(buffer, buffer.Length);

							var offset = stream.Position;

							var message = _serializer.Deserialize(version, buffer);
							var key = _serializer.GetKey(message);

							_snapshots.Add(key, message);

							_offsets.Add(key, offset);
							currOffset = stream.Position;
						}
					}
				}

				if (version.Major < 2)
				{
					File.Delete(fileName);
					version = _serializer.Version;
				}
			}
			else
			{
				version = _serializer.Version;
			}

			var isFlushing = false;

			ThreadingHelper.Timer(() =>
			{
				Tuple<long, byte[]>[] changed;

				lock (_snapshots.SyncRoot)
				{
					if (_dirtyKeys.Count == 0)
						return;

					if (isFlushing)
						return;

					isFlushing = true;

					changed = _dirtyKeys.Select(key =>
					{
						var buffer = _serializer.Serialize(version, _snapshots[key]);

						if (!_offsets.TryGetValue(key, out var offset))
						{
							_offsets.Add(key, currOffset);
							currOffset += buffer.Length;
						}

						return Tuple.Create(offset, buffer);
					}).OrderBy(t => t.Item1).ToArray();

					_dirtyKeys.Count = 0;
				}

				try
				{
					using (var stream = File.OpenWrite(fileName))
					{
						if (stream.Length == 0)
						{
							stream.WriteByte((byte)version.Major);
							stream.WriteByte((byte)version.Minor);
						}

						foreach (var tuple in changed)
						{
							stream.Seek(tuple.Item1, SeekOrigin.Begin);
							stream.Write(tuple.Item2);
						}
					}
				}
				catch (Exception ex)
				{
					ex.LogError();
				}

				lock (_snapshots.SyncRoot)
					isFlushing = false;

			}).Interval(TimeSpan.FromSeconds(10));
		}

		void ISnapshotStorage.ClearAll()
		{
			_snapshots.Clear();
		}

		void ISnapshotStorage<TKey>.Clear(TKey key)
		{
			_snapshots.Remove(key);
		}

		void ISnapshotStorage.Clear(object key)
		{
			((ISnapshotStorage<TKey>)this).Clear((TKey)key);
		}

		void ISnapshotStorage.Update(Message message)
		{
			if (message == null)
				throw new ArgumentNullException(nameof(message));

			var curr = (TMessage)message;

			var key = _serializer.GetKey(curr);

			lock (_snapshots.SyncRoot)
			{
				var prev = _snapshots.TryGetValue(key);

				if (prev == null)
				{
					_snapshots.Add(key, (TMessage)curr.Clone());
				}
				else
				{
					_serializer.Update(prev, curr);
				}

				_dirtyKeys.Add(key);
			}
		}

		Message ISnapshotStorage<TKey>.Get(TKey key)
		{
			return _snapshots.TryGetValue(key)?.Clone();
		}

		Message ISnapshotStorage.Get(object key)
		{
			return ((ISnapshotStorage<TKey>)this).Get((TKey)key);
		}
	}
}