namespace StockSharp.Diagram;

/// <summary>
/// Socket ids for static sockets.
/// </summary>
public enum StaticSocketIds
{
	/// <summary>
	/// Trigger.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TriggerKey)]
	Trigger,

	/// <summary>
	/// Flag.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FlagKey)]
	Flag,

	/// <summary>
	/// Price.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PriceKey)]
	Price,

	/// <summary>
	/// Volume.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.VolumeKey)]
	Volume,

	/// <summary>
	/// Security.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecurityKey)]
	Security,

	/// <summary>
	/// Candles.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CandlesKey)]
	Candles,

	/// <summary>
	/// Market depth.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MarketDepthKey)]
	MarketDepth,

	/// <summary>
	/// Order.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderKey)]
	Order,

	/// <summary>
	/// Order fail.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OrderFailKey)]
	OrderFail,

	/// <summary>
	/// Own trade.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OwnTradeKey)]
	MyTrade,

	/// <summary>
	/// Trade.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TradeKey)]
	Trade,

	/// <summary>
	/// Trades.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TradesKey)]
	Trades,

	/// <summary>
	/// First operand.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.FirstOperandKey)]
	FirstOperand,

	/// <summary>
	/// Second operand.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecondOperandKey)]
	SecondOperand,

	/// <summary>
	/// Input.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.InputKey)]
	Input,

	/// <summary>
	/// Second input.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SecondInputKey)]
	SecondInput,

	/// <summary>
	/// Output.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OutputKey)]
	Output,

	/// <summary>
	/// Maximum deviation.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.MaxDeviationKey)]
	MaxDeviation,

	/// <summary>
	/// Options.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.OptionsKey)]
	Options,

	/// <summary>
	/// Position.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PositionKey)]
	Position,

	/// <summary>
	/// Portfolio.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PortfolioKey)]
	Portfolio,

	/// <summary>
	/// Underlying asset.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.UnderlyingAssetKey)]
	UnderlyingAsset,

	/// <summary>
	/// Unrealized profit.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PnLUnrealKey)]
	PnLUnreal,

	/// <summary>
	/// Realized profit.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PnLRealizedKey)]
	PnLRealized,

	/// <summary>
	/// Commission.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.CommissionKey)]
	Commission,

	/// <summary>
	/// Side.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SideKey)]
	Side,

	/// <summary>
	/// Date.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DateKey)]
	Date,

	/// <summary>
	/// Time.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.TimeKey)]
	Time,

	/// <summary>
	/// Strategy.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.StrategyKey)]
	Strategy,

	/// <summary>
	/// Signal.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.SignalKey)]
	Signal,

	/// <summary>
	/// Direction.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.DirectionKey)]
	Direction,

	/// <summary>
	/// Black-Scholes.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.BlackScholesKey)]
	BlackScholes,

	/// <summary>
	/// Option premium.
	/// </summary>
	[Display(ResourceType = typeof(LocalizedStrings), Name = LocalizedStrings.PremiumKey)]
	Premium,
}