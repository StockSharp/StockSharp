namespace StockSharp.Samples.Strategies.LiveArbitrage;

using System;
using System.Collections.Generic;
using System.Linq;

using StockSharp.Algo;
using StockSharp.Algo.Strategies;
using StockSharp.BusinessEntities;
using StockSharp.Logging;
using StockSharp.Messages;

public class ArbitrageStrategy : Strategy
{
	public Security FutureSecurity { get; set; }
	public Security StockSecurity { get; set; }

	public Portfolio FuturePortfolio { get; set; }
	public Portfolio StockPortfolio { get; set; }

	public decimal StockMultiplicator { get; set; }

	public decimal FutureVolume { get; set; }
	public decimal StockVolume { get; set; }

	public decimal ProfitToExit { get; set; }

	public decimal SpreadToGenerateSignal { get; set; }

	private ArbitrageState _currentState = ArbitrageState.None;

	private decimal _enterSpread;
	private Func<decimal> _arbitragePnl;

	private decimal _profit;
	private decimal _futBid;
	private decimal _futAck;
	private decimal _stBid;
	private decimal _stAsk;

	private IOrderBookMessage _lastFut;
	private IOrderBookMessage _lastSt;

	private SecurityId _futId, _stockId;

	protected override void OnStarted(DateTimeOffset time)
	{
		_futId = FutureSecurity.ToSecurityId();
		_stockId = StockSecurity.ToSecurityId();

		var subFut = this.SubscribeMarketDepth(FutureSecurity);
		var subStock = this.SubscribeMarketDepth(StockSecurity);

		subFut.WhenOrderBookReceived(this).Do(ProcessMarketDepth).Apply(this);
		subStock.WhenOrderBookReceived(this).Do(ProcessMarketDepth).Apply(this);
		base.OnStarted(time);
	}

	private void ProcessMarketDepth(IOrderBookMessage depth)
	{
		if (depth.SecurityId == _futId)
			_lastFut = depth;
		else if (depth.SecurityId == _stockId)
			_lastSt = depth;

		if (_lastFut is null || _lastSt is null)
			return;

		_futBid = GetAveragePrice(_lastFut, Sides.Sell, FutureVolume);
		_futAck = GetAveragePrice(_lastFut, Sides.Buy, FutureVolume);

		_stBid = GetAveragePrice(_lastSt, Sides.Sell, FutureVolume) * StockMultiplicator;
		_stAsk = GetAveragePrice(_lastSt, Sides.Buy, FutureVolume) * StockMultiplicator;

		var contangoSpread = _futBid - _stAsk;
		var backvordationSpread = _stBid - _futAck;

		if (_futBid == 0 || _futAck == 0 || _stBid == 0 || _stAsk == 0) return;

		decimal spread;

		ArbitrageState arbitrageSignal;
		if (backvordationSpread > contangoSpread)
		{
			arbitrageSignal = ArbitrageState.Backvordation;
			spread = backvordationSpread;
		}
		else
		{
			arbitrageSignal = ArbitrageState.Contango;
			spread = contangoSpread;
		}

		this.AddInfoLog($"Current state {_currentState}, enter spread = {_enterSpread}");
		this.AddInfoLog($"{ArbitrageState.Backvordation} spread = {backvordationSpread}");
		this.AddInfoLog($"{ArbitrageState.Contango}        spread = {contangoSpread}");
		this.AddInfoLog($"Entry from spread:{SpreadToGenerateSignal}. Exit from profit:{ProfitToExit}");

		if (_arbitragePnl != null)
		{
			_profit = _arbitragePnl();
			this.AddInfoLog($"Profit: {_profit}");
		}

		if (_currentState == ArbitrageState.None && spread > SpreadToGenerateSignal)
		{
			_currentState = ArbitrageState.OrderRegistration;
			if (arbitrageSignal == ArbitrageState.Backvordation)
			{
				var (buy, sell) = GenerateOrdersBackvardation();

				new IMarketRule[]
				{
					buy.WhenMatched(this), sell.WhenMatched(this),
					buy.WhenAllTrades(this), sell.WhenAllTrades(this),
				}
				.And()
				.Do(() =>
				{
					var futurePrise = buy.GetTrades(Connector).GetAveragePrice();
					var stockPrise = sell.GetTrades(Connector).GetAveragePrice() * StockMultiplicator;

					_enterSpread = stockPrise - futurePrise;

					_arbitragePnl = () =>
					{
						return stockPrise - _stAsk + _futBid - futurePrise;
					};
					_currentState = ArbitrageState.Backvordation;

				}).Once().Apply(this);

				RegisterOrder(buy);
				RegisterOrder(sell);
			}
			else
			{
				var (sell, buy) = GenerateOrdersContango();

				new IMarketRule[]
				{
					sell.WhenMatched(this), buy.WhenMatched(this),
					sell.WhenAllTrades(this), buy.WhenAllTrades(this),
				}
				.And()
				.Do(() =>
				{
					var futurePrise = sell.GetTrades(Connector).GetAveragePrice();
					var stockPrise = buy.GetTrades(Connector).GetAveragePrice() * StockMultiplicator;

					_enterSpread = futurePrise - stockPrise;

					_arbitragePnl = () =>
					{
						return futurePrise - _futAck + _stBid - stockPrise;
					};
					_currentState = ArbitrageState.Contango;

				}).Once().Apply(this);

				RegisterOrder(sell);
				RegisterOrder(buy);
			}
		}
		else
		if (_currentState == ArbitrageState.Backvordation && _profit >= ProfitToExit)
		{
			_currentState = ArbitrageState.OrderRegistration;

			var (sell, buy) = GenerateOrdersContango();

			new IMarketRule[]
			{
				sell.WhenMatched(this), buy.WhenMatched(this),
			}
			.And()
			.Do(() =>
			{

				_enterSpread = 0;
				_arbitragePnl = null;
				_currentState = ArbitrageState.None;

			}).Once().Apply(this);

			RegisterOrder(sell);
			RegisterOrder(buy);
		}
		else
		if (_currentState == ArbitrageState.Contango && _profit >= ProfitToExit)
		{
			_currentState = ArbitrageState.OrderRegistration;

			var (buy, sell) = GenerateOrdersBackvardation();

			new IMarketRule[]
			{
				buy.WhenMatched(this), sell.WhenMatched(this),
			}
			.And()
			.Do(() =>
			{

				_enterSpread = 0;
				_arbitragePnl = null;
				_currentState = ArbitrageState.None;

			}).Once().Apply(this);

			RegisterOrder(buy);
			RegisterOrder(sell);
		}
	}

	private (Order buy, Order sell) GenerateOrdersBackvardation()
	{
		var o1 = this.CreateOrder(Sides.Buy, FutureVolume);
		o1.Portfolio = FuturePortfolio;
		o1.Security = FutureSecurity;
		o1.Type = OrderTypes.Market;

		var o2 = this.CreateOrder(Sides.Sell, FutureVolume);
		o2.Portfolio = StockPortfolio;
		o2.Security = StockSecurity;
		o2.Type = OrderTypes.Market;

		return new(o1, o2);
	}
	private (Order sell, Order buy) GenerateOrdersContango()
	{
		var o1 = this.CreateOrder(Sides.Sell, FutureVolume);
		o1.Portfolio = FuturePortfolio;
		o1.Security = FutureSecurity;
		o1.Type = OrderTypes.Market;

		var o2 = this.CreateOrder(Sides.Buy, FutureVolume);
		o2.Portfolio = StockPortfolio;
		o2.Security = StockSecurity;
		o2.Type = OrderTypes.Market;

		return new(o1, o2);
	}

	private static decimal GetAveragePrice(IOrderBookMessage depth, Sides orderDirection, decimal volume)
	{
		if (!depth.Bids.Any() || !depth.Asks.Any()) return 0;

		var q = orderDirection == Sides.Buy ? depth.Asks : depth.Bids;

		var listQuots = new List<QuoteChange>();

		decimal summVolume = 0;

		foreach (var quote in q)
		{
			if (summVolume >= volume) break;
			var diffVolume = volume - summVolume;
			if (quote.Volume <= diffVolume)
			{
				listQuots.Add(quote);
				summVolume += quote.Volume;
			}
			else
			{
				listQuots.Add(new QuoteChange { Price = quote.Price, Volume = diffVolume });
				summVolume += diffVolume;
			}
		}
		var sum = listQuots.Sum(s => s.Volume);
		return listQuots.Sum(s => s.Price * s.Volume) / sum;
	}
}