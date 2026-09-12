namespace StockSharp.Algo.Testing;

using StockSharp.Algo.Positions;
using StockSharp.Algo.Testing.Emulation;

/// <summary>
/// Emulation message adapter.
/// </summary>
public class EmulationMessageAdapter : MessageAdapterWrapper, IEmulationMessageAdapter
{
	private readonly SynchronizedSet<long> _subscriptionIds = [];
	private readonly SynchronizedSet<long> _emuOrderIds = [];

	private readonly IMessageAdapterWrapper _inAdapter;
	private readonly MarketEmulatorAdapter _emulatorAdapter;
	private readonly bool _isEmulationOnly;
	private readonly bool _isOrderEmulationOnly;

	// Pending EmulationState(Stopping) message to forward after emulator drains its queue.
	private volatile Message _pendingStopping;

	/// <summary>
	/// Initialize <see cref="EmulationMessageAdapter"/>.
	/// </summary>
	/// <param name="innerAdapter">Underlying adapter.</param>
	/// <param name="inChannel">Incoming messages channel.</param>
	/// <param name="isEmulationOnly">All messages do not contains real trading.</param>
	/// <param name="securityProvider">The provider of information about instruments.</param>
	/// <param name="portfolioProvider">The portfolio to be used to register orders. If value is not given, the portfolio with default name Simulator will be created.</param>
	/// <param name="exchangeInfoProvider">Exchanges and trading boards provider.</param>
	public EmulationMessageAdapter(IMessageAdapter innerAdapter, IMessageChannel inChannel, bool isEmulationOnly, ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider)
		: this(innerAdapter, inChannel, isEmulationOnly, securityProvider, portfolioProvider, exchangeInfoProvider, false)
	{
	}

	internal EmulationMessageAdapter(IMessageAdapter innerAdapter, IMessageChannel inChannel, bool isEmulationOnly, ISecurityProvider securityProvider, IPortfolioProvider portfolioProvider, IExchangeInfoProvider exchangeInfoProvider, bool isOrderEmulationOnly)
		: base(innerAdapter)
	{
		// The identifier generators start where the settings say and nowhere else: a run seeded from
		// the clock numbers its orders differently every time, and no two runs can be compared.
		Emulator = new MarketEmulator(securityProvider, portfolioProvider, exchangeInfoProvider, TransactionIdGenerator)
		{
			Parent = this,
			Settings =
			{
				ConvertTime = true,
			}
		};

		_emulatorAdapter = new MarketEmulatorAdapter(Emulator, TransactionIdGenerator);

		InChannel = inChannel;

		_inAdapter = new SubscriptionOnlineMessageAdapter(_emulatorAdapter);

		if (_emulatorAdapter.IsPositionsEmulationRequired is bool isPosEmu)
			_inAdapter = new PositionMessageAdapter(_inAdapter, new PositionManager(isPosEmu, new PositionManagerState()));

		_inAdapter = new ChannelMessageAdapter(_inAdapter, inChannel, new PassThroughMessageChannel());
		_inAdapter.NewOutMessageAsync += OnEmulatorNewOutMessageAsync;

		_isEmulationOnly = isEmulationOnly;
		_isOrderEmulationOnly = isOrderEmulationOnly;
	}

	private ValueTask OnEmulatorNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		// When a EmulationState message comes back from the emulator queue,
		// it means all preceding messages (candles etc.) have been processed.
		// Now forward the pending Stopping to the connector.
		if (message is EmulationStateMessage && _pendingStopping is { } stopping)
		{
			_pendingStopping = null;
			return RaiseNewOutMessageAsync(stopping, cancellationToken);
		}

		return RaiseNewOutMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	public override void Dispose()
	{
		// The same handler the constructor attached, or the torn-down adapter keeps raising whatever
		// still moves through the incoming channel.
		_inAdapter.NewOutMessageAsync -= OnEmulatorNewOutMessageAsync;
		base.Dispose();
	}

	/// <summary>
	/// Emulator.
	/// </summary>
	public IMarketEmulator Emulator { get; }

	/// <summary>
	/// Settings of exchange emulator.
	/// </summary>
	public MarketEmulatorSettings Settings => Emulator.Settings;

	/// <summary>
	/// Incoming messages channel.
	/// </summary>
	public IMessageChannel InChannel { get; }

	/// <inheritdoc />
	public override IEnumerable<MessageTypes> SupportedInMessages => [.. InnerAdapter.SupportedInMessages.Concat(_emulatorAdapter.SupportedInMessages).Distinct()];

	/// <inheritdoc />
	public override bool? IsPositionsEmulationRequired => _emulatorAdapter.IsPositionsEmulationRequired;

	/// <inheritdoc />
	public override bool IsSupportTransactionLog => _emulatorAdapter.IsSupportTransactionLog;

	private ValueTask SendToEmulator(Message message, CancellationToken cancellationToken)
	{
		return _inAdapter.SendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnSendInMessageAsync(Message message, CancellationToken cancellationToken)
	{
		switch (message.Type)
		{
			case MessageTypes.OrderRegister:
			{
				var regMsg = (OrderRegisterMessage)message;
				await ProcessOrderMessage(regMsg.PortfolioName, regMsg, cancellationToken);
				return;
			}
			case MessageTypes.OrderReplace:
			case MessageTypes.OrderCancel:
			{
				await ProcessOrderMessage(((OrderMessage)message).OriginalTransactionId, message, cancellationToken);
				return;
			}

			case MessageTypes.OrderGroupCancel:
			{
				await ProcessOrderGroupCancelMessage((OrderGroupCancelMessage)message, cancellationToken);
				return;
			}

			case MessageTypes.Reset:
			case MessageTypes.Connect:
			case MessageTypes.Disconnect:
			{
				await SendToEmulator(message, cancellationToken);

				if (message.Type == MessageTypes.Reset)
				{
					_subscriptionIds.Clear();
					_emuOrderIds.Clear();
				}

				if (OwnInnerAdapter)
					await base.OnSendInMessageAsync(message, cancellationToken);
				else
					return;

				break;
			}

			case MessageTypes.PortfolioLookup:
			case MessageTypes.Portfolio:
			case MessageTypes.OrderStatus:
			{
				// Track OrderStatus subscription so SubscriptionOnline is forwarded to emulator
				if (message is ISubscriptionMessage subscrMsg && subscrMsg.IsSubscribe)
					_subscriptionIds.Add(subscrMsg.TransactionId);

				if (OwnInnerAdapter)
					await base.OnSendInMessageAsync(message, cancellationToken);

				await SendToEmulator(message, cancellationToken);
				return;
			}

			case MessageTypes.SecurityLookup:
			case MessageTypes.DataTypeLookup:
			case MessageTypes.BoardLookup:
			case MessageTypes.MarketData:
			{
				// MarketEmulator works faster with order book increments
				if (message is MarketDataMessage mdMsg && mdMsg.IsSubscribe)
					mdMsg.DoNotBuildOrderBookIncrement = true;

				_subscriptionIds.Add(((ISubscriptionMessage)message).TransactionId);

				// sends to emu for init subscription ids
				await SendToEmulator(message, cancellationToken);

				await base.OnSendInMessageAsync(message, cancellationToken);
				return;
			}

			case MessageTypes.Level1Change:
			case HistoryMessageTypes.CommissionRule:
			{
				await SendToEmulator(message, cancellationToken);
				return;
			}

			default:
			{
				if (OwnInnerAdapter)
					await base.OnSendInMessageAsync(message, cancellationToken);

				return;
			}
		}
	}

	/// <inheritdoc />
	protected override async ValueTask InnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (OwnInnerAdapter || !message.IsBack())
			await base.InnerAdapterNewOutMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	protected override async ValueTask OnInnerAdapterNewOutMessageAsync(Message message, CancellationToken cancellationToken)
	{
		if (message.IsBack())
		{
			if (OwnInnerAdapter)
				await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

			return;
		}

		switch (message.Type)
		{
			case MessageTypes.Connect:
			case MessageTypes.Disconnect:
			case MessageTypes.Reset:
			{
				if (OwnInnerAdapter)
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

				break;
			}
			case MessageTypes.SubscriptionResponse:
			case MessageTypes.SubscriptionFinished:
			case MessageTypes.SubscriptionOnline:
			{
				if (_subscriptionIds.Contains(((IOriginalTransactionIdMessage)message).OriginalTransactionId))
					await SendToEmulator(message, cancellationToken);

				// Must forward to parent adapter so BasketMessageAdapter/Connector receive the response
				if (OwnInnerAdapter)
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

				break;
			}
			//case MessageTypes.BoardState:
			case MessageTypes.Portfolio:
			case MessageTypes.PositionChange:
			{
				if (OwnInnerAdapter)
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.IsMarketData())
					await TrySendToEmulator((ISubscriptionIdMessage)message, cancellationToken);
				else
				{
					if (OwnInnerAdapter)
						await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
				}

				break;
			}

			case MessageTypes.Security:
			case MessageTypes.Board:
			{
				if (OwnInnerAdapter)
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

				await SendToEmulator(message, cancellationToken);
				//TrySendToEmulator((ISubscriptionIdMessage)message);
				break;
			}

			case MessageTypes.EmulationState:
			{
				var emuState = ((EmulationStateMessage)message).State;

				if (emuState == ChannelStates.Stopping)
				{
					// Soft stop: save the Stopping message and send it through the
					// emulator channel queue as a marker. When it comes back through
					// _inAdapter.NewOutMessageAsync (after all queued candles/data),
					// we forward the saved Stopping to the connector.
					_pendingStopping = message;
					await SendToEmulator(message, cancellationToken);
				}
				else
				{
					// All other states: forward immediately + send to emulator as before
					if (OwnInnerAdapter)
						await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

					await SendToEmulator(message, cancellationToken);
				}

				break;
			}

			case MessageTypes.Time:
			{
				if (OwnInnerAdapter)
				{
					if (_isEmulationOnly)
						await SendToEmulator(message, cancellationToken);
					else
						await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);
				}

				break;
			}

			default:
			{
				if (message is ISubscriptionIdMessage subscrMsg)
					await TrySendToEmulator(subscrMsg, cancellationToken);

				if (OwnInnerAdapter)
					await base.OnInnerAdapterNewOutMessageAsync(message, cancellationToken);

				break;
			}
		}
	}

	private async ValueTask TrySendToEmulator(ISubscriptionIdMessage message, CancellationToken cancellationToken)
	{
		if (_isEmulationOnly)
			await SendToEmulator((Message)message, cancellationToken);
		else
		{
			foreach (var id in message.GetSubscriptionIds())
			{
				if (_subscriptionIds.Contains(id))
				{
					await SendToEmulator((Message)message, cancellationToken);
					break;
				}
			}
		}
	}

	private ValueTask ProcessOrderMessage(string portfolioName, OrderMessage message, CancellationToken cancellationToken)
	{
		if (_isOrderEmulationOnly || _isEmulationOnly || portfolioName.EqualsIgnoreCase(Extensions.SimulatorPortfolioName))
		{
			if (!_isEmulationOnly)
				_emuOrderIds.Add(message.TransactionId);

			return SendToEmulator(message, cancellationToken);
		}

		return base.OnSendInMessageAsync(message, cancellationToken);
	}

	private ValueTask ProcessOrderMessage(long transId, Message message, CancellationToken cancellationToken)
	{
		if (_isOrderEmulationOnly || _isEmulationOnly || _emuOrderIds.Contains(transId))
			return SendToEmulator(message, cancellationToken);
		else
			return base.OnSendInMessageAsync(message, cancellationToken);
	}

	private async ValueTask ProcessOrderGroupCancelMessage(OrderGroupCancelMessage message, CancellationToken cancellationToken)
	{
		if (_isOrderEmulationOnly || _isEmulationOnly || message.PortfolioName.EqualsIgnoreCase(Extensions.SimulatorPortfolioName))
		{
			await SendToEmulator(message, cancellationToken);
			return;
		}

		if (message.PortfolioName.IsEmpty())
			await SendToEmulator(message.TypedClone(), cancellationToken);

		await base.OnSendInMessageAsync(message, cancellationToken);
	}

	/// <inheritdoc />
	public override void Save(SettingsStorage storage)
	{
		base.Save(storage);

		storage.SetValue(nameof(MarketEmulator), Settings.Save());
	}

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		Settings.Load(storage, nameof(MarketEmulator));
	}

	/// <summary>
	/// Create a copy of <see cref="EmulationMessageAdapter"/>.
	/// </summary>
	/// <returns>Copy.</returns>
	public override IMessageAdapter Clone()
	{
		// A channel of its own: sharing one hands every message sent to either adapter to both
		// emulators, so the copy fills orders out of work it was never given.
		var clone = new EmulationMessageAdapter(InnerAdapter.TypedClone(), InChannel.Clone(), _isEmulationOnly,
			Emulator.SecurityProvider, Emulator.PortfolioProvider, Emulator.ExchangeInfoProvider, _isOrderEmulationOnly)
		{
			OwnInnerAdapter = OwnInnerAdapter,
		};

		// The copy emulates on the terms the original was given, carried by value so retuning one
		// run does not retune the other.
		clone.Settings.Load(Settings.Save());

		return clone;
	}
}
