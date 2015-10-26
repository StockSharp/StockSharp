using System.Configuration;

namespace StockSharp.Configuration.ConfigConnection
{
    /// <summary>
	/// Represents the custom message adapter.
	/// </summary>
	public class ConnectionElement : ConfigurationElement
	{
		private const string _typeKey = "type";

		/// <summary>
		/// Custom message adapter.
		/// </summary>
		[ConfigurationProperty(_typeKey)]
		public string Type
		{
			get { return (string)this[_typeKey]; }
			set { this[_typeKey] = value; }
		}
	}
}