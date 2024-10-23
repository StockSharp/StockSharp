﻿namespace StockSharp.Algo.Strategies;

using StockSharp.Algo.Positions;

/// <summary>
/// The position calculation manager.
/// </summary>
public class StrategyPositionManager : BaseLogReceiver, IPositionManager
{
	private readonly Dictionary<string, IPositionManager> _managersByStrategyId = new(StringComparer.InvariantCultureIgnoreCase);
	private readonly Dictionary<long, Tuple<IPositionManager, string>> _managersByTransId = [];

	/// <summary>
	/// Initializes a new instance of the <see cref="StrategyPositionManager"/>.
	/// </summary>
	/// <param name="byOrders">To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).</param>
	public StrategyPositionManager(bool byOrders)
	{
		ByOrders = byOrders;
	}

	/// <summary>
	/// To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).
	/// </summary>
	public bool ByOrders { get; }

	/// <inheritdoc />
	public PositionChangeMessage ProcessMessage(Message message)
	{
		IPositionManager CreateManager(long transId, string strategyId)
		{
			var manager = _managersByStrategyId.SafeAdd(strategyId, _ => new PositionManager(ByOrders) { Parent = this });
			_managersByTransId.SafeAdd(transId, _ => Tuple.Create(manager, strategyId));
			return manager;
		}

		void ProcessRegOrder(OrderRegisterMessage regMsg)
		{
			if (regMsg.StrategyId.IsEmpty())
				return;

			CreateManager(regMsg.TransactionId, regMsg.StrategyId).ProcessMessage(message);
		}

		switch (message.Type)
		{
			case MessageTypes.Reset:
			{
				_managersByStrategyId.Clear();
				_managersByTransId.Clear();

				break;
			}

			case MessageTypes.OrderRegister:
			case MessageTypes.OrderReplace:
			{
				ProcessRegOrder((OrderRegisterMessage)message);
				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.IsMarketData())
					break;

				string strategyId = null;
				IPositionManager manager = null;

				if (execMsg.TransactionId == 0)
				{
					if (_managersByTransId.TryGetValue(execMsg.OriginalTransactionId, out var tuple))
					{
						manager = tuple.Item1;
						strategyId = tuple.Item2;
					}
				}
				else
				{
					if (!execMsg.StrategyId.IsEmpty())
					{
						strategyId = execMsg.StrategyId;
						manager = CreateManager(execMsg.TransactionId, strategyId);
					}
				}

				if (manager == null)
					break;

				var change = manager.ProcessMessage(message);

				if (change != null)
					change.StrategyId = strategyId;

				return change;
			}
			case MessageTypes.PositionChange:
			{
				var posMsg = (PositionChangeMessage)message;

				if (posMsg.StrategyId.IsEmpty())
					break;

				_managersByStrategyId
					.SafeAdd(posMsg.StrategyId, key => new PositionManager(ByOrders) { Parent = this })
					.ProcessMessage(posMsg);

				break;
			}
		}

		return null;
	}
}