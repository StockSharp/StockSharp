namespace StockSharp.Algo.Server
{
	using System.Collections.Generic;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	/// <summary>
	/// Configuration of a module instance.
	/// </summary>
	/// <remarks>Each module can be used in multiple instances/configurations simultaneously.</remarks>
	public class ModuleInstanceConfig : NotifiableObject, IPersistable
	{
		private string _moduleInstanceId;
		private string _moduleId;
		private string _hostId;
		private string _internalModuleConfigSerialized;

		/// <summary>
		/// Module instance id. Several instances of the same module can be running in the system simultaneously.
		/// </summary>
		public string ModuleInstanceId
		{
			get => _moduleInstanceId;
			set
			{
				_moduleInstanceId = value;
				NotifyChanged(nameof(ModuleInstanceId));
			}
		}

		/// <summary>
		/// Module id.
		/// </summary>
		public string ModuleId
		{
			get => _moduleId;
			set
			{
				_moduleId = value;
				NotifyChanged(nameof(ModuleId));
			}
		}

		/// <summary>
		/// Host id.
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
		/// Configuration, specific to the module. Serialized to string.
		/// </summary>
		public string InternalModuleConfigSerialized
		{
			get => _internalModuleConfigSerialized;
			set
			{
				_internalModuleConfigSerialized = value;
				NotifyChanged(nameof(InternalModuleConfigSerialized));
			}
		}

		/// <summary>
		/// Ids of modules this module sends messages to.
		/// </summary>
		public List<string> NextModulesInstanceIds { get; private set; } = new List<string>();

		/// <inheritdoc />
		public void Load(SettingsStorage storage)
		{
			NextModulesInstanceIds.Clear();

			ModuleInstanceId = storage.GetValue<string>(nameof(ModuleInstanceId));
			ModuleId = storage.GetValue<string>(nameof(ModuleId));
			HostId = storage.GetValue<string>(nameof(HostId));
			InternalModuleConfigSerialized = storage.GetValue<string>(nameof(InternalModuleConfigSerialized));
			NextModulesInstanceIds.AddRange(storage.GetValue<string[]>(nameof(NextModulesInstanceIds)));
		}

		/// <inheritdoc />
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(ModuleInstanceId), ModuleInstanceId);
			storage.SetValue(nameof(ModuleId), ModuleId);
			storage.SetValue(nameof(HostId), HostId);
			storage.SetValue(nameof(InternalModuleConfigSerialized), InternalModuleConfigSerialized);
			storage.SetValue(nameof(NextModulesInstanceIds), NextModulesInstanceIds.ToArray());
		}
	}
}
