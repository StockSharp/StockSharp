namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;

	using Ecng.Collections;

	using StockSharp.Logging;
	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter build order book and tick data from order log flow.
	/// </summary>
	public class OrderLogMessageAdapter : MessageAdapterWrapper
	{
		private readonly Dictionary<SecurityId, IOrderLogMarketDepthBuilder> _depthBuilders = new Dictionary<SecurityId, IOrderLogMarketDepthBuilder>();
		private readonly HashSet<SecurityId> _tickBuilders = new HashSet<SecurityId>();
		private readonly Dictionary<long, Tuple<SecurityId, MarketDataTypes>> _subscriptionIds = new Dictionary<long, Tuple<SecurityId, MarketDataTypes>>();

		/// <summary>
		/// Initializes a new instance of the <see cref="OrderLogMessageAdapter"/>.
		/// </summary>
		/// <param name="innerAdapter">Underlying adapter.</param>
		public OrderLogMessageAdapter(IMessageAdapter innerAdapter)
			: base(innerAdapter)
		{
		}

		/// <inheritdoc />
		public override void SendInMessage(Message message)
		{
			switch (message.Type)
			{
				case MessageTypes.Reset:
				{
					_depthBuilders.Clear();
					_tickBuilders.Clear();
					_subscriptionIds.Clear();
					break;
				}

				case MessageTypes.MarketData:
					if (ProcessMarketDataRequest((MarketDataMessage)message))
						return;

					break;
			}

			base.SendInMessage(message);
		}

		private bool ProcessMarketDataRequest(MarketDataMessage message)
		{
			if (message.IsSubscribe)
			{
				if (!InnerAdapter.IsMarketDataTypeSupported(MarketDataTypes.OrderLog))
					return false;

				var isBuild = message.BuildMode == MarketDataBuildModes.Build && message.BuildFrom == MarketDataTypes.OrderLog;
					
				switch (message.DataType)
				{
					case MarketDataTypes.MarketDepth:
					{
						if (isBuild || !InnerAdapter.IsMarketDataTypeSupported(message.DataType))
						{
							var secId = GetSecurityId(message.SecurityId);

							if (InnerAdapter.IsSupportSubscriptionBySecurity)
								_depthBuilders.Add(secId, InnerAdapter.CreateOrderLogMarketDepthBuilder(secId));
							else
								_depthBuilders.TryAdd(secId, (IOrderLogMarketDepthBuilder)null);

							_subscriptionIds.Add(message.TransactionId, Tuple.Create(secId, message.DataType));

							var clone = (MarketDataMessage)message.Clone();
							clone.DataType = MarketDataTypes.OrderLog;
							base.SendInMessage(clone);

							this.AddInfoLog("OL->MD subscribed {0}.", secId);

							return true;
						}

						break;
					}

					case MarketDataTypes.Trades:
					{
						if (isBuild || !InnerAdapter.IsMarketDataTypeSupported(message.DataType))
						{
							var secId = GetSecurityId(message.SecurityId);

							_tickBuilders.Add(secId);
							_subscriptionIds.Add(message.TransactionId, Tuple.Create(secId, message.DataType));

							var clone = (MarketDataMessage)message.Clone();
							clone.DataType = MarketDataTypes.OrderLog;
							base.SendInMessage(clone);

							this.AddInfoLog("OL->TICK subscribed {0}.", secId);

							return true;
						}

						break;
					}
				}
			}
			else
			{
				if (!_subscriptionIds.TryGetValue(message.OriginalTransactionId, out var tuple))
					return false;

				var secId = tuple.Item1;

				if (tuple.Item2 == MarketDataTypes.MarketDepth)
				{
					_depthBuilders.Remove(secId);
					this.AddInfoLog("OL->MD unsubscribed {0}.", secId);
				}
				else
				{
					_tickBuilders.Remove(secId);
					this.AddInfoLog("OL->TICK unsubscribed {0}.", secId);
				}
			}

			return false;
		}

		/// <inheritdoc />
		protected override void OnInnerAdapterNewOutMessage(Message message)
		{
			base.OnInnerAdapterNewOutMessage(message);

			switch (message.Type)
			{
				case MessageTypes.Execution:
				{
					var execMsg = (ExecutionMessage)message;

					if (execMsg.ExecutionType == ExecutionTypes.OrderLog)
						ProcessBuilders(execMsg);

					break;
				}
			}
		}

		private SecurityId GetSecurityId(SecurityId securityId)
		{
			return InnerAdapter.IsSupportSubscriptionBySecurity
				? securityId
				: default(SecurityId);
		}

		private void ProcessBuilders(ExecutionMessage execMsg)
		{
			if (execMsg.IsSystem == false)
				return;

			var secId = GetSecurityId(execMsg.SecurityId);

			var depthBuilder = InnerAdapter.IsSupportSubscriptionBySecurity
				? _depthBuilders.TryGetValue(execMsg.SecurityId)
				: _depthBuilders.ContainsKey(secId) ? _depthBuilders.SafeAdd(execMsg.SecurityId, key => new OrderLogMarketDepthBuilder(key)) : null;

			if (depthBuilder != null)
			{
				try
				{
					var updated = depthBuilder.Update(execMsg);

					this.AddDebugLog("OL->MD processing {0}={1}.", execMsg.SecurityId, updated);

					if (updated)
					{
						base.OnInnerAdapterNewOutMessage(depthBuilder.Depth.Clone());
					}
				}
				catch (Exception ex)
				{
					// если ОЛ поврежден, то не нарушаем весь цикл обработки сообщения
					// а только выводим сообщение в лог
					base.OnInnerAdapterNewOutMessage(new ErrorMessage { Error = ex });
				}
			}
			else
				this.AddDebugLog("OL->MD processing {0} not found.", execMsg.SecurityId);

			if (_tickBuilders.Contains(secId))
			{
				this.AddDebugLog("OL->TICK processing {0}.", secId);
				base.OnInnerAdapterNewOutMessage(execMsg.ToTick());
			}
		}

		/// <summary>
		/// Create a copy of <see cref="OrderLogMessageAdapter"/>.
		/// </summary>
		/// <returns>Copy.</returns>
		public override IMessageChannel Clone()
		{
			return new OrderLogMessageAdapter((IMessageAdapter)InnerAdapter.Clone());
		}
	}
}