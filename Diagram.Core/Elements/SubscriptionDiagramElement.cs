namespace StockSharp.Diagram.Elements;

/// <summary>
/// The diagram element which supports subscription to market data.
/// </summary>
public abstract class SubscriptionDiagramElement : DiagramElement
{
	private Subscription _subscription;
	private DiagramSocket _signalInput;
	private Security _inputSecurity, _subscribedSecurity;
	private readonly DiagramSocket _securitySocket;
	private readonly DiagramElementParam<bool> _isManuallySubscription;

	/// <summary>
	/// Subscribe on signal.
	/// </summary>
	public bool IsManuallySubscription
	{
		get => _isManuallySubscription.Value;
		set => _isManuallySubscription.Value = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SubscriptionDiagramElement"/>.
	/// </summary>
	/// <param name="subscriptionCategory">The category of the diagram element parameter.</param>
	protected SubscriptionDiagramElement(string subscriptionCategory)
	{
		_securitySocket = AddInput(StaticSocketIds.Security, LocalizedStrings.Security, DiagramSocketType.Security, OnProcessSecurity);

		_isManuallySubscription = AddParam<bool>(nameof(IsManuallySubscription))
			.SetBasic(true)
			.SetDisplay(subscriptionCategory, LocalizedStrings.SubscribeOnSignal, LocalizedStrings.SubscribeOnSignal, 200)
			.SetOnValueChangedHandler(value =>
			{
				if (value)
					AddSignalInputSocket();
				else
					RemoveSignalInputSocket();
			});

		ProcessNullValues = false;
	}

	/// <inheritdoc />
	protected override void OnReseted()
	{
		base.OnReseted();

		_inputSecurity = default;
	}

	/// <inheritdoc />
	protected override void OnStart(DateTimeOffset time)
	{
		base.OnStart(time);

		if (!_securitySocket.IsConnected)
			EnsureSubscription();
	}

	/// <inheritdoc />
	protected override void OnStop() => EnsureSubscription(false);

	private void EnsureSubscription(bool? signal = null)
	{
		var security = _inputSecurity ?? Strategy.Security;

		var needToUnsubscribe =
			_subscribedSecurity != null && _subscription != null &&
			(signal == false || (_subscribedSecurity != security && !IsManuallySubscription));

		if (needToUnsubscribe)
		{
			Strategy.UnSubscribe(_subscription);

			_subscription = null;
			_subscribedSecurity = null;
		}

		var needToSubscribe = _subscription == null && security != null && (signal == true || (signal == null && !IsManuallySubscription));

		if (!needToSubscribe)
			return;

		if (security.IsAllSecurity())
			throw new InvalidOperationException(LocalizedStrings.SecurityNotSpecified);

		_subscription = OnCreateSubscription(security);
		LogInfo("Subscription={0}", _subscription);
		Strategy.Subscribe(_subscription);
		_subscribedSecurity = security;
	}

	/// <summary>
	/// The method is called at the subscribing to market data.
	/// </summary>
	/// <param name="security"><see cref="Security"/></param>
	/// <returns>Subscription.</returns>
	protected abstract Subscription OnCreateSubscription(Security security);

	/// <inheritdoc />
	public override void Load(SettingsStorage storage)
	{
		base.Load(storage);

		if (_signalInput != null)
			AddSignalInputSocket();
	}

	private void OnProcessSecurity(DiagramSocketValue value)
	{
		_inputSecurity = value.GetValue<Security>();
		EnsureSubscription();
	}

	private void OnProcessSignal(DiagramSocketValue value) => EnsureSubscription(value.GetValue<bool>());

	private void RemoveSignalInputSocket()
	{
		if (_signalInput == null)
			return;

		RemoveSocket(_signalInput);
		_signalInput = null;
	}

	private void AddSignalInputSocket()
	{
		RemoveSignalInputSocket();
		_signalInput = AddInput(GenerateSocketId("signal"), LocalizedStrings.Trigger, DiagramSocketType.Bool, OnProcessSignal, index: 2);
	}
}