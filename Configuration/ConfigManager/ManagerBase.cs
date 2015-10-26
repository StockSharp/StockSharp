using System.Windows;
using Ecng.Serialization;

namespace StockSharp.Configuration.ConfigManager
{
	/// <summary>
	/// Base manager class.  Enables persistence.
	/// </summary>
	public abstract class ManagerBase : Window, IPersistable
	{
        //TODO: convert serialization in Layout_Persist to SettingsStorage
		public virtual void Load(SettingsStorage storage)
		{
		}

		public virtual void Save(SettingsStorage storage)
		{
		}
	}
}