namespace StockSharp.Algo
{
	using System;
    
	using StockSharp.Logging;

	using Ecng.Collections;
	using Ecng.Common;
	using StockSharp.Localization;

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

	class Holder
	{
		public static readonly MemoryStatisticsValue<IMarketRule> RuleStat = new MemoryStatisticsValue<IMarketRule>(LocalizedStrings.Str1088);

		static Holder()
		{
			MemoryStatistics.Instance.Values.Add(RuleStat);
		}
	}

	/// <summary>
	/// The rule, activating action at market condition occurrence.
	/// </summary>
	/// <typeparam name="TToken">The type of token.</typeparam>
	/// <typeparam name="TArg">The type of accepted argument.</typeparam>
	public abstract class MarketRule<TToken, TArg> : Disposable, IMarketRule
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

			Holder.RuleStat.Add(this);
		}

		/// <summary>
		/// Can the rule be ended.
		/// </summary>
		/// <returns><see langword="true" />, if rule is not required any more. Otherwise, <see langword="false" />.</returns>
		protected virtual bool CanFinish()
		{
			return ReferenceEquals(_container, null) || _container.ProcessState != ProcessStates.Started;
		}

		private string _name;

		/// <summary>
		/// The name of the rule.
		/// </summary>
		public string Name
		{
			get { return _name; }
			set
			{
				if (value.IsEmpty())
					throw new ArgumentNullException("value");

				_name = value;
			}
		}

		private LogLevels _logLevel = LogLevels.Inherit;

		/// <summary>
		/// The level, at which logging of this rule is performed. The default is <see cref="LogLevels.Inherit"/>.
		/// </summary>
		public virtual LogLevels LogLevel
		{
			get { return _logLevel; } 
			set { _logLevel = value; } 
		}

		private bool _isSuspended;

		/// <summary>
		/// Is the rule suspended.
		/// </summary>
		public virtual bool IsSuspended
		{
			get { return _isSuspended; }
			set
			{
				_isSuspended = value;

				if (_container != null)
					_container.AddRuleLog(LogLevels.Info, this, value ? LocalizedStrings.Str1089 : LocalizedStrings.Str1090);
			}
		}

		private readonly TToken _token;

		/// <summary>
		/// Token-rules, it is associated with (for example, for rule <see cref="MarketRuleHelper.WhenRegistered"/> the order will be a token). If rule is not associated with anything, <see langword="null" /> will be returned.
		/// </summary>
		public virtual object Token
		{
			get { return _token; }
		}

		private readonly SynchronizedSet<IMarketRule> _exclusiveRules = new SynchronizedSet<IMarketRule>();

		/// <summary>
		/// Rules, opposite to given rule. They are deleted automatically at activation of this rule.
		/// </summary>
		public virtual ISynchronizedCollection<IMarketRule> ExclusiveRules
		{
			get { return _exclusiveRules; }
		}

		private IMarketRuleContainer _container;

		/// <summary>
		/// The rules container.
		/// </summary>
		public virtual IMarketRuleContainer Container
		{
			get { return _container; }
			set
			{
				if (value == null)
					throw new ArgumentNullException("value");

				if (Container != null)
					throw new ArgumentException(LocalizedStrings.Str1091Params.Put(Name, Container));

				_container = value;

				//_container.AddRuleLog(LogLevels.Info, this, "Добавлено.");
			}
		}

		/// <summary>
		/// To make the rule periodical (will be called until <paramref name="canFinish" /> returns <see langword="true" />).
		/// </summary>
		/// <param name="canFinish">The criteria for end of periodicity.</param>
		/// <returns>Rule.</returns>
		public MarketRule<TToken, TArg> Until(Func<bool> canFinish)
		{
			if (canFinish == null)
				throw new ArgumentNullException("canFinish");

			_canFinish = canFinish;
			return this;
		}

		/// <summary>
		/// To add the action, activated at occurrence of condition.
		/// </summary>
		/// <param name="action">Action.</param>
		/// <returns>Rule.</returns>
		public MarketRule<TToken, TArg> Do(Action<TArg> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			//return Do((r, a) => action(a));

			_process = ProcessRuleVoid;
			_actionVoid = action;

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
				throw new ArgumentNullException("action");

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
				throw new ArgumentNullException("action");

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
				throw new ArgumentNullException("action");

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
				throw new ArgumentNullException("action");

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
				throw new ArgumentNullException("action");

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
				throw new ArgumentNullException("handler");

			return Activated<object>(arg => handler());
		}

		/// <summary>
		/// To add the processor, accepting argument from <see cref="Do{TResult}(System.Func{TResult})"/>, which will be called at action activation.
		/// </summary>
		/// <typeparam name="TResult">The type of result, returned from the processor.</typeparam>
		/// <param name="handler">The handler.</param>
		/// <returns>Rule.</returns>
		public MarketRule<TToken, TArg> Activated<TResult>(Action<TResult> handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			_activatedHandler = arg => handler((TResult)arg);
			return this;
		}

		/// <summary>
		/// To activate the rule.
		/// </summary>
		protected void Activate()
		{
			Activate(default(TArg));
		}

		/// <summary>
		/// To activate the rule.
		/// </summary>
		/// <param name="arg">The value, which will be sent to processor, registered through <see cref="Do(System.Action{TArg})"/>.</param>
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

			var ah = _activatedHandler;
			if (ah != null)
				ah(result);

			return _canFinish();
		}

		private bool ProcessRuleVoid()
		{
			_actionVoid(_arg);
			return _canFinish();
		}

		/// <summary>
		/// Returns a string that represents the current object.
		/// </summary>
		/// <returns>A string that represents the current object.</returns>
		public override string ToString()
		{
			return "{0} (0x{1:X})".Put(Name, GetHashCode());
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		protected override void DisposeManaged()
		{
			_container.AddRuleLog(LogLevels.Debug, this, LocalizedStrings.Str1092);
			_container = null;

			base.DisposeManaged();

			Holder.RuleStat.Remove(this);
		}

		bool IMarketRule.CanFinish()
		{
			return !IsActive && IsReady && _canFinish();
		}

		/// <summary>
		/// Is the rule formed.
		/// </summary>
		public bool IsReady
		{
			get { return !IsDisposed && !ReferenceEquals(_container, null); }
		}

		/// <summary>
		/// Is the rule currently activated.
		/// </summary>
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
				throw new ArgumentNullException("action");

			return Do(arg => action(arg));
		}

		IMarketRule IMarketRule.Do<TResult>(Func<TResult> action)
		{
			return Do(action);
		}
	}
}