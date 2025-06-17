namespace StockSharp.Algo.Export;

/// <summary>
/// Txt templates registry.
/// </summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public class TemplateTxtRegistry : IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="TemplateTxtRegistry"/>.
	/// </summary>
	public TemplateTxtRegistry()
	{
	}

	/// <summary>
	/// Depth txt export template.
	/// </summary>
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TemplateDepthKey,
			Description = LocalizedStrings.TemplateTxtDepthKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 0)]
	public string TemplateTxtDepth { get; set; } = "{ServerTime:default:yyyyMMdd};{ServerTime:default:HH:mm:ss.ffffff};{Quote.Price};{Quote.Volume};{Side}";

	/// <summary>
	/// Ticks txt export template.
	/// </summary>
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TemplateTickKey,
			Description = LocalizedStrings.TemplateTxtTickKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 1)]
	[BasicSetting]
	public string TemplateTxtTick { get; set; } = "{ServerTime:default:yyyyMMdd};{ServerTime:default:HH:mm:ss.ffffff};{TradeId};{TradePrice};{TradeVolume};{OriginSide}";

	/// <summary>
	/// Candles txt export template.
	/// </summary>
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TemplateCandleKey,
			Description = LocalizedStrings.TemplateTxtCandleKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 2)]
	[BasicSetting]
	public string TemplateTxtCandle { get; set; } = "{OpenTime:default:yyyyMMdd};{OpenTime:default:HH:mm:ss};{OpenPrice};{HighPrice};{LowPrice};{ClosePrice};{TotalVolume}";

	/// <summary>
	/// Level1 txt export template.
	/// </summary>
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TemplateLevel1Key,
			Description = LocalizedStrings.TemplateTxtLevel1Key,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 3)]
	[BasicSetting]
	public string TemplateTxtLevel1 { get; set; } = "{ServerTime:default:yyyyMMdd};{ServerTime:default:HH:mm:ss.ffffff};{Changes:{BestBidPrice};{BestBidVolume};{BestAskPrice};{BestAskVolume};{LastTradePrice};{LastTradeVolume}}";

	/// <summary>
	/// Options greeks txt export template.
	/// </summary>
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TemplateOptionsKey,
			Description = LocalizedStrings.TemplateTxtOptionsKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 4)]
	public string TemplateTxtOptions { get; set; } = "{SecurityId.SecurityCode};{ServerTime:default:yyyyMMdd};{ServerTime:default:HH:mm:ss.ffffff};{Changes:{BestBidPrice};{BestAskPrice};{BestAskVolume};{Delta};{Gamma};{Vega};{Theta};{Rho};{HistoricalVolatility};{ImpliedVolatility};{OpenInterest};{TheorPrice};{Volume}}";

	/// <summary>
	/// Order log txt export template.
	/// </summary>
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TemplateOrderLogKey,
			Description = LocalizedStrings.TemplateTxtOrderLogKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 5)]
	public string TemplateTxtOrderLog { get; set; } = "{ServerTime:default:yyyyMMdd};{ServerTime:default:HH:mm:ss.ffffff};{IsSystem};{OrderId};{OrderPrice};{OrderVolume};{Side};{OrderState};{TimeInForce};{TradeId};{TradePrice}";

	/// <summary>
	/// Transactions txt export template.
	/// </summary>
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TemplateTransactionKey,
			Description = LocalizedStrings.TemplateTxtTransactionKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 6)]
	public string TemplateTxtTransaction { get; set; } = "{ServerTime:default:yyyyMMdd};{ServerTime:default:HH:mm:ss.ffffff};{PortfolioName};{TransactionId};{OrderId};{OrderPrice};{OrderVolume};{Balance};{Side};{OrderType};{OrderState};{TradeId};{TradePrice};{TradeVolume}";

	/// <summary>
	/// Security txt export template.
	/// </summary>
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TemplateSecurityKey,
			Description = LocalizedStrings.TemplateTxtSecurityKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 7)]
	public string TemplateTxtSecurity { get; set; } = "{SecurityId.SecurityCode};{SecurityId.BoardCode};{PriceStep};{SecurityType};{VolumeStep};{Multiplier};{Decimals}";

	/// <summary>
	/// News txt export template.
	/// </summary>
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.TemplateNewsKey,
			Description = LocalizedStrings.TemplateTxtNewsKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 8)]
	public string TemplateTxtNews { get; set; } = "{ServerTime:default:yyyyMMdd};{ServerTime:default:HH:mm:ss};{Headline};{Source};{Url}";

	/// <summary>
	/// Board txt export template.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TemplateBoardKey,
		Description = LocalizedStrings.TemplateTxtBoardKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 9)]
	public string TemplateTxtBoard { get; set; } = "{ExchangeCode};{Code};{ExpiryTime};{TimeZone}";

	/// <summary>
	/// Board state txt export template.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TemplateBoardKey,
		Description = LocalizedStrings.TemplateTxtBoardKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 9)]
	public string TemplateTxtBoardState { get; set; } = "{ServerTime:default:yyyyMMdd};{ServerTime:default:HH:mm:ss};{BoardCode};{State}";

	/// <summary>
	/// Indicator's value txt export template.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TemplateIndicatorKey,
		Description = LocalizedStrings.TemplateTxtIndicatorKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 10)]
	public string TemplateTxtIndicator { get; set; } = "{SecurityId.SecurityCode};{Time:default:yyyyMMdd};{Time:default:HH:mm:ss};{Value1}";

	/// <summary>
	/// Position change txt export template.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PositionChangeKey,
		Description = LocalizedStrings.TemplateTxtPositionChangeKey,
		GroupName = LocalizedStrings.GeneralKey,
		Order = 11)]
	public string TemplateTxtPositionChange { get; set; } = "{SecurityId.SecurityCode};{ServerTime:default:yyyyMMdd};{ServerTime:default:HH:mm:ss.ffffff};{Changes:{CurrentValue};{BlockedValue};{RealizedPnL};{UnrealizedPnL};{AveragePrice};{Commission}}";

	/// <summary>
	/// Do not show again.
	/// </summary>
	[Display(
			ResourceType = typeof(LocalizedStrings),
			Name = LocalizedStrings.DoNotShowAgainKey,
			Description = LocalizedStrings.DoNotShowAgainKey,
			GroupName = LocalizedStrings.GeneralKey,
			Order = 12)]
	public bool DoNotShowAgain { get; set; }

	void IPersistable.Load(SettingsStorage storage)
	{
		TemplateTxtDepth = storage.GetValue(nameof(TemplateTxtDepth), TemplateTxtDepth);
		TemplateTxtTick = storage.GetValue(nameof(TemplateTxtTick), TemplateTxtTick);
		TemplateTxtCandle = storage.GetValue(nameof(TemplateTxtCandle), TemplateTxtCandle);
		TemplateTxtLevel1 = storage.GetValue(nameof(TemplateTxtLevel1), TemplateTxtLevel1);
		TemplateTxtOptions = storage.GetValue(nameof(TemplateTxtOptions), TemplateTxtOptions);
		TemplateTxtOrderLog = storage.GetValue(nameof(TemplateTxtOrderLog), TemplateTxtOrderLog);
		TemplateTxtTransaction = storage.GetValue(nameof(TemplateTxtTransaction), TemplateTxtTransaction);
		TemplateTxtSecurity = storage.GetValue(nameof(TemplateTxtSecurity), TemplateTxtSecurity);
		TemplateTxtNews = storage.GetValue(nameof(TemplateTxtNews), TemplateTxtNews);
		TemplateTxtBoard = storage.GetValue(nameof(TemplateTxtBoard), TemplateTxtBoard);
		TemplateTxtBoardState = storage.GetValue(nameof(TemplateTxtBoardState), TemplateTxtBoardState);
		TemplateTxtIndicator = storage.GetValue(nameof(TemplateTxtIndicator), TemplateTxtIndicator);
		TemplateTxtPositionChange = storage.GetValue(nameof(TemplateTxtPositionChange), TemplateTxtPositionChange);

		DoNotShowAgain = storage.GetValue(nameof(DoNotShowAgain), DoNotShowAgain);
	}

	void IPersistable.Save(SettingsStorage storage)
	{
		storage.SetValue(nameof(TemplateTxtDepth), TemplateTxtDepth);
		storage.SetValue(nameof(TemplateTxtTick), TemplateTxtTick);
		storage.SetValue(nameof(TemplateTxtCandle), TemplateTxtCandle);
		storage.SetValue(nameof(TemplateTxtLevel1), TemplateTxtLevel1);
		storage.SetValue(nameof(TemplateTxtOptions), TemplateTxtOptions);
		storage.SetValue(nameof(TemplateTxtOrderLog), TemplateTxtOrderLog);
		storage.SetValue(nameof(TemplateTxtTransaction), TemplateTxtTransaction);
		storage.SetValue(nameof(TemplateTxtSecurity), TemplateTxtSecurity);
		storage.SetValue(nameof(TemplateTxtNews), TemplateTxtNews);
		storage.SetValue(nameof(TemplateTxtBoard), TemplateTxtBoard);
		storage.SetValue(nameof(TemplateTxtBoardState), TemplateTxtBoardState);
		storage.SetValue(nameof(TemplateTxtIndicator), TemplateTxtIndicator);
		storage.SetValue(nameof(TemplateTxtPositionChange), TemplateTxtPositionChange);

		storage.SetValue(nameof(DoNotShowAgain), DoNotShowAgain);
	}
}