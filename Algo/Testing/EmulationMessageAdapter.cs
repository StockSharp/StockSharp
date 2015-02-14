namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// Адаптер, исполняющий сообщения в <see cref="IMarketEmulator"/>.
	/// </summary>
	public class EmulationMessageAdapter : MessageAdapter<IMessageSessionHolder>
	{
		/// <summary>
		/// Создать <see cref="EmulationMessageAdapter"/>.
		/// </summary>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public EmulationMessageAdapter(IMessageSessionHolder sessionHolder)
			: this(new MarketEmulator(), sessionHolder)
		{
		}

		/// <summary>
		/// Создать <see cref="EmulationMessageAdapter"/>.
		/// </summary>
		/// <param name="emulator">Эмулятор торгов.</param>
		/// <param name="sessionHolder">Контейнер для сессии.</param>
		public EmulationMessageAdapter(IMarketEmulator emulator, IMessageSessionHolder sessionHolder)
			: base(MessageAdapterTypes.Transaction, sessionHolder)
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
				_emulator.Parent = SessionHolder;
				_emulator.NewOutMessage += SendOutMessage;
			}
		}

		/// <summary>
		/// Запустить таймер генерации с интервалом <see cref="MessageSessionHolder.MarketTimeChangedInterval"/> сообщений <see cref="TimeMessage"/>.
		/// </summary>
		protected override void StartMarketTimer()
		{
		}

		/// <summary>
		/// Отправить сообщение.
		/// </summary>
		/// <param name="message">Сообщение.</param>
		protected override void OnSendInMessage(Message message)
		{
			SessionHolder.DoIf<IMessageSessionHolder, HistorySessionHolder>(s => s.UpdateCurrentTime(message.GetServerTime()));

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