using System;

namespace StockSharp.Configuration.ConfigManager
{
    public class OutputManager
    {
        private readonly ConfigurationManager _configurationManager;

        public OutputManager(ConfigurationManager configurationManager)
        {
            if (configurationManager == null) throw new ArgumentNullException(nameof(configurationManager));
            _configurationManager = configurationManager;
        }
    }
}