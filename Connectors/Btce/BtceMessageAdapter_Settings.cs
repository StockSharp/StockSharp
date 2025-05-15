namespace StockSharp.Btce;

using System.ComponentModel.DataAnnotations;
using System.Security;

using Ecng.ComponentModel;

/// <summary>
/// The messages adapter for BTC-e.
/// </summary>
[MediaIcon(Media.MediaNames.btce)]
[Doc("topics/api/connectors/crypto_exchanges/wex_btc_e.html")]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BtceKey,
	Description = LocalizedStrings.CryptoConnectorKey,
	GroupName = LocalizedStrings.CryptocurrencyKey)]
[MessageAdapterCategory(MessageAdapterCategories.Crypto | MessageAdapterCategories.RealTime |
	MessageAdapterCategories.Free | MessageAdapterCategories.Ticks | MessageAdapterCategories.MarketDepth |
	MessageAdapterCategories.Level1 | MessageAdapterCategories.Transactions)]
public partial class BtceMessageAdapter : AsyncMessageAdapter, IKeySecretAdapter, IAddressAdapter<string>
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

	private string _address = DefaultDomain;

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DomainAddressKey,
		Description = LocalizedStrings.DomainAddressDescKey,
		GroupName = LocalizedStrings.ConnectionKey,
		Order = 2)]
	[BasicSetting]
	public string Address
	{
		get => _address;
		set
		{
			if (value.IsEmpty())
				throw new ArgumentNullException(nameof(value));

			_address = value;
		}
	}

	private TimeSpan _balanceCheckInterval;

	/// <summary>
	/// Balance check interval. Required in case of deposit and withdraw actions.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.BalanceKey,
		Description = LocalizedStrings.BalanceCheckIntervalKey,
		GroupName = LocalizedStrings.ConnectionKey,
		Order = 3)]
	public TimeSpan BalanceCheckInterval
	{
		get => _balanceCheckInterval;
		set
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value));

			_balanceCheckInterval = value;
		}
	}

	/// <summary>
	/// Default value for <see cref="Address"/>.
	/// </summary>
	public const string DefaultDomain = "https://wex.nz/";

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Address), Address);
		storage.SetValue(nameof(Key), Key);
		storage.SetValue(nameof(Secret), Secret);
		storage.SetValue(nameof(BalanceCheckInterval), BalanceCheckInterval);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Address = storage.GetValue<string>(nameof(Address));
		Key = storage.GetValue<SecureString>(nameof(Key));
		Secret = storage.GetValue<SecureString>(nameof(Secret));
		BalanceCheckInterval = storage.GetValue<TimeSpan>(nameof(BalanceCheckInterval));
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + ": " + LocalizedStrings.Key + " = " + Key.ToId();
	}
}