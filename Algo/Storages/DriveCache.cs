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

	using StockSharp.Algo;
	using StockSharp.Algo.History.Hydra;

	/// <summary>
	/// <see cref="IMarketDataDrive"/> cache.
	/// </summary>
	public class DriveCache : Disposable, IPersistable
	{
		private class PathComparer : IEqualityComparer<string>
		{
			bool IEqualityComparer<string>.Equals(string x, string y)
			{
				return x.ComparePaths(y);
			}

			int IEqualityComparer<string>.GetHashCode(string path)
			{
				return path.ToFullPath().TrimEnd('\\').ToLowerInvariant().GetHashCode();
			}
		}

		private readonly CachedSynchronizedDictionary<string, IMarketDataDrive> _drives = new CachedSynchronizedDictionary<string, IMarketDataDrive>(new PathComparer());

		/// <summary>
		/// Initializes a new instance of the <see cref="DriveCache"/>.
		/// </summary>
		/// <param name="defaultDrive">The storage used by default.</param>
		public DriveCache(IMarketDataDrive defaultDrive)
		{
			DefaultDrive = defaultDrive ?? throw new ArgumentNullException(nameof(defaultDrive));
			_drives.Add(DefaultDrive.Path, DefaultDrive);
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

		/// <summary>
		/// To get the storage for <paramref name="path"/>.
		/// </summary>
		/// <param name="path">Path.</param>
		/// <returns>Market data storage.</returns>
		public IMarketDataDrive GetDrive(string path)
		{
			if (path.IsEmpty() && Guid.TryParse(path, out _))
				return DefaultDrive;

			return _drives.SafeAdd(path ?? string.Empty, key =>
			{
				IMarketDataDrive drive;

				try
				{
					var addr = path.To<EndPoint>();
					drive = new RemoteMarketDataDrive(new RemoteStorageClient(ServicesRegistry.ExchangeInfoProvider, new Uri(addr.To<string>())));
				}
				catch
				{
					drive = new LocalMarketDataDrive(path);
				}

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

			if (_drives.Remove(drive.Path))
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
					_drives.TryAdd(drive.Path, drive);	
			}

			if (storage.ContainsKey(nameof(DefaultDrive)))
				DefaultDrive = _drives[storage.GetValue<string>(nameof(DefaultDrive))];
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