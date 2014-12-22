namespace StockSharp.Studio.Configuration
{
	using System.Configuration;

	using StockSharp.Logging;

	class ConnectionElement : ConfigurationElement
	{
		private const string _sessionHolderKey = "sessionHolder";

		[ConfigurationProperty(_sessionHolderKey, IsRequired = true, IsKey = true)]
		public string SessionHolder
		{
			get { return (string)this[_sessionHolderKey]; }
			set { this[_sessionHolderKey] = value; }
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