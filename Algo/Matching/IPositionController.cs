namespace StockSharp.Algo.Matching
{
	using System;
	using System.Collections.Generic;

	using Ecng.Common;
	using Ecng.Collections;

	using StockSharp.Messages;
	using StockSharp.Localization;
	using StockSharp.Algo.PnL;
	using StockSharp.Algo.Commissions;

	/// <summary>
	/// Interface described position controller.
	/// </summary>
	public interface IPositionController
	{
		/// <summary>
		/// The profit-loss manager, related for specified <see cref="PortfolioMessage.PortfolioName"/>.
		/// </summary>
		PortfolioPnLManager PnLManager { get; }

		/// <summary>
		/// Reqest positions states.
		/// </summary>
		/// <param name="lookupMsg">Message portfolio lookup for specified criteria.</param>
		/// <param name="result">Result messages.</param>
		void RequestState(PortfolioLookupMessage lookupMsg, Action<Message> result);

		/// <summary>
		/// Update position state.
		/// </summary>
		/// <param name="posMsg">The message contains information about the position changes.</param>
		/// <param name="result">Result messages.</param>
		void Update(PositionChangeMessage posMsg, Action<Message> result);

		/// <summary>
		/// Request margin state.
		/// </summary>
		/// <param name="time">Time.</param>
		/// <param name="securityId">Security ID.</param>
		/// <param name="result">Result messages.</param>
		void RequestMarginState(DateTimeOffset time, SecurityId securityId, Action<Message> result);

		/// <summary>
		/// Process order.
		/// </summary>
		/// <param name="securityId">Security ID.</param>
		/// <param name="side">Side.</param>
		/// <param name="volumeDelta">Volume delta.</param>
		/// <param name="orderMsg">The message contains information about the execution.</param>
		/// <param name="result">Result messages.</param>
		/// <returns>Commission.</returns>
		decimal? ProcessOrder(SecurityId securityId, Sides side, decimal volumeDelta, ExecutionMessage orderMsg, Action<Message> result);

		/// <summary>
		/// Process own trade.
		/// </summary>
		/// <param name="side">Side.</param>
		/// <param name="tradeMsg">The message contains information about the execution.</param>
		/// <param name="result">Result messages.</param>
		void ProcessMyTrade(Sides side, ExecutionMessage tradeMsg, Action<Message> result);

		/// <summary>
		/// Validate registration.
		/// </summary>
		/// <param name="regMsg">The message containing the information for the order registration.</param>
		/// <returns><see lagnword="null"/> is not error. Otherwise, error message.</returns>
		string ValidateRegistration(OrderRegisterMessage regMsg);

		/// <summary>
		/// Request portfolio state.
		/// </summary>
		/// <param name="time">Time.</param>
		/// <param name="result">Result messages.</param>
		void RequestPortfolioState(DateTimeOffset time, Action<Message> result);
	}

	/// <summary>
	/// Default implementation of <see cref="IPositionController"/>.
	/// </summary>
	public class PositionController : IPositionController
	{
		private class MoneyInfo
		{
			private readonly SecurityId _securityId;
			private readonly Func<SecurityId, Sides, decimal> _getMarginPrice;

			public MoneyInfo(SecurityId securityId, Func<SecurityId, Sides, decimal> getMarginPrice)
			{
				_securityId = securityId;
				_getMarginPrice = getMarginPrice ?? throw new ArgumentNullException(nameof(getMarginPrice));
			}

			public decimal PositionBeginValue;
			public decimal PositionDiff;
			public decimal PositionCurrentValue => PositionBeginValue + PositionDiff;

			public decimal PositionAveragePrice;

			public decimal PositionPrice
			{
				get
				{
					var pos = PositionCurrentValue;

					if (pos == 0)
						return 0;

					return pos.Abs() * PositionAveragePrice;
				}
			}

			public decimal TotalPrice => GetPrice(0, 0);

			public decimal GetPrice(decimal buyVol, decimal sellVol)
			{
				var totalMoney = PositionPrice;

				var buyOrderPrice = (TotalBidsVolume + buyVol) * _getMarginPrice(_securityId, Sides.Buy);
				var sellOrderPrice = (TotalAsksVolume + sellVol) * _getMarginPrice(_securityId, Sides.Sell);

				if (totalMoney != 0)
				{
					if (PositionCurrentValue > 0)
					{
						totalMoney += buyOrderPrice;
						totalMoney = totalMoney.Max(sellOrderPrice);
					}
					else
					{
						totalMoney += sellOrderPrice;
						totalMoney = totalMoney.Max(buyOrderPrice);
					}
				}
				else
				{
					totalMoney = buyOrderPrice + sellOrderPrice;
				}

				return totalMoney;
			}

			public decimal TotalBidsVolume;
			public decimal TotalAsksVolume;
		}

		private readonly ICommissionManager _commissionManager;
		private readonly Func<SecurityId, SecurityMessage> _getSecurityDefinition;
		private readonly Func<SecurityId, Sides, decimal> _getMarginPrice;
		private readonly string _portfolioName;
		private readonly Dictionary<SecurityId, MoneyInfo> _moneys = new Dictionary<SecurityId, MoneyInfo>();

		private decimal _beginMoney;
		private decimal _currentMoney;

		private decimal _totalBlockedMoney;

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionController"/>.
		/// </summary>
		/// <param name="portfolioName">Portfolio name.</param>
		/// <param name="commissionManager">Commission manager.</param>
		/// <param name="getSecurityDefinition">Handler to get security info.</param>
		/// <param name="getMarginPrice">Handler to get margin info.</param>
		public PositionController(string portfolioName, ICommissionManager commissionManager, Func<SecurityId, SecurityMessage> getSecurityDefinition, Func<SecurityId, Sides, decimal> getMarginPrice)
		{
			if (portfolioName.IsEmpty())
				throw new ArgumentNullException(nameof(portfolioName));

			_portfolioName = portfolioName;
			
			_commissionManager = commissionManager ?? throw new ArgumentNullException(nameof(commissionManager));
			_getSecurityDefinition = getSecurityDefinition ?? throw new ArgumentNullException(nameof(getSecurityDefinition));
			_getMarginPrice = getMarginPrice ?? throw new ArgumentNullException(nameof(getMarginPrice));

			PnLManager = new PortfolioPnLManager(portfolioName);
		}

		/// <inheritdoc />
		public PortfolioPnLManager PnLManager { get; }

		/// <summary>
		/// Check money balance.
		/// </summary>
		public bool CheckMoney { get; set; }

		/// <summary>
		/// Can have short positions.
		/// </summary>
		public bool CheckShortable { get; set; }

		/// <inheritdoc />
		public void RequestState(PortfolioLookupMessage lookupMsg, Action<Message> result)
		{
			var time = lookupMsg.LocalTime;

			RequestPortfolioState(time, result);

			foreach (var pair in _moneys)
			{
				var money = pair.Value;

				result(
					new PositionChangeMessage
					{
						LocalTime = time,
						ServerTime = time,
						PortfolioName = _portfolioName,
						SecurityId = pair.Key,
						OriginalTransactionId = lookupMsg.TransactionId,
					}
					.Add(PositionChangeTypes.CurrentValue, money.PositionCurrentValue)
					.TryAdd(PositionChangeTypes.AveragePrice, money.PositionAveragePrice)
				);
			}
		}

		/// <inheritdoc />
		public void Update(PositionChangeMessage posMsg, Action<Message> result)
		{
			var beginValue = posMsg.TryGetDecimal(PositionChangeTypes.BeginValue);

			if (posMsg.IsMoney())
			{
				if (beginValue == null)
					return;

				_currentMoney = _beginMoney = (decimal)beginValue;

				RequestPortfolioState(posMsg.ServerTime, result);
				return;
			}

			//if (!_moneys.ContainsKey(posMsg.SecurityId))
			//{
			//	result(new PositionMessage
			//	{
			//		SecurityId = posMsg.SecurityId,
			//		PortfolioName = posMsg.PortfolioName,
			//		DepoName = posMsg.DepoName,
			//		LocalTime = posMsg.LocalTime
			//	});
			//}

			var money = GetMoney(posMsg.SecurityId/*, posMsg.LocalTime, result*/);

			var prevPrice = money.PositionPrice;

			money.PositionBeginValue = beginValue ?? 0L;
			money.PositionAveragePrice = posMsg.TryGetDecimal(PositionChangeTypes.AveragePrice) ?? money.PositionAveragePrice;

			//if (beginValue == 0m)
			//	return;

			result(posMsg.Clone());

			_totalBlockedMoney = _totalBlockedMoney - prevPrice + money.PositionPrice;

			result(
				new PositionChangeMessage
				{
					SecurityId = SecurityId.Money,
					ServerTime = posMsg.ServerTime,
					LocalTime = posMsg.LocalTime,
					PortfolioName = _portfolioName,
				}.Add(PositionChangeTypes.BlockedValue, _totalBlockedMoney)
			);
		}

		private MoneyInfo GetMoney(SecurityId securityId/*, DateTimeOffset time, Action<Message> result*/)
		{
			//bool isNew;
			var money = _moneys.SafeAdd(securityId, k => new MoneyInfo(k, _getMarginPrice));

			//if (isNew)
			//{
			//	result(new PositionMessage
			//	{
			//		LocalTime = time,
			//		PortfolioName = _portfolioName,
			//		SecurityId = securityId,
			//	});
			//}

			return money;
		}

		/// <inheritdoc />
		public decimal? ProcessOrder(SecurityId securityId, Sides side, decimal volumeDelta, ExecutionMessage orderMsg, Action<Message> result)
		{
			var money = GetMoney(securityId/*, orderMsg.LocalTime, result*/);

			var prevPrice = money.TotalPrice;

			if (side == Sides.Buy)
				money.TotalBidsVolume += volumeDelta;
			else
				money.TotalAsksVolume += volumeDelta;

			_totalBlockedMoney = _totalBlockedMoney - prevPrice + money.TotalPrice;

			var commission = _commissionManager.Process(orderMsg);

			RequestPortfolioState(orderMsg.ServerTime, result);

			return commission;
		}

		/// <inheritdoc />
		public void ProcessMyTrade(Sides side, ExecutionMessage tradeMsg, Action<Message> result)
		{
			var time = tradeMsg.ServerTime;

			PnLManager.ProcessMyTrade(tradeMsg, out _);
			tradeMsg.Commission = _commissionManager.Process(tradeMsg);

			var position = tradeMsg.TradeVolume;

			if (position == null)
				return;

			if (side == Sides.Sell)
				position *= -1;

			var money = GetMoney(tradeMsg.SecurityId/*, time, result*/);

			var prevPrice = money.TotalPrice;

			var tradeVol = tradeMsg.TradeVolume.Value;

			if (tradeMsg.Side == Sides.Buy)
				money.TotalBidsVolume -= tradeVol;
			else
				money.TotalAsksVolume -= tradeVol;

			var prevPos = money.PositionCurrentValue;

			money.PositionDiff += position.Value;

			var tradePrice = tradeMsg.TradePrice.Value;
			var currPos = money.PositionCurrentValue;

			if (prevPos.Sign() == currPos.Sign())
				money.PositionAveragePrice = (money.PositionAveragePrice * prevPos + position.Value * tradePrice) / currPos;
			else
				money.PositionAveragePrice = currPos == 0 ? 0 : tradePrice;

			_totalBlockedMoney = _totalBlockedMoney - prevPrice + money.TotalPrice;

			result(
				new PositionChangeMessage
				{
					LocalTime = time,
					ServerTime = time,
					PortfolioName = _portfolioName,
					SecurityId = tradeMsg.SecurityId,
				}
				.Add(PositionChangeTypes.CurrentValue, money.PositionCurrentValue)
				.TryAdd(PositionChangeTypes.AveragePrice, money.PositionAveragePrice)
			);

			RequestPortfolioState(time, result);
		}

		/// <inheritdoc />
		public void RequestMarginState(DateTimeOffset time, SecurityId securityId, Action<Message> result)
		{
			var money = _moneys.TryGetValue(securityId);

			if (money == null)
				return;

			_totalBlockedMoney = 0;

			foreach (var pair in _moneys)
				_totalBlockedMoney += pair.Value.TotalPrice;

			result(
				new PositionChangeMessage
				{
					SecurityId = SecurityId.Money,
					ServerTime = time,
					LocalTime = time,
					PortfolioName = _portfolioName,
				}.Add(PositionChangeTypes.BlockedValue, _totalBlockedMoney)
			);
		}

		/// <inheritdoc />
		public void RequestPortfolioState(DateTimeOffset time, Action<Message> result)
		{
			var realizedPnL = PnLManager.RealizedPnL;
			var unrealizedPnL = PnLManager.UnrealizedPnL;
			var commission = _commissionManager.Commission;
			var totalPnL = PnLManager.PnL - commission;

			try
			{
				_currentMoney = _beginMoney + totalPnL;
			}
			catch (OverflowException ex)
			{
				result(ex.ToErrorMessage());
			}

			result(new PositionChangeMessage
			{
				SecurityId = SecurityId.Money,
				ServerTime = time,
				LocalTime = time,
				PortfolioName = _portfolioName,
			}
			.Add(PositionChangeTypes.RealizedPnL, realizedPnL)
			.TryAdd(PositionChangeTypes.UnrealizedPnL, unrealizedPnL, true)
			.Add(PositionChangeTypes.VariationMargin, totalPnL)
			.Add(PositionChangeTypes.CurrentValue, _currentMoney)
			.Add(PositionChangeTypes.BlockedValue, _totalBlockedMoney)
			.Add(PositionChangeTypes.Commission, commission));
		}

		/// <inheritdoc />
		public string ValidateRegistration(OrderRegisterMessage regMsg)
		{
			if (CheckMoney)
			{
				// если задан баланс, то проверям по нему (для частично исполненных заявок)
				var volume = (regMsg as OrderReplaceMessage)?.OldOrderVolume ?? regMsg.Volume;

				var money = GetMoney(regMsg.SecurityId/*, execMsg.LocalTime, result*/);

				var needBlock = money.GetPrice(regMsg.Side == Sides.Buy ? volume : 0, regMsg.Side == Sides.Sell ? volume : 0);

				if (_currentMoney < needBlock)
				{
					return LocalizedStrings
						.Str1169Params
						.Put(regMsg.PortfolioName, regMsg.TransactionId, needBlock, _currentMoney, money.TotalPrice);
				}
			}
			else if (CheckShortable && regMsg.Side == Sides.Sell)
			{
				var secDef = _getSecurityDefinition(regMsg.SecurityId);

				if (secDef?.Shortable == false)
				{
					var money = GetMoney(regMsg.SecurityId/*, execMsg.LocalTime, result*/);

					var potentialPosition = money.PositionCurrentValue - regMsg.Volume;

					if (potentialPosition < 0)
					{
						return LocalizedStrings
							.CannotShortPosition
							.Put(regMsg.PortfolioName, regMsg.TransactionId, money.PositionCurrentValue, regMsg.Volume);
					}
				}
			}

			return null;
		}
	}
}