using System.Configuration;

namespace StockSharp.Configuration.ConfigCandle
{
    /// <summary>
	/// Represents the custom candle.
	/// </summary>
	public class CandleElement : ConfigurationElement
	{
		private const string _typeKey = "type";

		/// <summary>
		/// Custom candle.
		/// </summary>
		[ConfigurationProperty(_typeKey, IsRequired = true, IsKey = true)]
		public string Type
		{
			get { return (string)this[_typeKey]; }
			set { this[_typeKey] = value; }
		}
	}
}