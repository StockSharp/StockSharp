namespace StockSharp.Algo.Strategies
{
	using System;

	using Ecng.Collections;
	using Ecng.Common;

	using StockSharp.Algo.Positions;
	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The position calculation manager.
	/// </summary>
	public class StrategyPositionManager : IPositionManager
	{
		private readonly ILogReceiver _logs;

		private readonly SynchronizedDictionary<string, IPositionManager> _managers = new SynchronizedDictionary<string, IPositionManager>(StringComparer.InvariantCultureIgnoreCase);

		/// <summary>
		/// Initializes a new instance of the <see cref="StrategyPositionManager"/>.
		/// </summary>
		/// <param name="logs">Logs.</param>
		/// <param name="byOrders">To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).</param>
		public StrategyPositionManager(ILogReceiver logs, bool byOrders)
		{
			_logs = logs ?? throw new ArgumentNullException(nameof(logs));
			ByOrders = byOrders;
		}

		/// <summary>
		/// To calculate the position on realized volume for orders (<see langword="true" />) or by trades (<see langword="false" />).
		/// </summary>
		public bool ByOrders { get; }

		private IPositionManager EnsureGetManager(string strategyId)
			=> _managers.SafeAdd(strategyId, key => new PositionManager(_logs, ByOrders));

		/// <inheritdoc />
		public PositionChangeMessage ProcessMessage(Message message)
		{
			void ProcessRegOrder(OrderRegisterMessage regMsg)
			{
				if (!regMsg.StrategyId.IsEmpty())
					return;
				
				EnsureGetManager(regMsg.StrategyId).ProcessMessage(message);
			}

			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_managers.Clear();
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

					if (execMsg.StrategyId.IsEmpty())
						break;

					var change = EnsureGetManager(execMsg.StrategyId).ProcessMessage(message);

					if (change != null)
						change.StrategyId = execMsg.StrategyId;

					return change;
				}
			}

			return null;
		}
	}
}