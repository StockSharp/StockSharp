namespace StockSharp.ETrade
{
	using System.ComponentModel;
	using System.Security;

	using Ecng.Common;
	using Ecng.Serialization;

	using StockSharp.ETrade.Native;
	using StockSharp.Localization;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// The messages adapter for ETrade.
	/// </summary>
	[DisplayName("E*TRADE")]
	[CategoryLoc(LocalizedStrings.AmericaKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "E*TRADE")]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	partial class ETradeMessageAdapter
	{
		#region properties

		/// <summary>
		/// Key.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3304Key)]
		[DescriptionLoc(LocalizedStrings.Str3304Key, true)]
		[PropertyOrder(1)]
		public string ConsumerKey { get; set; }

		/// <summary>
		/// Secret.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3306Key)]
		[DescriptionLoc(LocalizedStrings.Str3307Key)]
		[PropertyOrder(2)]
		public SecureString ConsumerSecret { get; set; }

		/// <summary>
		/// Demo mode.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.DemoKey)]
		[DescriptionLoc(LocalizedStrings.Str3369Key)]
		[PropertyOrder(3)]
		public bool Sandbox { get; set; }

		/// <summary>
		/// OAuth access token. Required to restore connection. Saved AccessToken can be valid until EST midnight.
		/// </summary>
		[Browsable(false)]
		public OAuthToken AccessToken { get; set; }

		///// <summary>
		///// Для режима Sandbox. Список инструментов, которые будут переданы в событии <see cref="IConnector.NewSecurities"/>.
		///// </summary>
		//[Browsable(false)]
		//public Security[] SandboxSecurities
		//{
		//	get { return _client.SandboxSecurities; }
		//	set { _client.SandboxSecurities = value; }
		//}

		/// <summary>
		/// Verification code, received by user in browser, after confirming program's permission to work.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3370Key)]
		[DescriptionLoc(LocalizedStrings.Str3371Key)]
		[PropertyOrder(4)]
		public string VerificationCode { get; set; }

		/// <summary>
		/// The parameters validity check.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid
		{
			get { return !ConsumerKey.IsEmpty() && !ConsumerSecret.IsEmpty(); }
		}

		#endregion

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			ConsumerKey = storage.GetValue<string>("ConsumerKey");
			ConsumerSecret = storage.GetValue<SecureString>("ConsumerSecret");
			Sandbox = storage.GetValue<bool>("Sandbox");
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("ConsumerKey", ConsumerKey);
			storage.SetValue("ConsumerSecret", ConsumerSecret);
			storage.SetValue("Sandbox", Sandbox);
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return ConsumerKey.IsEmpty() ? string.Empty : "ConsumerKey = {0}".Put(ConsumerKey);
		}
	}
}
