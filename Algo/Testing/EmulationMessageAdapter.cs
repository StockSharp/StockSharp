#region S# License
/******************************************************************************************
NOTICE!!!  This program and source code is owned and licensed by
StockSharp, LLC, www.stocksharp.com
Viewing or use of this code requires your acceptance of the license
agreement found at https://github.com/StockSharp/StockSharp/blob/master/LICENSE
Removal of this comment is a violation of the license agreement.

Project: StockSharp.Algo.Testing.Algo
File: EmulationMessageAdapter.cs
Created: 2015, 11, 11, 2:32 PM

Copyright 2010 by StockSharp, LLC
*******************************************************************************************/
#endregion S# License
namespace StockSharp.Algo.Testing
{
	using System;

	using Ecng.Common;

	using StockSharp.Messages;

	/// <summary>
	/// The adapter, executing messages in <see cref="IMarketEmulator"/>.
	/// </summary>
	public class EmulationMessageAdapter : MessageAdapter
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="EmulationMessageAdapter"/>.
		/// </summary>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public EmulationMessageAdapter(IdGenerator transactionIdGenerator)
			: this(new MarketEmulator(), transactionIdGenerator)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EmulationMessageAdapter"/>.
		/// </summary>
		/// <param name="emulator">Paper trading.</param>
		/// <param name="transactionIdGenerator">Transaction id generator.</param>
		public EmulationMessageAdapter(IMarketEmulator emulator, IdGenerator transactionIdGenerator)
			: base(transactionIdGenerator)
		{
			Emulator = emulator;

			this.AddTransactionalSupport();
			this.AddSupportedMessage(MessageTypes.Security);
			this.AddSupportedMessage(MessageTypes.Board);
			this.AddSupportedMessage(MessageTypes.Level1Change);
			this.AddSupportedMessage(MessageTypes.PortfolioChange);
			this.AddSupportedMessage(MessageTypes.PositionChange);
			this.AddSupportedMessage(ExtendedMessageTypes.CommissionRule);
			this.AddSupportedMessage(ExtendedMessageTypes.Clearing);
			this.AddSupportedMessage(ExtendedMessageTypes.Generator);
		}

		private IMarketEmulator _emulator;

		/// <summary>
		/// Paper trading.
		/// </summary>
		public IMarketEmulator Emulator
		{
			get => _emulator;
			set
			{
				if (value == null)
					throw new ArgumentNullException(nameof(value));

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

		/// <inheritdoc />
		public override DateTimeOffset CurrentTime => _currentTime;

		/// <summary>
		/// The number of processed messages.
		/// </summary>
		public int ProcessedMessageCount { get; private set; }

		/// <inheritdoc />
		public override bool IsFullCandlesOnly => false;

		/// <inheritdoc />
		public override bool IsSupportSubscriptions => false;

		/// <inheritdoc />
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

					if (TransactionIdGenerator is IncrementalIdGenerator incGen)
						incGen.Current = Emulator.Settings.InitialTransactionId;

					_currentTime = default(DateTimeOffset);
					break;

				case MessageTypes.Disconnect:
					SendOutMessage(new DisconnectMessage());
					return;

				//case ExtendedMessageTypes.EmulationState:
				//	//SendOutMessage(message.Clone());
				//	return;
			}

			ProcessedMessageCount++;
			_emulator.SendInMessage(message);
		}
	}
}