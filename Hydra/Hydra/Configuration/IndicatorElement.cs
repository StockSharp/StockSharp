namespace StockSharp.Hydra.Configuration
{
	using System.Configuration;

	class IndicatorElement : ConfigurationElement
	{
		private const string _typeKey = "type";

		[ConfigurationProperty(_typeKey, IsRequired = true, IsKey = true)]
		public string Type
		{
			get { return (string)this[_typeKey]; }
			set { this[_typeKey] = value; }
		}

		private const string _painterKey = "painter";

		[ConfigurationProperty(_painterKey, IsRequired = false)]
		public string Painter
		{
			get { return (string)this[_painterKey]; }
			set { this[_painterKey] = value; }
		}
	}
}