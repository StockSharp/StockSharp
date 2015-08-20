namespace StockSharp.Algo.Testing
{
	using System;

	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Базовое подключение эмуляции.
	/// </summary>
	public abstract class BaseEmulationConnector : Connector
	{
		/// <summary>
		/// Инициализировать <see cref="BaseEmulationConnector"/>.
		/// </summary>
		protected BaseEmulationConnector()
		{
			var adapter = new EmulationMessageAdapter(new MarketEmulator(), TransactionIdGenerator);
			Adapter.InnerAdapters.Add(adapter.ToChannel(this));
		}

		/// <summary>
		/// Поддерживается ли перерегистрация заявок через метод <see cref="IConnector.ReRegisterOrder(StockSharp.BusinessEntities.Order,StockSharp.BusinessEntities.Order)"/>
		/// в виде одной транзакции.
		/// </summary>
		public override bool IsSupportAtomicReRegister
		{
			get { return MarketEmulator.Settings.IsSupportAtomicReRegister; }
		}

		/// <summary>
		/// Эмулятор торгов.
		/// </summary>
		public IMarketEmulator MarketEmulator
		{
			get { return ((EmulationMessageAdapter)TransactionAdapter).Emulator; }
			set { ((EmulationMessageAdapter)TransactionAdapter).Emulator = value; }
		}

		/// <summary>
		/// Запустить таймер генерации сообщений <see cref="TimeMessage"/> с интервалом <see cref="Connector.MarketTimeChangedInterval"/>.
		/// </summary>
		protected override void StartMarketTimer()
		{
		}

		///// <summary>
		///// Обработать сообщение, содержащее рыночные данные.
		///// </summary>
		///// <param name="message">Сообщение, содержащее рыночные данные.</param>
		///// <param name="direction">Направление сообщения.</param>
		//protected override void OnProcessMessage(Message message, MessageDirections direction)
		//{
		//	if (adapter == MarketDataAdapter && direction == MessageDirections.Out)
		//	{
		//		switch (message.Type)
		//		{
		//			case MessageTypes.Connect:
		//			case MessageTypes.Disconnect:
		//			case MessageTypes.MarketData:
		//			case MessageTypes.Error:
		//			case MessageTypes.SecurityLookupResult:
		//			case MessageTypes.PortfolioLookupResult:
		//				base.OnProcessMessage(message, direction);
		//				break;

		//			case MessageTypes.Execution:
		//			{
		//				var execMsg = (ExecutionMessage)message;

		//				if (execMsg.ExecutionType != ExecutionTypes.Trade)
		//					SendInMessage(message);
		//				else
		//					base.OnProcessMessage(message, direction);

		//				break;
		//			}

		//			default:
		//				SendInMessage(message);
		//				break;
		//		}
		//	}
		//	else
		//		base.OnProcessMessage(message, direction);
		//}

		private void SendInGeneratorMessage(MarketDataGenerator generator, bool isSubscribe)
		{
			if (generator == null)
				throw new ArgumentNullException("generator");

			SendInMessage(new GeneratorMessage
			{
				IsSubscribe = isSubscribe,
				SecurityId = generator.SecurityId,
				Generator = generator,
				DataType = generator.DataType,
			});
		}

		/// <summary>
		/// Зарегистрировать генератор сделок.
		/// </summary>
		/// <param name="generator">Генератор сделок.</param>
		public void RegisterTrades(TradeGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// Удалить генератор сделок, ранее зарегистрированный через <see cref="RegisterTrades"/>.
		/// </summary>
		/// <param name="generator">Генератор сделок.</param>
		public void UnRegisterTrades(TradeGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
		}

		/// <summary>
		/// Зарегистрировать генератор стаканов.
		/// </summary>
		/// <param name="generator">Генератор стаканов.</param>
		public void RegisterMarketDepth(MarketDepthGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// Удалить генератор стаканов, ранее зарегистрированный через <see cref="RegisterMarketDepth"/>.
		/// </summary>
		/// <param name="generator">Генератор стаканов.</param>
		public void UnRegisterMarketDepth(MarketDepthGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
		}

		/// <summary>
		/// Зарегистрировать генератор лога заявок.
		/// </summary>
		/// <param name="generator">Генератор лога заявок.</param>
		public void RegisterOrderLog(OrderLogGenerator generator)
		{
			SendInGeneratorMessage(generator, true);
		}

		/// <summary>
		/// Удалить генератор лога заявок, ранее зарегистрированный через <see cref="RegisterOrderLog"/>.
		/// </summary>
		/// <param name="generator">Генератор лога заявок.</param>
		public void UnRegisterOrderLog(OrderLogGenerator generator)
		{
			SendInGeneratorMessage(generator, false);
		}
	}
}