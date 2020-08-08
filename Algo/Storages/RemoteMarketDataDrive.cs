namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.IO;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.ComponentModel;
	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Algo.Storages.Remote;

	/// <summary>
	/// Remote storage of market data working via <see cref="RemoteStorageClient"/>.
	/// </summary>
	public class RemoteMarketDataDrive : BaseMarketDataDrive
	{
		private sealed class RemoteStorageDrive : IMarketDataStorageDrive
		{
			private readonly RemoteMarketDataDrive _parent;
			private readonly SecurityId _securityId;
			private readonly DataType _dataType;
			private readonly StorageFormats _format;

			public RemoteStorageDrive(RemoteMarketDataDrive parent, SecurityId securityId, DataType dataType, StorageFormats format)
			{
				if (securityId.IsDefault())
					throw new ArgumentNullException(nameof(securityId));

				if (dataType == null)
					throw new ArgumentNullException(nameof(dataType));

				// TODO
				//if (drive == null)
				//	throw new ArgumentNullException(nameof(drive));

				_parent = parent ?? throw new ArgumentNullException(nameof(parent));
				_securityId = securityId;
				_dataType = dataType ?? throw new ArgumentNullException(nameof(dataType));
				_format = format;
			}

			IMarketDataDrive IMarketDataStorageDrive.Drive => _parent;

			private IEnumerable<DateTime> _dates;
			private DateTime _prevDatesSync;

			IEnumerable<DateTime> IMarketDataStorageDrive.Dates
			{
				get
				{
					if (_prevDatesSync.IsDefault() || (DateTime.Now - _prevDatesSync).TotalSeconds > 3)
					{
						_dates = _parent.CreateClient().GetDates(_securityId, _dataType, _format);

						_prevDatesSync = DateTime.Now;
					}

					return _dates;
				}
			}

			void IMarketDataStorageDrive.ClearDatesCache()
			{
				//_parent.Invoke(f => f.ClearDatesCache(_parent.SessionId, _security.Id, _dataType, _arg));
			}

			void IMarketDataStorageDrive.Delete(DateTime date)
				=> _parent.CreateClient().Delete(_securityId, _dataType, _format, date);

			void IMarketDataStorageDrive.SaveStream(DateTime date, Stream stream)
				=> _parent.CreateClient().SaveStream(_securityId, _dataType, _format, date, stream);

			Stream IMarketDataStorageDrive.LoadStream(DateTime date)
				=> _parent.CreateClient().LoadStream(_securityId, _dataType, _format, date);
		}

		private readonly SynchronizedDictionary<Tuple<SecurityId, DataType, StorageFormats>, RemoteStorageDrive> _remoteStorages = new SynchronizedDictionary<Tuple<SecurityId, DataType, StorageFormats>, RemoteStorageDrive>();
		private readonly Func<IMessageAdapter> _createAdapter;

		/// <summary>
		/// Default address.
		/// </summary>
		public static readonly EndPoint DefaultAddress = "127.0.0.1:5002".To<EndPoint>();

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteMarketDataDrive"/>.
		/// </summary>
		public RemoteMarketDataDrive()
			: this(DefaultAddress)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteMarketDataDrive"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		public RemoteMarketDataDrive(EndPoint address)
			: this(address, () => ServicesRegistry.AdapterProvider.CreateTransportAdapter(new IncrementalIdGenerator()))
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteMarketDataDrive"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		/// <param name="adapter">Message adapter.</param>
		public RemoteMarketDataDrive(EndPoint address, IMessageAdapter adapter)
			: this(address, adapter.TypedClone)
		{
			if (adapter is null)
				throw new ArgumentNullException(nameof(adapter));
		}

		private RemoteMarketDataDrive(EndPoint address, Func<IMessageAdapter> createAdapter)
		{
			Address = address;
			_createAdapter = createAdapter;
		}

		/// <summary>
		/// Information about the login and password for access to remote storage.
		/// </summary>
		public ServerCredentials Credentials { get; } = new ServerCredentials();

		private EndPoint _address = DefaultAddress;

		/// <summary>
		/// Server address.
		/// </summary>
		public EndPoint Address
		{
			get => _address;
			set => _address = value ?? throw new ArgumentNullException(nameof(value));
		}

		private string _targetCompId = "StockSharpHydraMD";

		/// <summary>
		/// Target ID.
		/// </summary>
		public string TargetCompId
		{
			get => _targetCompId;
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				_targetCompId = value;
			}
		}

		/// <inheritdoc />
		public override string Path
		{
			get => Address.To<string>();
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException(nameof(value));

				if (value.StartsWithIgnoreCase("net.tcp://"))
				{
					var uri = value.To<Uri>();
					value = $"{uri.Host}:{uri.Port}";
				}

				Address = value.To<EndPoint>();
			}
		}

		private RemoteStorageClient CreateClient()
		{
			var adapter = _createAdapter();

			((IAddressAdapter<EndPoint>)adapter).Address = Address;
			((ILoginPasswordAdapter)adapter).Password = Credentials.Password;

			var login = Credentials.Email;
			if (login.IsEmpty())
				login = "stocksharp";
			((ISenderTargetAdapter)adapter).SenderCompId = login;
			((ISenderTargetAdapter)adapter).TargetCompId = TargetCompId;

			return new RemoteStorageClient(adapter);
		}

		/// <inheritdoc />
		public override IEnumerable<SecurityId> AvailableSecurities
			=> CreateClient().AvailableSecurities;

		/// <inheritdoc />
		public override IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
			=> CreateClient().GetAvailableDataTypes(securityId, format);

		/// <inheritdoc />
		public override IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, DataType dataType, StorageFormats format)
		{
			if (dataType is null)
				throw new ArgumentNullException(nameof(dataType));

			return _remoteStorages.SafeAdd(Tuple.Create(securityId, dataType, format),
				key => new RemoteStorageDrive(this, securityId, dataType, format));
		}

		/// <inheritdoc />
		public override void Verify() => CreateClient().Verify();

		/// <inheritdoc />
		public override void LookupSecurities(SecurityLookupMessage criteria, ISecurityProvider securityProvider, Action<SecurityMessage> newSecurity, Func<bool> isCancelled, Action<int, int> updateProgress)
			=> CreateClient().LookupSecurities(criteria, securityProvider, newSecurity, isCancelled, updateProgress);

		/// <inheritdoc />
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Credentials.Load(storage.GetValue<SettingsStorage>(nameof(Credentials)));
		}

		/// <inheritdoc />
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Credentials), Credentials.Save());
		}
	}
}