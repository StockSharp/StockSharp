namespace StockSharp.Algo;

/// <summary>
/// The interface of the rule, activating action at occurrence of market condition.
/// </summary>
public interface IMarketRule : IDisposable
{
	/// <summary>
	/// The name of the rule.
	/// </summary>
	string Name { get; set; }

	/// <summary>
	/// The rules container.
	/// </summary>
	IMarketRuleContainer Container { get; set; }

	/// <summary>
	/// The level to perform this rule logging.
	/// </summary>
	LogLevels LogLevel { get; set; }

	/// <summary>
	/// Is the rule suspended.
	/// </summary>
	bool IsSuspended { get; set; }

	/// <summary>
	/// Is the rule formed.
	/// </summary>
	bool IsReady { get; }

	/// <summary>
	/// Is the rule currently activated.
	/// </summary>
	bool IsActive { get; set; }

	/// <summary>
	/// Token-rules, it is associated with (for example, for rule <see cref="MarketRuleHelper.WhenRegistered"/> the order will be a token). If rule is not associated with anything, <see langword="null" /> will be returned.
	/// </summary>
	object Token { get; }

	/// <summary>
	/// Rules, opposite to given rule. They are deleted automatically at activation of this rule.
	/// </summary>
	ISynchronizedCollection<IMarketRule> ExclusiveRules { get; }

	/// <summary>
	/// To make the rule periodical (will be called until <paramref name="canFinish" /> returns <see langword="true" />).
	/// </summary>
	/// <param name="canFinish">The criteria for end of periodicity.</param>
	/// <returns>Rule.</returns>
	IMarketRule Until(Func<bool> canFinish);

	/// <summary>
	/// To add the action, activated at occurrence of condition.
	/// </summary>
	/// <param name="action">Action.</param>
	/// <returns>Rule.</returns>
	IMarketRule Do(Action action);

	/// <summary>
	/// To add the action, activated at occurrence of condition.
	/// </summary>
	/// <param name="action">The action, taking a value.</param>
	/// <returns>Rule.</returns>
	IMarketRule Do(Action<object> action);

	/// <summary>
	/// To add the action, returning result, activated at occurrence of condition.
	/// </summary>
	/// <typeparam name="TResult">The type of returned result.</typeparam>
	/// <param name="action">The action, returning a result.</param>
	/// <returns>Rule.</returns>
	IMarketRule Do<TResult>(Func<TResult> action);

	/// <summary>
	/// Can the rule be ended.
	/// </summary>
	/// <returns><see langword="true" />, if rule is not required any more. Otherwise, <see langword="false" />.</returns>
	bool CanFinish();
}

/// <summary>
/// The rule, activating action at market condition occurrence.
/// </summary>
/// <typeparam name="TToken">The type of token.</typeparam>
/// <typeparam name="TArg">The type of accepted argument.</typeparam>
public abstract class MarketRule<TToken, TArg> : BaseLogReceiver, IMarketRule
{
	private Func<TArg, object> _action = a => a;
	private Action<object> _activatedHandler;
	private Action<TArg> _actionVoid;
	private Func<bool> _process;

	private TArg _arg;

	private Func<bool> _canFinish;

	/// <summary>
	/// Initialize <see cref="MarketRule{T1,T2}"/>.
	/// </summary>
	/// <param name="token">Token rules.</param>
	protected MarketRule(TToken token)
	{
		_token = token;
		Name = GetType().Name;

		Until(CanFinish);
	}

	/// <summary>
	/// Can the rule be ended.
	/// </summary>
	/// <returns><see langword="true" />, if rule is not required any more. Otherwise, <see langword="false" />.</returns>
	protected virtual bool CanFinish()
	{
		return _container is null || _container.ProcessState != ProcessStates.Started;
	}

	private bool _isSuspended;

	/// <inheritdoc />
	public virtual bool IsSuspended
	{
		get => _isSuspended;
		set
		{
			_isSuspended = value;

			_container?.AddRuleLog(LogLevels.Info, this, value ? LocalizedStrings.Suspended : LocalizedStrings.Resumed);
		}
	}

	private readonly TToken _token;
	object IMarketRule.Token => _token;

	private readonly SynchronizedSet<IMarketRule> _exclusiveRules = [];

	/// <inheritdoc />
	public virtual ISynchronizedCollection<IMarketRule> ExclusiveRules => _exclusiveRules;

	private IMarketRuleContainer _container;

	/// <inheritdoc />
	public IMarketRuleContainer Container
	{
		get => _container;
		set
		{
			if (_container != null)
				throw new ArgumentException(LocalizedStrings.RuleAlreadyExistInContainer.Put(this, _container));

			_container = value ?? throw new ArgumentNullException(nameof(value));
		}
	}

	/// <summary>
	/// To make the rule periodical (will be called until <paramref name="canFinish" /> returns <see langword="true" />).
	/// </summary>
	/// <param name="canFinish">The criteria for end of periodicity.</param>
	/// <returns>Rule.</returns>
	public MarketRule<TToken, TArg> Until(Func<bool> canFinish)
	{
		_canFinish = canFinish ?? throw new ArgumentNullException(nameof(canFinish));
		return this;
	}

	/// <summary>
	/// To add the action, activated at occurrence of condition.
	/// </summary>
	/// <param name="action">Action.</param>
	/// <returns>Rule.</returns>
	public MarketRule<TToken, TArg> Do(Action<TArg> action)
	{
		//return Do((r, a) => action(a));

		_process = ProcessRuleVoid;
		_actionVoid = action ?? throw new ArgumentNullException(nameof(action));

		return this;
	}

	/// <summary>
	/// To add the action, activated at occurrence of condition.
	/// </summary>
	/// <param name="action">Action.</param>
	/// <returns>Rule.</returns>
	public MarketRule<TToken, TArg> Do(Action<MarketRule<TToken, TArg>, TArg> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		return Do<object>((r, a) =>
		{
			action(this, a);
			return null;
		});
	}

	/// <summary>
	/// To add the action, returning result, activated at occurrence of condition.
	/// </summary>
	/// <typeparam name="TResult">The type of returned result.</typeparam>
	/// <param name="action">The action, returning a result.</param>
	/// <returns>Rule.</returns>
	public MarketRule<TToken, TArg> Do<TResult>(Func<TArg, TResult> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		return Do((r, a) => action(a));
	}

	/// <summary>
	/// To add the action, returning result, activated at occurrence of condition.
	/// </summary>
	/// <typeparam name="TResult">The type of returned result.</typeparam>
	/// <param name="action">The action, returning a result.</param>
	/// <returns>Rule.</returns>
	public MarketRule<TToken, TArg> Do<TResult>(Func<MarketRule<TToken, TArg>, TArg, TResult> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		_action = a => action(this, a);
		_process = ProcessRule;

		return this;
	}

	/// <summary>
	/// To add the action, activated at occurrence of condition.
	/// </summary>
	/// <param name="action">Action.</param>
	/// <returns>Rule.</returns>
	public MarketRule<TToken, TArg> Do(Action action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		return Do(a => action());
	}

	/// <summary>
	/// To add the action, returning result, activated at occurrence of condition.
	/// </summary>
	/// <typeparam name="TResult">The type of returned result.</typeparam>
	/// <param name="action">The action, returning a result.</param>
	/// <returns>Rule.</returns>
	public MarketRule<TToken, TArg> Do<TResult>(Func<TResult> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		return Do(a => action());
	}

	/// <summary>
	/// To add the processor, which will be called at action activation.
	/// </summary>
	/// <param name="handler">The handler.</param>
	/// <returns>Rule.</returns>
	public MarketRule<TToken, TArg> Activated(Action handler)
	{
		if (handler == null)
			throw new ArgumentNullException(nameof(handler));

		return Activated<object>(arg => handler());
	}

	/// <summary>
	/// To add the processor, accepting argument from <see cref="Do{TResult}(Func{TResult})"/>, which will be called at action activation.
	/// </summary>
	/// <typeparam name="TResult">The type of result, returned from the processor.</typeparam>
	/// <param name="handler">The handler.</param>
	/// <returns>Rule.</returns>
	public MarketRule<TToken, TArg> Activated<TResult>(Action<TResult> handler)
	{
		if (handler == null)
			throw new ArgumentNullException(nameof(handler));

		_activatedHandler = arg => handler((TResult)arg);
		return this;
	}

	/// <summary>
	/// To activate the rule.
	/// </summary>
	protected void Activate()
	{
		Activate(default);
	}

	/// <summary>
	/// To activate the rule.
	/// </summary>
	/// <param name="arg">The value, which will be sent to processor, registered through <see cref="Do(Action{TArg})"/>.</param>
	protected virtual void Activate(TArg arg)
	{
		if (!IsReady || IsSuspended)
			return;

		_arg = arg;
		_container.ActivateRule(this, _process);
	}

	private bool ProcessRule()
	{
		var result = _action(_arg);

		_activatedHandler?.Invoke(result);

		return _canFinish();
	}

	private bool ProcessRuleVoid()
	{
		_actionVoid(_arg);

		_activatedHandler?.Invoke(null);

		return _canFinish();
	}

	/// <inheritdoc />
	public override string ToString() => $"{Name} (0x{GetHashCode():X})";

	/// <summary>
	/// Release resources.
	/// </summary>
	protected override void DisposeManaged()
	{
		_container = null;

		base.DisposeManaged();
	}

	bool IMarketRule.CanFinish()
	{
		return !IsActive && IsReady && _canFinish();
	}

	/// <inheritdoc />
	public bool IsReady => !IsDisposed && _container is not null;

	/// <inheritdoc />
	public bool IsActive { get; set; }

	IMarketRule IMarketRule.Until(Func<bool> canFinish)
	{
		return Until(canFinish);
	}

	IMarketRule IMarketRule.Do(Action action)
	{
		return Do(action);
	}

	IMarketRule IMarketRule.Do(Action<object> action)
	{
		if (action == null)
			throw new ArgumentNullException(nameof(action));

		return Do(arg => action(arg));
	}

	IMarketRule IMarketRule.Do<TResult>(Func<TResult> action)
	{
		return Do(action);
	}
}