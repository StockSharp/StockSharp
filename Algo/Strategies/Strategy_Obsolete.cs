namespace StockSharp.Algo.Strategies;

partial class Strategy
{
	/// <summary>
	/// Subsidiary trade strategies.
	/// </summary>
	[Browsable(false)]
	[Obsolete("Child strategies no longer supported.")]
	public INotifyList<Strategy> ChildStrategies { get; } = new SynchronizedList<Strategy>();

	/// <summary>
	/// The event of order successful registration.
	/// </summary>
	[Obsolete("Use OrderReceived event.")]
	public event Action<Order> OrderRegistered;

	/// <inheritdoc />
	[Obsolete("Use OrderRegisterFailReceived event.")]
	public event Action<OrderFail> OrderRegisterFailed;

	/// <inheritdoc />
	[Obsolete("Use OrderCancelFailReceived event.")]
	public event Action<OrderFail> OrderCancelFailed;

	/// <inheritdoc />
	[Obsolete("Use OrderReceived event.")]
	public event Action<Order> OrderChanged;

	/// <inheritdoc />
	[Obsolete("Use OrderReceived event.")]
	public event Action<long, Order> OrderEdited;

	/// <inheritdoc />
	[Obsolete("Use OrderEditFailReceived event.")]
	public event Action<long, OrderFail> OrderEditFailed;

	/// <inheritdoc />
	[Obsolete("Use OwnTradeReceived event.")]
	public event Action<MyTrade> NewMyTrade;

	/// <summary>
	/// <see cref="PnL"/> change event.
	/// </summary>
	[Obsolete("Use PnLReceived2 event.")]
	public event Action<Subscription> PnLReceived;

	/// <summary>
	/// The method is called when the <see cref="Start()"/> method has been called and the <see cref="ProcessState"/> state has been taken the <see cref="ProcessStates.Started"/> value.
	/// </summary>
	[Obsolete("Use overload with time param.")]
	protected virtual void OnStarted()
	{
		OnStarted2(CurrentTimeUtc);
	}
}