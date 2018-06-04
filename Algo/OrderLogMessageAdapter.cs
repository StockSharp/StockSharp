namespace StockSharp.Algo
{
	using System;
	using System.Collections.Generic;
	
	using StockSharp.Messages;

	/// <summary>
	/// The messages adapter build orer book and tick data from order log flow.
	/// </summary>
	public class OrderLogMessageAdapter : MessageAdapterWrapper
	{
		private readonly Dictionary<SecurityId, IOrderLogMarketDepthBuilder> _olBuilders = new Dictionary<SecurityId, IOrderLogMarketDepthBuilder>();
		private readonly Dictionary<long, Tuple<SecurityId, MarketDataTypes>> _subscriptionIds = new Dictionary<long, Tuple<SecurityId, MarketDataTypes>>();
		private readonly HashSet<SecurityId> _tickBuilders = new HashSet<SecurityId>();

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
					_olBuilders.Clear();
					_subscriptionIds.Clear();
					_tickBuilders.Clear();
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
							_olBuilders.Add(message.SecurityId, InnerAdapter.CreateOrderLogMarketDepthBuilder(message.SecurityId));
							_subscriptionIds.Add(message.TransactionId, Tuple.Create(message.SecurityId, message.DataType));

							var clone = (MarketDataMessage)message.Clone();
							clone.DataType = MarketDataTypes.OrderLog;
							base.SendInMessage(clone);

							return true;
						}

						break;
					}

					case MarketDataTypes.Trades:
					{
						if (isBuild || !InnerAdapter.IsMarketDataTypeSupported(message.DataType))
						{
							_tickBuilders.Add(message.SecurityId);
							_subscriptionIds.Add(message.TransactionId, Tuple.Create(message.SecurityId, message.DataType));

							var clone = (MarketDataMessage)message.Clone();
							clone.DataType = MarketDataTypes.OrderLog;
							base.SendInMessage(clone);

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
					_olBuilders.Remove(secId);
				else
					_tickBuilders.Remove(secId);
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

		private void ProcessBuilders(ExecutionMessage execMsg)
		{
			if (execMsg.IsSystem == false)
				return;

			if (_olBuilders.TryGetValue(execMsg.SecurityId, out var builder))
			{
				try
				{
					var updated = builder.Update(execMsg);

					if (updated)
					{
						base.OnInnerAdapterNewOutMessage(builder.Depth.Clone());
					}
				}
				catch (Exception ex)
				{
					// если ОЛ поврежден, то не нарушаем весь цикл обработки сообщения
					// а только выводим сообщение в лог
					base.OnInnerAdapterNewOutMessage(new ErrorMessage { Error = ex });
				}
			}

			if (_tickBuilders.Contains(execMsg.SecurityId))
			{
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