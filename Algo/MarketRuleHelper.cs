namespace StockSharp.Algo;

/// <summary>
/// Extension class for <see cref="IMarketRule"/>.
/// </summary>
public static partial class MarketRuleHelper
{
	#region IConnector rules

	private abstract class ConnectorRule<TArg>(IConnector connector) : MarketRule<IConnector, TArg>(connector)
	{
		protected IConnector Connector { get; } = connector ?? throw new ArgumentNullException(nameof(connector));
	}

	private abstract class TransactionProviderRule<TArg>(ITransactionProvider provider) : MarketRule<ITransactionProvider, TArg>(provider)
	{
		protected ITransactionProvider Provider { get; } = provider ?? throw new ArgumentNullException(nameof(provider));
	}

	private class ConnectedRule : ConnectorRule<IMessageAdapter>
	{
		public ConnectedRule(IConnector connector)
			: base(connector)
		{
			Name = "Connected rule";
			Connector.ConnectedEx += OnConnectedEx;
		}

		private void OnConnectedEx(IMessageAdapter adapter)
		{
			Activate(adapter);
		}

		protected override void DisposeManaged()
		{
			Connector.ConnectedEx -= OnConnectedEx;
			base.DisposeManaged();
		}
	}

	private class DisconnectedRule : ConnectorRule<IMessageAdapter>
	{
		public DisconnectedRule(IConnector connector)
			: base(connector)
		{
			Name = "Disconnected rule";
			Connector.DisconnectedEx += OnDisconnectedEx;
		}

		private void OnDisconnectedEx(IMessageAdapter adapter)
		{
			Activate(adapter);
		}

		protected override void DisposeManaged()
		{
			Connector.DisconnectedEx -= OnDisconnectedEx;
			base.DisposeManaged();
		}
	}

	private class ConnectionLostRule : ConnectorRule<Tuple<IMessageAdapter, Exception>>
	{
		public ConnectionLostRule(IConnector connector)
			: base(connector)
		{
			Name = "Connection lost rule";
			Connector.ConnectionErrorEx += OnConnectionErrorEx;
		}

		private void OnConnectionErrorEx(IMessageAdapter adapter, Exception error)
		{
			Activate(Tuple.Create(adapter, error));
		}

		protected override void DisposeManaged()
		{
			Connector.ConnectionErrorEx -= OnConnectionErrorEx;
			base.DisposeManaged();
		}
	}

	/// <summary>
	/// To create a rule for the event of connection established.
	/// </summary>
	/// <param name="connector">The connection to be traced for state.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<IConnector, IMessageAdapter> WhenConnected(this IConnector connector)
	{
		return new ConnectedRule(connector);
	}

	/// <summary>
	/// To create a rule for the event of disconnection.
	/// </summary>
	/// <param name="connector">The connection to be traced for state.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<IConnector, IMessageAdapter> WhenDisconnected(this IConnector connector)
	{
		return new DisconnectedRule(connector);
	}

	/// <summary>
	/// To create a rule for the event of connection lost.
	/// </summary>
	/// <param name="connector">The connection to be traced for state.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<IConnector, Tuple<IMessageAdapter, Exception>> WhenConnectionLost(this IConnector connector)
	{
		return new ConnectionLostRule(connector);
	}

	#endregion

	#region Apply

	/// <summary>
	/// To form a rule (include <see cref="IMarketRule.IsReady"/>).
	/// </summary>
	/// <param name="rule">Rule.</param>
	/// <returns>Rule.</returns>
	public static IMarketRule Apply(this IMarketRule rule)
	{
		if (rule == null)
			throw new ArgumentNullException(nameof(rule));

		return rule.Apply(DefaultRuleContainer);
	}

	/// <summary>
	/// To form a rule (include <see cref="IMarketRule.IsReady"/>).
	/// </summary>
	/// <param name="rule">Rule.</param>
	/// <param name="container">The rules container.</param>
	/// <returns>Rule.</returns>
	public static IMarketRule Apply(this IMarketRule rule, IMarketRuleContainer container)
	{
		if (rule == null)
			throw new ArgumentNullException(nameof(rule));

		if (container == null)
			throw new ArgumentNullException(nameof(container));

		container.Rules.Add(rule);
		return rule;
	}

	/// <summary>
	/// To form a rule (include <see cref="IMarketRule.IsReady"/>).
	/// </summary>
	/// <typeparam name="TToken">The type of token.</typeparam>
	/// <typeparam name="TArg">The type of argument, accepted by the rule.</typeparam>
	/// <param name="rule">Rule.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<TToken, TArg> Apply<TToken, TArg>(this MarketRule<TToken, TArg> rule)
	{
		return rule.Apply(DefaultRuleContainer);
	}

	/// <summary>
	/// To form a rule (include <see cref="IMarketRule.IsReady"/>).
	/// </summary>
	/// <typeparam name="TToken">The type of token.</typeparam>
	/// <typeparam name="TArg">The type of argument, accepted by the rule.</typeparam>
	/// <param name="rule">Rule.</param>
	/// <param name="container">The rules container.</param>
	/// <returns>Rule.</returns>
	public static MarketRule<TToken, TArg> Apply<TToken, TArg>(this MarketRule<TToken, TArg> rule, IMarketRuleContainer container)
	{
		return (MarketRule<TToken, TArg>)((IMarketRule)rule).Apply(container);
	}

	/// <summary>
	/// To activate the rule.
	/// </summary>
	/// <param name="container">The rules container.</param>
	/// <param name="rule">Rule.</param>
	/// <param name="process">The handler.</param>
	public static void ActiveRule(this IMarketRuleContainer container, IMarketRule rule, Func<bool> process)
	{
		if (process == null)
			throw new ArgumentNullException(nameof(process));

		container.AddRuleLog(LogLevels.Debug, rule, LocalizedStrings.Activation);

		List<IMarketRule> removedRules = null;

		// mika
		// проверяем правило, так как оно могло быть удалено параллельным потоком
		if (!rule.IsReady)
			return;

		rule.IsActive = true;

		try
		{
			if (process())
			{
				container.Rules.Remove(rule);
				removedRules = [rule];
			}
		}
		finally
		{
			rule.IsActive = false;
		}

		if (removedRules == null)
			return;

		if (rule.ExclusiveRules.Count > 0)
		{
			foreach (var exclusiveRule in rule.ExclusiveRules.SyncGet(c => c.CopyAndClear()))
			{
				container.TryRemoveRule(exclusiveRule, false);
				removedRules.Add(exclusiveRule);
			}
		}

		foreach (var removedRule in removedRules)
		{
			container.AddRuleLog(LogLevels.Debug, removedRule, LocalizedStrings.Delete);
		}
	}

	private sealed class MarketRuleContainer : BaseLogReceiver, IMarketRuleContainer
	{
		private readonly object _rulesSuspendLock = new();
		private int _rulesSuspendCount;

		public MarketRuleContainer()
		{
			_rules = new MarketRuleList(this);
		}

		ProcessStates IMarketRuleContainer.ProcessState => ProcessStates.Started;

		void IMarketRuleContainer.ActivateRule(IMarketRule rule, Func<bool> process)
		{
			this.ActiveRule(rule, process);
		}

		bool IMarketRuleContainer.IsRulesSuspended => _rulesSuspendCount > 0;

		void IMarketRuleContainer.SuspendRules()
		{
			lock (_rulesSuspendLock)
				_rulesSuspendCount++;
		}

		void IMarketRuleContainer.ResumeRules()
		{
			lock (_rulesSuspendLock)
			{
				if (_rulesSuspendCount > 0)
					_rulesSuspendCount--;
			}
		}

		private readonly MarketRuleList _rules;

		IMarketRuleList IMarketRuleContainer.Rules => _rules;
	}

	/// <summary>
	/// The container of rules, which will be applied by default to all rules, not included into strategy.
	/// </summary>
	public static readonly IMarketRuleContainer DefaultRuleContainer = new MarketRuleContainer();

	/// <summary>
	/// To process rules in suspended mode (for example, create several rules and start them up simultaneously). After completion of method operation all rules, attached to the container resume their activity.
	/// </summary>
	/// <param name="action">The action to be processed at suspended rules. For example, to add several rules simultaneously.</param>
	public static void SuspendRules(Action action)
	{
		DefaultRuleContainer.SuspendRules(action);
	}

	/// <summary>
	/// To process rules in suspended mode (for example, create several rules and start them up simultaneously). After completion of method operation all rules, attached to the container resume their activity.
	/// </summary>
	/// <param name="container">The rules container.</param>
	/// <param name="action">The action to be processed at suspended rules. For example, to add several rules simultaneously.</param>
	public static void SuspendRules(this IMarketRuleContainer container, Action action)
	{
		if (container == null)
			throw new ArgumentNullException(nameof(container));

		if (action == null)
			throw new ArgumentNullException(nameof(action));

		container.SuspendRules();

		try
		{
			action();
		}
		finally
		{
			container.ResumeRules();
		}
	}

	#endregion

	/// <summary>
	/// To delete a rule. If a rule is executed at the time when this method is called, it will not be deleted.
	/// </summary>
	/// <param name="container">The rules container.</param>
	/// <param name="rule">Rule.</param>
	/// <param name="checkCanFinish">To check the possibility of rule suspension.</param>
	/// <returns><see langword="true" />, if a rule was successfully deleted, <see langword="false" />, if a rule cannot be currently deleted.</returns>
	public static bool TryRemoveRule(this IMarketRuleContainer container, IMarketRule rule, bool checkCanFinish = true)
	{
		if (container == null)
			throw new ArgumentNullException(nameof(container));

		if (rule == null)
			throw new ArgumentNullException(nameof(rule));

		var isRemoved = false;

		if ((!checkCanFinish && !rule.IsActive && rule.IsReady) || rule.CanFinish())
		{
			container.Rules.Remove(rule);
			isRemoved = true;
		}

		if (isRemoved)
		{
			container.AddRuleLog(LogLevels.Debug, rule, LocalizedStrings.Delete, rule);
		}

		return isRemoved;
	}

	/// <summary>
	/// To delete the rule and all opposite rules. If the rule is executed at the time when this method is called, it will not be deleted.
	/// </summary>
	/// <param name="container">The rules container.</param>
	/// <param name="rule">Rule.</param>
	/// <returns><see langword="true" />, if rule was removed, otherwise, <see langword="false" />.</returns>
	public static bool TryRemoveWithExclusive(this IMarketRuleContainer container, IMarketRule rule)
	{
		if (container == null)
			throw new ArgumentNullException(nameof(container));

		if (rule == null)
			throw new ArgumentNullException(nameof(rule));

		if (container.TryRemoveRule(rule))
		{
			if (rule.ExclusiveRules.Count > 0)
			{
				foreach (var exclusiveRule in rule.ExclusiveRules.SyncGet(c => c.CopyAndClear()))
				{
					container.TryRemoveRule(exclusiveRule, false);
				}
			}

			return true;
		}

		return false;
	}

	/// <summary>
	/// To make rules mutually exclusive.
	/// </summary>
	/// <param name="rule1">First rule.</param>
	/// <param name="rule2">Second rule.</param>
	public static void Exclusive(this IMarketRule rule1, IMarketRule rule2)
	{
		if (rule1 == null)
			throw new ArgumentNullException(nameof(rule1));

		if (rule2 == null)
			throw new ArgumentNullException(nameof(rule2));

		if (rule1 == rule2)
			throw new ArgumentException(LocalizedStrings.RulesSame.Put(rule1), nameof(rule2));

		rule1.ExclusiveRules.Add(rule2);
		rule2.ExclusiveRules.Add(rule1);
	}

	#region Or

	private abstract class BaseComplexRule<TToken, TArg> : MarketRule<TToken, TArg>, IMarketRuleContainer
	{
		private readonly List<IMarketRule> _innerRules = [];

		protected BaseComplexRule(IEnumerable<IMarketRule> innerRules)
			: base(default)
		{
			if (innerRules == null)
				throw new ArgumentNullException(nameof(innerRules));

			_innerRules.AddRange(innerRules.Select(Init));

			if (_innerRules.IsEmpty())
				throw new InvalidOperationException(LocalizedStrings.RulesEmpty);

			Name = _innerRules.Select(r => r.Name).Join(" OR ");

			_innerRules.ForEach(r => r.Container = this);
		}

		protected abstract IMarketRule Init(IMarketRule rule);

		public override LogLevels LogLevel
		{
			set
			{
				base.LogLevel = value;

				foreach (var rule in _innerRules)
					rule.LogLevel = value;
			}
		}

		public override bool IsSuspended
		{
			set
			{
				base.IsSuspended = value;
				_innerRules.ForEach(r => r.Suspend(value));
			}
		}

		protected override void DisposeManaged()
		{
			_innerRules.ForEach(r => r.Dispose());
			base.DisposeManaged();
		}

		#region Implementation of ILogSource

		Guid ILogSource.Id { get; } = Guid.NewGuid();

		ILogSource ILogSource.Parent
		{
			get => Container;
			set => throw new NotSupportedException();
		}

		LogLevels ILogSource.LogLevel
		{
			get => Container.LogLevel;
			set => throw new NotSupportedException();
		}

		event Action<LogMessage> ILogSource.Log
		{
			add => Container.Log += value;
			remove => Container.Log -= value;
		}

		DateTimeOffset ILogSource.CurrentTime => Container.CurrentTime;

		bool ILogSource.IsRoot => Container.IsRoot;

		#endregion

		void ILogReceiver.AddLog(LogMessage message)
		{
			Container.AddLog(new LogMessage(Container, message.Time, message.Level, () => message.Message));
		}

		#region Implementation of IMarketRuleContainer

		ProcessStates IMarketRuleContainer.ProcessState => Container.ProcessState;

		void IMarketRuleContainer.ActivateRule(IMarketRule rule, Func<bool> process)
		{
			process();
		}

		bool IMarketRuleContainer.IsRulesSuspended => Container.IsRulesSuspended;

		void IMarketRuleContainer.SuspendRules() => throw new NotSupportedException();

		void IMarketRuleContainer.ResumeRules() => throw new NotSupportedException();

		IMarketRuleList IMarketRuleContainer.Rules => throw new NotSupportedException();

		#endregion

		public override string ToString()
		{
			return _innerRules.Select(r => r.ToString()).Join(" OR ");
		}
	}

	private sealed class OrRule(IEnumerable<IMarketRule> innerRules) : BaseComplexRule<object, object>(innerRules)
	{
		protected override IMarketRule Init(IMarketRule rule)
		{
			return rule.Do(arg => Activate(arg));
		}
	}

	private sealed class OrRule<TToken, TArg>(IEnumerable<MarketRule<TToken, TArg>> innerRules) : BaseComplexRule<TToken, TArg>(innerRules)
	{
		protected override IMarketRule Init(IMarketRule rule)
		{
			return ((MarketRule<TToken, TArg>)rule).Do(a => Activate(a));
		}
	}

	private sealed class AndRule : BaseComplexRule<object, object>
	{
		private readonly List<object> _args = [];
		private readonly SynchronizedSet<IMarketRule> _nonActivatedRules = [];

		public AndRule(IMarketRule[] innerRules)
			: base(innerRules)
		{
			_nonActivatedRules.AddRange(innerRules);
		}

		protected override IMarketRule Init(IMarketRule rule)
		{
			return rule.Do(a =>
			{
				var canActivate = false;

				lock (_nonActivatedRules.SyncRoot)
				{
					if (_nonActivatedRules.Remove(rule))
					{
						_args.Add(a);

						if (_nonActivatedRules.IsEmpty())
							canActivate = true;
					}
				}

				if (canActivate)
					Activate(_args);
			});
		}
	}

	private sealed class AndRule<TToken, TArg> : BaseComplexRule<TToken, TArg>
	{
		private readonly List<TArg> _args = [];
		private readonly SynchronizedSet<IMarketRule> _nonActivatedRules = [];

		public AndRule(MarketRule<TToken, TArg>[] innerRules)
			: base(innerRules)
		{
			_nonActivatedRules.AddRange(innerRules);
		}

		protected override IMarketRule Init(IMarketRule rule)
		{
			return ((MarketRule<TToken, TArg>)rule).Do(a =>
			{
				var canActivate = false;

				lock (_nonActivatedRules.SyncRoot)
				{
					if (_nonActivatedRules.Remove(rule))
					{
						_args.Add(a);

						if (_nonActivatedRules.IsEmpty())
							canActivate = true;
					}
				}

				if (canActivate)
					Activate(_args.FirstOrDefault());
			});
		}
	}

	/// <summary>
	/// To combine rules by OR condition.
	/// </summary>
	/// <param name="rule">First rule.</param>
	/// <param name="rules">Additional rules.</param>
	/// <returns>Combined rule.</returns>
	public static IMarketRule Or(this IMarketRule rule, params IMarketRule[] rules)
	{
		return new OrRule(new[] { rule }.Concat(rules));
	}

	/// <summary>
	/// To combine rules by OR condition.
	/// </summary>
	/// <param name="rules">Rules.</param>
	/// <returns>Combined rule.</returns>
	public static IMarketRule Or(this IEnumerable<IMarketRule> rules)
	{
		return new OrRule(rules);
	}

	/// <summary>
	/// To combine rules by OR condition.
	/// </summary>
	/// <typeparam name="TToken">The type of token.</typeparam>
	/// <typeparam name="TArg">The type of argument, accepted by the rule.</typeparam>
	/// <param name="rule">First rule.</param>
	/// <param name="rules">Additional rules.</param>
	/// <returns>Combined rule.</returns>
	public static MarketRule<TToken, TArg> Or<TToken, TArg>(this MarketRule<TToken, TArg> rule, params MarketRule<TToken, TArg>[] rules)
	{
		return new OrRule<TToken, TArg>(new[] { rule }.Concat(rules));
	}

	/// <summary>
	/// To combine rules by AND condition.
	/// </summary>
	/// <param name="rule">First rule.</param>
	/// <param name="rules">Additional rules.</param>
	/// <returns>Combined rule.</returns>
	public static IMarketRule And(this IMarketRule rule, params IMarketRule[] rules)
	{
		return new AndRule(new[] { rule }.Concat(rules));
	}

	/// <summary>
	/// To combine rules by AND condition.
	/// </summary>
	/// <param name="rules">Rules.</param>
	/// <returns>Combined rule.</returns>
	public static IMarketRule And(this IEnumerable<IMarketRule> rules)
	{
		return new AndRule([.. rules]);
	}

	/// <summary>
	/// To combine rules by AND condition.
	/// </summary>
	/// <typeparam name="TToken">The type of token.</typeparam>
	/// <typeparam name="TArg">The type of argument, accepted by the rule.</typeparam>
	/// <param name="rule">First rule.</param>
	/// <param name="rules">Additional rules.</param>
	/// <returns>Combined rule.</returns>
	public static MarketRule<TToken, TArg> And<TToken, TArg>(this MarketRule<TToken, TArg> rule, params MarketRule<TToken, TArg>[] rules)
	{
		return new AndRule<TToken, TArg>(new[] { rule }.Concat(rules));
	}

	#endregion

	/// <summary>
	/// To assign the rule a new name <see cref="IMarketRule.Name"/>.
	/// </summary>
	/// <typeparam name="TRule">The type of the rule.</typeparam>
	/// <param name="rule">Rule.</param>
	/// <param name="name">The rule new name.</param>
	/// <returns>Rule.</returns>
	public static TRule UpdateName<TRule>(this TRule rule, string name)
		where TRule : IMarketRule
	{
		return rule.Modify(r => r.Name = name);
	}

	/// <summary>
	/// To set the logging level.
	/// </summary>
	/// <typeparam name="TRule">The type of the rule.</typeparam>
	/// <param name="rule">Rule.</param>
	/// <param name="level">The level, on which logging is performed.</param>
	/// <returns>Rule.</returns>
	public static TRule UpdateLogLevel<TRule>(this TRule rule, LogLevels level)
		where TRule : IMarketRule
	{
		return rule.Modify(r => r.LogLevel = level);
	}

	/// <summary>
	/// To suspend or resume the rule.
	/// </summary>
	/// <typeparam name="TRule">The type of the rule.</typeparam>
	/// <param name="rule">Rule.</param>
	/// <param name="suspend"><see langword="true" /> - suspend, <see langword="false" /> - resume.</param>
	/// <returns>Rule.</returns>
	public static TRule Suspend<TRule>(this TRule rule, bool suspend)
		where TRule : IMarketRule
	{
		return rule.Modify(r => r.IsSuspended = suspend);
	}

	///// <summary>
	///// Синхронизировать или рассинхронизировать реагирование правила с другими правилами.
	///// </summary>
	///// <typeparam name="TRule">Тип правила.</typeparam>
	///// <param name="rule">Правило.</param>
	///// <param name="syncToken">Объект синхронизации. Если значение равно <see langword="null"/>, то правило рассинхронизовывается.</param>
	///// <returns>Правило.</returns>
	//public static TRule Sync<TRule>(this TRule rule, SyncObject syncToken)
	//	where TRule : IMarketRule
	//{
	//	return rule.Modify(r => r.SyncRoot = syncToken);
	//}

	/// <summary>
	/// To make the rule one-time rule (will be called only once).
	/// </summary>
	/// <typeparam name="TRule">The type of the rule.</typeparam>
	/// <param name="rule">Rule.</param>
	/// <returns>Rule.</returns>
	public static TRule Once<TRule>(this TRule rule)
		where TRule : IMarketRule
	{
		return rule.Modify(r => r.Until(() => true));
	}

	private static TRule Modify<TRule>(this TRule rule, Action<TRule> action)
		where TRule : IMarketRule
	{
		if (rule is null)
			throw new ArgumentNullException(nameof(rule));

		action(rule);

		return rule;
	}

	/// <summary>
	/// To write the message from the rule.
	/// </summary>
	/// <param name="container">The rules container.</param>
	/// <param name="level">The level of the log message.</param>
	/// <param name="rule">Rule.</param>
	/// <param name="message">Text message.</param>
	/// <param name="args">Text message settings. Used if a message is the format string. For details, see <see cref="string.Format(string,object[])"/>.</param>
	public static void AddRuleLog(this IMarketRuleContainer container, LogLevels level, IMarketRule rule, string message, params object[] args)
	{
		if (container == null)
			return; // правило еще не было добавлено в контейнер

		if (rule == null)
			throw new ArgumentNullException(nameof(rule));

		if (rule.LogLevel != LogLevels.Inherit && rule.LogLevel > level)
			return;

		container.AddLog(level, () => $"Rule '{rule}'. {message.Put(args)}");
	}
}
