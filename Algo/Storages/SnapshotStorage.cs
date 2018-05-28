namespace StockSharp.Algo.Storages
{
	using System;
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
		private readonly SynchronizedDictionary<TKey, Tuple<long, TMessage>> _snapshots = new SynchronizedDictionary<TKey, Tuple<long, TMessage>>();
		private long _maxOffset;
		private readonly ISnapshotSerializer<TKey, TMessage> _serializer;

		private int _snapshotSize;

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

			byte[] buffer;

			Version version;

			if (File.Exists(fileName))
			{
				using (var stream = File.OpenRead(fileName))
				{
					version = new Version(stream.ReadByte(), stream.ReadByte());

					buffer = new byte[_serializer.GetSnapshotSize(version)];

					while (stream.Position < stream.Length)
					{
						stream.ReadBytes(buffer, buffer.Length);

						var message = _serializer.Deserialize(version, buffer);

						_snapshots.Add(_serializer.GetKey(message), Tuple.Create(stream.Position - buffer.Length, message));
					}

					_maxOffset = stream.Position;
				}
			}
			else
			{
				version = _serializer.Version;
				_maxOffset = 2; // version bytes

				buffer = new byte[_serializer.GetSnapshotSize(version)];
			}

			_snapshotSize = buffer.Length;

			var isFlushing = false;

			ThreadingHelper.Timer(() =>
			{
				Tuple<long, TMessage>[] changed;

				lock (_snapshots.SyncRoot)
				{
					if (_dirtyKeys.Count == 0)
						return;

					if (isFlushing)
						return;

					isFlushing = true;

					changed = _dirtyKeys.Select(key =>
					{
						var tuple = _snapshots[key];
						return Tuple.Create(tuple.Item1, (TMessage)tuple.Item2.Clone());
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

							Array.Clear(buffer, 0, buffer.Length);
							_serializer.Serialize(version, tuple.Item2, buffer);

							stream.Write(buffer, 0, buffer.Length);
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
				var tuple = _snapshots.TryGetValue(key);

				if (tuple == null)
				{
					_snapshots.Add(key, Tuple.Create(_maxOffset, (TMessage)curr.Clone()));
					_maxOffset += _snapshotSize;
				}
				else
				{
					_serializer.Update(tuple.Item2, curr);
				}

				_dirtyKeys.Add(key);
			}
		}

		Message ISnapshotStorage<TKey>.Get(TKey key)
		{
			return _snapshots.TryGetValue(key)?.Item2.Clone();
		}

		Message ISnapshotStorage.Get(object key)
		{
			return ((ISnapshotStorage<TKey>)this).Get((TKey)key);
		}
	}
}