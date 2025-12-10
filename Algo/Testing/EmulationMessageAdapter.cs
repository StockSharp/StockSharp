namespace StockSharp.Algo.Testing;

using StockSharp.Algo.Positions;

/// <summary>
/// The interface of the real time market data adapter.
/// </summary>
public interface IEmulationMessageAdapter : IMessageAdapterWrapper
{
}

/// <summary>
/// Emulation message adapter.
/// </summary>
public class EmulationMessageAdapter : MessageAdapterWrapper, IEmulationMessageAdapter
{
	private readonly SynchronizedSet<long> _subscriptionIds = [];
	private readonly SynchronizedSet<long> _emuOrderIds = [];

	private readonly IMessageAdapter _inAdapter;
	private readonly bool _isEmulationOnly;

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
		: base(innerAdapter)
	{
		var seed = DateTime.UtcNow.Ticks;

		Emulator = new MarketEmulator(securityProvider, portfolioProvider, exchangeInfoProvider, TransactionIdGenerator)
		{
			Parent = this,
			Settings =
			{
				ConvertTime = true,
				InitialOrderId = seed,
				InitialTradeId = seed,
			}
		};

		InChannel = inChannel;

		_inAdapter = new SubscriptionOnlineMessageAdapter(Emulator);
		
		if (Emulator.IsPositionsEmulationRequired is bool isPosEmu)
			_inAdapter = new PositionMessageAdapter(_inAdapter, new PositionManager(isPosEmu));

		_inAdapter = new ChannelMessageAdapter(_inAdapter, inChannel, new PassThroughMessageChannel());
		_inAdapter.NewOutMessage += RaiseNewOutMessage;

		_isEmulationOnly = isEmulationOnly;
	}

	/// <inheritdoc />
	public override void Dispose()
	{
		_inAdapter.NewOutMessage -= RaiseNewOutMessage;
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
	public override IEnumerable<MessageTypes> SupportedInMessages => [.. InnerAdapter.SupportedInMessages.Concat(Emulator.SupportedInMessages).Distinct()];

	/// <inheritdoc />
	public override bool? IsPositionsEmulationRequired => Emulator.IsPositionsEmulationRequired;

	/// <inheritdoc />
	public override bool IsSupportTransactionLog => Emulator.IsSupportTransactionLog;

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
				await SendToEmulator(message, cancellationToken);
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
			case ExtendedMessageTypes.CommissionRule:
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
	protected override void InnerAdapterNewOutMessage(Message message)
	{
		if (OwnInnerAdapter || !message.IsBack())
			base.InnerAdapterNewOutMessage(message);
	}

	/// <inheritdoc />
	protected override void OnInnerAdapterNewOutMessage(Message message)
	{
		if (message.IsBack())
		{
			if (OwnInnerAdapter)
				base.OnInnerAdapterNewOutMessage(message);

			return;
		}

		switch (message.Type)
		{
			case MessageTypes.Connect:
			case MessageTypes.Disconnect:
			case MessageTypes.Reset:
				break;
			case MessageTypes.SubscriptionResponse:
			case MessageTypes.SubscriptionFinished:
			case MessageTypes.SubscriptionOnline:
			{
				if (_subscriptionIds.Contains(((IOriginalTransactionIdMessage)message).OriginalTransactionId))
					SendToEmulator(message, default);

				break;
			}
			//case MessageTypes.BoardState:
			case MessageTypes.Portfolio:
			case MessageTypes.PositionChange:
			{
				if (OwnInnerAdapter)
					base.OnInnerAdapterNewOutMessage(message);

				break;
			}

			case MessageTypes.Execution:
			{
				var execMsg = (ExecutionMessage)message;

				if (execMsg.IsMarketData())
					TrySendToEmulator((ISubscriptionIdMessage)message, default);
				else
				{
					if (OwnInnerAdapter)
						base.OnInnerAdapterNewOutMessage(message);
				}

				break;
			}

			case MessageTypes.Security:
			case MessageTypes.Board:
			{
				if (OwnInnerAdapter)
					base.OnInnerAdapterNewOutMessage(message);

				SendToEmulator(message, default);
				//TrySendToEmulator((ISubscriptionIdMessage)message);
				break;
			}

			case MessageTypes.EmulationState:
				SendToEmulator(message, default);
				break;

			case MessageTypes.Time:
			{
				if (OwnInnerAdapter)
				{
					if (_isEmulationOnly)
						SendToEmulator(message, default);
					else
						base.OnInnerAdapterNewOutMessage(message);
				}

				break;
			}

			default:
			{
				if (message is ISubscriptionIdMessage subscrMsg)
					TrySendToEmulator(subscrMsg, default);
				else
				{
					if (OwnInnerAdapter)
						base.OnInnerAdapterNewOutMessage(message);
				}

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
		if (OwnInnerAdapter)
		{
			if (_isEmulationOnly || portfolioName.EqualsIgnoreCase(Extensions.SimulatorPortfolioName))
			{
				if (!_isEmulationOnly)
					_emuOrderIds.Add(message.TransactionId);

				return SendToEmulator(message, cancellationToken);
			}
			else
				return base.OnSendInMessageAsync(message, cancellationToken);
		}
		else
		{
			_emuOrderIds.Add(message.TransactionId);
			return SendToEmulator(message, cancellationToken);
		}
	}

	private ValueTask ProcessOrderMessage(long transId, Message message, CancellationToken cancellationToken)
	{
		if (_isEmulationOnly || _emuOrderIds.Contains(transId))
			return SendToEmulator(message, cancellationToken);
		else
			return base.OnSendInMessageAsync(message, cancellationToken);
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
		=> new EmulationMessageAdapter(InnerAdapter.TypedClone(), InChannel, _isEmulationOnly, Emulator.SecurityProvider, Emulator.PortfolioProvider, Emulator.ExchangeInfoProvider);
}