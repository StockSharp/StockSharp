using System.Linq;

namespace StockSharp.Algo.Server
{
	using System.Collections.Generic;
	using Ecng.Serialization;

	/// <summary>
	/// UniServer parameters.
	/// </summary>
	public class UniServerConfig : IPersistable
	{
		/// <summary>
		/// All existing modules in the system.
		/// </summary>
		public List<ModuleInfo> Modules { get; } = new List<ModuleInfo>();

		/// <summary>
		/// All hosts in the system.
		/// </summary>
		public List<HostInfo> Hosts { get; } = new List<HostInfo>();

		/// <summary>
		/// Module instances configured to run on specific hosts.
		/// </summary>
		public List<ModuleInstanceConfig> ModuleInstances { get; } = new List<ModuleInstanceConfig>();

		/// <summary>
		/// Get host info by module instance id.
		/// </summary>
		/// <param name="moduleInstanceId"></param>
		/// <returns></returns>
		public HostInfo GetHostInfo(string moduleInstanceId)
		{
			var instCfg = ModuleInstances.FirstOrDefault(mic => mic.ModuleInstanceId == moduleInstanceId);
			return instCfg == null ? null : Hosts.FirstOrDefault(hi => hi.HostId == instCfg.HostId);
		}

		/// <inheritdoc />
		public void Load(SettingsStorage storage)
		{
			Modules.Clear();
			Hosts.Clear();
			ModuleInstances.Clear();

			Modules.AddRange(storage.GetValue<ModuleInfo[]>(nameof(Modules)));
			Hosts.AddRange(storage.GetValue<HostInfo[]>(nameof(Hosts)));
			ModuleInstances.AddRange(storage.GetValue<ModuleInstanceConfig[]>(nameof(ModuleInstances)));
		}

		/// <inheritdoc />
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(Modules), Modules.ToArray());
			storage.SetValue(nameof(Hosts), Hosts.ToArray());
			storage.SetValue(nameof(ModuleInstances), ModuleInstances.ToArray());
		}
	}
}
