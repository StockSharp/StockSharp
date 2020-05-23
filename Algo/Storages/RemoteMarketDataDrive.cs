namespace StockSharp.Algo.Storages
{
	using System;
	using System.Collections.Generic;
	using System.Net;
	using System.IO;
	using System.Linq;

	using Ecng.Common;
	using Ecng.Serialization;
	using Ecng.ComponentModel;
	using Ecng.Collections;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;
	using StockSharp.Algo.Storages.Remote;
	using StockSharp.Algo.Storages.Remote.Messages;
	using Ecng.Configuration;

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
						_dates = _parent.CreateClient().Do<AvailableDataRequestMessage, AvailableDataInfoMessage>(new AvailableDataRequestMessage
						{
							SecurityId = _securityId,
							RequestDataType = _dataType,
							Format = _format,
						}).Select(i => i.Date.UtcDateTime).ToArray();

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
			{
				_parent.CreateClient().Do(new RemoteFileCommandMessage
				{
					Command = CommandTypes.Remove,
					Scope = CommandScopes.File,
					SecurityId = _securityId,
					FileDataType = _dataType,
					Format = _format,
					StartDate = date,
					EndDate = date,
				});
			}

			void IMarketDataStorageDrive.SaveStream(DateTime date, Stream stream)
			{
				_parent.CreateClient().Do(new RemoteFileCommandMessage
				{
					Command = CommandTypes.Update,
					Scope = CommandScopes.File,
					SecurityId = _securityId,
					FileDataType = _dataType,
					StartDate = date,
					EndDate = date,
					Format = _format,
					Body = stream.To<byte[]>(),
				});
			}

			Stream IMarketDataStorageDrive.LoadStream(DateTime date)
			{
				return _parent.CreateClient().Do<RemoteFileCommandMessage, RemoteFileMessage>(new RemoteFileCommandMessage
				{
					Command = CommandTypes.Get,
					Scope = CommandScopes.File,
					SecurityId = _securityId,
					FileDataType = _dataType,
					StartDate = date,
					EndDate = date,
					Format = _format,
				}).FirstOrDefault()?.Body.To<Stream>() ?? Stream.Null;
			}
		}

		private readonly SynchronizedDictionary<Tuple<SecurityId, DataType, StorageFormats>, RemoteStorageDrive> _remoteStorages = new SynchronizedDictionary<Tuple<SecurityId, DataType, StorageFormats>, RemoteStorageDrive>();
		private readonly IMessageAdapter _adapter;

		/// <summary>
		/// Default address.
		/// </summary>
		public static readonly EndPoint DefaultAddress = "localhost:8000".To<EndPoint>();

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
			: this(null, address)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="RemoteMarketDataDrive"/>.
		/// </summary>
		/// <param name="address">Server address.</param>
		/// <param name="adapter">Message adapter.</param>
		public RemoteMarketDataDrive(IMessageAdapter adapter, EndPoint address)
		{
			_adapter = adapter;
			Address = address;
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

		/// <inheritdoc />
		public override string Path
		{
			get => Address.ToString();
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
			var adapter = _adapter?.TypedClone() ?? ConfigManager.GetService<IMessageAdapterProvider>().CreateTransportAdapter(new IncrementalIdGenerator());
			
			((IAddressAdapter<EndPoint>)adapter).Address = Address;
			((ILoginPasswordAdapter)adapter).Login = Credentials.Email;
			((ILoginPasswordAdapter)adapter).Password = Credentials.Password;

			return new RemoteStorageClient(adapter);
		}

		/// <inheritdoc />
		public override IEnumerable<SecurityId> AvailableSecurities
			=> CreateClient().AvailableSecurities;

		/// <inheritdoc />
		public override IEnumerable<DataType> GetAvailableDataTypes(SecurityId securityId, StorageFormats format)
			=> CreateClient().GetAvailableDataTypes(securityId, format);

		/// <inheritdoc />
		public override IMarketDataStorageDrive GetStorageDrive(SecurityId securityId, Type dataType, object arg, StorageFormats format)
		{
			var dt = DataType.Create(dataType, arg);

			return _remoteStorages.SafeAdd(Tuple.Create(securityId, dt, format),
				key => new RemoteStorageDrive(this, securityId, dt, format));
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