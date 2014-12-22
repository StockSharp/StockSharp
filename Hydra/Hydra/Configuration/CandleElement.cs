namespace StockSharp.Hydra.Configuration
{
	using System.Configuration;

	class CandleElement : ConfigurationElement
	{
		private const string _nameKey = "name";

		[ConfigurationProperty(_nameKey, IsRequired = true)]
		public string Name
		{
			get { return (string)this[_nameKey]; }
			set { this[_nameKey] = value; }
		}

		private const string _typeKey = "type";

		[ConfigurationProperty(_typeKey, IsRequired = true, IsKey = true)]
		public string Type
		{
			get { return (string)this[_typeKey]; }
			set { this[_typeKey] = value; }
		}
	}
}