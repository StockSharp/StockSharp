namespace StockSharp.Hydra.Core
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Linq;
	using System.Net;

	using Ecng.Collections;
	using Ecng.Common;
	using Ecng.Configuration;
	using Ecng.Serialization;

	using MoreLinq;

	using StockSharp.Algo.History.Hydra;
	using StockSharp.Algo.Storages;

	/// <summary>
	/// Кэш <see cref="IMarketDataDrive"/>.
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

		private DriveCache()
		{
			var svc = ConfigManager.TryGetService<IStorageRegistry>();

			if (svc == null)
				return;

			DefaultDrive = svc.DefaultDrive;
			_drives.Add(DefaultDrive.Path, DefaultDrive);
		}

		private static readonly Lazy<DriveCache> _instance = new Lazy<DriveCache>(() => new DriveCache());

		/// <summary>
		/// Кэш.
		/// </summary>
		public static DriveCache Instance
		{
			get { return _instance.Value; }
		}

		/// <summary>
		/// Список всех хранилищ маркет-данных.
		/// </summary>
		public IEnumerable<IMarketDataDrive> AllDrives
		{
			get { return _drives.CachedValues; }
		}

		/// <summary>
		/// Хранилище маркет-данных, используемое по-умолчанию.
		/// </summary>
		public IMarketDataDrive DefaultDrive { get; private set; }

		/// <summary>
		/// Событие создания нового хранилища.
		/// </summary>
		public event Action<IMarketDataDrive> NewDriveCreated;

		/// <summary>
		/// Получить хранилище маркет-данных.
		/// </summary>
		/// <param name="path">Путь к данным.</param>
		/// <returns>Хранилище маркет-данных.</returns>
		public IMarketDataDrive GetDrive(string path)
		{
			if (path.IsEmpty())
				return DefaultDrive;

			return _drives.SafeAdd(path ?? string.Empty, key =>
			{
				IMarketDataDrive drive;

				try
				{
					var addr = path.To<EndPoint>();
					drive = new RemoteMarketDataDrive(new RemoteStorageClient(new Uri(addr.To<string>())));
				}
				catch
				{
					drive = new LocalMarketDataDrive(path);
				}

				NewDriveCreated.SafeInvoke(drive);

				return drive;
			});
		}

		/// <summary>
		/// Загрузить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Load(SettingsStorage storage)
		{
			var drives = storage
				.GetValue<IEnumerable<SettingsStorage>>("Drives")
				.Select(s => s.LoadEntire<IMarketDataDrive>())
				.ToArray();

			lock (_drives.SyncRoot)
			{
				foreach (var drive in drives)
					_drives.TryAdd(drive.Path, drive);	
			}

			if (storage.ContainsKey("DefaultDrive"))
				DefaultDrive = _drives[storage.GetValue<string>("DefaultDrive")];
		}

		/// <summary>
		/// Сохранить настройки.
		/// </summary>
		/// <param name="storage">Хранилище настроек.</param>
		public void Save(SettingsStorage storage)
		{
			storage.SetValue("Drives", AllDrives.Select(s => s.SaveEntire(false)).ToArray());

			if (DefaultDrive != null)
				storage.SetValue("DefaultDrive", DefaultDrive.Path);
		}

		/// <summary>
		/// Освободить занятые ресурсы.
		/// </summary>
		protected override void DisposeManaged()
		{
			AllDrives.ForEach(d => d.Dispose());
			base.DisposeManaged();
		}
	}
}