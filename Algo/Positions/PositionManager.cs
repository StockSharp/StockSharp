#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Positions.Algo
File: PositionManager.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Positions
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The position calculation manager.
	/// </summary>
	public class PositionManager : BaseLogReceiver, IPositionManager
	{
		private class OrderInfo
		{
			public OrderInfo(long transactionId, SecurityId securityId, string portfolioName, Sides side, decimal volume, decimal balance)
			{
				TransactionId = transactionId;
				SecurityId = securityId;
				PortfolioName = portfolioName;
				Side = side;
				Volume = volume;
				Balance = balance;
			}

			public long TransactionId { get; }
			public SecurityId SecurityId { get; }
			public string PortfolioName { get; }
			public Sides Side { get; }
			public decimal Volume { get; }
			public decimal Balance { get; set; }

			public override string ToString() => $"{TransactionId}: {Balance}/{Volume}";
		}

		private class PositionInfo
		{
			public decimal Value { get; set; }

			public override string ToString() => Value.ToString();
		}

		private readonly Dictionary<long, OrderInfo> _ordersInfo = new Dictionary<long, OrderInfo>();
		private readonly Dictionary<Tuple<SecurityId, string>, PositionInfo> _positions = new Dictionary<Tuple<SecurityId, string>, PositionInfo>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionManager"/>.
		/// </summary>
		/// <param name="byOrders">To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).</param>
		public PositionManager(bool byOrders)
		{
			ByOrders = byOrders;
		}

		/// <summary>
		/// To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).
		/// </summary>
		public bool ByOrders { get; }

		/// <inheritdoc />
		public virtual PositionChangeMessage ProcessMessage(Message message)
		{
			static Tuple<SecurityId, string> CreateKey(SecurityId secId, string pf)
				=> Tuple.Create(secId, pf.ToLowerInvariant());

			static Tuple<SecurityId, string> CreateKey2<TMessage>(TMessage message)
				where TMessage : Message, ISecurityIdMessage, IPortfolioNameMessage
				=> CreateKey(message.SecurityId, message.PortfolioName);

			OrderInfo EnsureGetInfo<TMessage>(TMessage msg, Sides side, decimal volume, decimal balance)
				where TMessage : Message, ITransactionIdMessage, ISecurityIdMessage, IPortfolioNameMessage
			{
				this.AddDebugLog("{0} bal_new {1}/{2}.", msg.TransactionId, balance, volume);
				return _ordersInfo.SafeAdd(msg.TransactionId, key => new OrderInfo(key, msg.SecurityId, msg.PortfolioName, side, volume, balance));
			}

			void ProcessRegOrder(OrderRegisterMessage regMsg)
				=> EnsureGetInfo(regMsg, regMsg.Side, regMsg.Volume, regMsg.Volume);

			PositionChangeMessage UpdatePositions(SecurityId secId, string portfolioName, decimal diff, DateTimeOffset time)
			{
				var position = _positions.SafeAdd(CreateKey(secId, portfolioName));
				position.Value += diff;

				return new PositionChangeMessage
				{
					SecurityId = secId,
					PortfolioName = portfolioName,
					ServerTime = time,
					BuildFrom = DataType.Transactions,
				}.Add(PositionChangeTypes.CurrentValue, position.Value);
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_ordersInfo.Clear();
					_positions.Clear();

					break;
				}

				case MessageTypes.OrderRegister:
				case MessageTypes.OrderReplace:
				{
					ProcessRegOrder((OrderRegisterMessage)message);
					break;
				}

				case MessageTypes.OrderPairReplace:
				{
					var pairMsg = (OrderPairReplaceMessage)message;

					ProcessRegOrder(pairMsg.Message1);
					ProcessRegOrder(pairMsg.Message2);

					break;
				}

				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.IsMarketData())
						break;

					var isOrderInfo = execMsg.HasOrderInfo();

					var info =
						isOrderInfo
							? execMsg.TransactionId != 0
								? EnsureGetInfo(execMsg, execMsg.Side, execMsg.OrderVolume ?? 0, execMsg.Balance ?? 0)
								: _ordersInfo.TryGetValue(execMsg.OriginalTransactionId)
							: execMsg.OriginalTransactionId != 0 && execMsg.HasTradeInfo()
								? _ordersInfo.TryGetValue(execMsg.OriginalTransactionId)
								: null;

					var canUpdateOrder = isOrderInfo && info != null;

					decimal? balDiff = null;

					if (canUpdateOrder)
					{
						var oldBalance = execMsg.TransactionId != 0 ? execMsg.OrderVolume : info.Balance;
						balDiff = oldBalance - execMsg.Balance;
						if (balDiff.HasValue && balDiff != 0)
						{
							// ReSharper disable once PossibleInvalidOperationException
							info.Balance = execMsg.Balance.Value;
							this.AddDebugLog("{0} bal_upd {1}/{2}.", info.TransactionId, info.Balance, info.Volume);
						}
					}

					if (ByOrders)
					{
						if (!canUpdateOrder)
							break;

						if (balDiff.HasValue && balDiff != 0)
						{
							var posDiff = info.Side == Sides.Buy ? balDiff.Value : -balDiff.Value;
							return UpdatePositions(info.SecurityId, info.PortfolioName, posDiff, execMsg.ServerTime);
						}
					}
					else
					{
						if (!execMsg.HasTradeInfo())
							break;

						var tradeVol = execMsg.TradeVolume;

						if (tradeVol == null)
							break;

						if (tradeVol == 0)
						{
							this.AddWarningLog("Trade {0}/{1} of order {2} has zero volume.", execMsg.TradeId, execMsg.TradeStringId, execMsg.OriginalTransactionId);
							break;
						}

						if (execMsg.Side == Sides.Sell)
							tradeVol = -tradeVol;

						var secId = info?.SecurityId ?? execMsg.SecurityId;
						var portfolioName = info?.PortfolioName ?? execMsg.PortfolioName;

						return UpdatePositions(secId, portfolioName, tradeVol.Value, execMsg.ServerTime);
					}

					break;
				}

				case MessageTypes.PositionChange:
				{
					var posMsg = (PositionChangeMessage)message;

					if (posMsg.Changes.TryGetValue(PositionChangeTypes.CurrentValue, out var curr))
						_positions.SafeAdd(CreateKey2(posMsg)).Value = (decimal)curr;

					break;
				}
			}

			return null;
		}
	}
}