using System;

namespace StockSharp.Configuration.ConfigManager
{
    public class OutputManager : ManagerBase
    {
        private readonly ConfigurationManager _configurationManager;

        public OutputManager(ConfigurationManager configurationManager)
        {
            if (configurationManager == null) throw new ArgumentNullException("configurationManager");
            _configurationManager = configurationManager;
        }
    }
}