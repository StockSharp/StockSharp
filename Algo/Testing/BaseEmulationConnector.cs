namespace StockSharp.Algo.Testing
{
	using StockSharp.BusinessEntities;
	using StockSharp.Messages;

	/// <summary>
	/// Базовое подключение эмуляции.
	/// </summary>
	public abstract class BaseEmulationConnector : Connector
	{
		private readonly EmulationMessageAdapter _adapter;

		/// <summary>
		/// Инициализировать <see cref="BaseEmulationConnector"/>.
		/// </summary>
		protected BaseEmulationConnector()
		{
			TransactionAdapter = _adapter = new EmulationMessageAdapter(new MarketEmulator(), new PassThroughSessionHolder(TransactionIdGenerator));
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
			get { return _adapter.Emulator; }
			set { _adapter.Emulator = value; }
		}

		/// <summary>
		/// Обработать сообщение, содержащее рыночные данные.
		/// </summary>
		/// <param name="message">Сообщение, содержащее рыночные данные.</param>
		/// <param name="adapterType">Тип адаптера, от которого пришло сообщение.</param>
		/// <param name="direction">Направление сообщения.</param>
		protected override void OnProcessMessage(Message message, MessageAdapterTypes adapterType, MessageDirections direction)
		{
			if (adapterType == MessageAdapterTypes.MarketData && direction == MessageDirections.Out)
			{
				switch (message.Type)
				{
					case MessageTypes.Connect:
					case MessageTypes.Disconnect:
					case MessageTypes.MarketData:
					case MessageTypes.Error:
					case MessageTypes.SecurityLookupResult:
					case MessageTypes.PortfolioLookupResult:
						base.OnProcessMessage(message, adapterType, direction);
						break;

					case MessageTypes.Execution:
					{
						var execMsg = (ExecutionMessage)message;

						if (execMsg.ExecutionType != ExecutionTypes.Trade)
							TransactionAdapter.SendInMessage(message);
						else
							base.OnProcessMessage(message, adapterType, direction);

						break;
					}

					default:
						TransactionAdapter.SendInMessage(message);
						break;
				}
			}
			else
				base.OnProcessMessage(message, adapterType, direction);
		}
	}
}