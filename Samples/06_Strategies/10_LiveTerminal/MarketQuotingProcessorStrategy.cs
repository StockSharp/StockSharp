namespace StockSharp.Samples.Strategies.LiveTerminal;

using System;

using Ecng.ComponentModel;
using Ecng.Logging;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.Algo.Strategies.Quoting;
using StockSharp.Localization;
using StockSharp.Messages;

/// <summary>
/// The quoting by the market price. Based on <see cref="QuotingProcessor"/>.
/// </summary>
public class MarketQuotingProcessorStrategy : Strategy
{
	private readonly StrategyParam<Sides> _quotingSide;
	private readonly StrategyParam<decimal> _quotingVolume;
	private readonly StrategyParam<TimeSpan> _timeOut;
	private readonly StrategyParam<bool> _useBidAsk;
	private readonly StrategyParam<bool> _useLastTradePrice;
	private readonly StrategyParam<Unit> _bestPriceOffset;
	private readonly StrategyParam<Unit> _priceOffset;
	private readonly StrategyParam<MarketPriceTypes> _priceType;

	private QuotingProcessor _processor;

	/// <summary>
	/// Initializes a new instance of the <see cref="MarketQuotingProcessorStrategy"/>.
	/// </summary>
	public MarketQuotingProcessorStrategy()
	{
		_quotingSide = Param(nameof(QuotingSide), Sides.Buy);
		_quotingVolume = Param(nameof(QuotingVolume), 1m).SetGreaterThanZero();
		_timeOut = Param<TimeSpan>(nameof(TimeOut)).SetNotNegative();
		_useBidAsk = Param(nameof(UseBidAsk), true);
		_useLastTradePrice = Param(nameof(UseLastTradePrice), false);
		_bestPriceOffset = Param(nameof(BestPriceOffset), new Unit());
		_priceOffset = Param(nameof(PriceOffset), new Unit());
		_priceType = Param(nameof(PriceType), MarketPriceTypes.Following);
	}

	/// <summary>
	/// Quoting direction.
	/// </summary>
	public Sides QuotingSide
	{
		get => _quotingSide.Value;
		set => _quotingSide.Value = value;
	}

	/// <summary>
	/// Total quoting volume.
	/// </summary>
	public decimal QuotingVolume
	{
		get => _quotingVolume.Value;
		set => _quotingVolume.Value = value;
	}

	/// <summary>
	/// The time limit during which the quoting should be fulfilled. If the total volume of <see cref="QuotingVolume"/> will not be fulfilled by this time, the strategy will stop operating.
	/// </summary>
	/// <remarks>
	/// By default, the limit is disabled and it is equal to <see cref="TimeSpan.Zero"/>.
	/// </remarks>
	public TimeSpan TimeOut
	{
		get => _timeOut.Value;
		set => _timeOut.Value = value;
	}

	/// <summary>
	/// To use the best bid and ask prices from the order book. If the information in the order book is missed, the processor will not recommend any actions.
	/// </summary>
	/// <remarks>
	/// The default is enabled.
	/// </remarks>
	public bool UseBidAsk
	{
		get => _useBidAsk.Value;
		set => _useBidAsk.Value = value;
	}

	/// <summary>
	/// To use the last trade price, if the information in the order book is missed.
	/// </summary>
	/// <remarks>
	/// The default is disabled.
	/// </remarks>
	public bool UseLastTradePrice
	{
		get => _useLastTradePrice.Value;
		set => _useLastTradePrice.Value = value;
	}

	/// <summary>
	/// The shift from the best price, on which quoted order can be changed.
	/// </summary>
	public Unit BestPriceOffset
	{
		get => _bestPriceOffset.Value;
		set => _bestPriceOffset.Value = value;
	}

	/// <summary>
	/// The price shift for the registering order. It determines the amount of shift from the best quote (for the buy it is added to the price, for the sell it is subtracted).
	/// </summary>
	public Unit PriceOffset
	{
		get => _priceOffset.Value;
		set => _priceOffset.Value = value;
	}

	/// <summary>
	/// The market price type. The default value is <see cref="MarketPriceTypes.Following"/>.
	/// </summary>
	public MarketPriceTypes PriceType
	{
		get => _priceType.Value;
		set => _priceType.Value = value;
	}

	/// <inheritdoc />
	protected override void OnStarted2(DateTime time)
	{
		base.OnStarted2(time);

		_processor = new(
			new MarketQuotingBehavior(PriceOffset, BestPriceOffset, PriceType),
			Security,
			Portfolio,
			QuotingSide,
			QuotingVolume,
			Volume, // max volume of a single order
			TimeOut,
			this, // Strategy implements ISubscriptionProvider
			this, // Strategy implements IMarketRuleContainer
			this, // Strategy implements ITransactionProvider
			this, // Strategy implements ITimeProvider
			this, // Strategy implements IMarketDataProvider
			IsFormedAndOnlineAndAllowTrading, // check if trading is allowed
			UseBidAsk,
			UseLastTradePrice)
		{
			Parent = this,
		};

		_processor.Finished += OnProcessorFinished;
		_processor.Start();
	}

	/// <inheritdoc />
	protected override void OnStopped()
	{
		if (_processor != null)
		{
			if (_processor.LeftVolume > 0)
				LogWarning(LocalizedStrings.QuotingFinishedNotFull, _processor.LeftVolume);

			_processor.Finished -= OnProcessorFinished;
			_processor.Dispose();
			_processor = null;
		}

		base.OnStopped();
	}

	private async void OnProcessorFinished(bool isOk)
		=> await StopAsync();
}
