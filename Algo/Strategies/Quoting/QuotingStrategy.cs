namespace StockSharp.Algo.Strategies.Quoting;

/// <summary>
/// Base quoting strategy class.
/// </summary>
[Obsolete("Use QuotingProcessor.")]
public abstract class QuotingStrategy : Strategy
{
	private QuotingProcessor _processor;

	/// <summary>
	/// Initialize <see cref="QuotingStrategy"/>.
	/// </summary>
	protected QuotingStrategy()
	{
		_quotingSide = Param(nameof(QuotingSide), Sides.Buy);
		_quotingVolume = Param(nameof(QuotingVolume), 1m).SetGreaterThanZero();
		_timeOut = Param<TimeSpan>(nameof(TimeOut)).SetNotNegative();
		_useBidAsk = Param(nameof(UseBidAsk), true);
		_useLastTradePrice = Param(nameof(UseLastTradePrice), true);

		DisposeOnStop = true;
	}

	private readonly StrategyParam<bool> _useBidAsk;

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

	private readonly StrategyParam<bool> _useLastTradePrice;

	/// <summary>
	/// To use the last trade price, if the information in the order book is missed.
	/// </summary>
	/// <remarks>
	/// The default is enabled.
	/// </remarks>
	public bool UseLastTradePrice
	{
		get => _useLastTradePrice.Value;
		set => _useLastTradePrice.Value = value;
	}

	private readonly StrategyParam<Sides> _quotingSide;

	/// <summary>
	/// Quoting direction.
	/// </summary>
	public Sides QuotingSide
	{
		get => _quotingSide.Value;
		set => _quotingSide.Value = value;
	}

	private readonly StrategyParam<decimal> _quotingVolume;

	/// <summary>
	/// Total quoting volume.
	/// </summary>
	public decimal QuotingVolume
	{
		get => _quotingVolume.Value;
		set => _quotingVolume.Value = value;
	}

	private readonly StrategyParam<TimeSpan> _timeOut;

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
	/// Create <see cref="IQuotingBehavior"/>.
	/// </summary>
	/// <returns><see cref="IQuotingBehavior"/></returns>
	protected abstract IQuotingBehavior CreateBehavior();

	/// <inheritdoc />
	protected override void OnStarted(DateTimeOffset time)
	{
		base.OnStarted(time);

		_processor = new(CreateBehavior(), Security, Portfolio, QuotingSide, QuotingVolume, Volume, TimeOut, this, this, this, this, this, IsFormedAndOnlineAndAllowTrading, UseBidAsk, UseLastTradePrice)
		{
			Parent = this
		};

		this
			.WhenStopping()
			.Do(() =>
			{
				if (_processor.LeftVolume > 0)
					LogWarning(LocalizedStrings.QuotingFinishedNotFull, _processor.LeftVolume);

				_processor.Finished -= OnProcessorFinished;
			})
			.Once()
			.Apply(this);

		_processor.Finished += OnProcessorFinished;

		_processor.Start();
	}

	private void OnProcessorFinished(bool success) => Stop();
}