namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.History.Hydra;
	using StockSharp.Messages;

	/// <summary>
	/// Remote storage of market data working via <see cref="RemoteStorageClient"/>.
	/// </summary>
	public class RemoteMarketDataDrive : BaseMarketDataDrive
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteMarketDataDrive"/>.
		/// </summary>
		/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
		public RemoteMarketDataDrive(IExchangeInfoProvider exchangeInfoProvider)
			: this(new RemoteStorageClient(exchangeInfoProvider))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteMarketDataDrive"/>.
		/// </summary>
		/// <param name="client">The client for access to the history server <see cref="IRemoteStorage"/>.</param>
		public RemoteMarketDataDrive(RemoteStorageClient client)
		{
			Client = client;
			Client.Drive = this;
		}

		private RemoteStorageClient _client;

		/// <summary>
		/// The client for access to the history server <see cref="IRemoteStorage"/>.
		/// </summary>
		public RemoteStorageClient Client
		{
			get => _client;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				if (value == _client)
					return;

				_client?.Dispose();
				_client = value;
			}
		}

		/// <summary>
		/// Path to market data.
		/// </summary>
		public override string Path
		{
			get => Client.Address.ToString();
			set => Client = new RemoteStorageClient(_client.ExchangeInfoProvider, value.To<Uri>());
		}

		/// <summary>
		/// Get all available instruments.
		/// </summary>
		public override IEnumerable<SecurityId> AvailableSecurities => Client.AvailableSecurities;

		/// <summary>
		/// Get all available data types.
		/// </summary>
		/// <param name="securityId">Instrument identifier.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Data types.</returns>
		public override IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
		{
			return Client.GetAvailableDataTypes(securityId, format);
		}

		/// <summary>
		/// Create storage for <see cref="IMarketDataStorage"/>.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="dataType">Market data type.</param>
		/// <param name="arg">The parameter associated with the <paramref name="dataType" /> type. For example, <see cref="CandleMessage.Arg"/>.</param>
		/// <param name="format">Format type.</param>
		/// <returns>Storage for <see cref="IMarketDataStorage"/>.</returns>
		public override IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format)
		{
			return Client.GetRemoteStorage(securityId, dataType, arg, format);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Client.Credentials.Load(storage.GetValue<SettingsStorage>(nameof(Client.Credentials)));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Client.Credentials), Client.Credentials.Save());
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_client.Dispose();
			base.DisposeManaged();
		}
	}
}