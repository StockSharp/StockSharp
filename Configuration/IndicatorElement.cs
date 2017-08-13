#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Configuration.ConfigurationPublic
File: IndicatorElement.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Configuration
{
	using System.Configuration;

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
			get => (string)this[_typeKey];
			set => this[_typeKey] = value;
		}

		private const string _painterKey = "painter";

		/// <summary>
		/// Custom indicator painter.
		/// </summary>
		[ConfigurationProperty(_painterKey, IsRequired = false)]
		public string Painter
		{
			get => (string)this[_painterKey];
			set => this[_painterKey] = value;
		}
	}
}