namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.Algo.Storages.Remote;
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Remote storage of market data working via <see cref="RemoteStorageClient"/>.
	/// </summary>
	public class RemoteMarketDataDrive : BaseMarketDataDrive
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteMarketDataDrive"/>.
		/// </summary>
		public RemoteMarketDataDrive()
			: this(new RemoteStorageClient())
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

		/// <inheritdoc />
		public override string Path
		{
			get => Client.Address.ToString();
			set => Client = new RemoteStorageClient(value.To<Uri>());
		}

		/// <inheritdoc />
		public override IEnumerable<SecurityId> AvailableSecurities => Client.AvailableSecurities;

		/// <inheritdoc />
		public override IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
		{
			return Client.GetAvailableDataTypes(securityId, format);
		}

		/// <inheritdoc />
		public override IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format)
		{
			return Client.GetRemoteStorage(securityId, dataType, arg, format);
		}

		/// <inheritdoc />
		public override void Verify()
		{
			Client.Login();
		}

		/// <inheritdoc />
		public override void LookupSecurities(SecurityLookupMessage criteria, ISecurityProvider securityProvider, Action<SecurityMessage> newSecurity, Func<bool> isCancelled, Action<int, int> updateProgress)
		{
			using (var client = new RemoteStorageClient(new Uri(Path)))
			{
				client.Credentials.Load(Client.Credentials.Save());

				client.LookupSecurities(criteria, securityProvider, newSecurity, isCancelled, updateProgress);
			}
		}

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Client.Credentials.Load(storage.GetValue<SettingsStorage>(nameof(Client.Credentials)));
		}

		/// <inheritdoc />
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