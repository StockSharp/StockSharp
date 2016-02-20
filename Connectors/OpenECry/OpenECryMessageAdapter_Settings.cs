#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.OpenECry.OpenECry
File: OpenECryMessageAdapter_Settings.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.OpenECry
{
	using System;
	using System.Collections.Generic;
	using System.ComponentModel;
	using System.Net;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.OpenECry.Xaml;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// Connection to the terminal modes. Description of functionality http://www.openecry.com/api/OECAPIRemoting.pdf.
	/// </summary>
	public enum OpenECryRemoting
	{
		/// <summary>
		/// Disabled.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str2558Key)]
		None,

		/// <summary>
		/// If there is another connection with the same Login/Password, it can be diconnected.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.MainKey)]
		Primary,

		/// <summary>
		/// An attempt to activate the mode <see cref="OpenECryRemoting.Secondary"/>, in case of failure the mode <see cref="OpenECryRemoting.None"/> is activated.
		/// </summary>
		[EnumDisplayNameLoc(LocalizedStrings.Str2560Key)]
		Secondary
	}

	/// <summary>
	/// The messages adapter for OpenECry.
	/// </summary>
	[Icon("OpenECry_logo.png")]
	[Doc("http://stocksharp.com/doc/html/f8cae46b-57e1-4954-a4cf-832854840981.htm")]
	[DisplayName("OpenECry")]
	[CategoryLoc(LocalizedStrings.AmericaKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "OpenECry")]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	partial class OpenECryMessageAdapter
	{
		private EndPoint _address = OpenECryAddresses.Api;

		/// <summary>
		/// The OpenECry server API address. The default is <see cref="OpenECryAddresses.Api"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.AddressKey)]
		[DescriptionLoc(LocalizedStrings.Str2562Key)]
		[PropertyOrder(0)]
		[Editor(typeof(OpenECryEndPointEditor), typeof(OpenECryEndPointEditor))]
		public EndPoint Address
		{
			get { return _address; }
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				_address = value;
			}
		}

		/// <summary>
		/// OpenECry login.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.LoginKey)]
		[DescriptionLoc(LocalizedStrings.Str2563Key)]
		[PropertyOrder(1)]
		public string Login { get; set; }

		/// <summary>
		/// OpenECry password.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.PasswordKey)]
		[DescriptionLoc(LocalizedStrings.Str2564Key)]
		[PropertyOrder(2)]
		public SecureString Password { get; set; }

		/// <summary>
		/// Unique software ID.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayName("UUID")]
		[DescriptionLoc(LocalizedStrings.Str2565Key)]
		[PropertyOrder(3)]
		public SecureString Uuid { get; set; }

		/// <summary>
		/// The required mode of connection to the terminal. The default is <see cref="OpenECryRemoting.None"/>.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayName("Remoting")]
		[DescriptionLoc(LocalizedStrings.Str2566Key)]
		[PropertyOrder(4)]
		public OpenECryRemoting Remoting { get; set; }

		/// <summary>
		/// To use the 'native' reconnection process. Enabled by default.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str180Key)]
		[DescriptionLoc(LocalizedStrings.Str2567Key)]
		[PropertyOrder(5)]
		public bool UseNativeReconnect { get; set; }

		/// <summary>
		/// Use OpenECry API logging.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str9Key)]
		[DescriptionLoc(LocalizedStrings.Str2568Key)]
		[PropertyOrder(6)]
		public bool EnableOECLogging { get; set; }

		private static readonly HashSet<TimeSpan> _timeFrames = new HashSet<TimeSpan>(new[]
		{
			TimeSpan.FromSeconds(1),
			TimeSpan.FromMinutes(1),
			TimeSpan.FromHours(1),
			TimeSpan.FromDays(1),
			TimeSpan.FromTicks(TimeHelper.TicksPerWeek),
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

			storage.SetValue(nameof(Uuid), Uuid);
			storage.SetValue(nameof(Address), Address.To<string>());
			storage.SetValue(nameof(Login), Login);
			storage.SetValue(nameof(Password), Password);
			storage.SetValue(nameof(Remoting), Remoting.To<string>());
			storage.SetValue(nameof(UseNativeReconnect), UseNativeReconnect);
			storage.SetValue(nameof(EnableOECLogging), EnableOECLogging);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Uuid = storage.GetValue<SecureString>(nameof(Uuid));
			Address = storage.GetValue<EndPoint>(nameof(Address));
			Login = storage.GetValue<string>(nameof(Login));
			Password = storage.GetValue<SecureString>(nameof(Password));
			Remoting = storage.GetValue<OpenECryRemoting>(nameof(Remoting));
			UseNativeReconnect = storage.GetValue<bool>(nameof(UseNativeReconnect));
			EnableOECLogging = storage.GetValue<bool>(nameof(EnableOECLogging));
		}
	}
}