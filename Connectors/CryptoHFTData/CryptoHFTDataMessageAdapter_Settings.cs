namespace StockSharp.CryptoHFTData;

using System.ComponentModel.DataAnnotations;

/// <summary>
/// Historical market-data adapter for CryptoHFTData.
/// </summary>
[Display(Name = "CryptoHFTData", Description = "Historical cryptocurrency trades and order books.", GroupName = "Cryptocurrency")]
[MessageAdapterCategory(MessageAdapterCategories.Crypto | MessageAdapterCategories.History |
	MessageAdapterCategories.Free | MessageAdapterCategories.Ticks | MessageAdapterCategories.MarketDepth)]
public partial class CryptoHFTDataMessageAdapter : MessageAdapter, ITokenAdapter, IAddressAdapter<string>
{
	/// <inheritdoc />
	[Display(Name = "API key", Description = "Optional CryptoHFTData API key. Keyless access uses the rate-limited free tier.", GroupName = "Connection", Order = 0)]
	[BasicSetting]
	public SecureString Token { get; set; }

	/// <summary>
	/// CryptoHFTData exchange identifier, such as <c>binance_futures</c>.
	/// </summary>
	[Display(Name = "Exchange", Description = "CryptoHFTData exchange identifier.", GroupName = "Connection", Order = 1)]
	[BasicSetting]
	public string Exchange { get; set; } = "binance_futures";

	/// <inheritdoc />
	[Display(Name = "API address", Description = "CryptoHFTData API base address.", GroupName = "Connection", Order = 2)]
	[BasicSetting]
	public string Address { get; set; } = "https://api.cryptohftdata.com";

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);
		storage
			.Set(nameof(Token), Token)
			.Set(nameof(Exchange), Exchange)
			.Set(nameof(Address), Address);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);
		Token = storage.GetValue<SecureString>(nameof(Token));
		Exchange = storage.GetValue<string>(nameof(Exchange), Exchange);
		Address = storage.GetValue<string>(nameof(Address), Address);
	}
}
