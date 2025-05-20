namespace StockSharp.Algo.Testing;

using StockSharp.Algo.Commissions;

/// <summary>
/// Candle price for execution.
/// </summary>
public enum EmulationCandlePrices
{
	/// <summary>
	/// Middle price.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MiddleKey,
		Description = LocalizedStrings.MiddlePriceKey)]
	Middle,

	/// <summary>
	/// Open price.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OpenPriceKey,
		Description = LocalizedStrings.CandleOpenPriceKey)]
	Open,

	/// <summary>
	/// High price.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.HighestPriceKey,
		Description = LocalizedStrings.HighPriceOfCandleKey)]
	High,

	/// <summary>
	/// Low price.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LowestPriceKey,
		Description = LocalizedStrings.LowPriceOfCandleKey)]
	Low,

	/// <summary>
	/// Close price.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ClosingPriceKey,
		Description = LocalizedStrings.ClosePriceOfCandleKey)]
	Close,
}

/// <summary>
/// Settings of exchange emulator.
/// </summary>
public class MarketEmulatorSettings : NotifiableObject, IPersistable
{
	/// <summary>
	/// Initializes a new instance of the <see cref="MarketEmulatorSettings"/>.
	/// </summary>
	public MarketEmulatorSettings()
	{
	}

	private EmulationCandlePrices _candlePrice;

	/// <summary>
	/// <see cref="EmulationCandlePrices"/>
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CandleKey,
		Description = LocalizedStrings.CandleExecPriceKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 200)]
	public EmulationCandlePrices CandlePrice
	{
		get => _candlePrice;
		set
		{
			if (_candlePrice == value)
				return;

			_candlePrice = value;
			NotifyChanged();
		}
	}

	private bool _matchOnTouch = true;

	/// <summary>
	/// At emulation of clearing by trades, to perform clearing of orders, when trade price touches the order price (is equal to order price), rather than only when the trade price is better than order price. Is On by default (optimistic scenario).
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MatchOnTouchKey,
		Description = LocalizedStrings.MatchOnTouchDescKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 201)]
	public bool MatchOnTouch
	{
		get => _matchOnTouch;
		set
		{
			if (_matchOnTouch == value)
				return;

			_matchOnTouch = value;
			NotifyChanged();
		}
	}

	private double _failing;

	/// <summary>
	/// The percentage value of new orders registration error. The value may be from 0 (not a single error) to 100. By default is Off.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ErrorPercentKey,
		Description = LocalizedStrings.ErrorPercentDescKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 202)]
	public double Failing
	{
		get => _failing;
		set
		{
			if (value < 0 || value > 100)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_failing = value;
			NotifyChanged();
		}
	}

	private TimeSpan _latency;

	/// <summary>
	/// The minimal value of the registered orders delay. By default, it is <see cref="TimeSpan.Zero"/>, which means instant adoption of registered orders by exchange.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.LatencyKey,
		Description = LocalizedStrings.LatencyDescKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 203)]
	public TimeSpan Latency
	{
		get => _latency;
		set
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_latency = value;
			NotifyChanged();
		}
	}

	private long _initialOrderId;

	/// <summary>
	/// The number, starting at which the emulator will generate identifiers for orders <see cref="Order.Id"/>.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.OrderIdKey,
		Description = LocalizedStrings.OrderIdGenerationKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 206)]
	public long InitialOrderId
	{
		get => _initialOrderId;
		set
		{
			_initialOrderId = value;
			NotifyChanged();
		}
	}

	private long _initialTradeId;

	/// <summary>
	/// The number, starting at which the emulator will generate identifiers fir trades <see cref="Trade.Id"/>.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TradeIdKey,
		Description = LocalizedStrings.TradeIdGenerationKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 207)]
	public long InitialTradeId
	{
		get => _initialTradeId;
		set
		{
			_initialTradeId = value;
			NotifyChanged();
		}
	}

	private int _spreadSize = 2;

	/// <summary>
	/// The size of spread in price increments. It used at determination of spread for generation of order book from tick trades. By default equals to 2.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SpreadKey,
		Description = LocalizedStrings.SpreadSizeDescKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 209)]
	public int SpreadSize
	{
		get => _spreadSize;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_spreadSize = value;
			NotifyChanged();
		}
	}

	private int _maxDepth = 5;

	/// <summary>
	/// The maximal depth of order book, which will be generated from ticks. It used, if there is no order book history. By default equals to 5.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.DepthOfBookKey,
		Description = LocalizedStrings.DepthOfBookDescKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 210)]
	public int MaxDepth
	{
		get => _maxDepth;
		set
		{
			if (value < 1)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_maxDepth = value;
			NotifyChanged();
		}
	}

	private TimeSpan _portfolioRecalcInterval = TimeSpan.Zero;

	/// <summary>
	/// The interval for recalculation of data on portfolios. If interval equals <see cref="TimeSpan.Zero"/>, recalculation is not performed.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PortfoliosIntervalKey,
		Description = LocalizedStrings.PortfoliosIntervalDescKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 212)]
	public TimeSpan PortfolioRecalcInterval
	{
		get => _portfolioRecalcInterval;
		set
		{
			if (value < TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(value), value, LocalizedStrings.InvalidValue);

			_portfolioRecalcInterval = value;
			NotifyChanged();
		}
	}

	private bool _convertTime;

	/// <summary>
	/// To convert time for orders and trades into exchange time. By default, it is disabled.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ConvertTimeKey,
		Description = LocalizedStrings.ConvertTimeDescKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 213)]
	public bool ConvertTime
	{
		get => _convertTime;
		set
		{
			_convertTime = value;
			NotifyChanged();
		}
	}

	private TimeZoneInfo _timeZone;

	/// <summary>
	/// Information about the time zone where the exchange is located.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.TimeZoneKey,
		Description = LocalizedStrings.BoardTimeZoneKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 214)]
	public TimeZoneInfo TimeZone
	{
		get => _timeZone;
		set
		{
			_timeZone = value;
			NotifyChanged();
		}
	}

	private Unit _priceLimitOffset = new(40, UnitTypes.Percent);

	/// <summary>
	/// The price shift from the previous trade, determining boundaries of maximal and minimal prices for the next session. Used only if there is no saved information <see cref="Level1ChangeMessage"/>. By default, it equals to 40%.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.PriceShiftKey,
		Description = LocalizedStrings.PriceShiftDescKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 215)]
	public Unit PriceLimitOffset
	{
		get => _priceLimitOffset;
		set
		{
			_priceLimitOffset = value ?? throw new ArgumentNullException(nameof(value));
			NotifyChanged();
		}
	}

	private bool _increaseDepthVolume = true;

	/// <summary>
	/// To add the additional volume into order book at registering orders with greater volume. By default, it is enabled.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ExtraVolumeKey,
		Description = LocalizedStrings.ExtraVolumeDescKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 216)]
	public bool IncreaseDepthVolume
	{
		get => _increaseDepthVolume;
		set
		{
			_increaseDepthVolume = value;
			NotifyChanged();
		}
	}

	private bool _checkTradingState;

	/// <summary>
	/// Check trading state.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.SessionStateKey,
		Description = LocalizedStrings.CheckTradingStateKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 217)]
	public bool CheckTradingState
	{
		get => _checkTradingState;
		set
		{
			_checkTradingState = value;
			NotifyChanged();
		}
	}

	private bool _checkMoney;

	/// <summary>
	/// Check money balance.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.MoneyKey,
		Description = LocalizedStrings.CheckMoneyKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 218)]
	public bool CheckMoney
	{
		get => _checkMoney;
		set
		{
			_checkMoney = value;
			NotifyChanged();
		}
	}

	/// <summary>
	/// Can have short positions.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.ShortableKey,
		Description = LocalizedStrings.ShortableDescKey,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 218)]
	public bool CheckShortable { get; set; }

	/// <summary>
	/// Allow store generated by <see cref="IMarketEmulator"/> messages.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.StorageKey,
		//Description = ,
		GroupName = LocalizedStrings.BacktestExtraKey,
		Order = 219)]
	public bool AllowStoreGenerateMessages { get; set; }

	private bool _checkTradableDates;

	/// <summary>
	/// Check loading dates are they tradable.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		Name = LocalizedStrings.CheckDatesKey,
		Description = LocalizedStrings.CheckDatesDescKey,
		GroupName = LocalizedStrings.BacktestKey,
		Order = 106)]
	public bool CheckTradableDates
	{
		get => _checkTradableDates;
		set
		{
			_checkTradableDates = value;
			NotifyPropertyChanged();
		}
	}

	private IEnumerable<ICommissionRule> _commissionRules = [];

	/// <summary>
	/// Commission rules.
	/// </summary>
	[Display(
		ResourceType = typeof(LocalizedStrings),
		GroupName = LocalizedStrings.BacktestKey,
		Name = LocalizedStrings.CommissionKey,
		Description = LocalizedStrings.CommissionDescKey,
		Order = 110)]
	public IEnumerable<ICommissionRule> CommissionRules
	{
		get => _commissionRules;
		set
		{
			_commissionRules = value ?? throw new ArgumentNullException(nameof(value));
			NotifyChanged();
		}
	}

	/// <summary>
	/// To save the state of paper trading parameters.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public virtual void Save(SettingsStorage storage)
	{
		storage
			.Set(nameof(CandlePrice), CandlePrice)
			.Set(nameof(MatchOnTouch), MatchOnTouch)
			.Set(nameof(Failing), Failing)
			.Set(nameof(Latency), Latency)
			.Set(nameof(InitialOrderId), InitialOrderId)
			.Set(nameof(InitialTradeId), InitialTradeId)
			.Set(nameof(SpreadSize), SpreadSize)
			.Set(nameof(MaxDepth), MaxDepth)
			.Set(nameof(PortfolioRecalcInterval), PortfolioRecalcInterval)
			.Set(nameof(ConvertTime), ConvertTime)
			.Set(nameof(PriceLimitOffset), PriceLimitOffset)
			.Set(nameof(IncreaseDepthVolume), IncreaseDepthVolume)
			.Set(nameof(CheckTradingState), CheckTradingState)
			.Set(nameof(CheckMoney), CheckMoney)
			.Set(nameof(CheckShortable), CheckShortable)
			.Set(nameof(AllowStoreGenerateMessages), AllowStoreGenerateMessages)
			.Set(nameof(CheckTradableDates), CheckTradableDates)
			.Set(nameof(CommissionRules), CommissionRules.Select(c => c.SaveEntire(false)).ToArray());

		if (TimeZone != null)
			storage.Set(nameof(TimeZone), TimeZone);
	}

	/// <summary>
	/// To load the state of paper trading parameters.
	/// </summary>
	/// <param name="storage">Storage.</param>
	public virtual void Load(SettingsStorage storage)
	{
		CandlePrice = storage.GetValue(nameof(CandlePrice), CandlePrice);
		MatchOnTouch = storage.GetValue(nameof(MatchOnTouch), MatchOnTouch);
		Failing = storage.GetValue(nameof(Failing), Failing);
		Latency = storage.GetValue(nameof(Latency), Latency);
		InitialOrderId = storage.GetValue(nameof(InitialOrderId), InitialOrderId);
		InitialTradeId = storage.GetValue(nameof(InitialTradeId), InitialTradeId);
		SpreadSize = storage.GetValue(nameof(SpreadSize), SpreadSize);
		MaxDepth = storage.GetValue(nameof(MaxDepth), MaxDepth);
		PortfolioRecalcInterval = storage.GetValue(nameof(PortfolioRecalcInterval), PortfolioRecalcInterval);
		ConvertTime = storage.GetValue(nameof(ConvertTime), ConvertTime);
		PriceLimitOffset = storage.GetValue(nameof(PriceLimitOffset), PriceLimitOffset);
		IncreaseDepthVolume = storage.GetValue(nameof(IncreaseDepthVolume), IncreaseDepthVolume);
		CheckTradingState = storage.GetValue(nameof(CheckTradingState), CheckTradingState);
		CheckMoney = storage.GetValue(nameof(CheckMoney), CheckMoney);
		CheckShortable = storage.GetValue(nameof(CheckShortable), CheckShortable);
		AllowStoreGenerateMessages = storage.GetValue(nameof(AllowStoreGenerateMessages), AllowStoreGenerateMessages);
		CheckTradableDates = storage.GetValue(nameof(CheckTradableDates), CheckTradableDates);

		if (storage.Contains(nameof(TimeZone)))
			TimeZone = storage.GetValue<TimeZoneInfo>(nameof(TimeZone));

		var commRules = storage.GetValue<SettingsStorage[]>(nameof(CommissionRules));
		if (commRules is not null)
		{
			try
			{
				CommissionRules = [.. commRules.Select(i => i.LoadEntire<ICommissionRule>())];
			}
			catch (Exception ex)
			{
				ex.LogError();
			}
		}
	}
}