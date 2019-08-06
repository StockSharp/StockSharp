namespace StockSharp.Algo.Server
{
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	/// <summary>
	/// This class contains UniServer host properties.
	/// </summary>
	public class HostInfo : NotifiableObject, IPersistable
	{
		/// <summary>
		/// Host id running controller module.
		/// </summary>
		public const string ControllerHostId = "ctl_host";

		private string _hostId;
		private string _address;

		/// <summary>
		/// Unique host identifier.
		/// </summary>
		public string HostId
		{
			get => _hostId;
			set
			{
				_hostId = value;
				NotifyChanged(nameof(HostId));
			}
		}

		/// <summary>
		/// Host address.
		/// </summary>
		public string Address
		{
			get => _address;
			set
			{
				_address = value;
				NotifyChanged(nameof(Address));
			}
		}

		/// <inheritdoc />
		public void Load(SettingsStorage storage)
		{
			HostId = storage.GetValue<string>(nameof(HostId));
			Address = storage.GetValue<string>(nameof(Address));
		}

		/// <inheritdoc />
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(HostId), HostId);
			storage.SetValue(nameof(Address), Address);
		}
	}
}
