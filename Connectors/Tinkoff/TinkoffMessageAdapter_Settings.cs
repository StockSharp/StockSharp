namespace StockSharp.Tinkoff;

using System.ComponentModel.DataAnnotations;
using System.Security;

using Ecng.ComponentModel;
using Ecng.Serialization;

/// <summary>
/// The message adapter for <see cref="Tinkoff"/>.
/// </summary>
[MediaIcon(Media.MediaNames.tinkoff)]
[Doc("topics/api/connectors/russia/tinkoff.html")]
[Display(
	ResourceType = typeof(LocalizedStrings),
	Name = LocalizedStrings.TinkoffKey,
	Description = LocalizedStrings.StockConnectorKey,
	GroupName = LocalizedStrings.RussiaKey)]
[MessageAdapterCategory(MessageAdapterCategories.Russia | MessageAdapterCategories.Transactions | MessageAdapterCategories.RealTime |
	MessageAdapterCategories.Candles | MessageAdapterCategories.Level1 | MessageAdapterCategories.MarketDepth |
	MessageAdapterCategories.Futures | MessageAdapterCategories.Options |
	MessageAdapterCategories.Stock | MessageAdapterCategories.Free | MessageAdapterCategories.Ticks)]
[OrderCondition(typeof(TinkoffOrderCondition))]
public partial class TinkoffMessageAdapter : AsyncMessageAdapter, ITokenAdapter, IDemoAdapter
{
	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TokenKey,
		Description = LocalizedStrings.TokenKey + LocalizedStrings.Dot,
		GroupName = LocalizedStrings.ConnectionKey,
		Order = 0)]
	[BasicSetting]
	public SecureString Token { get; set; }

	/// <inheritdoc />
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DemoKey,
		Description = LocalizedStrings.DemoTradingConnectKey,
		GroupName = LocalizedStrings.ConnectionKey,
		Order = 3)]
	[BasicSetting]
	public bool IsDemo { get; set; }

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage
			.Set(nameof(Token), Token)
			.Set(nameof(IsDemo), IsDemo);
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Token = storage.GetValue<SecureString>(nameof(Token));
		IsDemo = storage.GetValue<bool>(nameof(IsDemo));
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return base.ToString() + ": " + LocalizedStrings.Key + " = " + Token.ToId();
	}
}