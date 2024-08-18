namespace StockSharp.Algo;

/// <summary>
/// The interface, describing the rules container.
/// </summary>
public interface IMarketRuleContainer : ILogReceiver
{
	/// <summary>
	/// The operation state.
	/// </summary>
	ProcessStates ProcessState { get; }

	/// <summary>
	/// To activate the rule.
	/// </summary>
	/// <param name="rule">Rule.</param>
	/// <param name="process">The processor returning <see langword="true" /> if the rule has ended its operation, otherwise - <see langword="false" />.</param>
	void ActivateRule(IMarketRule rule, Func<bool> process);

	/// <summary>
	/// Is rules execution suspended.
	/// </summary>
	/// <remarks>
	/// Rules suspension is performed through the method <see cref="SuspendRules"/>.
	/// </remarks>
	bool IsRulesSuspended { get; }

	/// <summary>
	/// To suspend rules execution until next restoration through the method <see cref="ResumeRules"/>.
	/// </summary>
	void SuspendRules();

	/// <summary>
	/// To restore rules execution, suspended through the method <see cref="SuspendRules"/>.
	/// </summary>
	void ResumeRules();

	/// <summary>
	/// Registered rules.
	/// </summary>
	IMarketRuleList Rules { get; }
}