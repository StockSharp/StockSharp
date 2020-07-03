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
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The position calculation manager.
	/// </summary>
	public class PositionManager : IPositionManager
	{
		private class OrderInfo
		{
			public OrderInfo(SecurityId securityId, string portfolioName, Sides side, decimal volume, decimal balance)
			{
				SecurityId = securityId;
				PortfolioName = portfolioName;
				Side = side;
				Volume = volume;
				Balance = balance;
			}

			public SecurityId SecurityId { get; }
			public string PortfolioName { get; }
			public Sides Side { get; }
			public decimal Volume { get; }
			public decimal Balance { get; set; }
		}

		private class PositionInfo
		{
			public decimal Value { get; set; }
		}

		private readonly ILogReceiver _logs;

		private readonly SyncObject _sync = new SyncObject();

		private readonly Dictionary<long, OrderInfo> _ordersInfo = new Dictionary<long, OrderInfo>();
		private readonly Dictionary<Tuple<SecurityId, string>, PositionInfo> _positions = new Dictionary<Tuple<SecurityId, string>, PositionInfo>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionManager"/>.
		/// </summary>
		/// <param name="logs">Logs.</param>
		/// <param name="byOrders">To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).</param>
		public PositionManager(ILogReceiver logs, bool byOrders)
		{
			_logs = logs ?? throw new ArgumentNullException(nameof(logs));
			ByOrders = byOrders;
		}

		/// <summary>
		/// To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).
		/// </summary>
		public bool ByOrders { get; }

		/// <inheritdoc />
		public virtual PositionChangeMessage ProcessMessage(Message message)
		{
			void ProcessRegOrder(OrderRegisterMessage regMsg)
			{
				_ordersInfo.Add(regMsg.TransactionId, new OrderInfo(regMsg.SecurityId, regMsg.PortfolioName, regMsg.Side, regMsg.Volume, regMsg.Volume));
			}

			PositionChangeMessage UpdatePositions(OrderInfo info, decimal diff)
			{
				var secId = info.SecurityId;
				var pf = info.PortfolioName;

				var position = _positions.SafeAdd(Tuple.Create(secId, pf));
				position.Value += diff;

				return new PositionChangeMessage
				{
					SecurityId = secId,
					PortfolioName = pf,
				}.Add(PositionChangeTypes.CurrentValue, position.Value);
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					lock (_sync)
					{
						_ordersInfo.Clear();
						_positions.Clear();
					}

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

					lock (_sync)
					{
						if (execMsg.HasOrderInfo())
						{
							if (execMsg.TransactionId != 0)
							{
								var info = new OrderInfo(execMsg.SecurityId, execMsg.PortfolioName, execMsg.Side, execMsg.OrderVolume ?? 0, execMsg.Balance ?? 0);
								_ordersInfo.Add(execMsg.TransactionId, info);

								if (ByOrders)
								{
									var orderPos = (execMsg.Side == Sides.Buy ? 1 : -1) * (execMsg.OrderVolume - execMsg.Balance);

									if (orderPos != null && orderPos != 0)
										return UpdatePositions(info, orderPos.Value);
								}

								break;
							}
							else
							{
								var balance = execMsg.Balance;

								if (balance == null)
									break;

								var transId = execMsg.OriginalTransactionId;

								if (!_ordersInfo.TryGetValue(transId, out var info))
									break;

								var balDiff = info.Balance - balance.Value;

								if (balDiff > 0)
								{
									info.Balance = balance.Value;

									if (ByOrders)
									{
										var position = _positions.SafeAdd(Tuple.Create(execMsg.SecurityId, execMsg.PortfolioName));
										position.Value += balDiff;

										return UpdatePositions(info, balDiff);
									}
								}
							}
						}

						if (!ByOrders && execMsg.HasTradeInfo() && _ordersInfo.TryGetValue(execMsg.OriginalTransactionId, out var info1))
						{
							var tradeVol = execMsg.TradeVolume;

							if (tradeVol == null)
								break;
							else if (tradeVol == 0)
							{
								_logs.AddWarningLog("Trade {0}/{1} of order {2} has zero volume.", execMsg.TradeId, execMsg.TradeStringId, execMsg.OriginalTransactionId);
								break;
							}

							if (info1.Side == Sides.Sell)
								tradeVol *= -1;

							return UpdatePositions(info1, tradeVol.Value);
						}
					}

					break;
				}
			}

			return null;
		}
	}
}