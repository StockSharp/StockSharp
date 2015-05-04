namespace StockSharp.Studio.Configuration
{
	using System.Configuration;

	using StockSharp.Logging;

	class ConnectionElement : ConfigurationElement
	{
		private const string _adapterKey = "adapter";

		[ConfigurationProperty(_adapterKey, IsRequired = true, IsKey = true)]
		public string Adapter
		{
			get { return (string)this[_adapterKey]; }
			set { this[_adapterKey] = value; }
		}

		private const string _logLevelKey = "logLevel";

		[ConfigurationProperty(_logLevelKey, DefaultValue = LogLevels.Inherit)]
		public LogLevels LogLevel
		{
			get { return (LogLevels)this[_logLevelKey]; }
			set { this[_logLevelKey] = value; }
		}
	}
}