using System;

#pragma warning disable 1591

namespace StockSharp.Configuration.ConfigManager
{
    public class SettingsManager : ManagerBase
    {
        private readonly ConfigurationManager _configurationManager;

        public SettingsManager(ConfigurationManager configurationManager)
        {
            if (configurationManager == null) throw new ArgumentNullException("configurationManager");
            _configurationManager = configurationManager;
        }
    }
}