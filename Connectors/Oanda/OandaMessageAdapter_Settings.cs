#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Oanda.Oanda
File: OandaMessageAdapter_Settings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Oanda
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
	/// Servers types.
	/// </summary>
	public enum OandaServers
	{
		/// <summary>
		/// Demo.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.DemoKey)]
		Sandbox,

		/// <summary>
		/// Simulator.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str1209Key)]
		Practice,

		/// <summary>
		/// Real.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str3448Key)]
		Real,
	}

	/// <summary>
	/// The messages adapter for OANDA (REST protocol).
	/// </summary>
	[Icon("Oanda_logo.png")]
	[Doc("http://stocksharp.com/doc/html/c2162c96-d12f-4107-ac96-0238b793f466.htm")]
	[DisplayName("OANDA")]
	[CategoryLoc(LocalizedStrings.ForexKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "OANDA")]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	partial class OandaMessageAdapter
	{
		/// <summary>
		/// Server.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3416Key)]
		[DescriptionLoc(LocalizedStrings.Str3450Key)]
		[PropertyOrder(0)]
		public OandaServers Server { get; set; }

		/// <summary>
		/// Token.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3451Key)]
		[DescriptionLoc(LocalizedStrings.Str3451Key, true)]
		[PropertyOrder(1)]
		public SecureString Token { get; set; }

		private static readonly HashSet<TimeSpan> _timeFrames = new HashSet<TimeSpan>(new[]
		{
			TimeSpan.FromSeconds(5),
			TimeSpan.FromSeconds(10),
			TimeSpan.FromSeconds(15),
			TimeSpan.FromSeconds(30),
			TimeSpan.FromMinutes(1),
			TimeSpan.FromMinutes(2),
			TimeSpan.FromMinutes(3),
			TimeSpan.FromMinutes(5),
			TimeSpan.FromMinutes(10),
			TimeSpan.FromMinutes(15),
			TimeSpan.FromMinutes(30),
			TimeSpan.FromHours(1),
			TimeSpan.FromHours(2),
			TimeSpan.FromHours(3),
			TimeSpan.FromHours(4),
			TimeSpan.FromHours(6),
			TimeSpan.FromHours(8),
			TimeSpan.FromHours(12),
			TimeSpan.FromDays(1),
			TimeSpan.FromDays(7),
			TimeSpan.FromTicks(TimeHelper.TicksPerMonth)
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

			storage.SetValue(nameof(Server), Server.To<string>());
			storage.SetValue(nameof(Token), Token);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Server = storage.GetValue<OandaServers>(nameof(Server));
			Token = storage.GetValue<SecureString>(nameof(Token));
		}
	}
}