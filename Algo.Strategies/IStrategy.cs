namespace StockSharp.Algo.Strategies;

using StockSharp.Reporting;

/// <summary>
/// Common external contract implemented by both the current <see cref="Strategy"/> and the legacy
/// <see cref="StrategyOld"/>, so external code and shared infrastructure can work with either strategy engine.
/// </summary>
/// <remarks>
/// The interface aggregates the provider contracts the monolith already exposes and adds the strategy-specific
/// surface. The legacy <see cref="StrategyOld"/> satisfies it as-is; the decomposed <see cref="Strategy"/>
/// implements the missing pieces until full parity is reached, after which usages of the concrete type are
/// replaced by this interface.
/// </remarks>
public interface IStrategy :
	INotifyPropertyChangedEx,
	IMarketRuleContainer,
	IMarketDataProvider,
	ISubscriptionProvider,
	ISecurityProvider,
	ITransactionProvider,
	IPortfolioProvider,
	IPositionProvider,
	ITimeProvider,
	IReportSource
{
	/// <summary>
	/// Strategy name. Re-declared to unify the name inherited from both <see cref="ILogSource"/> and
	/// <see cref="IReportSource"/>, which are otherwise ambiguous through this interface.
	/// </summary>
	new string Name { get; }

	/// <summary>
	/// Raise the parameters-changed notification for the parameter with the specified name.
	/// </summary>
	/// <param name="name">Parameter name.</param>
	void RaiseParametersChanged(string name);

	/// <summary>
	/// The <see cref="ProcessState"/> change event.
	/// </summary>
	event Action<IStrategy> ProcessStateChanged;

	/// <summary>
	/// The error event.
	/// </summary>
	event Action<IStrategy, Exception> Error;

	/// <summary>
	/// The profit-loss change event.
	/// </summary>
	event Action PnLChanged;

	/// <summary>
	/// The event of order registration.
	/// </summary>
	event Action<Order> OrderRegistered;

	/// <summary>
	/// The error state.
	/// </summary>
	LogLevels ErrorState { get; }

	/// <summary>
	/// The security.
	/// </summary>
	Security Security { get; set; }

	/// <summary>
	/// The portfolio.
	/// </summary>
	Portfolio Portfolio { get; set; }

	/// <summary>
	/// The last error.
	/// </summary>
	Exception LastError { get; }

	/// <summary>
	/// Start the strategy.
	/// </summary>
	void Start();

	/// <summary>
	/// Stop the strategy.
	/// </summary>
	void Stop();
}
