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
	using System.Linq;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The position calculation manager.
	/// </summary>
	public class PositionManager : IPositionManager
	{
		private readonly ILogReceiver _logs;
		private readonly Dictionary<long, RefTriple<Sides, decimal, decimal>> _ordersInfo = new Dictionary<long, RefTriple<Sides, decimal, decimal>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="PositionManager"/>.
		/// </summary>
		/// <param name="logs">Logs.</param>
		/// <param name="byOrders">To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).</param>
		public PositionManager(ILogReceiver logs, bool byOrders = true)
		{
			_logs = logs ?? throw new ArgumentNullException(nameof(logs));
			ByOrders = byOrders;
		}

		/// <summary>
		/// To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).
		/// </summary>
		public bool ByOrders { get; }

		private SecurityId? _securityId;

		/// <inheritdoc />
		public SecurityId? SecurityId
		{
			get => _securityId;
			set
			{
				_securityId = value;

				lock (_positions.SyncRoot)
				{
					UpdatePositionValue(_positions);
				}
			}
		}

		/// <inheritdoc />
		public decimal Position { get; set; }

		private readonly CachedSynchronizedDictionary<Tuple<SecurityId, string>, decimal> _positions = new CachedSynchronizedDictionary<Tuple<SecurityId, string>, decimal>();

		/// <inheritdoc />
		public IEnumerable<KeyValuePair<Tuple<SecurityId, string>, decimal>> Positions
		{
			get => _positions.CachedPairs;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				lock (_positions.SyncRoot)
				{
					_ordersInfo.Clear();

					_positions.Clear();
					_positions.AddRange(value);

					UpdatePositionValue(value);
				}
			}
		}

		private void UpdatePositionValue(IEnumerable<KeyValuePair<Tuple<SecurityId, string>, decimal>> positions)
		{
			var secId = SecurityId;

			Position = secId == null
				? positions.Sum(p => p.Value)
				: positions.Where(p => p.Key.Item1 == secId.Value).Sum(p => p.Value);
		}

		/// <inheritdoc />
		public event Action<Tuple<SecurityId, string>, decimal> NewPosition;

		/// <inheritdoc />
		public event Action<Tuple<SecurityId, string>, decimal> PositionChanged;

		/// <inheritdoc />
		public decimal? ProcessMessage(Message message)
		{
			void ProcessRegOrder(OrderRegisterMessage regMsg)
			{
				_ordersInfo.Add(regMsg.TransactionId, RefTuple.Create(regMsg.Side, regMsg.Volume, regMsg.Volume));
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					Positions = Enumerable.Empty<KeyValuePair<Tuple<SecurityId, string>, decimal>>();
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
					var key = Tuple.Create(execMsg.SecurityId, execMsg.PortfolioName);

					if (execMsg.HasOrderInfo())
					{
						if (execMsg.TransactionId != 0)
						{
							_ordersInfo.Add(execMsg.TransactionId, RefTuple.Create(execMsg.Side, execMsg.OrderVolume ?? 0, execMsg.Balance ?? 0));

							if (ByOrders)
							{
								var pos = (execMsg.Side == Sides.Buy ? 1 : -1) * (execMsg.OrderVolume - execMsg.Balance);

								if (pos != null && pos != 0)
								{

								}
							}

							return null;
						}
						else
						{
							var balance = execMsg.Balance;

							if (balance == null)
								break;

							var transId = execMsg.OriginalTransactionId;

							if (!_ordersInfo.TryGetValue(transId, out var info))
								break;

							var balDiff = info.Third - balance.Value;

							if (balDiff > 0)
							{
								info.Third = balance.Value;

								if (ByOrders)
								{
									bool isNew;
									decimal diff;
									decimal abs;

									lock (_positions.SyncRoot)
									{
										isNew = !_positions.TryGetValue(key, out var position);

										diff = (info.First == Sides.Buy ? 1 : -1) * balDiff;

										position += diff;

										_positions[key] = position;

										if (SecurityId == null || SecurityId.Value == execMsg.SecurityId)
											Position += diff;

										abs = position;
									}

									if (isNew)
										NewPosition?.Invoke(key, abs);
									else
										PositionChanged?.Invoke(key, abs);

									return diff;
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

						if (info1.First == Sides.Sell)
							tradeVol *= -1;

						bool isNew;
						decimal abs;

						lock (_positions.SyncRoot)
						{
							isNew = !_positions.TryGetValue(key, out var prev);
							abs = prev + tradeVol.Value;

							_positions[key] = abs;

							if (SecurityId == null || SecurityId.Value == execMsg.SecurityId)
								Position += tradeVol.Value;
						}

						if (isNew)
							NewPosition?.Invoke(key, abs);
						else
							PositionChanged?.Invoke(key, abs);

						return tradeVol;
					}

					break;
				}
			}

			return null;
		}
	}
}