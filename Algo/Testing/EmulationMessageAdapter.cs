namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Адаптер, исполняющий сообщения в <see cref="IMarketEmulator"/>.
	/// </summary>
	public class EmulationMessageAdapter : MessageAdapter
	{
		/// <summary>
		/// Создать <see cref="EmulationMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public EmulationMessageAdapter(IdGenerator transactionIdGenerator)
			: this(new MarketEmulator(), transactionIdGenerator)
		{
		}

		/// <summary>
		/// Создать <see cref="EmulationMessageAdapter"/>.
		/// </summary>
		/// <param name="emulator">Эмулятор торгов.</param>
		/// <param name="transactionIdGenerator">Генератор идентификаторов транзакций.</param>
		public EmulationMessageAdapter(IMarketEmulator emulator, IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Emulator = emulator;
			IsMarketDataEnabled = false;
		}

		private IMarketEmulator _emulator;

		/// <summary>
		/// Эмулятор торгов.
		/// </summary>
		public IMarketEmulator Emulator
		{
			get { return _emulator; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (value == _emulator)
					return;

				if (_emulator != null)
				{
					_emulator.NewOutMessage -= SendOutMessage;
					_emulator.Parent = null;
				}

				_emulator = value;
				_emulator.Parent = this;
				_emulator.NewOutMessage += SendOutMessage;
			}
		}

		private DateTimeOffset _currentTime;

		/// <summary>
		/// Текущее время.
		/// </summary>
		public override DateTimeOffset CurrentTime
		{
			get { return _currentTime; }
		}

		/// <summary>
		/// Число обработанных сообщений.
		/// </summary>
		public int ProcessedMessageCount { get; private set; }

		/// <summary>
		/// Требуется ли дополнительное сообщение <see cref="PortfolioLookupMessage"/> для получения списка портфелей и позиций.
		/// </summary>
		public override bool PortfolioLookupRequired
		{
			get { return true; }
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			var localTime = message.LocalTime;

			if (!localTime.IsDefault())
				_currentTime = localTime;

			switch (message.Type)
			{
				case MessageTypes.Connect:
					SendOutMessage(new ConnectMessage());
					return;

				case MessageTypes.Reset:
					ProcessedMessageCount = 0;

					var incGen = TransactionIdGenerator as IncrementalIdGenerator;
					if (incGen != null)
						incGen.Current = Emulator.Settings.InitialTransactionId;

					_currentTime = default(DateTimeOffset);
					break;

				case MessageTypes.Disconnect:
					SendOutMessage(new DisconnectMessage());
					return;

				case ExtendedMessageTypes.EmulationState:
					//SendOutMessage(message.Clone());
					return;
			}

			ProcessedMessageCount++;
			_emulator.SendInMessage(message);
		}
	}
}