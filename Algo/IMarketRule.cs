namespace StockSharp.Algo
{
	using System;
    
	using StockSharp.Logging;

	using Ecng.Collections;
	using Ecng.Common;
	using StockSharp.Localization;

	/// <summary>
	/// Интерфейс правила, активизирующее действие при наступлении рыночного условия.
	/// </summary>
	public interface IMarketRule : IDisposable
	{
		/// <summary>
		/// Имя правила.
		/// </summary>
		string Name { get; set; }

		/// <summary>
		/// Контейнер правил.
		/// </summary>
		IMarketRuleContainer Container { get; set; }

		/// <summary>
		/// Уровень, на котором осуществлять логирование данного правила.
		/// </summary>
		LogLevels LogLevel { get; set; }

		/// <summary>
		/// Приостановлено ли правило.
		/// </summary>
		bool IsSuspended { get; set; }

		/// <summary>
		/// Сформировано ли правило.
		/// </summary>
		bool IsReady { get; }

		/// <summary>
		/// Активировано ли правило в данный момент.
		/// </summary>
		bool IsActive { get; set; }

		/// <summary>
		/// Токен правила, с которым он ассоциирован (например, для правила <see cref="MarketRuleHelper.WhenRegistered"/> токеном будет являтся заявка).
		/// Если правильно ни с чем не ассоциировано, то будет возвращено null.
		/// </summary>
		object Token { get; }

		/// <summary>
		/// Правила, которые противоположны данному. Удалаются автоматически при активации данного правила.
		/// </summary>
		ISynchronizedCollection<IMarketRule> ExclusiveRules { get; }

		/// <summary>
		/// Сделать правило периодичным (будет вызываться до тех пор, пока <paramref name="canFinish"/> не вернет true).
		/// </summary>
		/// <param name="canFinish">Критерий окончания периодичности.</param>
		/// <returns>Правило.</returns>
		IMarketRule Until(Func<bool> canFinish);

		/// <summary>
		/// Добавить действие, активизирующееся при наступлении условия.
		/// </summary>
		/// <param name="action">Действие.</param>
		/// <returns>Правило.</returns>
		IMarketRule Do(Action action);

		/// <summary>
		/// Добавить действие, активизирующееся при наступлении условия.
		/// </summary>
		/// <param name="action">Действие, принимающее значение.</param>
		/// <returns>Правило.</returns>
		IMarketRule Do(Action<object> action);

		/// <summary>
		/// Добавить действие, возвращающее результат, активизирующееся при наступлении условия.
		/// </summary>
		/// <typeparam name="TResult">Тип возвращаемого результата.</typeparam>
		/// <param name="action">Действие, возвращающее результат.</param>
		/// <returns>Правило.</returns>
		IMarketRule Do<TResult>(Func<TResult> action);

		/// <summary>
		/// Можно ли закончить правило.
		/// </summary>
		/// <returns><see langword="true"/>, если правило больше не нужно. Иначе, <see langword="false"/>.</returns>
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
	/// Правило, активизирующее действие при наступлении рыночного условия.
	/// </summary>
	/// <typeparam name="TToken">Тип токена.</typeparam>
	/// <typeparam name="TArg">Тип принимаемого аргумента.</typeparam>
	public abstract class MarketRule<TToken, TArg> : Disposable, IMarketRule
	{
		private Func<TArg, object> _action = a => a;
		private Action<object> _activatedHandler;
		private Action<TArg> _actionVoid;
		private Func<bool> _process;

		private TArg _arg;

		private Func<bool> _canFinish;

		/// <summary>
		/// Инициализировать <see cref="MarketRule{TToken, TArg}"/>.
		/// </summary>
		/// <param name="token">Токен правила.</param>
		protected MarketRule(TToken token)
		{
			_token = token;
			Name = GetType().Name;

			Until(CanFinish);

			Holder.RuleStat.Add(this);
		}

		/// <summary>
		/// Можно ли закончить правило.
		/// </summary>
		/// <returns><see langword="true"/>, если правило больше не нужно. Иначе, <see langword="false"/>.</returns>
		protected virtual bool CanFinish()
		{
			return ReferenceEquals(_container, null) || _container.ProcessState != ProcessStates.Started;
		}

		private string _name;

		/// <summary>
		/// Имя правила.
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
		/// Уровень, на котором осуществлять логирование данного правила. По-умолчанию, <see cref="LogLevels.Inherit"/>.
		/// </summary>
		public virtual LogLevels LogLevel
		{
			get { return _logLevel; } 
			set { _logLevel = value; } 
		}

		private bool _isSuspended;

		/// <summary>
		/// Приостановлено ли правило.
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
		/// Токен правила, с которым он ассоциирован (например, для правила <see cref="MarketRuleHelper.WhenRegistered"/> токеном будет являтся заявка).
		/// Если правильно ни с чем не ассоциировано, то будет возвращено null.
		/// </summary>
		public virtual object Token
		{
			get { return _token; }
		}

		private readonly SynchronizedSet<IMarketRule> _exclusiveRules = new SynchronizedSet<IMarketRule>();

		/// <summary>
		/// Правила, которые противоположны данному. Удалаются автоматически при активации данного правила.
		/// </summary>
		public virtual ISynchronizedCollection<IMarketRule> ExclusiveRules
		{
			get { return _exclusiveRules; }
		}

		private IMarketRuleContainer _container;

		/// <summary>
		/// Контейнер правил.
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
		/// Сделать правило периодичным (будет вызываться до тех пор, пока <paramref name="canFinish"/> не вернет true).
		/// </summary>
		/// <param name="canFinish">Критерий окончания периодичности.</param>
		/// <returns>Правило.</returns>
		public MarketRule<TToken, TArg> Until(Func<bool> canFinish)
		{
			if (canFinish == null)
				throw new ArgumentNullException("canFinish");

			_canFinish = canFinish;
			return this;
		}

		/// <summary>
		/// Добавить действие, активизирующееся при наступлении условия.
		/// </summary>
		/// <param name="action">Действие.</param>
		/// <returns>Правило.</returns>
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
		/// Добавить действие, активизирующееся при наступлении условия.
		/// </summary>
		/// <param name="action">Действие.</param>
		/// <returns>Правило.</returns>
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
		/// Добавить действие, возвращающее результат, активизирующееся при наступлении условия.
		/// </summary>
		/// <typeparam name="TResult">Тип возвращаемого результата.</typeparam>
		/// <param name="action">Действие, возвращающее результат.</param>
		/// <returns>Правило.</returns>
		public MarketRule<TToken, TArg> Do<TResult>(Func<TArg, TResult> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			return Do((r, a) => action(a));
		}

		/// <summary>
		/// Добавить действие, возвращающее результат, активизирующееся при наступлении условия.
		/// </summary>
		/// <typeparam name="TResult">Тип возвращаемого результата.</typeparam>
		/// <param name="action">Действие, возвращающее результат.</param>
		/// <returns>Правило.</returns>
		public MarketRule<TToken, TArg> Do<TResult>(Func<MarketRule<TToken, TArg>, TArg, TResult> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			_action = a => action(this, a);
			_process = ProcessRule;

			return this;
		}

		/// <summary>
		/// Добавить действие, активизирующееся при наступлении условия.
		/// </summary>
		/// <param name="action">Действие.</param>
		/// <returns>Правило.</returns>
		public MarketRule<TToken, TArg> Do(Action action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			return Do(a => action());
		}

		/// <summary>
		/// Добавить действие, возвращающее результат, активизирующееся при наступлении условия.
		/// </summary>
		/// <typeparam name="TResult">Тип возвращаемого результата.</typeparam>
		/// <param name="action">Действие, возвращающее результат.</param>
		/// <returns>Правило.</returns>
		public MarketRule<TToken, TArg> Do<TResult>(Func<TResult> action)
		{
			if (action == null)
				throw new ArgumentNullException("action");

			return Do(a => action());
		}

		/// <summary>
		/// Добавить обработчик, который будет вызван при активации действия.
		/// </summary>
		/// <param name="handler">Обработчик.</param>
		/// <returns>Правило.</returns>
		public MarketRule<TToken, TArg> Activated(Action handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			return Activated<object>(arg => handler());
		}

		/// <summary>
		/// Добавить обработчик, принимающий аргумент из <see cref="Do{TResult}(System.Func{TResult})"/>, который будет вызван при активации действия.
		/// </summary>
		/// <typeparam name="TResult">Тип возвращаемого результата из обработчика правила.</typeparam>
		/// <param name="handler">Обработчик.</param>
		/// <returns>Правило.</returns>
		public MarketRule<TToken, TArg> Activated<TResult>(Action<TResult> handler)
		{
			if (handler == null)
				throw new ArgumentNullException("handler");

			_activatedHandler = arg => handler((TResult)arg);
			return this;
		}

		/// <summary>
		/// Активировать правило.
		/// </summary>
		protected void Activate()
		{
			Activate(default(TArg));
		}

		/// <summary>
		/// Активировать правило.
		/// </summary>
		/// <param name="arg">Значение, которое будет передано в обработчик, зарегистрированный через <see cref="Do(System.Action{TArg})"/>.</param>
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
		/// Получить строковое представление.
		/// </summary>
		/// <returns>Строковое представление.</returns>
		public override string ToString()
		{
			return "{0} (0x{1:X})".Put(Name, GetHashCode());
		}

		/// <summary>
		/// Освободить занятые ресурсы.
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
		/// Сформировано ли правило.
		/// </summary>
		public bool IsReady
		{
			get { return !IsDisposed && !ReferenceEquals(_container, null); }
		}

		/// <summary>
		/// Активировано ли правило в данный момент.
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