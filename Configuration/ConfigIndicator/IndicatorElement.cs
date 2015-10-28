using System.Configuration;

namespace StockSharp.Configuration.ConfigIndicator
{
    /// <summary>
	/// Represents the custom indicator.
	/// </summary>
	public class IndicatorElement : ConfigurationElement
	{
		private const string _typeKey = "type";

		/// <summary>
		/// Custom indicator.
		/// </summary>
		[ConfigurationProperty(_typeKey, IsRequired = true, IsKey = true)]
		public string Type
		{
			get { return (string)this[_typeKey]; }
			set { this[_typeKey] = value; }
		}

		private const string _painterKey = "painter";

		/// <summary>
		/// Custom indicator painter.
		/// </summary>
		[ConfigurationProperty(_painterKey, IsRequired = false)]
		public string Painter
		{
			get { return (string)this[_painterKey]; }
			set { this[_painterKey] = value; }
		}
	}
}