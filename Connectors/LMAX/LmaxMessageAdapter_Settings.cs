#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.LMAX.LMAX
File: LmaxMessageAdapter_Settings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.LMAX
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// The messages adapter for LMAX.
	/// </summary>
	[Icon("Lmax_logo.png")]
	[Doc("http://stocksharp.com/doc/html/4f50724b-00de-4ed4-b043-7dacb6277c98.htm")]
	[DisplayName("LMAX")]
	[CategoryLoc(LocalizedStrings.ForexKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "LMAX")]
	[CategoryOrderLoc(LocalizedStrings.GeneralKey, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 2)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 3)]
	partial class LmaxMessageAdapter
	{
		/// <summary>
		/// Login.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.LoginKey, true)]
		[PropertyOrder(1)]
		public string Login { get; set; }

		/// <summary>
		/// Password.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.PasswordKey)]
		[DescriptionLoc(LocalizedStrings.PasswordKey, true)]
		[PropertyOrder(2)]
		public SecureString Password { get; set; }

		/// <summary>
		/// Connect to demo trading instead of real trading server.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.DemoKey)]
		[DescriptionLoc(LocalizedStrings.Str3388Key)]
		[PropertyOrder(1)]
		public bool IsDemo { get; set; }

		/// <summary>
		/// Should the whole set of securities be loaded from LMAX website. Switched off by default.
		/// </summary>
		[CategoryLoc(LocalizedStrings.GeneralKey)]
		[DisplayNameLoc(LocalizedStrings.Str2135Key)]
		[DescriptionLoc(LocalizedStrings.Str3389Key)]
		[PropertyOrder(2)]
		public bool IsDownloadSecurityFromSite { get; set; }

		/// <summary>
		/// The parameters validity check.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid => !Login.IsEmpty() && !Password.IsEmpty();

		private static readonly HashSet<TimeSpan> _timeFrames = new HashSet<TimeSpan>(new[]
		{
			TimeSpan.FromTicks(1),
			TimeSpan.FromMinutes(1),
			TimeSpan.FromDays(1)
		});

		/// <summary>
		/// Available time frames.
		/// </summary>
		[Browsable(false)]
		public static IEnumerable<TimeSpan> TimeFrames => _timeFrames;

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue(nameof(Login), Login);
			storage.SetValue(nameof(Password), Password);
			storage.SetValue(nameof(IsDemo), IsDemo);
			storage.SetValue(nameof(IsDownloadSecurityFromSite), IsDownloadSecurityFromSite);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Login = storage.GetValue<string>(nameof(Login));
			Password = storage.GetValue<SecureString>(nameof(Password));
			IsDemo = storage.GetValue<bool>(nameof(IsDemo));
			IsDownloadSecurityFromSite = storage.GetValue<bool>(nameof(IsDownloadSecurityFromSite));
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str3390Params.Put(Login, IsDemo);
		}
	}
}