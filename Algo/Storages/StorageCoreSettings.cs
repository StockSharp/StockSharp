namespace StockSharp.Algo.Storages
{
	using System;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	/// <summary>
	/// Storage modes.
	/// </summary>
	[Flags]
	public enum StorageModes
	{
		/// <summary>
		/// None.
		/// </summary>
		None = 1,

		/// <summary>
		/// Incremental.
		/// </summary>
		Incremental = None << 1,

		/// <summary>
		/// Snapshot.
		/// </summary>
		Snapshot = Incremental << 1,
	}

	/// <summary>
	/// Storage settings.
	/// </summary>
	public class StorageCoreSettings : IPersistable
	{
		/// <summary>
		/// The storage of market data.
		/// </summary>
		public IStorageRegistry StorageRegistry { get; set; }

		/// <summary>
		/// The storage (database, file etc.).
		/// </summary>
		public IMarketDataDrive Drive { get; set; }

		/// <summary>
		/// Format.
		/// </summary>
		public StorageFormats Format { get; set; }

		private TimeSpan _daysLoad;

		/// <summary>
		/// Max days to load stored data.
		/// </summary>
		public TimeSpan DaysLoad
		{
			get => _daysLoad;
			set
			{
				if (value < TimeSpan.Zero)
					throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.Str1219);

				_daysLoad = value;
			}
		}

		/// <summary>
		/// Storage mode.
		/// </summary>
		/// <remarks>By default is <see cref="StorageModes.Incremental"/>.</remarks>
		public StorageModes Mode { get; set; } = StorageModes.Incremental;

		/// <summary>
		/// To get the market-data storage.
		/// </summary>
		/// <typeparam name="TMessage">Message type.</typeparam>
		/// <param name="securityId">Security ID.</param>
		/// <param name="arg">The parameter associated with the <typeparamref name="TMessage" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <returns>Market-data storage.</returns>
		public IMarketDataStorage<TMessage> GetStorage<TMessage>(SecurityId securityId, object arg)
			where TMessage : Message
		{
			return (IMarketDataStorage<TMessage>)GetStorage(securityId, typeof(TMessage), arg);
		}

		/// <summary>
		/// To get the market-data storage.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="messageType"></param>
		/// <param name="arg">The parameter associated with the <paramref name="messageType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <returns>Market-data storage.</returns>
		public IMarketDataStorage GetStorage(SecurityId securityId, Type messageType, object arg)
		{
			return StorageRegistry.GetStorage(securityId, messageType, arg, Drive, Format);
		}

		/// <summary>
		/// Check the specified mode turned on.
		/// </summary>
		/// <param name="mode">Storage mode.</param>
		/// <returns>Check result.</returns>
		public bool IsMode(StorageModes mode) => Mode.Contains(mode);

		void IPersistable.Load(SettingsStorage storage)
		{
			Mode = storage.GetValue(nameof(Mode), Mode);
			Format = storage.GetValue(nameof(Format), Format);
			DaysLoad = storage.GetValue(nameof(DaysLoad), DaysLoad);
		}

		void IPersistable.Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Mode), Mode);
			storage.SetValue(nameof(Format), Format);
			storage.SetValue(nameof(DaysLoad), DaysLoad);
		}
	}
}