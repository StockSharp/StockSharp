namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Net;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo.Storages.Remote;
	using StockSharp.Localization;

	using PathPair = System.Tuple<string, System.Net.EndPoint>;

	/// <summary>
	/// <see cref="IMarketDataDrive"/> cache.
	/// </summary>
	public class DriveCache : Disposable, IPersistable
	{
		private class PathComparer : IEqualityComparer<PathPair>
		{
			bool IEqualityComparer<PathPair>.Equals(PathPair x, PathPair y)
			{
				if (x == null && y == null)
					return true;

				if (x == null || y == null)
					return false;

				if (Equals(x.Item2, y.Item2))
				{
					if (x.Item2 is null)
						return x.Item1.ComparePaths(y.Item1);
					else
						return true;
				}
				else
					return false;
			}

			int IEqualityComparer<PathPair>.GetHashCode(PathPair obj)
			{
				return obj.Item2?.GetHashCode() ?? obj.Item1.ToFullPath().TrimEnd('\\').ToLowerInvariant().GetHashCode();
			}
		}

		private readonly CachedSynchronizedDictionary<PathPair, IMarketDataDrive> _drives = new CachedSynchronizedDictionary<PathPair, IMarketDataDrive>(new PathComparer());

		/// <summary>
		/// Initializes a new instance of the <see cref="DriveCache"/>.
		/// </summary>
		/// <param name="defaultDrive">The storage used by default.</param>
		public DriveCache(IMarketDataDrive defaultDrive)
		{
			DefaultDrive = defaultDrive ?? throw new ArgumentNullException(nameof(defaultDrive));
			_drives.Add(CreatePair(DefaultDrive.Path), DefaultDrive);
		}

		/// <summary>
		/// Available storages.
		/// </summary>
		public IEnumerable<IMarketDataDrive> Drives => _drives.CachedValues;

		/// <summary>
		/// The storage used by default.
		/// </summary>
		public IMarketDataDrive DefaultDrive { get; private set; }

		/// <summary>
		/// New storage created event.
		/// </summary>
		public event Action<IMarketDataDrive> NewDriveCreated;

		/// <summary>
		/// Storage removed event.
		/// </summary>
		public event Action<IMarketDataDrive> DriveDeleted;

		/// <summary>
		/// Cache changed event.
		/// </summary>
		public event Action Changed;

		private static PathPair CreatePair(string path)
		{
			if (path.IsEmpty())
				throw new ArgumentNullException(nameof(path));

			if (path.Length < 3)
				throw new ArgumentOutOfRangeException(nameof(path), path, LocalizedStrings.Str3014);

			EndPoint addr = null;

			if (!(path[0] >= 'A' && path[1] <= 'z' && path[1] == ':' && path[2] == '\\'))
			{
				try
				{
					addr = path.To<EndPoint>();
				}
				catch
				{
				}
			}

			return new PathPair(path, addr);
		}

		/// <summary>
		/// To get the storage for <paramref name="path"/>.
		/// </summary>
		/// <param name="path">Path.</param>
		/// <returns>Market data storage.</returns>
		public IMarketDataDrive GetDrive(string path)
		{
			if (path.IsEmpty() || Guid.TryParse(path, out _)/* TODO remove few versions later 2019-08-06 */)
				return DefaultDrive;

			var pair = CreatePair(path);

			return _drives.SafeAdd(pair, key =>
			{
				IMarketDataDrive drive;

				if (pair.Item2 == null)
					drive = new LocalMarketDataDrive(path);
				else
					drive = new RemoteMarketDataDrive(pair.Item2);

				NewDriveCreated?.Invoke(drive);

				return drive;
			});
		}

		/// <summary>
		/// Delete storage.
		/// </summary>
		/// <param name="drive">Market data storage.</param>
		public void DeleteDrive(IMarketDataDrive drive)
		{
			if (drive == null)
				throw new ArgumentNullException(nameof(drive));

			if (drive == DefaultDrive)
				throw new ArgumentException(nameof(drive));

			if (_drives.Remove(CreatePair(drive.Path)))
				DriveDeleted?.Invoke(drive);
		}

		/// <summary>
		/// Invoke <see cref="Changed"/> event.
		/// </summary>
		public void Update()
		{
			Changed?.Invoke();
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Load(SettingsStorage storage)
		{
			var drives = storage
				.GetValue<IEnumerable<SettingsStorage>>(nameof(Drives))
				.Select(s => s.LoadEntire<IMarketDataDrive>())
				.ToArray();

			lock (_drives.SyncRoot)
			{
				foreach (var drive in drives)
					_drives.TryAdd(CreatePair(drive.Path), drive);	
			}

			if (storage.ContainsKey(nameof(DefaultDrive)))
			{
				var pair = CreatePair(storage.GetValue<string>(nameof(DefaultDrive)));

				if (_drives.TryGetValue(pair, out var drive))
					DefaultDrive = drive;
			}
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Drives), Drives.Select(s => s.SaveEntire(false)).ToArray());

			if (DefaultDrive != null)
				storage.SetValue(nameof(DefaultDrive), DefaultDrive.Path);
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			Drives.ForEach(d => d.Dispose());
			base.DisposeManaged();
		}
	}
}