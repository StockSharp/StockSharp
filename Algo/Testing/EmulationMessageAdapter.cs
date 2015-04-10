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

		/// <summary>
		/// <see langword="true"/>, если сессия используется для получения маркет-данных, иначе, <see langword="false"/>.
		/// </summary>
		public override bool IsMarketDataEnabled
		{
			get { return false; }
		}

		/// <summary>
		/// <see langword="true"/>, если сессия используется для отправки транзакций, иначе, <see langword="false"/>.
		/// </summary>
		public override bool IsTransactionEnabled
		{
			get { return true; }
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
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			_currentTime = message.GetServerTime();

			switch (message.Type)
			{
				case MessageTypes.Connect:
					_emulator.SendInMessage(new ResetMessage());
					SendOutMessage(new ConnectMessage());
					return;
				case MessageTypes.Disconnect:
					SendOutMessage(new DisconnectMessage());
					return;
				case ExtendedMessageTypes.EmulationState:
					SendOutMessage(message.Clone());
					return;
			}

			_emulator.SendInMessage(message);
		}
	}
}