namespace StockSharp.FTX;

using System.ComponentModel.DataAnnotations;
using System.Security;

using Ecng.ComponentModel;

/// <summary>
/// The message adapter for <see cref="FTX"/>.
/// </summary>
[MediaIcon(Media.MediaNames.ftx)]
[Doc("topics/api/connectors/crypto_exchanges/ftx.html")]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.FTXKey,
	Description = LocalizedStrings.CryptoConnectorKey,
	GroupName = LocalizedStrings.CryptocurrencyKey)]
[MessageAdapterCategory(MessageAdapterCategories.Crypto | MessageAdapterCategories.RealTime |
	MessageAdapterCategories.Free | MessageAdapterCategories.Ticks | MessageAdapterCategories.MarketDepth |
	MessageAdapterCategories.Level1 | MessageAdapterCategories.Transactions)]
public partial class FtxMessageAdapter : AsyncMessageAdapter, IKeySecretAdapter
{
	/// <summary>
	/// Default value for <see cref="MessageAdapter.HeartbeatInterval"/>.
	/// </summary>
	public static readonly TimeSpan DefaultHeartbeatInterval = TimeSpan.FromSeconds(1);

	/// <inheritdoc />
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.KeyKey,
			Description = LocalizedStrings.KeyKey + LocalizedStrings.Dot,
			GroupName = LocalizedStrings.ConnectionKey,
			Order = 0)]
	[BasicSetting]
	public SecureString Key { get; set; }

	/// <inheritdoc />
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.SecretKey,
			Description = LocalizedStrings.SecretDescKey,
			GroupName = LocalizedStrings.ConnectionKey,
			Order = 1)]
	[BasicSetting]
	public SecureString Secret { get; set; }
	/// <summary>
	/// SubAccount name from GUI
	/// </summary>
	[Display(
		        ResourceType = typeof(LocalizedStrings),
		        Name = LocalizedStrings.SubAccKey,
		        Description = LocalizedStrings.SubAccKey,
		        GroupName = LocalizedStrings.ConnectionKey,
		        Order = 2)]
	[BasicSetting]
	public string SubaccountName { get; set; }

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Key), Key);
		storage.SetValue(nameof(Secret), Secret);
		storage.SetValue(nameof(SubaccountName), SubaccountName);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Key = storage.GetValue<SecureString>(nameof(Key));
		Secret = storage.GetValue<SecureString>(nameof(Secret));
		SubaccountName = storage.GetValue<string>(nameof(SubaccountName));
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + ": " + LocalizedStrings.Key + " = " + Key.ToId();
	}
}