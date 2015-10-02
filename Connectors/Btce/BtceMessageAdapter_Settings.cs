namespace StockSharp.Btce
{
	using System.ComponentModel;
	using System.Security;

	using Ecng.Common;
	using Ecng.ComponentModel;
	using Ecng.Serialization;

	using StockSharp.Localization;
	using StockSharp.Messages;

	using Xceed.Wpf.Toolkit.PropertyGrid.Attributes;

	/// <summary>
	/// The messages adapter for BTC-e.
	/// </summary>
	[Icon("Btce_logo.png")]
	[Doc("http://stocksharp.com/doc/html/5d162089-902a-4a4e-885f-f38ff94fbe58.htm")]
	[DisplayName("BTC-e")]
	[CategoryLoc(LocalizedStrings.BitcoinsKey)]
	[DescriptionLoc(LocalizedStrings.Str1770Key, "BTC-e")]
	[CategoryOrderLoc(LocalizedStrings.Str174Key, 0)]
	[CategoryOrderLoc(LocalizedStrings.Str186Key, 1)]
	[CategoryOrderLoc(LocalizedStrings.LoggingKey, 2)]
	partial class BtceMessageAdapter
	{
		/// <summary>
		/// Key.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3304Key)]
		[DescriptionLoc(LocalizedStrings.Str3304Key, true)]
		[PropertyOrder(1)]
		public SecureString Key { get; set; }

		/// <summary>
		/// Secret.
		/// </summary>
		[CategoryLoc(LocalizedStrings.Str174Key)]
		[DisplayNameLoc(LocalizedStrings.Str3306Key)]
		[DescriptionLoc(LocalizedStrings.Str3307Key)]
		[PropertyOrder(2)]
		public SecureString Secret { get; set; }

		/// <summary>
		/// The parameters validity check.
		/// </summary>
		[Browsable(false)]
		public override bool IsValid
		{
			get
			{
				if (this.IsMessageSupported(MessageTypes.OrderRegister))
					return !Key.IsEmpty() && !Secret.IsEmpty();
				else
					return true;
			}
		}

		/// <summary>
		/// Save settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Save(SettingsStorage storage)
		{
			base.Save(storage);

			storage.SetValue("Key", Key);
			storage.SetValue("Secret", Secret);
		}

		/// <summary>
		/// Load settings.
		/// </summary>
		/// <param name="storage">Settings storage.</param>
		public override void Load(SettingsStorage storage)
		{
			base.Load(storage);

			Key = storage.GetValue<SecureString>("Key");
			Secret = storage.GetValue<SecureString>("Secret");
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return LocalizedStrings.Str3304 + " = " + Key;
		}
	}
}