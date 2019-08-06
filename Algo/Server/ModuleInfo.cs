namespace StockSharp.Algo.Server
{
	using System;
	using Ecng.Serialization;
	using Ecng.ComponentModel;

	/// <summary>
	/// This class contains meta data for a module.
	/// </summary>
	public class ModuleInfo : NotifiableObject, IPersistable
	{
		/// <summary>
		/// Module id of controller.
		/// </summary>
		public const string ControllerModuleId = "controller";

		private string _moduleId;
		private string _assemblyQualifiedTypeName;
		private string _name;

		/// <summary>
		/// Unique module identifier.
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
		/// Module type id (<see cref="Type.AssemblyQualifiedName"/>).
		/// </summary>
		public string AssemblyQualifiedTypeName
		{
			get => _assemblyQualifiedTypeName;
			set
			{
				_assemblyQualifiedTypeName = value;
				NotifyChanged(nameof(AssemblyQualifiedTypeName));
			}
		}

		/// <summary>
		/// Human readable module name.
		/// </summary>
		public string Name
		{
			get => _name;
			set
			{
				_name = value;
				NotifyChanged(nameof(AssemblyQualifiedTypeName));
			}
		}

		/// <inheritdoc />
		public void Load(SettingsStorage storage)
		{
			ModuleId = storage.GetValue<string>(nameof(ModuleId));
			AssemblyQualifiedTypeName = storage.GetValue<string>(nameof(AssemblyQualifiedTypeName));
			Name = storage.GetValue<string>(nameof(Name));
		}

		/// <inheritdoc />
		public void Save(SettingsStorage storage)
		{
			storage.SetValue(nameof(ModuleId), ModuleId);
			storage.SetValue(nameof(AssemblyQualifiedTypeName), AssemblyQualifiedTypeName);
			storage.SetValue(nameof(Name), Name);
		}
	}
}
