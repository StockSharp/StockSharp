namespace StockSharp.Coinbase;

using System.ComponentModel.DataAnnotations;
using System.Security;

using Ecng.ComponentModel;

/// <summary>
/// The message adapter for <see cref="Coinbase"/>.
/// </summary>
/// <remarks>
/// Supports two authentication methods:
/// 1. Legacy (HMAC): Key + Secret (base64) + Passphrase
/// 2. CDP (JWT): Key (key name) + Secret (EC PEM private key), Passphrase not required
/// The auth type is auto-detected by Secret format (PEM starts with "-----BEGIN").
/// </remarks>
[MediaIcon(Media.MediaNames.coinbase)]
[Doc("topics/api/connectors/crypto_exchanges/coinbase.html")]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.CoinbaseKey,
	Description = LocalizedStrings.CryptoConnectorKey,
	GroupName = LocalizedStrings.CryptocurrencyKey)]
[MessageAdapterCategory(MessageAdapterCategories.Crypto | MessageAdapterCategories.RealTime | MessageAdapterCategories.OrderLog |
	MessageAdapterCategories.Free | MessageAdapterCategories.Level1 | MessageAdapterCategories.Transactions)]
public partial class CoinbaseMessageAdapter : MessageAdapter, IKeySecretAdapter, IPassphraseAdapter
{
	/// <summary>
	/// API key (Legacy) or CDP API key name.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.KeyKey,
		Description = LocalizedStrings.KeyKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.ConnectionKey,
		Order = 0)]
	[BasicSetting]
	public SecureString Key { get; set; }

	/// <summary>
	/// Secret key (Legacy base64) or CDP private key (EC PEM format).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SecretKey,
		Description = LocalizedStrings.SecretDescKey,
		GroupName = LocalizedStrings.ConnectionKey,
		Order = 1)]
	[BasicSetting]
	public SecureString Secret { get; set; }

	/// <summary>
	/// Passphrase (Legacy auth only, not required for CDP).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PassphraseKey,
		Description = LocalizedStrings.PassphraseKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.ConnectionKey,
		Order = 2)]
	[BasicSetting]
	public SecureString Passphrase { get; set; }

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Key), Key);
		storage.SetValue(nameof(Secret), Secret);
		storage.SetValue(nameof(Passphrase), Passphrase);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Key = storage.GetValue<SecureString>(nameof(Key));
		Secret = storage.GetValue<SecureString>(nameof(Secret));
		Passphrase = storage.GetValue<SecureString>(nameof(Passphrase));
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + ": " + LocalizedStrings.Key + " = " + Key.ToId();
	}
}