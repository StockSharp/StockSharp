namespace StockSharp.Configuration
{
	using System.Configuration;

	/// <summary>
	/// Represents the custom diagram element.
	/// </summary>
	public class DiagramElement : ConfigurationElement
	{
		private const string _typeKey = "type";

		/// <summary>
		/// Custom diagram element.
		/// </summary>
		[ConfigurationProperty(_typeKey, IsRequired = true, IsKey = true)]
		public string Type
		{
			get { return (string)this[_typeKey]; }
			set { this[_typeKey] = value; }
		}
	}
}
