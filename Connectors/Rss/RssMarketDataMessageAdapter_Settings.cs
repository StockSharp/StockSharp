#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Rss.Rss
File: RssMarketDataMessageAdapter_Settings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Rss
{
	using System;
	using System.ComponentModel;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Rss.Xaml;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// The market-data message adapter for Rss.
	/// </summary>
	[Icon("Rss_logo.png")]
	// TODO
	//[Doc("")]
	[DisplayName("RSS")]
	[DescriptionLoc(LocalizedStrings.Str3504Key)]
	[CategoryOrder(_rss, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	partial class RssMarketDataMessageAdapter
	{
		private const string _rss = "RSS";

		private Uri _address;

		/// <summary>
		/// RSS feed address.
		/// </summary>
		[Category(_rss)]
		[DisplayNameLoc(LocalizedStrings.AddressKey)]
		[DescriptionLoc(LocalizedStrings.Str3505Key)]
		[PropertyOrder(0)]
		[Editor(typeof(RssAddressEditor), typeof(RssAddressEditor))]
		public Uri Address
		{
			get { return _address; }
			set
			{
				_address = value;

				if (value == RssAddresses.Nyse)
				{
					CustomDateFormat = "ddd, dd MMM yyyy HH:mm:ss zzzz GMT";
				}
			}
		}

		/// <summary>
		/// Dates format. Required to be filled if RSS stream format is different from ddd, dd MMM yyyy HH:mm:ss zzzz.
		/// </summary>
		[Category(_rss)]
		[DisplayNameLoc(LocalizedStrings.Str3506Key)]
		[DescriptionLoc(LocalizedStrings.Str3507Key)]
		[PropertyOrder(1)]
		public string CustomDateFormat { get; set; }

		/// <summary>
		/// The parameters validity check.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid => Address != null;

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Address = storage.GetValue<Uri>(nameof(Address));
			CustomDateFormat = storage.GetValue<string>(nameof(CustomDateFormat));
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Address), Address.To<string>());
			storage.SetValue(nameof(CustomDateFormat), CustomDateFormat);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return Address == null ? string.Empty : Address.To<string>();
		}
	}
}