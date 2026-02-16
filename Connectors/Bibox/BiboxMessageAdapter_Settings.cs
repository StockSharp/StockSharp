namespace StockSharp.Bibox;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// The message adapter for <see cref="Bibox"/>.
/// </summary>
[MediaIcon(Media.MediaNames.bibox)]
[Doc("topics/api/connectors/crypto_exchanges/bibox.html")]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.BiboxKey,
	Description = LocalizedStrings.CryptoConnectorKey,
	GroupName = LocalizedStrings.CryptocurrencyKey)]
[MessageAdapterCategory(MessageAdapterCategories.Crypto | MessageAdapterCategories.RealTime |
	MessageAdapterCategories.Free | MessageAdapterCategories.Ticks | MessageAdapterCategories.MarketDepth |
	MessageAdapterCategories.Level1 | MessageAdapterCategories.Transactions)]
public partial class BiboxMessageAdapter : MessageAdapter, IKeySecretAdapter
{
	/// <summary>
	/// Possible time-frames.
	/// </summary>
	public static IEnumerable<TimeSpan> AllTimeFrames => Native.Extensions.TimeFrames.Keys.ToArray();

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
	/// Administrative password.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PasswordKey,
		Description = LocalizedStrings.AdminPasswordKey,
		GroupName = LocalizedStrings.WithdrawKey,
		Order = 2)]
	public SecureString AdminPassword { get; set; }

	//private TimeSpan _balanceCheckInterval;

	///// <summary>
	///// Balance check interval. Required in case of deposit and withdraw actions.
	///// </summary>
	//[Display(
	//	ResourceType = typeof(LocalizedStrings),
	//	Name = LocalizedStrings.BalanceKey,
	//	Description = LocalizedStrings.BalanceCheckIntervalKey,
	//	GroupName = LocalizedStrings.ConnectionKey,
	//	Order = 3)]
	//public TimeSpan BalanceCheckInterval
	//{
	//	get => _balanceCheckInterval;
	//	set
	//	{
	//		if (value < TimeSpan.Zero)
	//			throw new ArgumentOutOfRangeException(nameof(value));

	//		_balanceCheckInterval = value;
	//	}
	//}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(Key), Key);
		storage.SetValue(nameof(Secret), Secret);
		storage.SetValue(nameof(AdminPassword), AdminPassword);
		//storage.SetValue(nameof(BalanceCheckInterval), BalanceCheckInterval);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Key = storage.GetValue<SecureString>(nameof(Key));
		Secret = storage.GetValue<SecureString>(nameof(Secret));
		AdminPassword = storage.GetValue<SecureString>(nameof(AdminPassword));
		//BalanceCheckInterval = storage.GetValue<TimeSpan>(nameof(BalanceCheckInterval));
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + ": " + LocalizedStrings.Key + " = " + Key.ToId();
	}
}